using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SetlistStudio.Web.Migrations
{
    public partial class AddDurationFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EstimatedDurationTicks",
                table: "Songs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CustomDurationOverrideTicks",
                table: "SetlistSongs",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedDurationTicks",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "CustomDurationOverrideTicks",
                table: "SetlistSongs");
        }
    }
}
