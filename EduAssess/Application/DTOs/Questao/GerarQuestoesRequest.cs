using System.ComponentModel.DataAnnotations;
using EduAssess.Models.Local;

namespace EduAssess.Application.DTOs.Questao
{
    // NOVO: corpo da rota POST /questoes/gerar-ia.
    // O professor informa a disciplina, o assunto, a dificuldade e quantas questões quer;
    // a IA devolve as questões prontas (com 5 alternativas, a correta e a explicação).
    public record GerarQuestoesRequest(
        [Required(ErrorMessage = "A disciplina é obrigatória!")]
        Guid? disciplinaId,

        [Required(ErrorMessage = "O assunto é obrigatório!")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "O assunto deve ter entre 2 e 100 caracteres!")]
        string assunto,

        [Required(ErrorMessage = "A dificuldade é obrigatória!")]
        [EnumDataType(typeof(DificuldadeQuestao),
            ErrorMessage = "A dificuldade informada é inválida!")]
        DificuldadeQuestao? dificuldade,

        // O serviço de IA limita a 10 por chamada (protege a cota gratuita do Gemini).
        [Range(1, 10, ErrorMessage = "Peça entre 1 e 10 questões por vez!")]
        int quantidade = 5,

        // NOVO: orientações livres do professor/gestor para a IA seguir
        // (ex.: "use casos práticos", "estilo ENADE", "sem pegadinhas").
        [StringLength(500, ErrorMessage = "As recomendações devem ter até 500 caracteres!")]
        string? recomendacoes = null
    );
}
