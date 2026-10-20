using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrixvox_API.Infraestructure.Persistense.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionWeightAndOptions : Migration
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
                defaultValue: 0.0);
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
        }
    }
}
