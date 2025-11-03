using JobStream.Api.Data;
using JobStream.Api.DTOs;
using JobStream.Api.Models;
using JobStream.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace JobStream.Api.Tests;

/// <summary>
/// Tests für GetLivePostingsAsync - Schritt für Schritt erklärt
/// </summary>
public class GetLivePostingsTests
{
    // ===== SETUP: Wird für JEDEN Test neu erstellt =====
    private readonly DbContextOptions<JobStreamDbContext> _dbOptions;
    private readonly Mock<IBlockchainService> _mockBlockchainService;
    private readonly Mock<ILogger<JobPostingService>> _mockLogger;

    // CONSTRUCTOR: Läuft VOR JEDEM Test
    public GetLivePostingsTests()
    {
        // In-Memory Database mit UNIQUE Namen (jeder Test bekommt eigene DB)
        _dbOptions = new DbContextOptionsBuilder<JobStreamDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())  // ← Unique!
            .Options;

        // Mock-Objekte erstellen (Fake-Services)
        _mockBlockchainService = new Mock<IBlockchainService>();
        _mockLogger = new Mock<ILogger<JobPostingService>>();
    }

    // ===== TEST 1: Nur Published Postings werden zurückgegeben =====
    [Fact]  // ← Markiert diese Methode als Test
    public async Task GetLivePostingsAsync_ReturnsOnlyPublishedPostings()
    //     ^^^^^                                                        ^
    //     async Task bei async Code                          Namenskonvention
    {
        // ============= ARRANGE =============
        // Vorbereitung: Test-Daten erstellen

        using var context = new JobStreamDbContext(_dbOptions);

        // Erstelle 3 Postings: 2 Published, 1 Draft
        var publishedPosting1 = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = "company-1",
            Title = "Published Job 1",
            Description = "Description 1",
            Status = JobPostingStatus.Published,  // ← PUBLISHED
            RequiredSkillsJson = "[\"C#\"]",
            PaymentStructureJson = "{}",
            TechnologyStack = "Stack",
            AcceptanceCriteria = "Criteria",
            PublishedAt = DateTime.UtcNow.AddDays(-2),  // Vor 2 Tagen
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var publishedPosting2 = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = "company-2",
            Title = "Published Job 2",
            Description = "Description 2",
            Status = JobPostingStatus.Published,  // ← PUBLISHED
            RequiredSkillsJson = "[\"Java\"]",
            PaymentStructureJson = "{}",
            TechnologyStack = "Stack",
            AcceptanceCriteria = "Criteria",
            PublishedAt = DateTime.UtcNow.AddDays(-1),  // Vor 1 Tag (neuer!)
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var draftPosting = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = "company-3",
            Title = "Draft Job",
            Description = "Description 3",
            Status = JobPostingStatus.Draft,  // ← DRAFT (sollte NICHT zurückgegeben werden)
            RequiredSkillsJson = "[\"Python\"]",
            PaymentStructureJson = "{}",
            TechnologyStack = "Stack",
            AcceptanceCriteria = "Criteria",
            CreatedAt = DateTime.UtcNow
        };

        // Zu Datenbank hinzufügen
        context.JobPostings.Add(publishedPosting1);
        context.JobPostings.Add(publishedPosting2);
        context.JobPostings.Add(draftPosting);
        await context.SaveChangesAsync();

        // Service erstellen mit Mocks
        var service = new JobPostingService(
            _mockBlockchainService.Object,  // ← .Object gibt Mock als echtes Objekt
            context,
            _mockLogger.Object
        );

        // ============= ACT =============
        // Ausführung: Methode testen

        var result = await service.GetLivePostingsAsync();

        // ============= ASSERT =============
        // Überprüfung: Sind die Ergebnisse korrekt?

        // 1. Result ist nicht null
        Assert.NotNull(result);

        // 2. Genau 2 Postings (nur Published, nicht Draft)
        Assert.Equal(2, result.Count);

        // 3. Alle sind Published
        Assert.All(result, posting => Assert.Equal("Published", posting.Status));

        // 4. Sortierung: Neueste zuerst (nach PublishedAt descending)
        Assert.Equal("Published Job 2", result[0].Title);  // Neuere (vor 1 Tag)
        Assert.Equal("Published Job 1", result[1].Title);  // Ältere (vor 2 Tagen)

        // 5. Draft Posting ist NICHT in der Liste
        Assert.DoesNotContain(result, p => p.Title == "Draft Job");
    }

    // ===== TEST 2: Leere Liste wenn keine Published Postings =====
    [Fact]
    public async Task GetLivePostingsAsync_WithOnlyDrafts_ReturnsEmptyList()
    {
        // ARRANGE
        using var context = new JobStreamDbContext(_dbOptions);

        // Nur Draft Postings
        var draftPosting = new JobPosting
        {
            Id = Guid.NewGuid(),
            CompanyId = "company-1",
            Title = "Draft Only",
            Description = "Description",
            Status = JobPostingStatus.Draft,
            RequiredSkillsJson = "[]",
            PaymentStructureJson = "{}",
            TechnologyStack = "Stack",
            AcceptanceCriteria = "Criteria",
            CreatedAt = DateTime.UtcNow
        };

        context.JobPostings.Add(draftPosting);
        await context.SaveChangesAsync();

        var service = new JobPostingService(
            _mockBlockchainService.Object,
            context,
            _mockLogger.Object
        );

        // ACT
        var result = await service.GetLivePostingsAsync();

        // ASSERT
        Assert.NotNull(result);
        Assert.Empty(result);  // ← Liste ist leer
    }

    // ===== TEST 3: Leere Liste wenn DB leer =====
    [Fact]
    public async Task GetLivePostingsAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        // ARRANGE
        using var context = new JobStreamDbContext(_dbOptions);
        // ← Keine Postings hinzugefügt!

        var service = new JobPostingService(
            _mockBlockchainService.Object,
            context,
            _mockLogger.Object
        );

        // ACT
        var result = await service.GetLivePostingsAsync();

        // ASSERT
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
