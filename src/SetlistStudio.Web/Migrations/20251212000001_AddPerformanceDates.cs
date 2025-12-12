using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SetlistStudio.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerformanceDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SetlistId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Venue = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceDates_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceDates_Setlists_SetlistId",
                        column: x => x.SetlistId,
                        principalTable: "Setlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceDates_Date",
                table: "PerformanceDates",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceDates_SetlistId_Date",
                table: "PerformanceDates",
                columns: new[] { "SetlistId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceDates_UserId",
                table: "PerformanceDates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceDates_UserId_Date",
                table: "PerformanceDates",
                columns: new[] { "UserId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceDates");
        }
    }
}
