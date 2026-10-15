using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations
{
    /// <inheritdoc />
    public partial class RemovendoProfessorDeCursos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
            name: "Professor",
            table: "Cursos"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
