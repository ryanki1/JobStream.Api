using System.ComponentModel.DataAnnotations;

namespace JobStream.Api.DTOs;

/// <summary>
/// Request to create a draft job posting
/// </summary>
public class CreateJobPostingRequest
{
    /// <summary>
    /// Company ID from registration system
    /// </summary>
    [Required]
    public string CompanyId { get; set; } = string.Empty;

    /// <summary>
    /// Job posting title
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed job description
    /// </summary>
    [Required]
    [StringLength(5000, MinimumLength = 50)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Required skills list
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> RequiredSkills { get; set; } = new();

    /// <summary>
    /// Technology stack (e.g., ".NET Core/SQL Server/Angular")
    /// </summary>
    [Required]
    [StringLength(500)]
    public string TechnologyStack { get; set; } = string.Empty;

    /// <summary>
    /// Sprint duration in weeks (1-52)
    /// </summary>
    [Range(1, 52)]
    public byte SprintDuration { get; set; }

    /// <summary>
    /// Project duration in days
    /// </summary>
    [Range(1, 3650)]
    public ushort ProjectDuration { get; set; }

    /// <summary>
    /// Payment structure details
    /// </summary>
    [Required]
    public PaymentStructureDto PaymentStructure { get; set; } = new();

    /// <summary>
    /// Acceptance criteria / definition of done
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 20)]
    public string AcceptanceCriteria { get; set; } = string.Empty;

    /// <summary>
    /// Wallet address of creator
    /// </summary>
    public string? WalletAddress { get; set; }
}

/// <summary>
/// Payment structure DTO
/// </summary>
public class PaymentStructureDto
{
    /// <summary>
    /// Payment per completed ticket
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal PaymentPerTicket { get; set; }

    /// <summary>
    /// Payment per completed sprint
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal PaymentPerSprint { get; set; }

    /// <summary>
    /// Payment per completed milestone
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal PaymentPerMilestone { get; set; }

    /// <summary>
    /// Percentage paid during sprint (0-100)
    /// </summary>
    [Range(0, 100)]
    public byte PartPaymentPercentage { get; set; }
}
