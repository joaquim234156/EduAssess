using System;
using EduAssess.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations
{
    // NOVO: guarda em qual unidade (prédio) cada pessoa está lotada.
    // Coluna opcional: os vínculos que já existem continuam válidos, só sem unidade.
    // Os atributos [DbContext] e [Migration] ficam aqui mesmo (sem arquivo .Designer.cs).
    [DbContext(typeof(AppDbContext))]
    [Migration("20261105120000_MigrationUnidadeDoUsuario")]
    public partial class MigrationUnidadeDoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UnidadeId",
                table: "UsuariosInstituicoes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosInstituicoes_UnidadeId",
                table: "UsuariosInstituicoes",
                column: "UnidadeId");

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosInstituicoes_Unidades_UnidadeId",
                table: "UsuariosInstituicoes",
                column: "UnidadeId",
                principalTable: "Unidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosInstituicoes_Unidades_UnidadeId",
                table: "UsuariosInstituicoes");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosInstituicoes_UnidadeId",
                table: "UsuariosInstituicoes");

            migrationBuilder.DropColumn(
                name: "UnidadeId",
                table: "UsuariosInstituicoes");
        }
    }
}
