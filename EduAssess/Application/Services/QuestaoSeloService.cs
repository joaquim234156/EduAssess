using EduAssess.Application.DTOs.Selo;
using EduAssess.Data;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Services
{
    /// <summary>
    /// Regras de selos compartilhadas pela API (/questoes) e pelas telas Blazor, para não duplicar validação.
    /// Os campos pedidos (ano, instituição) vêm dos flags do catálogo (SelosTipos), então um novo tipo
    /// de selo funciona sem mudar código nem esquema.
    /// </summary>
    public static class QuestaoSeloService
    {
        public static readonly string[] Cores = ["violet", "blue", "green", "amber", "red", "gray"];

        /// <summary>Texto do selo: "ENEM 2021", "Gerada com IA", "FUVEST 2020"...</summary>
        public static string Texto(SeloTipoModel t, int? ano, string? detalhe)
        {
            var main = t.Codigo == SeloCodigos.Vestibular && !string.IsNullOrWhiteSpace(detalhe) ? "Vestibular " + detalhe : t.Nome;
            return ano is null ? main : main + " " + ano;
        }

        public static SeloDto ToDto(QuestaoSeloModel s) =>
            new(s.SeloTipo.Codigo, s.SeloTipo.Nome, s.SeloTipo.Cor, s.Ano, s.Detalhe, s.Exibir, Texto(s.SeloTipo, s.Ano, s.Detalhe));

        public static async Task<List<SeloTipoDto>> ListarTiposAsync(AppDbContext db, CancellationToken ct = default) =>
            await db.SelosTipos.AsNoTracking().Where(t => t.Ativo).OrderBy(t => t.Ordem).ThenBy(t => t.Nome)
                .Select(t => new SeloTipoDto(t.Id, t.Codigo, t.Nome, t.Descricao, t.Cor, t.PermiteAno, t.ExigeAno, t.PermiteDetalhe, t.ExigeDetalhe, t.RotuloDetalhe))
                .ToListAsync(ct);

        /// <summary>Selos (com o tipo) de várias questões, agrupados por questão. Se onlyVisible, só os marcados para exibir.</summary>
        public static async Task<Dictionary<Guid, List<SeloDto>>> CarregarAsync(AppDbContext db, IEnumerable<Guid> questionIds, bool onlyVisible, CancellationToken ct = default)
        {
            var ids = questionIds.Distinct().ToList();
            if (ids.Count == 0) return new();
            var q = db.QuestoesSelos.AsNoTracking().Include(s => s.SeloTipo).Where(s => ids.Contains(s.QuestaoId) && s.SeloTipo.Ativo);
            if (onlyVisible) q = q.Where(s => s.Exibir);
            var rows = await q.ToListAsync(ct);
            return rows.GroupBy(s => s.QuestaoId)
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.SeloTipo.Ordem).Select(ToDto).ToList());
        }

        /// <summary>
        /// Valida e normaliza os selos pedidos contra o catálogo. Lança ArgumentException com mensagem clara.
        /// Duplicidade (dois selos do mesmo tipo) é recusada.
        /// </summary>
        public static async Task<List<(SeloTipoModel Tipo, int? Ano, string? Detalhe, bool Exibir)>> ValidarAsync(
            AppDbContext db, IEnumerable<SeloInput>? inputs, CancellationToken ct = default)
        {
            var result = new List<(SeloTipoModel, int?, string?, bool)>();
            var list = inputs?.ToList() ?? new();
            if (list.Count == 0) return result;

            var catalog = await db.SelosTipos.AsNoTracking().Where(t => t.Ativo).ToListAsync(ct);
            var maxYear = DateTime.UtcNow.Year + 1;
            foreach (var input in list)
            {
                var code = (input.Codigo ?? "").Trim().ToUpperInvariant();
                var tipo = catalog.FirstOrDefault(t => t.Codigo == code)
                    ?? throw new ArgumentException($"Selo desconhecido: {input.Codigo}.");
                if (result.Any(r => r.Item1.Id == tipo.Id))
                    throw new ArgumentException($"O selo {tipo.Nome} foi informado mais de uma vez.");

                var detalhe = string.IsNullOrWhiteSpace(input.Detalhe) ? null : input.Detalhe.Trim();
                if (!tipo.PermiteDetalhe && detalhe is not null) detalhe = null;
                if (detalhe is not null && detalhe.Length > 80)
                    throw new ArgumentException($"{tipo.RotuloDetalhe ?? "Detalhe"} do selo {tipo.Nome}: até 80 caracteres.");
                if (tipo.ExigeDetalhe && detalhe is null)
                    throw new ArgumentException($"Informe {(tipo.RotuloDetalhe ?? "o detalhe").ToLowerInvariant()} do selo {tipo.Nome}.");

                var ano = tipo.PermiteAno ? input.Ano : null;
                if (ano is not null && (ano < 1980 || ano > maxYear))
                    throw new ArgumentException($"Ano inválido no selo {tipo.Nome} (use de 1980 a {maxYear}).");
                if (tipo.ExigeAno && ano is null)
                    throw new ArgumentException($"Informe o ano do selo {tipo.Nome}.");

                result.Add((tipo, ano, detalhe, input.Exibir));
            }
            return result;
        }

        /// <summary>
        /// Deixa os selos da questão exatamente como pedido (adiciona, atualiza e remove). Não chama SaveChanges.
        /// </summary>
        public static async Task SincronizarAsync(AppDbContext db, Guid questaoId, IEnumerable<SeloInput>? inputs, CancellationToken ct = default)
        {
            var wanted = await ValidarAsync(db, inputs, ct);
            var current = await db.QuestoesSelos.Where(s => s.QuestaoId == questaoId).ToListAsync(ct);

            foreach (var old in current.Where(c => wanted.All(w => w.Tipo.Id != c.SeloTipoId)))
                db.QuestoesSelos.Remove(old);

            foreach (var w in wanted)
            {
                var existing = current.FirstOrDefault(c => c.SeloTipoId == w.Tipo.Id);
                if (existing is null) db.QuestoesSelos.Add(new QuestaoSeloModel(questaoId, w.Tipo.Id, w.Ano, w.Detalhe, w.Exibir));
                else if (existing.Ano != w.Ano || existing.Detalhe != w.Detalhe || existing.Exibir != w.Exibir)
                    existing.Update(w.Ano, w.Detalhe, w.Exibir);
            }
        }

        /// <summary>Catálogo inicial. Idempotente: só insere o que ainda não existe (por código).</summary>
        public static async Task GarantirCatalogoAsync(AppDbContext db, CancellationToken ct = default)
        {
            var padrao = new[]
            {
                new SeloTipoModel(SeloCodigos.IA, "Gerada com IA", "violet", 1, "Questão criada com apoio de Inteligência Artificial"),
                new SeloTipoModel(SeloCodigos.Enade, "ENADE", "blue", 2, "Exame Nacional de Desempenho dos Estudantes (Ensino Superior)", permiteAno: true),
                new SeloTipoModel(SeloCodigos.Enamed, "ENAMED", "green", 3, "Exame relacionado ao ENAMED", permiteAno: true),
                new SeloTipoModel(SeloCodigos.Enem, "ENEM", "amber", 4, "Exame Nacional do Ensino Médio", permiteAno: true),
                new SeloTipoModel(SeloCodigos.Vestibular, "Vestibular", "red", 5, "Vestibulares tradicionais", permiteAno: true, exigeDetalhe: true, rotuloDetalhe: "Vestibular / instituição"),
            };
            var codes = await db.SelosTipos.Select(t => t.Codigo).ToListAsync(ct);
            var novos = padrao.Where(p => !codes.Contains(p.Codigo)).ToList();
            if (novos.Count > 0)
            {
                db.SelosTipos.AddRange(novos);
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
