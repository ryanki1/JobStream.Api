using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobStream.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PrimaryContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmailVerificationToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    EmailVerificationTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VatId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddressJson = table.Column<string>(type: "TEXT", nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompanySize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    BankName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EncryptedIban = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccountHolderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BalanceProofDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    WalletAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StakeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    SmartContractAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReviewQueuePosition = table.Column<int>(type: "integer", nullable: true),
                    TermsAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    TermsAcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    EncryptionKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SecureUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SecureUrlExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrationDocuments_CompanyRegistrations_CompanyRegistrat~",
                        column: x => x.CompanyRegistrationId,
                        principalTable: "CompanyRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_CompanyEmail",
                table: "CompanyRegistrations",
                column: "CompanyEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_CreatedAt",
                table: "CompanyRegistrations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_EmailVerificationToken",
                table: "CompanyRegistrations",
                column: "EmailVerificationToken");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_ExpiresAt",
                table: "CompanyRegistrations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_Status",
                table: "CompanyRegistrations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationDocuments_CompanyRegistrationId",
                table: "RegistrationDocuments",
                column: "CompanyRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationDocuments_DocumentType",
                table: "RegistrationDocuments",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationDocuments_UploadedAt",
                table: "RegistrationDocuments",
                column: "UploadedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrationDocuments");

            migrationBuilder.DropTable(
                name: "CompanyRegistrations");
        }
    }
}
