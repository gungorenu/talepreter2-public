using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Talepreter.Data.Migrations.PersonSvc.Migrations
{
    /// <inheritdoc />
    public partial class CmdUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasChild",
                table: "Commands");

            migrationBuilder.DropColumn(
                name: "Prequisite",
                table: "Commands");

            migrationBuilder.AlterColumn<string>(
                name: "Parameter",
                table: "Triggers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<long>(
                name: "Duration",
                table: "Commands",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Commands");

            migrationBuilder.AlterColumn<string>(
                name: "Parameter",
                table: "Triggers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasChild",
                table: "Commands",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Prequisite",
                table: "Commands",
                type: "int",
                nullable: true);
        }
    }
}
