using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations
{
    /// <inheritdoc />
    public partial class MigrationQuestao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Questoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisciplinaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Enunciado = table.Column<string>(type: "TEXT", nullable: false),
                    AlternativaCorreta = table.Column<string>(type: "TEXT", nullable: false),
                    Alternativa1 = table.Column<string>(type: "TEXT", nullable: false),
                    Alternativa2 = table.Column<string>(type: "TEXT", nullable: false),
                    Alternativa3 = table.Column<string>(type: "TEXT", nullable: false),
                    Alternativa4 = table.Column<string>(type: "TEXT", nullable: false),
                    Alternativa5 = table.Column<string>(type: "TEXT", nullable: false),
                    Assunto = table.Column<string>(type: "TEXT", nullable: false),
                    Explicacao = table.Column<string>(type: "TEXT", nullable: false),
                    Dificuldade = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questoes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Questoes");
        }
    }
}
