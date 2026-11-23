using System;
using EduAssess.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations;

/// <summary>
/// NOVO (escrita à mão, igual à migration anterior do projeto, que também não tem snapshot):
///  1) Instituicoes: Tipo (Faculdade/Colegio) e Sigla  → estrutura do CLF;
///  2) SelosTipos + QuestoesSelos                       → selos/classificações das questões;
///  3) ResultadosPratica                                → notas de treino/simulado do aluno (separadas das notas oficiais);
///  4) RegistrosAuditoria + gatilhos                    → histórico somente leitura (o banco bloqueia UPDATE/DELETE/TRUNCATE).
/// O catálogo de selos é preenchido pelo DatabaseInitializer (idempotente), não aqui.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261215120000_AddInstitutionTypesSealsPracticeResultsAndAudit")]
public partial class AddInstitutionTypesSealsPracticeResultsAndAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1) Instituição: categoria e sigla
        migrationBuilder.AddColumn<string>(name: "Tipo", table: "Instituicoes", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Faculdade");
        migrationBuilder.AddColumn<string>(name: "Sigla", table: "Instituicoes", type: "character varying(10)", maxLength: 10, nullable: true);
        migrationBuilder.Sql("""UPDATE "Instituicoes" SET "Sigla" = 'FLF' WHERE "Slug" = 'faculdade-luciano-feijao' AND "Sigla" IS NULL;""");

        // 2) Selos
        migrationBuilder.CreateTable(
            name: "SelosTipos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Cor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Ordem = table.Column<int>(type: "integer", nullable: false),
                PermiteAno = table.Column<bool>(type: "boolean", nullable: false),
                ExigeAno = table.Column<bool>(type: "boolean", nullable: false),
                PermiteDetalhe = table.Column<bool>(type: "boolean", nullable: false),
                ExigeDetalhe = table.Column<bool>(type: "boolean", nullable: false),
                RotuloDetalhe = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                Ativo = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SelosTipos", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_SelosTipos_Codigo", table: "SelosTipos", column: "Codigo", unique: true);

        migrationBuilder.CreateTable(
            name: "QuestoesSelos",
            columns: table => new
            {
                QuestaoId = table.Column<Guid>(type: "uuid", nullable: false),
                SeloTipoId = table.Column<Guid>(type: "uuid", nullable: false),
                Ano = table.Column<int>(type: "integer", nullable: true),
                Detalhe = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                Exibir = table.Column<bool>(type: "boolean", nullable: false),
                CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuestoesSelos", x => new { x.QuestaoId, x.SeloTipoId });
                table.ForeignKey("FK_QuestoesSelos_Questoes_QuestaoId", x => x.QuestaoId, "Questoes", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_QuestoesSelos_SelosTipos_SeloTipoId", x => x.SeloTipoId, "SelosTipos", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_QuestoesSelos_SeloTipoId", table: "QuestoesSelos", column: "SeloTipoId");

        // 3) Resultados de prática do aluno
        migrationBuilder.CreateTable(
            name: "ResultadosPratica",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                InstituicaoId = table.Column<Guid>(type: "uuid", nullable: false),
                UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                Origem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                AvaliacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                DisciplinaId = table.Column<Guid>(type: "uuid", nullable: true),
                Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                DisciplinaNome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TotalQuestoes = table.Column<int>(type: "integer", nullable: false),
                Acertos = table.Column<int>(type: "integer", nullable: false),
                Nota = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                RealizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Contabilizar = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResultadosPratica", x => x.Id);
                table.ForeignKey("FK_ResultadosPratica_Instituicoes_InstituicaoId", x => x.InstituicaoId, "Instituicoes", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ResultadosPratica_Users_UsuarioId", x => x.UsuarioId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_ResultadosPratica_InstituicaoId", table: "ResultadosPratica", column: "InstituicaoId");
        migrationBuilder.CreateIndex(name: "IX_ResultadosPratica_UsuarioId_InstituicaoId_RealizadoEm", table: "ResultadosPratica", columns: new[] { "UsuarioId", "InstituicaoId", "RealizadoEm" });

        // 4) Auditoria
        migrationBuilder.CreateTable(
            name: "RegistrosAuditoria",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OcorridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Acao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Categoria = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Entidade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                EntidadeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                EntidadeRotulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Detalhes = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                AtorId = table.Column<Guid>(type: "uuid", nullable: true),
                AtorNome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                AtorPerfil = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                InstituicaoId = table.Column<Guid>(type: "uuid", nullable: true),
                ProfessorResponsavelId = table.Column<Guid>(type: "uuid", nullable: true),
                AlunoId = table.Column<Guid>(type: "uuid", nullable: true),
                Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_RegistrosAuditoria_InstituicaoId_OcorridoEm", table: "RegistrosAuditoria", columns: new[] { "InstituicaoId", "OcorridoEm" });
        migrationBuilder.CreateIndex(name: "IX_RegistrosAuditoria_ProfessorResponsavelId_OcorridoEm", table: "RegistrosAuditoria", columns: new[] { "ProfessorResponsavelId", "OcorridoEm" });
        migrationBuilder.CreateIndex(name: "IX_RegistrosAuditoria_AtorId", table: "RegistrosAuditoria", column: "AtorId");
        migrationBuilder.CreateIndex(name: "IX_RegistrosAuditoria_Categoria_OcorridoEm", table: "RegistrosAuditoria", columns: new[] { "Categoria", "OcorridoEm" });

        // Trava no BANCO: nem a aplicação, nem uma consulta manual comum conseguem alterar/apagar o histórico.
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION fn_auditoria_somente_leitura() RETURNS trigger AS $$
            BEGIN
                RAISE EXCEPTION 'RegistrosAuditoria é somente leitura (% bloqueado).', TG_OP
                    USING ERRCODE = 'insufficient_privilege';
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER trg_auditoria_sem_update_delete
                BEFORE UPDATE OR DELETE ON "RegistrosAuditoria"
                FOR EACH ROW EXECUTE FUNCTION fn_auditoria_somente_leitura();

            CREATE TRIGGER trg_auditoria_sem_truncate
                BEFORE TRUNCATE ON "RegistrosAuditoria"
                FOR EACH STATEMENT EXECUTE FUNCTION fn_auditoria_somente_leitura();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_auditoria_sem_truncate ON "RegistrosAuditoria";
            DROP TRIGGER IF EXISTS trg_auditoria_sem_update_delete ON "RegistrosAuditoria";
            DROP FUNCTION IF EXISTS fn_auditoria_somente_leitura();
            """);
        migrationBuilder.DropTable(name: "RegistrosAuditoria");
        migrationBuilder.DropTable(name: "ResultadosPratica");
        migrationBuilder.DropTable(name: "QuestoesSelos");
        migrationBuilder.DropTable(name: "SelosTipos");
        migrationBuilder.DropColumn(name: "Sigla", table: "Instituicoes");
        migrationBuilder.DropColumn(name: "Tipo", table: "Instituicoes");
    }
}
