using System.ComponentModel.DataAnnotations;

namespace JobStream.Api.DTOs;

/// <summary>
/// Request to update a draft job posting
/// </summary>
public class UpdateJobPostingRequest
{
    /// <summary>
    /// Job posting title
    /// </summary>
    [StringLength(200, MinimumLength = 10)]
    public string? Title { get; set; }

    /// <summary>
    /// Detailed job description
    /// </summary>
    [StringLength(5000, MinimumLength = 50)]
    public string? Description { get; set; }

    /// <summary>
    /// Required skills list
    /// </summary>
    public List<string>? RequiredSkills { get; set; }

    /// <summary>
    /// Technology stack
    /// </summary>
    [StringLength(500)]
    public string? TechnologyStack { get; set; }

    /// <summary>
    /// Sprint duration in weeks (1-52)
    /// </summary>
    [Range(1, 52)]
    public byte? SprintDuration { get; set; }

    /// <summary>
    /// Project duration in days
    /// </summary>
    [Range(1, 3650)]
    public ushort? ProjectDuration { get; set; }

    /// <summary>
    /// Payment structure details
    /// </summary>
    public PaymentStructureDto? PaymentStructure { get; set; }

    /// <summary>
    /// Acceptance criteria
    /// </summary>
    [StringLength(2000, MinimumLength = 20)]
    public string? AcceptanceCriteria { get; set; }
}
