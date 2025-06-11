using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GateTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    numberOfOpenCurrectly = table.Column<int>(type: "INTEGER", nullable: false),
                    numberOfOpenIllegel = table.Column<int>(type: "INTEGER", nullable: false),
                    ReachUpperLimitSwitch = table.Column<int>(type: "INTEGER", nullable: false),
                    ReachLowerLimitSwitch = table.Column<int>(type: "INTEGER", nullable: false),
                    LoopDetector = table.Column<int>(type: "INTEGER", nullable: false),
                    isSent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateTransactions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GateTransactions");
        }
    }
}
