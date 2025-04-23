using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Talepreter.Data.Migrations.AnecdoteSvc.Migrations
{
    /// <inheritdoc />
    public partial class BackupProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            using Stream stream = asm.GetManifestResourceStream("Talepreter.Data.Migrations.AnecdoteSvc.Scripts.BackupScript.sql");
            using StreamReader reader = new(stream);
            string result = reader.ReadToEnd();
            migrationBuilder.Sql(result, true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
