using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarModeloAtual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatriculaModel_TurmaModel_TurmaId",
                table: "MatriculaModel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TurmaModel",
                table: "TurmaModel");

            migrationBuilder.DropColumn(
                name: "Professor",
                table: "Cursos");

            migrationBuilder.RenameTable(
                name: "TurmaModel",
                newName: "Turmas");

            migrationBuilder.RenameColumn(
                name: "Descricao",
                table: "Turmas",
                newName: "ProfessorId");

            migrationBuilder.RenameColumn(
                name: "Ativa",
                table: "Turmas",
                newName: "Periodo");

            migrationBuilder.AlterColumn<Guid>(
                name: "CursoId",
                table: "Turmas",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Turmas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Turmas",
                table: "Turmas",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_CursoId",
                table: "Turmas",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_ProfessorId",
                table: "Turmas",
                column: "ProfessorId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatriculaModel_Turmas_TurmaId",
                table: "MatriculaModel",
                column: "TurmaId",
                principalTable: "Turmas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Turmas_Cursos_CursoId",
                table: "Turmas",
                column: "CursoId",
                principalTable: "Cursos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Turmas_Users_ProfessorId",
                table: "Turmas",
                column: "ProfessorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatriculaModel_Turmas_TurmaId",
                table: "MatriculaModel");

            migrationBuilder.DropForeignKey(
                name: "FK_Turmas_Cursos_CursoId",
                table: "Turmas");

            migrationBuilder.DropForeignKey(
                name: "FK_Turmas_Users_ProfessorId",
                table: "Turmas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Turmas",
                table: "Turmas");

            migrationBuilder.DropIndex(
                name: "IX_Turmas_CursoId",
                table: "Turmas");

            migrationBuilder.DropIndex(
                name: "IX_Turmas_ProfessorId",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Turmas");

            migrationBuilder.RenameTable(
                name: "Turmas",
                newName: "TurmaModel");

            migrationBuilder.RenameColumn(
                name: "ProfessorId",
                table: "TurmaModel",
                newName: "Descricao");

            migrationBuilder.RenameColumn(
                name: "Periodo",
                table: "TurmaModel",
                newName: "Ativa");

            migrationBuilder.AddColumn<string>(
                name: "Professor",
                table: "Cursos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "CursoId",
                table: "TurmaModel",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TurmaModel",
                table: "TurmaModel",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MatriculaModel_TurmaModel_TurmaId",
                table: "MatriculaModel",
                column: "TurmaId",
                principalTable: "TurmaModel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
