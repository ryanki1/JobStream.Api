using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobStream.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMLVerificationResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MLVerificationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallRiskScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    WebIntelligenceJson = table.Column<string>(type: "TEXT", nullable: true),
                    SentimentAnalysisJson = table.Column<string>(type: "TEXT", nullable: true),
                    RiskFlagsJson = table.Column<string>(type: "TEXT", nullable: true),
                    RecommendationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ProcessingTimeMs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MLVerificationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MLVerificationResults_CompanyRegistrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "CompanyRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MLVerificationResults_RegistrationId",
                table: "MLVerificationResults",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_MLVerificationResults_RiskLevel",
                table: "MLVerificationResults",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_MLVerificationResults_VerifiedAt",
                table: "MLVerificationResults",
                column: "VerifiedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MLVerificationResults");
        }
    }
}
