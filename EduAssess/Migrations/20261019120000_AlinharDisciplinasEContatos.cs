using EduAssess.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations;

/// <summary>
/// Alinha o banco ao modelo atual:
///  - Disciplinas: adiciona ProfessorId e Ativa (o modelo já usava as duas colunas, mas nenhuma migration as criava,
///    o que fazia qualquer consulta de disciplinas falhar com "no such column").
///  - Contatos: nova tabela com as solicitações do formulário público.
/// Usa SQL direto (como a migration RepairLegacyAcademicSchema) para não depender de um TargetModel gerado.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261019120000_AlinharDisciplinasEContatos")]
public partial class AlinharDisciplinasEContatos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "Disciplinas" ADD COLUMN "ProfessorId" TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
            ALTER TABLE "Disciplinas" ADD COLUMN "Ativa" INTEGER NOT NULL DEFAULT 1;

            CREATE TABLE "Contatos" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Contatos" PRIMARY KEY,
                "Nome" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "Telefone" TEXT NOT NULL,
                "Instituicao" TEXT NOT NULL,
                "Papel" TEXT NOT NULL,
                "Mensagem" TEXT NOT NULL,
                "CriadoEm" TEXT NOT NULL,
                "Lido" INTEGER NOT NULL
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE "Contatos";
            ALTER TABLE "Disciplinas" DROP COLUMN "Ativa";
            ALTER TABLE "Disciplinas" DROP COLUMN "ProfessorId";
            """);
    }
}
