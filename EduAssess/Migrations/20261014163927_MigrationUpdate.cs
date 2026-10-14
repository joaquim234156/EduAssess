using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations
{
    /// <inheritdoc />
    public partial class MigrationUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matriculas_Turmas_TurmaId",
                table: "Matriculas");

            migrationBuilder.DropForeignKey(
                name: "FK_Matriculas_Users_UsuarioId",
                table: "Matriculas");

            migrationBuilder.DropForeignKey(
                name: "FK_Notas_Matriculas_MatriculaId",
                table: "Notas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Turmas",
                table: "Turmas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Notas",
                table: "Notas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Matriculas",
                table: "Matriculas");

            migrationBuilder.RenameTable(
                name: "Turmas",
                newName: "TurmaModel");

            migrationBuilder.RenameTable(
                name: "Notas",
                newName: "NotaModel");

            migrationBuilder.RenameTable(
                name: "Matriculas",
                newName: "MatriculaModel");

            migrationBuilder.RenameIndex(
                name: "IX_Notas_MatriculaId",
                table: "NotaModel",
                newName: "IX_NotaModel_MatriculaId");

            migrationBuilder.RenameIndex(
                name: "IX_Matriculas_UsuarioId",
                table: "MatriculaModel",
                newName: "IX_MatriculaModel_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Matriculas_TurmaId",
                table: "MatriculaModel",
                newName: "IX_MatriculaModel_TurmaId");

            migrationBuilder.AddColumn<Guid>(
                name: "IdTurma",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_TurmaModel",
                table: "TurmaModel",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotaModel",
                table: "NotaModel",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MatriculaModel",
                table: "MatriculaModel",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MatriculaModel_TurmaModel_TurmaId",
                table: "MatriculaModel",
                column: "TurmaId",
                principalTable: "TurmaModel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MatriculaModel_Users_UsuarioId",
                table: "MatriculaModel",
                column: "UsuarioId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotaModel_MatriculaModel_MatriculaId",
                table: "NotaModel",
                column: "MatriculaId",
                principalTable: "MatriculaModel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatriculaModel_TurmaModel_TurmaId",
                table: "MatriculaModel");

            migrationBuilder.DropForeignKey(
                name: "FK_MatriculaModel_Users_UsuarioId",
                table: "MatriculaModel");

            migrationBuilder.DropForeignKey(
                name: "FK_NotaModel_MatriculaModel_MatriculaId",
                table: "NotaModel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TurmaModel",
                table: "TurmaModel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotaModel",
                table: "NotaModel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MatriculaModel",
                table: "MatriculaModel");

            migrationBuilder.DropColumn(
                name: "IdTurma",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "TurmaModel",
                newName: "Turmas");

            migrationBuilder.RenameTable(
                name: "NotaModel",
                newName: "Notas");

            migrationBuilder.RenameTable(
                name: "MatriculaModel",
                newName: "Matriculas");

            migrationBuilder.RenameIndex(
                name: "IX_NotaModel_MatriculaId",
                table: "Notas",
                newName: "IX_Notas_MatriculaId");

            migrationBuilder.RenameIndex(
                name: "IX_MatriculaModel_UsuarioId",
                table: "Matriculas",
                newName: "IX_Matriculas_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_MatriculaModel_TurmaId",
                table: "Matriculas",
                newName: "IX_Matriculas_TurmaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Turmas",
                table: "Turmas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Notas",
                table: "Notas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Matriculas",
                table: "Matriculas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matriculas_Turmas_TurmaId",
                table: "Matriculas",
                column: "TurmaId",
                principalTable: "Turmas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matriculas_Users_UsuarioId",
                table: "Matriculas",
                column: "UsuarioId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notas_Matriculas_MatriculaId",
                table: "Notas",
                column: "MatriculaId",
                principalTable: "Matriculas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
