using JobStream.Api.Models;

namespace JobStream.Api.DTOs;

/// <summary>
/// Response containing job posting details
/// </summary>
public class JobPostingResponse
{
    public Guid Id { get; set; }
    public long? BlockchainPostingId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> RequiredSkills { get; set; } = new();
    public string TechnologyStack { get; set; } = string.Empty;
    public byte SprintDuration { get; set; }
    public ushort ProjectDuration { get; set; }
    public PaymentStructureDto PaymentStructure { get; set; } = new();
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public string? RepositoryLink { get; set; }
    public string? JiraProjectId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CreatedByWalletAddress { get; set; }
    public string? CreationTransactionHash { get; set; }
    public string? PublishTransactionHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response after creating/publishing a posting
/// </summary>
public class JobPostingActionResponse
{
    public Guid PostingId { get; set; }
    public long? BlockchainPostingId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TransactionHash { get; set; }
    public string Message { get; set; } = string.Empty;
}
