using JobStream.Api.Data;
using JobStream.Api.DTOs;
using JobStream.Api.Models;
using JobStream.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace JobStream.Api.Tests;

public class JobPostingServiceTests
{
    private readonly DbContextOptions<JobStreamDbContext> _dbOptions;
    private readonly Mock<IBlockchainService> _mockBlockchainService;
    private readonly Mock<ILogger<JobPostingService>> _mockLogger;

    public JobPostingServiceTests()
    {
        // Create unique in-memory database for each test run
        _dbOptions = new DbContextOptionsBuilder<JobStreamDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockBlockchainService = new Mock<IBlockchainService>();
        _mockLogger = new Mock<ILogger<JobPostingService>>();
    }

    #region CreateDraftAsync Tests

    [Fact]
    public async Task CreateDraftAsync_WithValidRequest_CreatesJobPosting()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        // Seed a company registration
        var company = new CompanyRegistration
        {
            Id = Guid.NewGuid(),
            CompanyEmail = "test@company.com",
            PrimaryContactName = "John Doe",
            EmailVerified = true,
            WalletAddress = "0x1234567890123456789012345678901234567890",
            Status = RegistrationStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };
        context.CompanyRegistrations.Add(company);
        await context.SaveChangesAsync();

        var companyId = company.Id.ToString();
        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        var request = new CreateJobPostingRequest
        {
            CompanyId = companyId,
            Title = "Senior .NET Developer",
            Description = "We need a senior developer",
            RequiredSkills = new List<string> { "C#", "ASP.NET Core", "SQL" },
            TechnologyStack = "Azure, Docker",
            SprintDuration = 2,
            ProjectDuration = 12,
            PaymentStructure = new PaymentStructureDto
            {
                PaymentPerTicket = 100,
                PaymentPerSprint = 5000,
                PaymentPerMilestone = 20000,
                PartPaymentPercentage = 30
            },
            AcceptanceCriteria = "Must pass all tests",
            WalletAddress = company.WalletAddress
        };

        // Mock blockchain service to return posting ID and transaction hash
        _mockBlockchainService
            .Setup(x => x.CreateDraftPostingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync((1000L, "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"));

        // Act
        var result = await service.CreateDraftAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PostingId != Guid.Empty);
        Assert.Equal(1000L, result.BlockchainPostingId);
        Assert.Equal("Draft", result.Status);
        Assert.NotNull(result.TransactionHash);
        Assert.Contains("created successfully", result.Message);

        // Verify the posting was saved to database
        var savedPosting = await context.JobPostings.FirstOrDefaultAsync(p => p.Id == result.PostingId);
        Assert.NotNull(savedPosting);
        Assert.Equal(companyId, savedPosting.CompanyId);
        Assert.Equal(request.Title, savedPosting.Title);
        Assert.Equal(request.Description, savedPosting.Description);
        Assert.Equal(request.TechnologyStack, savedPosting.TechnologyStack);
        Assert.Equal(request.SprintDuration, savedPosting.SprintDuration);
        Assert.Equal(request.ProjectDuration, savedPosting.ProjectDuration);
        Assert.Equal(JobPostingStatus.Draft, savedPosting.Status);

        // Verify blockchain service was called with correct parameters
        _mockBlockchainService.Verify(
            x => x.CreateDraftPostingAsync(
                companyId,
                request.Title,
                request.Description,
                request.WalletAddress),
            Times.Once);
    }

    [Fact]
    public async Task CreateDraftAsync_WithNonExistentCompany_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);
        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        var request = new CreateJobPostingRequest
        {
            CompanyId = "non-existent-company",
            Title = "Test Job",
            Description = "Test Description",
            RequiredSkills = new List<string> { "C#" },
            WalletAddress = "0x1234567890123456789012345678901234567890"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateDraftAsync(request));

        Assert.Contains("does not exist", exception.Message);

        // Verify blockchain service was never called
        _mockBlockchainService.Verify(
            x => x.CreateDraftPostingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_WithValidDraftPosting_PublishesSuccessfully()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var companyId = "company-123";
        var posting = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Title = "Test Job",
            Description = "Test Description",
            BlockchainPostingId = 1001L,
            Status = JobPostingStatus.Draft,
            RequiredSkillsJson = "[\"C#\"]",
            CreatedAt = DateTime.UtcNow,
            CreatedByWalletAddress = "0x1234567890123456789012345678901234567890"
        };
        context.JobPostings.Add(posting);
        await context.SaveChangesAsync();

        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        // Mock blockchain publish
        _mockBlockchainService
            .Setup(x => x.PublishPostingAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync("0xpublish1234567890abcdef1234567890abcdef1234567890abcdef1234567890");

        // Act
        var result = await service.PublishAsync(posting.Id, companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(posting.Id, result.PostingId);
        Assert.Equal("Published", result.Status);
        Assert.NotNull(result.TransactionHash);
        Assert.Contains("published successfully", result.Message);

        // Verify the posting status was updated in database
        var updatedPosting = await context.JobPostings.FindAsync(posting.Id);
        Assert.NotNull(updatedPosting);
        Assert.Equal(JobPostingStatus.Published, updatedPosting.Status);
        Assert.NotNull(updatedPosting.PublishedAt);

        // Verify blockchain service was called
        _mockBlockchainService.Verify(
            x => x.PublishPostingAsync(posting.BlockchainPostingId!.Value, posting.CreatedByWalletAddress ?? string.Empty),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithNonExistentPosting_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);
        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PublishAsync(nonExistentId, "company-123"));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WithDifferentCompany_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var posting = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = "company-123",
            Title = "Test Job",
            Description = "Test Description",
            BlockchainPostingId = 1001L,
            Status = JobPostingStatus.Draft,
            RequiredSkillsJson = "[\"C#\"]",
            CreatedAt = DateTime.UtcNow,
            CreatedByWalletAddress = "0x1234567890123456789012345678901234567890"
        };
        context.JobPostings.Add(posting);
        await context.SaveChangesAsync();

        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.PublishAsync(posting.Id, "different-company"));

        Assert.Contains("permission", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WithAlreadyPublishedPosting_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var companyId = "company-123";
        var posting = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Title = "Test Job",
            Description = "Test Description",
            BlockchainPostingId = 1001L,
            Status = JobPostingStatus.Published, // Already published
            RequiredSkillsJson = "[\"C#\"]",
            CreatedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow,
            CreatedByWalletAddress = "0x1234567890123456789012345678901234567890"
        };
        context.JobPostings.Add(posting);
        await context.SaveChangesAsync();

        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PublishAsync(posting.Id, companyId));

        Assert.Contains("Only draft postings can be published", exception.Message);
    }

    #endregion

    #region UpdateDraftAsync Tests

    [Fact]
    public async Task UpdateDraftAsync_WithValidRequest_UpdatesPosting()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var companyId = "company-123";
        var posting = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Title = "Original Title",
            Description = "Original Description",
            BlockchainPostingId = 1001L,
            Status = JobPostingStatus.Draft,
            RequiredSkillsJson = "[\"C#\"]",
            SprintDuration = 2,
            CreatedAt = DateTime.UtcNow,
            CreatedByWalletAddress = "0x1234567890123456789012345678901234567890"
        };
        context.JobPostings.Add(posting);
        await context.SaveChangesAsync();

        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        var updateRequest = new UpdateJobPostingRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            SprintDuration = 3
        };

        // Mock blockchain update
        _mockBlockchainService
            .Setup(x => x.UpdatePostingAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync("0xupdate1234567890abcdef1234567890abcdef1234567890abcdef1234567890");

        // Act
        var result = await service.UpdateDraftAsync(posting.Id, updateRequest, companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(posting.Id, result.PostingId);
        Assert.Equal("Draft", result.Status);
        Assert.Contains("updated successfully", result.Message);

        // Verify the posting was updated in database
        var updatedPosting = await context.JobPostings.FindAsync(posting.Id);
        Assert.NotNull(updatedPosting);
        Assert.Equal("Updated Title", updatedPosting.Title);
        Assert.Equal("Updated Description", updatedPosting.Description);
        Assert.Equal(3, updatedPosting.SprintDuration);

        // Verify blockchain service was called
        _mockBlockchainService.Verify(
            x => x.UpdatePostingAsync(posting.BlockchainPostingId!.Value, posting.CreatedByWalletAddress ?? string.Empty),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDraftAsync_WithDifferentCompany_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var posting = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = "company-123",
            Title = "Test Job",
            Description = "Test Description",
            BlockchainPostingId = 1001L,
            Status = JobPostingStatus.Draft,
            RequiredSkillsJson = "[\"C#\"]",
            CreatedAt = DateTime.UtcNow,
            CreatedByWalletAddress = "0x1234567890123456789012345678901234567890"
        };
        context.JobPostings.Add(posting);
        await context.SaveChangesAsync();

        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        var updateRequest = new UpdateJobPostingRequest
        {
            Title = "Updated Title"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.UpdateDraftAsync(posting.Id, updateRequest, "different-company"));

        Assert.Contains("permission", exception.Message);
    }

    [Fact]
    public async Task UpdateDraftAsync_WithPublishedPosting_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var companyId = "company-123";
        var posting = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Title = "Test Job",
            Description = "Test Description",
            BlockchainPostingId = 1001L,
            Status = JobPostingStatus.Published, // Already published
            RequiredSkillsJson = "[\"C#\"]",
            CreatedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow,
            CreatedByWalletAddress = "0x1234567890123456789012345678901234567890"
        };
        context.JobPostings.Add(posting);
        await context.SaveChangesAsync();

        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        var updateRequest = new UpdateJobPostingRequest
        {
            Title = "Updated Title"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateDraftAsync(posting.Id, updateRequest, companyId));

        Assert.Contains("Only draft postings can be updated", exception.Message);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithPublishedPosting_ReturnsPosting()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var posting = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = "company-123",
            Title = "Test Job",
            Description = "Test Description",
            BlockchainPostingId = 1001L,
            Status = JobPostingStatus.Published,
            RequiredSkillsJson = "[\"C#\",\"ASP.NET\"]",
            PaymentStructureJson = "{\"PaymentPerTicket\":100,\"PaymentPerSprint\":5000}",
            CreatedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow,
            CreatedByWalletAddress = "0x1234567890123456789012345678901234567890"
        };
        context.JobPostings.Add(posting);
        await context.SaveChangesAsync();

        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        // Act
        var result = await service.GetByIdAsync(posting.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(posting.Id, result.Id);
        Assert.Equal(posting.Title, result.Title);
        Assert.Equal(posting.Description, result.Description);
        Assert.Equal("Published", result.Status);
        Assert.NotNull(result.RequiredSkills);
        Assert.Equal(2, result.RequiredSkills.Count);
        Assert.Contains("C#", result.RequiredSkills);
        Assert.Contains("ASP.NET", result.RequiredSkills);
    }

    [Fact]
    public async Task GetByIdAsync_WithDraftPostingAndMatchingCompany_ReturnsPosting()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var companyId = "company-123";
        var posting = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Title = "Draft Job",
            Description = "Draft Description",
            BlockchainPostingId = 1001L,
            Status = JobPostingStatus.Draft,
            RequiredSkillsJson = "[\"C#\"]",
            PaymentStructureJson = "{\"PaymentPerTicket\":100,\"PaymentPerSprint\":5000}",
            TechnologyStack = "Test Stack",
            AcceptanceCriteria = "Test criteria",
            CreatedAt = DateTime.UtcNow,
            CreatedByWalletAddress = "0x1234567890123456789012345678901234567890"
        };
        context.JobPostings.Add(posting);
        await context.SaveChangesAsync();

        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        // Act
        var result = await service.GetByIdAsync(posting.Id, companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(posting.Id, result.Id);
        Assert.Equal("Draft", result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WithDraftPostingAndDifferentCompany_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);

        var posting = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = "company-123",
            Title = "Draft Job",
            Description = "Draft Description",
            BlockchainPostingId = 1001L,
            Status = JobPostingStatus.Draft,
            RequiredSkillsJson = "[\"C#\"]",
            CreatedAt = DateTime.UtcNow,
            CreatedByWalletAddress = "0x1234567890123456789012345678901234567890"
        };
        context.JobPostings.Add(posting);
        await context.SaveChangesAsync();

        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetByIdAsync(posting.Id, "different-company"));

        Assert.Contains("permission", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentPosting_ReturnsNull()
    {
        // Arrange
        using var context = new JobStreamDbContext(_dbOptions);
        var service = new JobPostingService(_mockBlockchainService.Object, context, _mockLogger.Object);

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
