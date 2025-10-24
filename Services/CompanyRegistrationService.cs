using JobStream.Api.Data;
using JobStream.Api.DTOs;
using JobStream.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace JobStream.Api.Services;

public interface ICompanyRegistrationService
{
    Task<CompanyRegistration> StartRegistrationAsync(StartRegistrationRequest request);
    Task<bool> VerifyEmailAsync(Guid registrationId, string verificationToken);
    Task<CompanyRegistration?> GetRegistrationAsync(Guid registrationId);
    Task<CompanyRegistration> UpdateCompanyDetailsAsync(Guid registrationId, UpdateCompanyDetailsRequest request);
    Task<RegistrationDocument> UploadDocumentAsync(Guid registrationId, Stream fileStream, string fileName, string contentType, string documentType);
    Task<CompanyRegistration> SubmitFinancialVerificationAsync(Guid registrationId, FinancialVerificationRequest request);
    Task<CompanyRegistration> SubmitForReviewAsync(Guid registrationId, SubmitRegistrationRequest request);
    Task<RegistrationStatusResponse> GetRegistrationStatusAsync(Guid registrationId);
    Task<bool> IsBusinessEmail(string email);
    Task<bool> IsEmailAlreadyRegistered(string email);
    Task CleanupExpiredRegistrationsAsync();
}

public class CompanyRegistrationService : ICompanyRegistrationService
{
    private readonly JobStreamDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IStorageService _storageService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<CompanyRegistrationService> _logger;

    // Common free email domains to block
    private static readonly HashSet<string> FreeEmailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "live.com",
        "aol.com", "icloud.com", "mail.com", "protonmail.com", "gmx.com"
    };

    public CompanyRegistrationService(
        JobStreamDbContext context,
        IEmailService emailService,
        IStorageService storageService,
        IEncryptionService encryptionService,
        ILogger<CompanyRegistrationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _storageService = storageService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<CompanyRegistration> StartRegistrationAsync(StartRegistrationRequest request)
    {
        _logger.LogInformation("Starting registration for email: {Email}", request.CompanyEmail);

        // Validate business email
        if (!await IsBusinessEmail(request.CompanyEmail))
        {
            throw new InvalidOperationException("Please use a business email address, not a free email provider");
        }

        // Check if email already registered
        if (await IsEmailAlreadyRegistered(request.CompanyEmail))
        {
            throw new InvalidOperationException("This email address is already registered");
        }

        // Generate verification token
        var verificationToken = GenerateSecureToken();

        // Create registration record
        var registration = new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            CompanyEmail = request.CompanyEmail,
            PrimaryContactName = request.PrimaryContactName,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            Status = RegistrationStatus.Initiated,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.CompanyRegistrations.Add(registration);
        await _context.SaveChangesAsync();

        // Send verification email
        await _emailService.SendEmailVerificationAsync(
            registration.CompanyEmail,
            registration.PrimaryContactName,
            registration.Id,
            verificationToken
        );

        _logger.LogInformation("Registration started successfully. ID: {RegistrationId}", registration.Id);

        return registration;
    }

    public async Task<bool> VerifyEmailAsync(Guid registrationId, string verificationToken)
    {
        _logger.LogInformation("Verifying email for registration: {RegistrationId}", registrationId);

        var registration = await _context.CompanyRegistrations
            .FirstOrDefaultAsync(r => r.Id == registrationId);

        if (registration == null)
        {
            _logger.LogWarning("Registration not found: {RegistrationId}", registrationId);
            throw new InvalidOperationException("Registration not found");
        }

        if (registration.EmailVerified)
        {
            _logger.LogInformation("Email already verified for registration: {RegistrationId}", registrationId);
            return true;
        }

        if (registration.EmailVerificationToken != verificationToken)
        {
            _logger.LogWarning("Invalid verification token for registration: {RegistrationId}", registrationId);
            throw new InvalidOperationException("Invalid verification token");
        }

        if (registration.EmailVerificationTokenExpiry < DateTime.UtcNow)
        {
            _logger.LogWarning("Verification token expired for registration: {RegistrationId}", registrationId);
            throw new InvalidOperationException("Verification token has expired");
        }

        // Mark email as verified
        registration.EmailVerified = true;
        registration.Status = RegistrationStatus.EmailVerified;
        registration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Email verified successfully for registration: {RegistrationId}", registrationId);

        return true;
    }

    public async Task<CompanyRegistration?> GetRegistrationAsync(Guid registrationId)
    {
        return await _context.CompanyRegistrations
            .Include(r => r.Documents)
            .FirstOrDefaultAsync(r => r.Id == registrationId);
    }

    public async Task<CompanyRegistration> UpdateCompanyDetailsAsync(Guid registrationId, UpdateCompanyDetailsRequest request)
    {
        _logger.LogInformation("Updating company details for registration: {RegistrationId}", registrationId);

        var registration = await GetRegistrationAsync(registrationId);

        if (registration == null)
        {
            throw new InvalidOperationException("Registration not found");
        }

        if (!registration.EmailVerified)
        {
            throw new InvalidOperationException("Email must be verified before updating company details");
        }

        // Validate description word count (minimum 200 words)
        var wordCount = request.Description.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < 200)
        {
            throw new InvalidOperationException($"Description must contain at least 200 words (current: {wordCount})");
        }

        // Update company details
        registration.LegalName = request.LegalName;
        registration.RegistrationNumber = request.RegistrationNumber;
        registration.VatId = request.VatId;
        registration.LinkedInUrl = request.LinkedInUrl;
        registration.Address = new Address
        {
            Street = request.Address.Street,
            City = request.Address.City,
            PostalCode = request.Address.PostalCode,
            Country = request.Address.Country
        };
        registration.Industry = request.Industry;
        registration.CompanySize = request.CompanySize;
        registration.Description = request.Description;
        registration.Status = RegistrationStatus.DetailsSubmitted;
        registration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Company details updated successfully for registration: {RegistrationId}", registrationId);

        return registration;
    }

    public async Task<RegistrationDocument> UploadDocumentAsync(
        Guid registrationId,
        Stream fileStream,
        string fileName,
        string contentType,
        string documentType)
    {
        _logger.LogInformation("Uploading document for registration: {RegistrationId}, Type: {DocumentType}", registrationId, documentType);

        var registration = await GetRegistrationAsync(registrationId);

        if (registration == null)
        {
            throw new InvalidOperationException("Registration not found");
        }

        if (registration.Status < RegistrationStatus.DetailsSubmitted)
        {
            throw new InvalidOperationException("Company details must be submitted before uploading documents");
        }

        // Validate file type
        var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/jpg", "image/png" };
        if (!allowedTypes.Contains(contentType.ToLower()))
        {
            throw new InvalidOperationException("Only PDF, JPG, and PNG files are allowed");
        }

        // Validate file size (max 10MB)
        if (fileStream.Length > 10 * 1024 * 1024)
        {
            throw new InvalidOperationException("File size must not exceed 10MB");
        }

        // Upload to storage
        var storagePath = await _storageService.UploadFileAsync(fileStream, fileName, contentType);

        // Generate secure URL with expiry (7 days)
        var secureUrl = $"/api/documents/{Guid.NewGuid()}/download"; // Simplified for now
        var secureUrlExpiry = DateTime.UtcNow.AddDays(7);

        // Create document record
        var document = new RegistrationDocument
        {
            Id = Guid.NewGuid(),
            CompanyRegistrationId = registrationId,
            FileName = fileName,
            FileType = contentType,
            StoragePath = storagePath,
            FileSize = fileStream.Length,
            DocumentType = documentType,
            UploadedAt = DateTime.UtcNow,
            Status = "pending_verification",
            SecureUrl = secureUrl,
            SecureUrlExpiry = secureUrlExpiry
        };

        _context.RegistrationDocuments.Add(document);

        // Update registration status
        registration.Status = RegistrationStatus.DocumentsUploaded;
        registration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Document uploaded successfully. Document ID: {DocumentId}", document.Id);

        return document;
    }

    public async Task<CompanyRegistration> SubmitFinancialVerificationAsync(Guid registrationId, FinancialVerificationRequest request)
    {
        _logger.LogInformation("Submitting financial verification for registration: {RegistrationId}", registrationId);

        var registration = await GetRegistrationAsync(registrationId);

        if (registration == null)
        {
            throw new InvalidOperationException("Registration not found");
        }

        if (registration.Status < RegistrationStatus.DocumentsUploaded)
        {
            throw new InvalidOperationException("Documents must be uploaded before submitting financial verification");
        }

        // Encrypt IBAN
        var encryptedIban = await _encryptionService.EncryptAsync(request.Iban);

        // Update financial information
        registration.BankName = request.BankName;
        registration.EncryptedIban = encryptedIban;
        registration.AccountHolderName = request.AccountHolderName;
        registration.BalanceProofDocumentId = request.BalanceProofDocumentId;
        registration.Status = RegistrationStatus.FinancialSubmitted;
        registration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Financial verification submitted successfully for registration: {RegistrationId}", registrationId);

        return registration;
    }

    public async Task<CompanyRegistration> SubmitForReviewAsync(Guid registrationId, SubmitRegistrationRequest request)
    {
        _logger.LogInformation("Submitting registration for review: {RegistrationId}", registrationId);

        var registration = await GetRegistrationAsync(registrationId);

        if (registration == null)
        {
            throw new InvalidOperationException("Registration not found");
        }

        // Validate all steps completed
        if (!registration.EmailVerified)
        {
            throw new InvalidOperationException("Email must be verified");
        }

        if (registration.Status < RegistrationStatus.FinancialSubmitted)
        {
            throw new InvalidOperationException("All previous steps must be completed before submission");
        }

        if (!request.TermsAccepted)
        {
            throw new InvalidOperationException("Terms and conditions must be accepted");
        }

        // Get review queue position
        var queuePosition = await _context.CompanyRegistrations
            .CountAsync(r => r.Status == RegistrationStatus.PendingReview) + 1;

        // Update registration
        registration.TermsAccepted = request.TermsAccepted;
        registration.TermsAcceptedAt = DateTime.UtcNow;
        registration.StakeAmount = request.StakeAmount;
        registration.WalletAddress = request.WalletAddress;
        registration.Status = RegistrationStatus.PendingReview;
        registration.SubmittedAt = DateTime.UtcNow;
        registration.UpdatedAt = DateTime.UtcNow;
        registration.ReviewQueuePosition = queuePosition;
        registration.SmartContractAddress = $"0x{Guid.NewGuid():N}".Substring(0, 42); // Mock smart contract address

        await _context.SaveChangesAsync();

        // Send confirmation email
        await _emailService.SendRegistrationConfirmationAsync(
            registration.CompanyEmail,
            registration.LegalName ?? registration.PrimaryContactName,
            registration.Id
        );

        _logger.LogInformation("Registration submitted for review successfully. Queue position: {Position}", queuePosition);

        return registration;
    }

    public async Task<RegistrationStatusResponse> GetRegistrationStatusAsync(Guid registrationId)
    {
        var registration = await GetRegistrationAsync(registrationId);

        if (registration == null)
        {
            throw new InvalidOperationException("Registration not found");
        }

        var completedSteps = new List<string>();
        var currentStep = "email";

        if (registration.EmailVerified)
        {
            completedSteps.Add("email");
            currentStep = "company-details";
        }

        if (registration.Status >= RegistrationStatus.DetailsSubmitted)
        {
            completedSteps.Add("company-details");
            currentStep = "documents";
        }

        if (registration.Status >= RegistrationStatus.DocumentsUploaded)
        {
            completedSteps.Add("documents");
            currentStep = "financial-verification";
        }

        if (registration.Status >= RegistrationStatus.FinancialSubmitted)
        {
            completedSteps.Add("financial-verification");
            currentStep = "submit";
        }

        if (registration.Status >= RegistrationStatus.PendingReview)
        {
            completedSteps.Add("submit");
            currentStep = "review";
        }

        return new RegistrationStatusResponse
        {
            RegistrationId = registration.Id,
            CurrentStep = currentStep,
            CompletedSteps = completedSteps,
            Status = registration.Status.ToString(),
            LastUpdated = registration.UpdatedAt ?? registration.CreatedAt,
            ExpiresAt = registration.ExpiresAt ?? DateTime.UtcNow.AddDays(7)
        };
    }

    public Task<bool> IsBusinessEmail(string email)
    {
        var domain = email.Split('@').LastOrDefault()?.ToLower();
        if (string.IsNullOrEmpty(domain))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(!FreeEmailDomains.Contains(domain));
    }

    public async Task<bool> IsEmailAlreadyRegistered(string email)
    {
        return await _context.CompanyRegistrations
            .AnyAsync(r => r.CompanyEmail == email &&
                          (r.Status == RegistrationStatus.Approved ||
                           r.Status == RegistrationStatus.PendingReview));
    }

    public async Task CleanupExpiredRegistrationsAsync()
    {
        var expiredRegistrations = await _context.CompanyRegistrations
            .Where(r => r.ExpiresAt < DateTime.UtcNow &&
                       r.Status != RegistrationStatus.Approved &&
                       r.Status != RegistrationStatus.PendingReview)
            .ToListAsync();

        if (expiredRegistrations.Any())
        {
            _logger.LogInformation("Cleaning up {Count} expired registrations", expiredRegistrations.Count);
            _context.CompanyRegistrations.RemoveRange(expiredRegistrations);
            await _context.SaveChangesAsync();
        }
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
