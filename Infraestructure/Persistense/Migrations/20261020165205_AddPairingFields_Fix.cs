using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrixvox_API.Infraestructure.Persistense.Migrations
{
    /// <inheritdoc />
    public partial class AddPairingFields_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                table: "SurveyQuestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Weight",
                table: "SurveyQuestions",
                type: "float",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaired",
                table: "Kiosks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PairingCode",
                table: "Kiosks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PairingCodeExpiresAt",
                table: "Kiosks",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OptionsJson",
                table: "SurveyQuestions");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "SurveyQuestions");

            migrationBuilder.DropColumn(
                name: "IsPaired",
                table: "Kiosks");

            migrationBuilder.DropColumn(
                name: "PairingCode",
                table: "Kiosks");

            migrationBuilder.DropColumn(
                name: "PairingCodeExpiresAt",
                table: "Kiosks");
        }
    }
}
