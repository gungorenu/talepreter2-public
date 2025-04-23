using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Talepreter.Data.Migrations.ActorSvc.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "shared");

            migrationBuilder.CreateSequence(
                name: "SubIndexSequence",
                schema: "shared");

            migrationBuilder.CreateTable(
                name: "Commands",
                columns: table => new
                {
                    TaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaleVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    Phase = table.Column<int>(type: "int", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    SubIndex = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "NEXT VALUE FOR shared.SubIndexSequence"),
                    OperationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Prequisite = table.Column<int>(type: "int", nullable: true),
                    HasChild = table.Column<bool>(type: "bit", nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Result = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commands", x => new { x.TaleId, x.TaleVersionId, x.ChapterId, x.PageId, x.Index, x.Phase, x.SubIndex });
                });

            migrationBuilder.CreateTable(
                name: "Triggers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaleVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    TriggerAt = table.Column<long>(type: "bigint", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GrainType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GrainId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Parameter = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Triggers", x => new { x.TaleId, x.TaleVersionId, x.Id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commands_TaleId_TaleVersionId_ChapterId_PageId_Phase",
                table: "Commands",
                columns: new[] { "TaleId", "TaleVersionId", "ChapterId", "PageId", "Phase" });

            migrationBuilder.CreateIndex(
                name: "IX_Triggers_TaleId",
                table: "Triggers",
                column: "TaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Triggers_TaleId_TaleVersionId_Id_Type",
                table: "Triggers",
                columns: new[] { "TaleId", "TaleVersionId", "Id", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Triggers_TaleId_TaleVersionId_Id_Type_GrainType",
                table: "Triggers",
                columns: new[] { "TaleId", "TaleVersionId", "Id", "Type", "GrainType" });

            migrationBuilder.CreateIndex(
                name: "IX_Triggers_TaleId_TaleVersionId_State_TriggerAt",
                table: "Triggers",
                columns: new[] { "TaleId", "TaleVersionId", "State", "TriggerAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Commands");

            migrationBuilder.DropTable(
                name: "Triggers");

            migrationBuilder.DropSequence(
                name: "SubIndexSequence",
                schema: "shared");
        }
    }
}
