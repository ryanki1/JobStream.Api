using System.Text.Json;
using JobStream.Api.Data;
using JobStream.Api.DTOs;
using JobStream.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JobStream.Api.Services;

/// <summary>
/// Service for managing job postings with blockchain integration
/// </summary>
public class JobPostingService : IJobPostingService
{
    private readonly IBlockchainService _blockchainService;
    private readonly JobStreamDbContext _dbContext;
    private readonly ILogger<JobPostingService> _logger;

    public JobPostingService(
        IBlockchainService blockchainService,
        JobStreamDbContext dbContext,
        ILogger<JobPostingService> logger)
    {
        _blockchainService = blockchainService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Creates a draft job posting
    /// </summary>
    public async Task<JobPostingActionResponse> CreateDraftAsync(CreateJobPostingRequest request)
    {
        _logger.LogInformation("Creating draft job posting for company {CompanyId}", request.CompanyId);

        try
        {
            // Validate company exists and get wallet address
            var company = await _dbContext.CompanyRegistrations
                .FirstOrDefaultAsync(c => c.Id.ToString() == request.CompanyId);

            if (company == null)
            {
                _logger.LogWarning("Company {CompanyId} not found", request.CompanyId);
                throw new InvalidOperationException($"Company with ID '{request.CompanyId}' does not exist");
            }

            // Use wallet address from request if provided, otherwise from company registration
            var walletAddress = request.WalletAddress ?? company.WalletAddress ?? string.Empty;

            // Serialize complex fields to JSON
            var requiredSkillsJson = JsonSerializer.Serialize(request.RequiredSkills);
            var paymentStructureJson = JsonSerializer.Serialize(request.PaymentStructure);

            // Create blockchain draft posting
            var (blockchainPostingId, transactionHash) = await _blockchainService.CreateDraftPostingAsync(
                request.CompanyId,
                request.Title,
                request.Description,
                walletAddress);

            _logger.LogInformation(
                "Blockchain draft created with ID {BlockchainPostingId} and transaction hash {TransactionHash}",
                blockchainPostingId,
                transactionHash);

            // Create database entity
            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                BlockchainPostingId = blockchainPostingId,
                CompanyId = request.CompanyId,
                Title = request.Title,
                Description = request.Description,
                RequiredSkillsJson = requiredSkillsJson,
                TechnologyStack = request.TechnologyStack,
                SprintDuration = request.SprintDuration,
                ProjectDuration = request.ProjectDuration,
                PaymentStructureJson = paymentStructureJson,
                AcceptanceCriteria = request.AcceptanceCriteria,
                Status = JobPostingStatus.Draft,
                CreatedByWalletAddress = walletAddress,
                CreationTransactionHash = transactionHash,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.JobPostings.Add(jobPosting);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Draft job posting created successfully with ID {PostingId}",
                jobPosting.Id);

            return new JobPostingActionResponse
            {
                PostingId = jobPosting.Id,
                BlockchainPostingId = blockchainPostingId,
                Status = jobPosting.Status.ToString(),
                TransactionHash = transactionHash,
                Message = "Draft job posting created successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating draft job posting for company {CompanyId}", request.CompanyId);
            throw;
        }
    }

    /// <summary>
    /// Publishes a draft job posting
    /// </summary>
    public async Task<JobPostingActionResponse> PublishAsync(Guid postingId, string companyId)
    {
        _logger.LogInformation("Publishing job posting {PostingId} for company {CompanyId}", postingId, companyId);

        try
        {
            // Load posting and verify ownership
            var posting = await _dbContext.JobPostings
                .FirstOrDefaultAsync(p => p.Id == postingId);

            if (posting == null)
            {
                _logger.LogWarning("Job posting {PostingId} not found", postingId);
                throw new InvalidOperationException($"Job posting with ID '{postingId}' not found");
            }

            if (posting.CompanyId != companyId)
            {
                _logger.LogWarning(
                    "Company {CompanyId} attempted to publish posting {PostingId} owned by {OwnerCompanyId}",
                    companyId,
                    postingId,
                    posting.CompanyId);
                throw new UnauthorizedAccessException("You do not have permission to publish this posting");
            }

            // Verify status is Draft
            if (posting.Status != JobPostingStatus.Draft)
            {
                _logger.LogWarning(
                    "Attempted to publish posting {PostingId} with status {Status}",
                    postingId,
                    posting.Status);
                throw new InvalidOperationException($"Only draft postings can be published. Current status: {posting.Status}");
            }

            if (!posting.BlockchainPostingId.HasValue)
            {
                _logger.LogError("Job posting {PostingId} has no blockchain ID", postingId);
                throw new InvalidOperationException("Job posting has no blockchain ID");
            }

            // Call blockchain to publish
            var transactionHash = await _blockchainService.PublishPostingAsync(
                posting.BlockchainPostingId.Value,
                posting.CreatedByWalletAddress ?? string.Empty);

            _logger.LogInformation(
                "Blockchain posting {BlockchainPostingId} published with transaction hash {TransactionHash}",
                posting.BlockchainPostingId,
                transactionHash);

            // Update database
            posting.Status = JobPostingStatus.Published;
            posting.PublishedAt = DateTime.UtcNow;
            posting.PublishTransactionHash = transactionHash;
            posting.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Job posting {PostingId} published successfully", postingId);

            return new JobPostingActionResponse
            {
                PostingId = posting.Id,
                BlockchainPostingId = posting.BlockchainPostingId,
                Status = posting.Status.ToString(),
                TransactionHash = transactionHash,
                Message = "Job posting published successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing job posting {PostingId}", postingId);
            throw;
        }
    }

    /// <summary>
    /// Updates a draft job posting
    /// </summary>
    public async Task<JobPostingActionResponse> UpdateDraftAsync(
        Guid postingId,
        UpdateJobPostingRequest request,
        string companyId)
    {
        _logger.LogInformation("Updating draft job posting {PostingId} for company {CompanyId}", postingId, companyId);

        try
        {
            // Load posting and verify ownership
            var posting = await _dbContext.JobPostings
                .FirstOrDefaultAsync(p => p.Id == postingId);

            if (posting == null)
            {
                _logger.LogWarning("Job posting {PostingId} not found", postingId);
                throw new InvalidOperationException($"Job posting with ID '{postingId}' not found");
            }

            if (posting.CompanyId != companyId)
            {
                _logger.LogWarning(
                    "Company {CompanyId} attempted to update posting {PostingId} owned by {OwnerCompanyId}",
                    companyId,
                    postingId,
                    posting.CompanyId);
                throw new UnauthorizedAccessException("You do not have permission to update this posting");
            }

            // Verify status is Draft
            if (posting.Status != JobPostingStatus.Draft)
            {
                _logger.LogWarning(
                    "Attempted to update posting {PostingId} with status {Status}",
                    postingId,
                    posting.Status);
                throw new InvalidOperationException($"Only draft postings can be updated. Current status: {posting.Status}");
            }

            // Update only non-null fields
            if (request.Title != null)
            {
                posting.Title = request.Title;
            }

            if (request.Description != null)
            {
                posting.Description = request.Description;
            }

            if (request.RequiredSkills != null)
            {
                posting.RequiredSkillsJson = JsonSerializer.Serialize(request.RequiredSkills);
            }

            if (request.TechnologyStack != null)
            {
                posting.TechnologyStack = request.TechnologyStack;
            }

            if (request.SprintDuration.HasValue)
            {
                posting.SprintDuration = request.SprintDuration.Value;
            }

            if (request.ProjectDuration.HasValue)
            {
                posting.ProjectDuration = request.ProjectDuration.Value;
            }

            if (request.PaymentStructure != null)
            {
                posting.PaymentStructureJson = JsonSerializer.Serialize(request.PaymentStructure);
            }

            if (request.AcceptanceCriteria != null)
            {
                posting.AcceptanceCriteria = request.AcceptanceCriteria;
            }

            if (!posting.BlockchainPostingId.HasValue)
            {
                _logger.LogError("Job posting {PostingId} has no blockchain ID", postingId);
                throw new InvalidOperationException("Job posting has no blockchain ID");
            }

            // Call blockchain to update
            var transactionHash = await _blockchainService.UpdatePostingAsync(
                posting.BlockchainPostingId.Value,
                posting.CreatedByWalletAddress ?? string.Empty);

            _logger.LogInformation(
                "Blockchain posting {BlockchainPostingId} updated with transaction hash {TransactionHash}",
                posting.BlockchainPostingId,
                transactionHash);

            // Update timestamp
            posting.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Job posting {PostingId} updated successfully", postingId);

            return new JobPostingActionResponse
            {
                PostingId = posting.Id,
                BlockchainPostingId = posting.BlockchainPostingId,
                Status = posting.Status.ToString(),
                TransactionHash = transactionHash,
                Message = "Draft job posting updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job posting {PostingId}", postingId);
            throw;
        }
    }

    /// <summary>
    /// Gets a job posting by ID
    /// </summary>
    public async Task<JobPostingResponse?> GetByIdAsync(Guid postingId, string? companyId = null)
    {
        _logger.LogInformation("Getting job posting {PostingId}", postingId);

        try
        {
            var posting = await _dbContext.JobPostings
                .FirstOrDefaultAsync(p => p.Id == postingId);

            if (posting == null)
            {
                _logger.LogWarning("Job posting {PostingId} not found", postingId);
                return null;
            }

            // If draft, verify company access (unless companyId is null for admin access)
            if (posting.Status == JobPostingStatus.Draft && companyId != null)
            {
                if (posting.CompanyId != companyId)
                {
                    _logger.LogWarning(
                        "Company {CompanyId} attempted to access draft posting {PostingId} owned by {OwnerCompanyId}",
                        companyId,
                        postingId,
                        posting.CompanyId);
                    throw new UnauthorizedAccessException("You do not have permission to view this draft posting");
                }
            }

            return MapToResponse(posting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job posting {PostingId}", postingId);
            throw;
        }
    }

    /// <summary>
    /// Gets all published job postings
    /// </summary>
    public async Task<List<JobPostingResponse>> GetLivePostingsAsync()
    {
        _logger.LogInformation("Getting all live job postings");

        try
        {
            var postings = await _dbContext.JobPostings
                .Where(p => p.Status == JobPostingStatus.Published)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();

            _logger.LogInformation("Found {Count} live job postings", postings.Count);

            return postings.Select(MapToResponse).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting live job postings");
            throw;
        }
    }

    /// <summary>
    /// Gets all job postings for a company
    /// </summary>
    public async Task<List<JobPostingResponse>> GetByCompanyIdAsync(string companyId)
    {
        _logger.LogInformation("Getting job postings for company {CompanyId}", companyId);

        try
        {
            var postings = await _dbContext.JobPostings
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Found {Count} job postings for company {CompanyId}", postings.Count, companyId);

            return postings.Select(MapToResponse).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job postings for company {CompanyId}", companyId);
            throw;
        }
    }

    /// <summary>
    /// Gets all draft job postings for a company
    /// </summary>
    public async Task<List<JobPostingResponse>> GetDraftsByCompanyIdAsync(string companyId)
    {
        _logger.LogInformation("Getting draft job postings for company {CompanyId}", companyId);

        try
        {
            var postings = await _dbContext.JobPostings
                .Where(p => p.CompanyId == companyId && p.Status == JobPostingStatus.Draft)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Found {Count} draft job postings for company {CompanyId}", postings.Count, companyId);

            return postings.Select(MapToResponse).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting draft job postings for company {CompanyId}", companyId);
            throw;
        }
    }

    /// <summary>
    /// Maps a JobPosting entity to a JobPostingResponse DTO
    /// </summary>
    private JobPostingResponse MapToResponse(JobPosting posting)
    {
        try
        {
            // Deserialize JSON fields
            var requiredSkills = JsonSerializer.Deserialize<List<string>>(posting.RequiredSkillsJson) ?? new List<string>();
            var paymentStructure = JsonSerializer.Deserialize<PaymentStructureDto>(posting.PaymentStructureJson) ?? new PaymentStructureDto();

            return new JobPostingResponse
            {
                Id = posting.Id,
                BlockchainPostingId = posting.BlockchainPostingId,
                CompanyId = posting.CompanyId,
                Title = posting.Title,
                Description = posting.Description,
                RequiredSkills = requiredSkills,
                TechnologyStack = posting.TechnologyStack,
                SprintDuration = posting.SprintDuration,
                ProjectDuration = posting.ProjectDuration,
                PaymentStructure = paymentStructure,
                AcceptanceCriteria = posting.AcceptanceCriteria,
                RepositoryLink = posting.RepositoryLink,
                JiraProjectId = posting.JiraProjectId,
                Status = posting.Status.ToString(),
                CreatedByWalletAddress = posting.CreatedByWalletAddress,
                CreationTransactionHash = posting.CreationTransactionHash,
                PublishTransactionHash = posting.PublishTransactionHash,
                CreatedAt = posting.CreatedAt,
                PublishedAt = posting.PublishedAt,
                UpdatedAt = posting.UpdatedAt
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing JSON fields for posting {PostingId}", posting.Id);
            throw new InvalidOperationException($"Error deserializing job posting data: {ex.Message}", ex);
        }
    }
}
