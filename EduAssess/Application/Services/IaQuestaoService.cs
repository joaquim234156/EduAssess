using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduAssess.Application.DTOs.Questao;
using EduAssess.Application.Interfaces;
using EduAssess.Models;

namespace EduAssess.Application.Services
{
    // Exceção própria: permite à rota diferenciar "a IA falhou"
    // de um erro qualquer do sistema.
    public class IaQuestaoException : Exception
    {
        public IaQuestaoException(string message) : base(message) { }
    }

    public class IaQuestaoService : IIaQuestaoService
    {
        private const int MaxQuestoes = 10;

        private const string InstrucaoSistema =
            "Você é um professor especialista em elaborar questões de múltipla escolha " +
            "em português do Brasil. Cada questão deve ter exatamente 5 alternativas " +
            "e apenas UMA correta. As alternativas erradas devem ser plausíveis e " +
            "diferentes entre si. Não use 'todas as anteriores' nem 'nenhuma das " +
            "anteriores'. Não coloque letras ou números no início das alternativas. " +
            "Enunciado com até 900 caracteres, cada alternativa com até 250 " +
            "caracteres e explicação com até 800 caracteres.";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;
        private readonly ILogger<IaQuestaoService> _logger;
        private readonly string? _apiKey;
        private readonly string _modelo;

        public IaQuestaoService(
            HttpClient http,
            IConfiguration config,
            ILogger<IaQuestaoService> logger)
        {
            _http = http;
            _logger = logger;
            _apiKey = config["Gemini:ApiKey"];
            _modelo = config["Gemini:Model"] ?? "gemini-flash-lite-latest";
        }

        public async Task<List<QuestaoRequest>> GerarQuestoesAsync(
            Guid disciplinaId,
            string nomeDisciplina,
            string assunto,
            DificuldadeQuestao dificuldade,
            int quantidade)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new IaQuestaoException("A chave do Gemini não está configurada.");
            }

            // Protege sua cota gratuita: no máximo 10 por chamada.
            quantidade = Math.Clamp(quantidade, 1, MaxQuestoes);

            var descDificuldade = dificuldade switch
            {
                DificuldadeQuestao.Facil => "fácil (conceitos básicos, resposta direta)",
                DificuldadeQuestao.Media => "média (exige aplicar o conceito)",
                _ => "difícil (exige análise e raciocínio em várias etapas)"
            };

            var prompt =
                $"Disciplina: {nomeDisciplina}\n" +
                $"Assunto: {assunto}\n" +
                $"Dificuldade: {descDificuldade}\n" +
                $"Gere {quantidade} questão(ões) diferentes entre si.\n" +
                "Em 'indiceCorreto' informe o número (de 1 a 5) da alternativa correta.";

            // Corpo da requisição no formato que o Gemini espera.
            var body = new
            {
                systemInstruction = new { parts = new[] { new { text = InstrucaoSistema } } },
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            questoes = new
                            {
                                type = "ARRAY",
                                items = new
                                {
                                    type = "OBJECT",
                                    properties = new
                                    {
                                        enunciado = new { type = "STRING" },
                                        alternativas = new
                                        {
                                            type = "ARRAY",
                                            items = new { type = "STRING" }
                                        },
                                        indiceCorreto = new { type = "INTEGER" },
                                        explicacao = new { type = "STRING" }
                                    },
                                    required = new[]
                                    {
                                        "enunciado", "alternativas",
                                        "indiceCorreto", "explicacao"
                                    }
                                }
                            }
                        },
                        required = new[] { "questoes" }
                    }
                }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{_modelo}:generateContent")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Add("x-goog-api-key", _apiKey);

            // 1. Chamar a API.
            string json;
            try
            {
                using var resposta = await _http.SendAsync(request);
                json = await resposta.Content.ReadAsStringAsync();

                if (resposta.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new IaQuestaoException(
                        "O limite gratuito da IA foi atingido. Tente de novo em alguns minutos.");
                }

                if (!resposta.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini retornou {Status}: {Json}",
                        (int)resposta.StatusCode, json);
                    throw new IaQuestaoException(
                        $"A IA retornou um erro ({(int)resposta.StatusCode}).");
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "Falha de rede ao chamar o Gemini.");
                throw new IaQuestaoException("Não foi possível falar com a IA agora.");
            }

            // 2. Extrair o texto da resposta e converter o JSON.
            List<QuestaoGerada>? geradas;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var texto = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                geradas = JsonSerializer.Deserialize<RespostaIa>(texto!, JsonOptions)?.Questoes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resposta inesperada do Gemini: {Json}", json);
                throw new IaQuestaoException(
                    "A IA devolveu uma resposta em formato inesperado. Tente novamente.");
            }

            // 3. Converter cada questão gerada em um QuestaoRequest.
            var resultado = new List<QuestaoRequest>();

            foreach (var q in geradas ?? new List<QuestaoGerada>())
            {
                if (string.IsNullOrWhiteSpace(q.Enunciado) ||
                    string.IsNullOrWhiteSpace(q.Explicacao) ||
                    q.Alternativas is null ||
                    q.Alternativas.Count != 5 ||
                    q.IndiceCorreto < 1 || q.IndiceCorreto > 5)
                {
                    continue; // descarta questão malformada
                }

                var alt = q.Alternativas.Select(a => a.Trim()).ToList();

                resultado.Add(new QuestaoRequest(
                    disciplinaId,
                    q.Enunciado.Trim(),
                    alt[0], alt[1], alt[2], alt[3], alt[4],
                    alt[q.IndiceCorreto - 1], // número vira o texto da correta
                    assunto,
                    q.Explicacao.Trim(),
                    dificuldade));
            }

            if (resultado.Count == 0)
            {
                throw new IaQuestaoException(
                    "A IA não gerou nenhuma questão válida. Tente novamente.");
            }

            return resultado;
        }

        // Formato do JSON que a IA devolve (só usado aqui dentro).
        private record RespostaIa(List<QuestaoGerada>? Questoes);

        private record QuestaoGerada(
            string? Enunciado,
            List<string>? Alternativas,
            int IndiceCorreto,
            string? Explicacao);
    }
}