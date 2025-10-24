namespace JobStream.Api.Services;

// Storage Service Interface
public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<Stream> DownloadFileAsync(string filePath);
    Task<bool> DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
}

// Email Service Interface
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
    Task SendEmailVerificationAsync(string to, string companyName, Guid registrationId, string verificationToken);
    Task SendRegistrationConfirmationAsync(string to, string companyName, Guid registrationId);
    Task SendStatusUpdateAsync(string to, string companyName, string status, string? notes = null);
}

// Encryption Service Interface
public interface IEncryptionService
{
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string encryptedText);
    Task<byte[]> EncryptFileAsync(Stream fileStream);
    Task<byte[]> DecryptFileAsync(byte[] encryptedData);
    string GenerateEncryptionKey();
}

// Mock Storage Service Implementation
public class MockStorageService : IStorageService
{
    private readonly ILogger<MockStorageService> _logger;
    private readonly string _storagePath;

    public MockStorageService(ILogger<MockStorageService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _storagePath = configuration.GetValue<string>("Storage:LocalPath") ?? "uploads";

        // Ensure storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(_storagePath, uniqueFileName);

        _logger.LogInformation("MockStorageService: Uploading file {FileName} to {FilePath}", fileName, filePath);

        using (var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }

        _logger.LogInformation("MockStorageService: File uploaded successfully. Size: {Size} bytes", new FileInfo(filePath).Length);

        return filePath;
    }

    public async Task<Stream> DownloadFileAsync(string filePath)
    {
        _logger.LogInformation("MockStorageService: Downloading file from {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("MockStorageService: File not found at {FilePath}", filePath);
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var memoryStream = new MemoryStream();
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            await fileStream.CopyToAsync(memoryStream);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        _logger.LogInformation("MockStorageService: Deleting file at {FilePath}", filePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("MockStorageService: File deleted successfully");
            return Task.FromResult(true);
        }

        _logger.LogWarning("MockStorageService: File not found at {FilePath}", filePath);
        return Task.FromResult(false);
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        var exists = File.Exists(filePath);
        _logger.LogInformation("MockStorageService: File exists check for {FilePath}: {Exists}", filePath, exists);
        return Task.FromResult(exists);
    }
}

// Mock Email Service Implementation
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        _logger.LogInformation("MockEmailService: Sending email");
        _logger.LogInformation("  To: {To}", to);
        _logger.LogInformation("  Subject: {Subject}", subject);
        _logger.LogInformation("  IsHtml: {IsHtml}", isHtml);
        _logger.LogInformation("  Body: {Body}", body);

        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(string to, string companyName, Guid registrationId, string verificationToken)
    {
        var subject = "Verify Your Email - JobStream Registration";
        var verificationUrl = $"http://localhost:4200/register/verify?token={verificationToken}&id={registrationId}";
        var body = $@"
Dear {companyName},

Thank you for starting your registration with JobStream!

Please verify your email address by clicking the link below:
{verificationUrl}

This verification link will expire in 24 hours.

Registration ID: {registrationId}

If you didn't request this registration, please ignore this email.

Best regards,
JobStream Team
";

        _logger.LogInformation("MockEmailService: Sending email verification to {To} for {CompanyName} (ID: {RegistrationId}). Subject: {Subject}",
            to, companyName, registrationId, subject);
        _logger.LogInformation("Verification URL: {Url}", verificationUrl);
        _logger.LogInformation("Email Body:\n{Body}", body);

        return Task.CompletedTask;
    }

    public Task SendRegistrationConfirmationAsync(string to, string companyName, Guid registrationId)
    {
        var subject = "Registration Submitted Successfully - JobStream";
        var body = $@"
Dear {companyName},

Your registration has been successfully submitted for review!

Registration ID: {registrationId}
Company Name: {companyName}
Status: Pending Review

Our team will carefully review your application and get back to you within 24-48 hours.

You will receive an email notification once your registration has been reviewed.

Best regards,
JobStream Team
";

        _logger.LogInformation("MockEmailService: Sending registration confirmation to {To} for {CompanyName} (ID: {RegistrationId}). Subject: {Subject}",
            to, companyName, registrationId, subject);
        _logger.LogInformation("Email Body:\n{Body}", body);

        return Task.CompletedTask;
    }

    public Task SendStatusUpdateAsync(string to, string companyName, string status, string? notes = null)
    {
        var subject = $"Company Registration Status Update - {status}";
        var body = $@"
Dear {companyName},

Your company registration status has been updated to: {status}

{(notes != null ? $"Notes: {notes}\n" : "")}
If you have any questions, please contact our support team.

Best regards,
JobStream Team
";

        _logger.LogInformation("MockEmailService: Sending status update to {To} for {CompanyName}. Status: {Status}",
            to, companyName, status);
        _logger.LogInformation("Email Body:\n{Body}", body);

        return Task.CompletedTask;
    }
}

// Mock Encryption Service Implementation
public class MockEncryptionService : IEncryptionService
{
    private readonly ILogger<MockEncryptionService> _logger;

    public MockEncryptionService(ILogger<MockEncryptionService> logger)
    {
        _logger = logger;
    }

    public Task<string> EncryptAsync(string plainText)
    {
        _logger.LogInformation("MockEncryptionService: Encrypting text (length: {Length})", plainText.Length);

        // Mock encryption - just base64 encode for demo purposes
        var encrypted = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));

        _logger.LogInformation("MockEncryptionService: Text encrypted successfully");
        return Task.FromResult(encrypted);
    }

    public Task<string> DecryptAsync(string encryptedText)
    {
        _logger.LogInformation("MockEncryptionService: Decrypting text");

        // Mock decryption - just base64 decode
        var decrypted = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));

        _logger.LogInformation("MockEncryptionService: Text decrypted successfully");
        return Task.FromResult(decrypted);
    }

    public Task<byte[]> EncryptFileAsync(Stream fileStream)
    {
        _logger.LogInformation("MockEncryptionService: Encrypting file stream");

        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        var data = memoryStream.ToArray();

        _logger.LogInformation("MockEncryptionService: File encrypted (size: {Size} bytes)", data.Length);
        return Task.FromResult(data);
    }

    public Task<byte[]> DecryptFileAsync(byte[] encryptedData)
    {
        _logger.LogInformation("MockEncryptionService: Decrypting file data (size: {Size} bytes)", encryptedData.Length);

        // Mock decryption - return as-is
        return Task.FromResult(encryptedData);
    }

    public string GenerateEncryptionKey()
    {
        var key = Guid.NewGuid().ToString("N");
        _logger.LogInformation("MockEncryptionService: Generated encryption key: {Key}", key);
        return key;
    }
}
