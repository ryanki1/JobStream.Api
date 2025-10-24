using System.ComponentModel.DataAnnotations;
using JobStream.Api.Models;

namespace JobStream.Api.DTOs;

// ==================== Request DTOs ====================

public class StartRegistrationRequest
{
    [Required(ErrorMessage = "Company email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255)]
    public string CompanyEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Primary contact name is required")]
    [MinLength(2, ErrorMessage = "Contact name must be at least 2 characters")]
    [MaxLength(200)]
    public string PrimaryContactName { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    [Required(ErrorMessage = "Registration ID is required")]
    public Guid RegistrationId { get; set; }

    [Required(ErrorMessage = "Verification token is required")]
    [MaxLength(500)]
    public string VerificationToken { get; set; } = string.Empty;
}

public class AddressDto
{
    [Required(ErrorMessage = "Street address is required")]
    [MaxLength(500)]
    public string Street { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Postal code is required")]
    [MaxLength(50)]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country is required")]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;
}

public class UpdateCompanyDetailsRequest
{
    [Required(ErrorMessage = "Legal name is required")]
    [MinLength(2, ErrorMessage = "Legal name must be at least 2 characters")]
    [MaxLength(200)]
    public string LegalName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Registration number is required")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-]{5,50}$", ErrorMessage = "Registration number must be 5-50 alphanumeric characters")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "VAT ID is required")]
    [MaxLength(50)]
    public string VatId { get; set; } = string.Empty;

    [Required(ErrorMessage = "LinkedIn URL is required")]
    [Url(ErrorMessage = "Invalid LinkedIn URL format")]
    [RegularExpression(@"^https:\/\/(www\.)?linkedin\.com\/company\/.*", ErrorMessage = "Must be a valid LinkedIn company URL")]
    [MaxLength(500)]
    public string LinkedInUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    public AddressDto Address { get; set; } = new AddressDto();

    [Required(ErrorMessage = "Industry is required")]
    [MaxLength(100)]
    public string Industry { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company size is required")]
    [MaxLength(50)]
    public string CompanySize { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company description is required")]
    [MinLength(200, ErrorMessage = "Description must be at least 200 characters (approximately 200 words)")]
    [MaxLength(5000)]
    public string Description { get; set; } = string.Empty;
}

public class UploadDocumentRequest
{
    [Required(ErrorMessage = "Document type is required")]
    [MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty;
}

public class FinancialVerificationRequest
{
    [Required(ErrorMessage = "Bank name is required")]
    [MaxLength(200)]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "IBAN is required")]
    [RegularExpression(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}$", ErrorMessage = "Invalid IBAN format")]
    public string Iban { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account holder name is required")]
    [MaxLength(200)]
    public string AccountHolderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Balance proof document ID is required")]
    public Guid BalanceProofDocumentId { get; set; }
}

public class SubmitRegistrationRequest
{
    [Required(ErrorMessage = "Terms acceptance is required")]
    public bool TermsAccepted { get; set; }

    [Required(ErrorMessage = "Stake amount is required")]
    [Range(2500, double.MaxValue, ErrorMessage = "Minimum stake amount is 2500")]
    public decimal StakeAmount { get; set; }

    [Required(ErrorMessage = "Wallet address is required")]
    [RegularExpression(@"^0x[a-fA-F0-9]{40}$", ErrorMessage = "Invalid Ethereum wallet address format")]
    public string WalletAddress { get; set; } = string.Empty;
}

// ==================== Response DTOs ====================

public class StartRegistrationResponse
{
    public Guid RegistrationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class VerifyEmailResponse
{
    public bool Verified { get; set; }
    public string NextStep { get; set; } = string.Empty;
}

public class UpdateCompanyDetailsResponse
{
    public bool Saved { get; set; }
    public List<string> ValidationErrors { get; set; } = new List<string>();
    public string NextStep { get; set; } = string.Empty;
}

public class UploadDocumentResponse
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class FinancialVerificationResponse
{
    public bool Verified { get; set; }
    public string Status { get; set; } = string.Empty;
    public string EstimatedReviewTime { get; set; } = string.Empty;
}

public class RegistrationStatusResponse
{
    public Guid RegistrationId { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public List<string> CompletedSteps { get; set; } = new List<string>();
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class SubmitRegistrationResponse
{
    public bool Submitted { get; set; }
    public int ReviewQueuePosition { get; set; }
    public string EstimatedReviewTime { get; set; } = string.Empty;
    public string? SmartContractAddress { get; set; }
    public string NextSteps { get; set; } = string.Empty;
}

// ==================== Error Response DTO ====================

public class ErrorResponse
{
    public bool Error { get; set; } = true;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ErrorDetails? Details { get; set; }
}

public class ErrorDetails
{
    public string? Field { get; set; }
    public string? Reason { get; set; }
}
