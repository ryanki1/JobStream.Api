using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobStream.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPostings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobPostings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockchainPostingId = table.Column<long>(type: "bigint", nullable: true),
                    CompanyId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    RequiredSkillsJson = table.Column<string>(type: "TEXT", nullable: false),
                    TechnologyStack = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SprintDuration = table.Column<byte>(type: "smallint", nullable: false),
                    ProjectDuration = table.Column<int>(type: "integer", nullable: false),
                    PaymentStructureJson = table.Column<string>(type: "TEXT", nullable: false),
                    AcceptanceCriteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RepositoryLink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    JiraProjectId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedByWalletAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreationTransactionHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PublishTransactionHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPostings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_BlockchainPostingId",
                table: "JobPostings",
                column: "BlockchainPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_CompanyId",
                table: "JobPostings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_CreatedAt",
                table: "JobPostings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_PublishedAt",
                table: "JobPostings",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_Status",
                table: "JobPostings",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobPostings");
        }
    }
}
