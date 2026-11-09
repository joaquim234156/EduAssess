using System;
using EduAssess.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations
{
    // NOVO: cria as tabelas das avaliações (provas/simulados) e a ligação com as questões.
    // Os atributos [DbContext] e [Migration] ficam aqui mesmo (em vez de num arquivo .Designer.cs).
    [DbContext(typeof(AppDbContext))]
    [Migration("20261025120000_MigrationAvaliacoes")]
    public partial class MigrationAvaliacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Avaliacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisciplinaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Detalhes = table.Column<string>(type: "TEXT", nullable: false),
                    Periodicidade = table.Column<int>(type: "INTEGER", nullable: false),
                    Duracao = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avaliacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AvaliacoesQuestoes",
                columns: table => new
                {
                    AvaliacaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuestaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaliacoesQuestoes", x => new { x.AvaliacaoId, x.QuestaoId });
                    table.ForeignKey(
                        name: "FK_AvaliacoesQuestoes_Avaliacoes_AvaliacaoId",
                        column: x => x.AvaliacaoId,
                        principalTable: "Avaliacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_Codigo",
                table: "Avaliacoes",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AvaliacoesQuestoes");
            migrationBuilder.DropTable(name: "Avaliacoes");
        }
    }
}
