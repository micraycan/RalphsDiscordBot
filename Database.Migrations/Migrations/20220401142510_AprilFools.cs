using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations.Migrations
{
    public partial class AprilFools : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CockFightWinStreak",
                table: "Users",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStimulus",
                table: "Users",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CockFightWinStreak",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastStimulus",
                table: "Users");
        }
    }
}
