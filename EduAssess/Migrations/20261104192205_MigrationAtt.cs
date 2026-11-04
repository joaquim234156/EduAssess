using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations
{
    /// <inheritdoc />
    public partial class MigrationAtt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Unidades_InstituicaoId_Codigo",
                table: "Unidades");

            migrationBuilder.RenameColumn(
                name: "Codigo",
                table: "Unidades",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "NomeFantasia",
                table: "Instituicoes",
                newName: "Telefone");

            migrationBuilder.AddColumn<DateTime>(
                name: "DesvinculoEm",
                table: "UsuariosInstituicoes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sigla",
                table: "Unidades",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Instituicoes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
                table: "Instituicoes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Instituicoes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_InstituicaoId_Sigla_Slug",
                table: "Unidades",
                columns: new[] { "InstituicaoId", "Sigla", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Unidades_InstituicaoId_Sigla_Slug",
                table: "Unidades");

            migrationBuilder.DropColumn(
                name: "DesvinculoEm",
                table: "UsuariosInstituicoes");

            migrationBuilder.DropColumn(
                name: "Sigla",
                table: "Unidades");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Instituicoes");

            migrationBuilder.DropColumn(
                name: "Endereco",
                table: "Instituicoes");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "Instituicoes");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "Unidades",
                newName: "Codigo");

            migrationBuilder.RenameColumn(
                name: "Telefone",
                table: "Instituicoes",
                newName: "NomeFantasia");

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_InstituicaoId_Codigo",
                table: "Unidades",
                columns: new[] { "InstituicaoId", "Codigo" },
                unique: true);
        }
    }
}
