using System;
using EduAssess.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261212090000_AddInstitutionTenancyAndMaintainer")]
public partial class AddInstitutionTenancyAndMaintainer : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Mantenedores",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Nome = table.Column<string>(type: "text", nullable: false),
                CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Mantenedores", x => x.Id));

        migrationBuilder.AddColumn<Guid>(name: "MantenedorId", table: "Instituicoes", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "LogoPath", table: "Instituicoes", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "CorPrimaria", table: "Instituicoes", type: "text", nullable: false, defaultValue: "#1F4E79");
        migrationBuilder.AddColumn<string>(name: "CorSecundaria", table: "Instituicoes", type: "text", nullable: false, defaultValue: "#D6A94B");

        migrationBuilder.AddColumn<Guid>(name: "InstituicaoId", table: "Cursos", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "InstituicaoId", table: "Disciplinas", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "InstituicaoId", table: "Turmas", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "InstituicaoId", table: "Matriculas", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "InstituicaoId", table: "Notas", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "InstituicaoId", table: "Questoes", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "InstituicaoId", table: "Avaliacoes", type: "uuid", nullable: true);

        migrationBuilder.CreateIndex(name: "IX_Instituicoes_MantenedorId", table: "Instituicoes", column: "MantenedorId");
        migrationBuilder.AddForeignKey(name: "FK_Instituicoes_Mantenedores_MantenedorId", table: "Instituicoes", column: "MantenedorId", principalTable: "Mantenedores", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

        AddInstitutionForeignKey(migrationBuilder, "Cursos");
        AddInstitutionForeignKey(migrationBuilder, "Disciplinas");
        AddInstitutionForeignKey(migrationBuilder, "Turmas");
        AddInstitutionForeignKey(migrationBuilder, "Matriculas");
        AddInstitutionForeignKey(migrationBuilder, "Notas");
        AddInstitutionForeignKey(migrationBuilder, "Questoes");
        AddInstitutionForeignKey(migrationBuilder, "Avaliacoes");

        migrationBuilder.Sql("""
            INSERT INTO "Mantenedores" ("Id", "Nome", "CriadoEm")
            SELECT gen_random_uuid(), 'Luciano Feijão', NOW()
            WHERE EXISTS (SELECT 1 FROM "Instituicoes" WHERE "Slug" = 'faculdade-luciano-feijao')
              AND NOT EXISTS (SELECT 1 FROM "Mantenedores" WHERE "Nome" = 'Luciano Feijão');

            UPDATE "Instituicoes"
            SET "MantenedorId" = (
                SELECT "Id" FROM "Mantenedores" WHERE "Nome" = 'Luciano Feijão' ORDER BY "CriadoEm" LIMIT 1
            )
            WHERE "Slug" = 'faculdade-luciano-feijao' AND "MantenedorId" IS NULL;

            UPDATE "Cursos" SET "InstituicaoId" = (
                SELECT "Id" FROM "Instituicoes" WHERE "Slug" = 'faculdade-luciano-feijao' LIMIT 1
            ) WHERE "InstituicaoId" IS NULL;
            UPDATE "Disciplinas" SET "InstituicaoId" = (
                SELECT "Id" FROM "Instituicoes" WHERE "Slug" = 'faculdade-luciano-feijao' LIMIT 1
            ) WHERE "InstituicaoId" IS NULL;
            UPDATE "Turmas" SET "InstituicaoId" = (
                SELECT "Id" FROM "Instituicoes" WHERE "Slug" = 'faculdade-luciano-feijao' LIMIT 1
            ) WHERE "InstituicaoId" IS NULL;
            UPDATE "Matriculas" SET "InstituicaoId" = (
                SELECT "Id" FROM "Instituicoes" WHERE "Slug" = 'faculdade-luciano-feijao' LIMIT 1
            ) WHERE "InstituicaoId" IS NULL;
            UPDATE "Notas" SET "InstituicaoId" = (
                SELECT "Id" FROM "Instituicoes" WHERE "Slug" = 'faculdade-luciano-feijao' LIMIT 1
            ) WHERE "InstituicaoId" IS NULL;
            UPDATE "Questoes" SET "InstituicaoId" = (
                SELECT "Id" FROM "Instituicoes" WHERE "Slug" = 'faculdade-luciano-feijao' LIMIT 1
            ) WHERE "InstituicaoId" IS NULL;
            UPDATE "Avaliacoes" SET "InstituicaoId" = (
                SELECT "Id" FROM "Instituicoes" WHERE "Slug" = 'faculdade-luciano-feijao' LIMIT 1
            ) WHERE "InstituicaoId" IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropInstitutionForeignKey(migrationBuilder, "Avaliacoes");
        DropInstitutionForeignKey(migrationBuilder, "Questoes");
        DropInstitutionForeignKey(migrationBuilder, "Notas");
        DropInstitutionForeignKey(migrationBuilder, "Matriculas");
        DropInstitutionForeignKey(migrationBuilder, "Turmas");
        DropInstitutionForeignKey(migrationBuilder, "Disciplinas");
        DropInstitutionForeignKey(migrationBuilder, "Cursos");

        migrationBuilder.DropForeignKey(name: "FK_Instituicoes_Mantenedores_MantenedorId", table: "Instituicoes");
        migrationBuilder.DropIndex(name: "IX_Instituicoes_MantenedorId", table: "Instituicoes");
        migrationBuilder.DropColumn(name: "InstituicaoId", table: "Avaliacoes");
        migrationBuilder.DropColumn(name: "InstituicaoId", table: "Questoes");
        migrationBuilder.DropColumn(name: "InstituicaoId", table: "Notas");
        migrationBuilder.DropColumn(name: "InstituicaoId", table: "Matriculas");
        migrationBuilder.DropColumn(name: "InstituicaoId", table: "Turmas");
        migrationBuilder.DropColumn(name: "InstituicaoId", table: "Disciplinas");
        migrationBuilder.DropColumn(name: "InstituicaoId", table: "Cursos");
        migrationBuilder.DropColumn(name: "CorSecundaria", table: "Instituicoes");
        migrationBuilder.DropColumn(name: "CorPrimaria", table: "Instituicoes");
        migrationBuilder.DropColumn(name: "LogoPath", table: "Instituicoes");
        migrationBuilder.DropColumn(name: "MantenedorId", table: "Instituicoes");
        migrationBuilder.DropTable(name: "Mantenedores");
    }

    private static void AddInstitutionForeignKey(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.CreateIndex($"IX_{table}_InstituicaoId", table, "InstituicaoId");
        migrationBuilder.AddForeignKey(
            name: $"FK_{table}_Instituicoes_InstituicaoId",
            table: table,
            column: "InstituicaoId",
            principalTable: "Instituicoes",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    private static void DropInstitutionForeignKey(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.DropForeignKey($"FK_{table}_Instituicoes_InstituicaoId", table);
        migrationBuilder.DropIndex($"IX_{table}_InstituicaoId", table);
    }
}
