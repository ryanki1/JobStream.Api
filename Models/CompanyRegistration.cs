using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobStream.Api.Models;

public enum RegistrationStatus
{
    Initiated,
    EmailVerified,
    DetailsSubmitted,
    DocumentsUploaded,
    FinancialSubmitted,
    PendingReview,
    Approved,
    Rejected
}

public class Address
{
    [Required]
    [MaxLength(500)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;
}

public class CompanyRegistration
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Initial Registration Info
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string CompanyEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string PrimaryContactName { get; set; } = string.Empty;

    // Email Verification
    [MaxLength(500)]
    public string? EmailVerificationToken { get; set; }

    public bool EmailVerified { get; set; } = false;

    public DateTime? EmailVerificationTokenExpiry { get; set; }

    // Company Details
    [MaxLength(200)]
    public string? LegalName { get; set; }

    [MaxLength(100)]
    public string? RegistrationNumber { get; set; }

    [MaxLength(50)]
    public string? VatId { get; set; }

    [MaxLength(500)]
    public string? LinkedInUrl { get; set; }

    // Address stored as JSON string in SQLite
    [Column(TypeName = "TEXT")]
    public string? AddressJson { get; set; }

    [NotMapped]
    public Address? Address
    {
        get => string.IsNullOrEmpty(AddressJson)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<Address>(AddressJson);
        set => AddressJson = value == null
            ? null
            : System.Text.Json.JsonSerializer.Serialize(value);
    }

    [MaxLength(100)]
    public string? Industry { get; set; }

    [MaxLength(50)]
    public string? CompanySize { get; set; }

    [MaxLength(5000)]
    public string? Description { get; set; }

    // Financial Verification
    [MaxLength(200)]
    public string? BankName { get; set; }

    [MaxLength(500)]
    public string? EncryptedIban { get; set; }

    [MaxLength(200)]
    public string? AccountHolderName { get; set; }

    public Guid? BalanceProofDocumentId { get; set; }

    // Blockchain
    [MaxLength(100)]
    public string? WalletAddress { get; set; }

    public decimal? StakeAmount { get; set; }

    [MaxLength(100)]
    public string? SmartContractAddress { get; set; }

    // Status & Timestamps
    [Required]
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Initiated;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(1000)]
    public string? ReviewNotes { get; set; }

    [MaxLength(100)]
    public string? ReviewedBy { get; set; }

    public int? ReviewQueuePosition { get; set; }

    // Terms & Conditions
    public bool TermsAccepted { get; set; } = false;

    public DateTime? TermsAcceptedAt { get; set; }

    // Navigation property for documents
    public ICollection<RegistrationDocument> Documents { get; set; } = new List<RegistrationDocument>();
}

public class RegistrationDocument
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyRegistrationId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FileType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [Required]
    [MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? EncryptionKey { get; set; }

    [MaxLength(500)]
    public string? SecureUrl { get; set; }

    public DateTime? SecureUrlExpiry { get; set; }

    public string Status { get; set; } = "pending_verification";

    // Navigation property
    [ForeignKey(nameof(CompanyRegistrationId))]
    public CompanyRegistration? CompanyRegistration { get; set; }
}
