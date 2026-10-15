using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metrixvox_API.Infraestructure.Persistense.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiInsights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SurveyResponseRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DetectedCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VerbatimSnippet = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SummaryDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiInsights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveyDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SurveyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IndustryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveyResponseRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubjectId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BranchOrLocationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GlobalCalculatedScore = table.Column<double>(type: "float", nullable: false),
                    Verbatim = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Sentiment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyResponseRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveySections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveySections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveySections_SurveyDefinitions_SurveyDefinitionId",
                        column: x => x.SurveyDefinitionId,
                        principalTable: "SurveyDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetricValueRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyResponseRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetricKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumericValue = table.Column<double>(type: "float", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricValueRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricValueRecords_SurveyResponseRecords_SurveyResponseRecordId",
                        column: x => x.SurveyResponseRecordId,
                        principalTable: "SurveyResponseRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveySectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyQuestions_SurveySections_SurveySectionId",
                        column: x => x.SurveySectionId,
                        principalTable: "SurveySections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiInsights_TenantId_IsResolved_CreatedAt",
                table: "AiInsights",
                columns: new[] { "TenantId", "IsResolved", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricValueRecords_SurveyResponseRecordId",
                table: "MetricValueRecords",
                column: "SurveyResponseRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyDefinitions_TenantId_SurveyCode",
                table: "SurveyDefinitions",
                columns: new[] { "TenantId", "SurveyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestions_SurveySectionId",
                table: "SurveyQuestions",
                column: "SurveySectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponseRecords_TenantId_CreatedAt",
                table: "SurveyResponseRecords",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveySections_SurveyDefinitionId",
                table: "SurveySections",
                column: "SurveyDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiInsights");

            migrationBuilder.DropTable(
                name: "MetricValueRecords");

            migrationBuilder.DropTable(
                name: "SurveyQuestions");

            migrationBuilder.DropTable(
                name: "SurveyResponseRecords");

            migrationBuilder.DropTable(
                name: "SurveySections");

            migrationBuilder.DropTable(
                name: "SurveyDefinitions");
        }
    }
}
