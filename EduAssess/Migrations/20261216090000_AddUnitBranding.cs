using EduAssess.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduAssess.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261216090000_AddUnitBranding")]
public partial class AddUnitBranding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LogoPath", table: "Unidades", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "CorPrimaria", table: "Unidades", type: "character varying(7)", maxLength: 7, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "CorSecundaria", table: "Unidades", type: "character varying(7)", maxLength: 7, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "LogoPath", table: "Unidades");
        migrationBuilder.DropColumn(name: "CorPrimaria", table: "Unidades");
        migrationBuilder.DropColumn(name: "CorSecundaria", table: "Unidades");
    }
}
