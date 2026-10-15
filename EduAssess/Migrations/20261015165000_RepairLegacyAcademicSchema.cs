using EduAssess.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261015165000_RepairLegacyAcademicSchema")]
public partial class RepairLegacyAcademicSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE "__EduAssessMigrationGuard" ("Value" INTEGER NOT NULL);
            CREATE TRIGGER "__EduAssessMigrationGuardTrigger"
            BEFORE INSERT ON "__EduAssessMigrationGuard"
            BEGIN
                SELECT CASE WHEN
                    EXISTS (SELECT 1 FROM "TurmaModel")
                    OR EXISTS (SELECT 1 FROM "MatriculaModel")
                    OR EXISTS (SELECT 1 FROM "NotaModel")
                THEN RAISE(ABORT, 'Academic records in the legacy schema require manual reconciliation before migration.') END;
            END;
            INSERT INTO "__EduAssessMigrationGuard" ("Value") VALUES (1);
            DROP TRIGGER "__EduAssessMigrationGuardTrigger";
            DROP TABLE "__EduAssessMigrationGuard";

            DROP TABLE "NotaModel";
            DROP TABLE "MatriculaModel";
            DROP TABLE "TurmaModel";

            ALTER TABLE "Cursos" RENAME TO "__LegacyCursos";
            CREATE TABLE "Cursos" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Cursos" PRIMARY KEY,
                "Nome" TEXT NOT NULL,
                "Descricao" TEXT NOT NULL,
                "DuracaoEmAnos" INTEGER NOT NULL,
                "Ativo" INTEGER NOT NULL
            );
            INSERT INTO "Cursos" ("Id", "Nome", "Descricao", "DuracaoEmAnos", "Ativo")
            SELECT "Id", "Nome", "Descricao", "DuracaoEmAnos", "Ativo" FROM "__LegacyCursos";
            DROP TABLE "__LegacyCursos";

            CREATE TABLE "Turmas" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Turmas" PRIMARY KEY,
                "ProfessorId" TEXT NOT NULL,
                "CursoId" TEXT NOT NULL,
                "Periodo" INTEGER NOT NULL,
                "Nome" TEXT NOT NULL,
                "Ativo" INTEGER NOT NULL,
                CONSTRAINT "FK_Turmas_Cursos_CursoId" FOREIGN KEY ("CursoId") REFERENCES "Cursos" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_Turmas_Users_ProfessorId" FOREIGN KEY ("ProfessorId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX "IX_Turmas_CursoId" ON "Turmas" ("CursoId");
            CREATE INDEX "IX_Turmas_ProfessorId" ON "Turmas" ("ProfessorId");

            CREATE TABLE "Matriculas" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Matriculas" PRIMARY KEY,
                "UsuarioId" TEXT NOT NULL,
                "TurmaId" TEXT NOT NULL,
                "DataMatricula" TEXT NOT NULL,
                "DataExpiracao" TEXT NOT NULL,
                "Ativa" INTEGER NOT NULL,
                CONSTRAINT "FK_Matriculas_Turmas_TurmaId" FOREIGN KEY ("TurmaId") REFERENCES "Turmas" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_Matriculas_Users_UsuarioId" FOREIGN KEY ("UsuarioId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX "IX_Matriculas_TurmaId" ON "Matriculas" ("TurmaId");
            CREATE INDEX "IX_Matriculas_UsuarioId" ON "Matriculas" ("UsuarioId");

            CREATE TABLE "Notas" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Notas" PRIMARY KEY,
                "MatriculaId" TEXT NOT NULL,
                "Descricao" TEXT NOT NULL,
                "Valor" TEXT NOT NULL,
                "DataLancamento" TEXT NOT NULL,
                CONSTRAINT "FK_Notas_Matriculas_MatriculaId" FOREIGN KEY ("MatriculaId") REFERENCES "Matriculas" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX "IX_Notas_MatriculaId" ON "Notas" ("MatriculaId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE "__EduAssessMigrationGuard" ("Value" INTEGER NOT NULL);
            CREATE TRIGGER "__EduAssessMigrationGuardTrigger"
            BEFORE INSERT ON "__EduAssessMigrationGuard"
            BEGIN
                SELECT CASE WHEN
                    EXISTS (SELECT 1 FROM "Turmas")
                    OR EXISTS (SELECT 1 FROM "Matriculas")
                    OR EXISTS (SELECT 1 FROM "Notas")
                THEN RAISE(ABORT, 'Academic records must be exported before reverting this schema migration.') END;
            END;
            INSERT INTO "__EduAssessMigrationGuard" ("Value") VALUES (1);
            DROP TRIGGER "__EduAssessMigrationGuardTrigger";
            DROP TABLE "__EduAssessMigrationGuard";

            DROP TABLE "Notas";
            DROP TABLE "Matriculas";
            DROP TABLE "Turmas";

            CREATE TABLE "TurmaModel" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_TurmaModel" PRIMARY KEY,
                "Ativa" INTEGER NOT NULL,
                "CursoId" INTEGER NOT NULL,
                "Descricao" TEXT NOT NULL,
                "Nome" TEXT NOT NULL
            );
            CREATE TABLE "MatriculaModel" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_MatriculaModel" PRIMARY KEY,
                "Ativa" INTEGER NOT NULL,
                "DataExpiracao" TEXT NOT NULL,
                "DataMatricula" TEXT NOT NULL,
                "TurmaId" TEXT NOT NULL,
                "UsuarioId" TEXT NOT NULL
            );
            CREATE TABLE "NotaModel" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_NotaModel" PRIMARY KEY,
                "DataLancamento" TEXT NOT NULL,
                "Descricao" TEXT NOT NULL,
                "MatriculaId" TEXT NOT NULL,
                "Valor" TEXT NOT NULL
            );

            ALTER TABLE "Cursos" RENAME TO "__CurrentCursos";
            CREATE TABLE "Cursos" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Cursos" PRIMARY KEY,
                "Nome" TEXT NOT NULL,
                "Descricao" TEXT NOT NULL,
                "Professor" TEXT NOT NULL,
                "DuracaoEmAnos" INTEGER NOT NULL,
                "Ativo" INTEGER NOT NULL
            );
            INSERT INTO "Cursos" ("Id", "Nome", "Descricao", "Professor", "DuracaoEmAnos", "Ativo")
            SELECT "Id", "Nome", "Descricao", '', "DuracaoEmAnos", "Ativo" FROM "__CurrentCursos";
            DROP TABLE "__CurrentCursos";
            """);
    }
}
