using EduAssess.Application.DTOs.Auditoria;
using EduAssess.Data;
using EduAssess.Data.Audit;
using EduAssess.Models.Global;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Services
{
    /// <summary>
    /// Histórico de atividades. SÓ LÊ e INSERE: não existe nenhum método para editar ou excluir registros.
    /// As regras de visibilidade ficam aqui (backend), e valem igual para o painel e para a API:
    ///  • Admin: tudo (opcionalmente filtrado por instituição);
    ///  • Gestor: gestão acadêmica (cadastros, turmas, matrículas, notas, instituição) das suas instituições;
    ///  • Professor: apenas o que envolve suas turmas e seus conteúdos, incluindo a atividade dos alunos;
    ///  • demais perfis: sem acesso.
    /// </summary>
    public sealed class AuditoriaService(IDbContextFactory<AppDbContext> dbFactory, ILogger<AuditoriaService> logger)
    {
        // ───────────────────────── Registro explícito (acessos e atividade do aluno) ─────────────────────────

        public async Task RegistrarAsync(AcaoAuditoria acao, CategoriaAuditoria categoria, string entidade, AuditActor? actor,
            string? entidadeId = null, string? rotulo = null, string? detalhes = null, Guid? professorId = null, Guid? alunoId = null,
            Guid? instituicaoId = null)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                db.RegistrosAuditoria.Add(new RegistroAuditoriaModel(acao, categoria, entidade, actor?.UserId,
                        actor?.Name ?? "Sistema", actor?.Role ?? "", instituicaoId ?? actor?.InstitutionId)
                    .Com(entidadeId, rotulo, detalhes, professorId, alunoId, actor?.Ip));
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // O histórico não deve derrubar o login nem a prática do aluno; a falha fica no log do servidor.
                logger.LogError(ex, "Falha ao gravar registro de auditoria ({Acao} {Entidade}).", acao, entidade);
            }
        }

        // ───────────────────────── Consulta ─────────────────────────

        private static IQueryable<RegistroAuditoriaModel> Scope(IQueryable<RegistroAuditoriaModel> q, AuditViewer v)
        {
            switch (v.Role)
            {
                case UserRole.Admin:
                    return v.InstitutionIds is null ? q : q.Where(r => r.InstituicaoId != null && v.InstitutionIds.Contains(r.InstituicaoId.Value));
                case UserRole.Gestor:
                    var ids = v.InstitutionIds ?? Array.Empty<Guid>();
                    return q.Where(r => r.Categoria == CategoriaAuditoria.Gestao && r.InstituicaoId != null && ids.Contains(r.InstituicaoId.Value));
                case UserRole.Professor:
                    var mine = v.UserId;
                    var inst = v.InstitutionIds ?? Array.Empty<Guid>();
                    return q.Where(r => r.ProfessorResponsavelId == mine && r.InstituicaoId != null && inst.Contains(r.InstituicaoId.Value));
                default:
                    throw new UnauthorizedAccessException("Seu perfil não tem acesso ao histórico de atividades.");
            }
        }

        public async Task<AuditPageDto> ListarAsync(AuditViewer viewer, AuditFilter f, CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var q = Filter(Scope(db.RegistrosAuditoria.AsNoTracking(), viewer), f, viewer);

            var page = Math.Max(1, f.Page);
            var size = Math.Clamp(f.PageSize, 5, 100);
            var total = await q.CountAsync(ct);
            var rows = await q.OrderByDescending(r => r.OcorridoEm).Skip((page - 1) * size).Take(size).ToListAsync(ct);

            var alunoIds = rows.Where(r => r.AlunoId != null).Select(r => r.AlunoId!.Value).Distinct().ToList();
            var names = alunoIds.Count == 0 ? new Dictionary<Guid, string>()
                : await db.Users.AsNoTracking().Where(u => alunoIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);

            var showIp = viewer.Role == UserRole.Admin;   // IP só para o Admin
            return new AuditPageDto(rows.Select(r => new AuditRowDto(
                r.Id, r.OcorridoEm, AuditoriaLabels.Acao(r.Acao), AuditoriaLabels.Categoria(r.Categoria), AuditoriaLabels.Entidade(r.Entidade),
                r.EntidadeId, r.EntidadeRotulo, r.Detalhes, r.AtorNome, r.AtorPerfil,
                r.AlunoId is Guid a && names.TryGetValue(a, out var n) ? n : null, showIp ? r.Ip : null)).ToList(), total, page, size);
        }

        private static IQueryable<RegistroAuditoriaModel> Filter(IQueryable<RegistroAuditoriaModel> q, AuditFilter f, AuditViewer viewer)
        {
            if (f.OnlyStudentActivity) q = q.Where(r => r.Categoria == CategoriaAuditoria.Atividade);
            if (f.From is DateTime from) q = q.Where(r => r.OcorridoEm >= DateTime.SpecifyKind(from, DateTimeKind.Utc));
            if (f.To is DateTime to) q = q.Where(r => r.OcorridoEm < DateTime.SpecifyKind(to, DateTimeKind.Utc));
            if (f.Acao is AcaoAuditoria a) q = q.Where(r => r.Acao == a);
            if (f.Categoria is CategoriaAuditoria c) q = q.Where(r => r.Categoria == c);
            if (!string.IsNullOrWhiteSpace(f.Entidade)) { var e = f.Entidade.Trim(); q = q.Where(r => r.Entidade == e); }
            if (viewer.Role == UserRole.Admin && f.InstituicaoId is Guid i) q = q.Where(r => r.InstituicaoId == i);
            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var like = "%" + f.Search.Trim().Replace("%", "").Replace("_", "") + "%";
                q = q.Where(r => EF.Functions.ILike(r.AtorNome, like) || (r.EntidadeRotulo != null && EF.Functions.ILike(r.EntidadeRotulo, like))
                                 || (r.Detalhes != null && EF.Functions.ILike(r.Detalhes, like)));
            }
            return q;
        }

        /// <summary>Resumo do que os alunos fizeram (dentro do escopo do professor/admin) e as questões mais acessadas.</summary>
        public async Task<StudentActivitySummary> ResumoAtividadeAsync(AuditViewer viewer, DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            if (viewer.Role is not (UserRole.Admin or UserRole.Professor))
                throw new UnauthorizedAccessException("Seu perfil não tem acesso à atividade dos alunos.");

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var q = Scope(db.RegistrosAuditoria.AsNoTracking(), viewer).Where(r => r.Categoria == CategoriaAuditoria.Atividade);
            if (from is DateTime f) q = q.Where(r => r.OcorridoEm >= DateTime.SpecifyKind(f, DateTimeKind.Utc));
            if (to is DateTime t) q = q.Where(r => r.OcorridoEm < DateTime.SpecifyKind(t, DateTimeKind.Utc));

            var rows = await q.Select(r => new { r.Acao, r.AlunoId, r.EntidadeId, r.EntidadeRotulo, r.Detalhes, r.OcorridoEm }).ToListAsync(ct);
            var answers = rows.Where(r => r.Acao == AcaoAuditoria.QuestaoRespondida).ToList();
            var perQuestion = answers.GroupBy(r => r.EntidadeId ?? "").Select(g => new QuestionActivityRow(
                g.Key, g.First().EntidadeRotulo ?? "Questão", g.Count(), g.Select(x => x.AlunoId).Distinct().Count(),
                g.Count(x => x.Detalhes != null && x.Detalhes.StartsWith("Acertou")), g.Max(x => x.OcorridoEm)))
                .OrderByDescending(x => x.Respostas).Take(10).ToList();

            return new StudentActivitySummary(
                rows.Where(r => r.AlunoId != null).Select(r => r.AlunoId).Distinct().Count(),
                answers.Count, answers.Count(r => r.Detalhes != null && r.Detalhes.StartsWith("Acertou")),
                rows.Count(r => r.Acao == AcaoAuditoria.TesteConcluido), perQuestion);
        }
    }

    public static class AuditoriaLabels
    {
        public static string Acao(AcaoAuditoria a) => a switch
        {
            AcaoAuditoria.Criacao => "Criação",
            AcaoAuditoria.Edicao => "Edição",
            AcaoAuditoria.Exclusao => "Exclusão",
            AcaoAuditoria.StatusAlterado => "Mudança de status",
            AcaoAuditoria.Acesso => "Acesso",
            AcaoAuditoria.FalhaAcesso => "Acesso recusado",
            AcaoAuditoria.Saida => "Saída",
            AcaoAuditoria.TesteIniciado => "Teste iniciado",
            AcaoAuditoria.QuestaoRespondida => "Questão respondida",
            AcaoAuditoria.TesteConcluido => "Teste concluído",
            AcaoAuditoria.MaterialVisualizado => "Material visualizado",
            _ => a.ToString()
        };

        public static string Categoria(CategoriaAuditoria c) => c switch
        {
            CategoriaAuditoria.Gestao => "Gestão",
            CategoriaAuditoria.Conteudo => "Conteúdo",
            CategoriaAuditoria.Acesso => "Acessos",
            CategoriaAuditoria.Atividade => "Atividade do aluno",
            CategoriaAuditoria.Desempenho => "Desempenho",
            _ => "Sistema"
        };

        public static string Entidade(string e) => e switch
        {
            "Usuario" => "Usuário", "Curso" => "Curso", "Disciplina" => "Disciplina", "Turma" => "Turma", "Matricula" => "Matrícula",
            "Nota" => "Nota", "Instituicao" => "Instituição", "Unidade" => "Unidade", "Mantenedor" => "Mantenedor", "Vinculo" => "Vínculo",
            "Questao" => "Questão", "Selo" => "Selo", "Avaliacao" => "Avaliação", "AvaliacaoQuestao" => "Composição da avaliação",
            "ResultadoPratica" => "Resultado de simulado", "SeloTipo" => "Tipo de selo", "Sessao" => "Sessão",
            _ => e
        };
    }
}
