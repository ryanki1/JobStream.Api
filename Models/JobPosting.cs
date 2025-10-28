using System.ComponentModel.DataAnnotations;

namespace JobStream.Api.Models;

/// <summary>
/// Represents a blockchain-based job posting for MVP/deliverable-based projects
/// </summary>
public class JobPosting
{
    /// <summary>
    /// Unique identifier (database primary key)
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Blockchain posting ID (returned from smart contract)
    /// </summary>
    public long? BlockchainPostingId { get; set; }

    /// <summary>
    /// Company ID from the registration system
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CompanyId { get; set; } = string.Empty;

    /// <summary>
    /// Job posting title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed job description
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Required skills as JSON array
    /// Example: ["C#", ".NET Core", "PostgreSQL", "Angular"]
    /// </summary>
    [Required]
    public string RequiredSkillsJson { get; set; } = "[]";

    /// <summary>
    /// Technology stack description
    /// Example: ".NET Core/SQL Server/Angular"
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string TechnologyStack { get; set; } = string.Empty;

    /// <summary>
    /// Sprint duration in weeks (1-52)
    /// </summary>
    [Range(1, 52)]
    public byte SprintDuration { get; set; }

    /// <summary>
    /// Total project duration in days
    /// </summary>
    [Range(1, 3650)] // Up to 10 years
    public ushort ProjectDuration { get; set; }

    /// <summary>
    /// Payment structure stored as JSON
    /// </summary>
    [Required]
    public string PaymentStructureJson { get; set; } = string.Empty;

    /// <summary>
    /// Acceptance criteria / definition of validated work
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string AcceptanceCriteria { get; set; } = string.Empty;

    /// <summary>
    /// Git repository link (populated after developer selection)
    /// </summary>
    [MaxLength(500)]
    public string? RepositoryLink { get; set; }

    /// <summary>
    /// Jira project ID (populated after developer selection)
    /// </summary>
    [MaxLength(100)]
    public string? JiraProjectId { get; set; }

    /// <summary>
    /// Current status of the posting
    /// </summary>
    [Required]
    public JobPostingStatus Status { get; set; } = JobPostingStatus.Draft;

    /// <summary>
    /// Wallet address of the company that created the posting
    /// </summary>
    [MaxLength(100)]
    public string? CreatedByWalletAddress { get; set; }

    /// <summary>
    /// Blockchain transaction hash for creation
    /// </summary>
    [MaxLength(100)]
    public string? CreationTransactionHash { get; set; }

    /// <summary>
    /// Blockchain transaction hash for publishing
    /// </summary>
    [MaxLength(100)]
    public string? PublishTransactionHash { get; set; }

    /// <summary>
    /// When the posting was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the posting was published (if published)
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
