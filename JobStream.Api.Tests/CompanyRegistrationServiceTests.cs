using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using JobStream.Api.Data;
using JobStream.Api.Services;
using JobStream.Api.Models;
using JobStream.Api.DTOs;

namespace JobStream.Api.Tests;

public class CompanyRegistrationServiceTests
{
    private readonly Mock<ILogger<CompanyRegistrationService>> _loggerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly DbContextOptions<JobStreamDbContext> _dbOptions;

    public CompanyRegistrationServiceTests()
    {
        _loggerMock = new Mock<ILogger<CompanyRegistrationService>>();
        _emailServiceMock = new Mock<IEmailService>();
        _storageServiceMock = new Mock<IStorageService>();
        _encryptionServiceMock = new Mock<IEncryptionService>();

        // Setup in-memory database
        _dbOptions = new DbContextOptionsBuilder<JobStreamDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task StartRegistrationAsync_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);
        var service = new CompanyRegistrationService(
            context,
            _emailServiceMock.Object,
            _storageServiceMock.Object,
            _encryptionServiceMock.Object,
            _loggerMock.Object
        );

        var request = new StartRegistrationRequest
        {
            CompanyEmail = "test@acme-corp.com",
            PrimaryContactName = "John Doe"
        };

        _emailServiceMock
            .Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.StartRegistrationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(RegistrationStatus.Initiated, result.Status);
        Assert.NotNull(result.ExpiresAt);

        // Verify email was sent
        _emailServiceMock.Verify(
            x => x.SendEmailVerificationAsync(request.CompanyEmail, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Once
        );

        // Verify registration was saved to database
        var savedRegistration = await context.CompanyRegistrations
            .FirstOrDefaultAsync(r => r.CompanyEmail == request.CompanyEmail);
        Assert.NotNull(savedRegistration);
        Assert.Equal(request.PrimaryContactName, savedRegistration.PrimaryContactName);
    }

    [Fact]
    public async Task StartRegistrationAsync_FreeEmailProvider_ThrowsException()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);
        var service = new CompanyRegistrationService(
            context,
            _emailServiceMock.Object,
            _storageServiceMock.Object,
            _encryptionServiceMock.Object,
            _loggerMock.Object
        );

        var request = new StartRegistrationRequest
        {
            CompanyEmail = "test@gmail.com",
            PrimaryContactName = "John Doe"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartRegistrationAsync(request)
        );

        Assert.Contains("free email provider", exception.Message);
    }

    [Fact]
    public async Task StartRegistrationAsync_DuplicateEmail_ThrowsException()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        // Add existing registration with PendingReview status (which should block new registrations)
        var existingRegistration = new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            CompanyEmail = "test@acme-corp.com",
            PrimaryContactName = "Jane Doe",
            EmailVerificationToken = "token123",
            Status = RegistrationStatus.PendingReview,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        context.CompanyRegistrations.Add(existingRegistration);
        await context.SaveChangesAsync();

        var service = new CompanyRegistrationService(
            context,
            _emailServiceMock.Object,
            _storageServiceMock.Object,
            _encryptionServiceMock.Object,
            _loggerMock.Object
        );

        var request = new StartRegistrationRequest
        {
            CompanyEmail = "test@acme-corp.com",
            PrimaryContactName = "John Doe"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartRegistrationAsync(request)
        );
    }

    [Fact]
    public async Task VerifyEmailAsync_ValidToken_ReturnsSuccess()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var token = "valid-token-123";
        var registration = new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            CompanyEmail = "test@acme-corp.com",
            PrimaryContactName = "John Doe",
            EmailVerificationToken = token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            Status = RegistrationStatus.Initiated,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        context.CompanyRegistrations.Add(registration);
        await context.SaveChangesAsync();

        var service = new CompanyRegistrationService(
            context,
            _emailServiceMock.Object,
            _storageServiceMock.Object,
            _encryptionServiceMock.Object,
            _loggerMock.Object
        );

        // Act
        var result = await service.VerifyEmailAsync(registration.Id, token);

        // Assert
        Assert.True(result);

        // Verify database was updated
        var updatedRegistration = await context.CompanyRegistrations.FindAsync(registration.Id);
        Assert.NotNull(updatedRegistration);
        Assert.True(updatedRegistration.EmailVerified);
    }

    [Fact]
    public async Task VerifyEmailAsync_ExpiredToken_ThrowsException()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var token = "expired-token";
        var registration = new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            CompanyEmail = "test@acme-corp.com",
            PrimaryContactName = "John Doe",
            EmailVerificationToken = token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
            Status = RegistrationStatus.Initiated,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        context.CompanyRegistrations.Add(registration);
        await context.SaveChangesAsync();

        var service = new CompanyRegistrationService(
            context,
            _emailServiceMock.Object,
            _storageServiceMock.Object,
            _encryptionServiceMock.Object,
            _loggerMock.Object
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.VerifyEmailAsync(registration.Id, token)
        );

        Assert.Contains("expired", exception.Message.ToLower());
    }

    [Fact]
    public async Task VerifyEmailAsync_InvalidToken_ThrowsException()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var correctToken = "correct-token";
        var registration = new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            CompanyEmail = "test@acme-corp.com",
            PrimaryContactName = "John Doe",
            EmailVerificationToken = correctToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            Status = RegistrationStatus.Initiated,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        context.CompanyRegistrations.Add(registration);
        await context.SaveChangesAsync();

        var service = new CompanyRegistrationService(
            context,
            _emailServiceMock.Object,
            _storageServiceMock.Object,
            _encryptionServiceMock.Object,
            _loggerMock.Object
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.VerifyEmailAsync(registration.Id, "wrong-token")
        );

        Assert.Contains("invalid", exception.Message.ToLower());
    }
}
