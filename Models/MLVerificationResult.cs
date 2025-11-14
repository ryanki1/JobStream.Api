using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobStream.Api.Models;

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public class MLVerificationResult
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign key to CompanyRegistration
    [Required]
    public Guid RegistrationId { get; set; }

    [ForeignKey(nameof(RegistrationId))]
    public CompanyRegistration? Registration { get; set; }

    // Risk Assessment Results
    [Required]
    public decimal OverallRiskScore { get; set; } // 0-100 scale

    [Required]
    public RiskLevel RiskLevel { get; set; }

    [Required]
    [Range(0, 1)]
    public decimal Confidence { get; set; } // 0-1 scale

    // Web Intelligence (stored as JSON)
    [Column(TypeName = "TEXT")]
    public string? WebIntelligenceJson { get; set; }

    // Sentiment Analysis (stored as JSON)
    [Column(TypeName = "TEXT")]
    public string? SentimentAnalysisJson { get; set; }

    // Risk Flags (stored as JSON array)
    [Column(TypeName = "TEXT")]
    public string? RiskFlagsJson { get; set; }

    // Recommendations (stored as JSON array)
    [Column(TypeName = "TEXT")]
    public string? RecommendationsJson { get; set; }

    // Metadata
    [Required]
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int ProcessingTimeMs { get; set; }

    // Computed property for convenient access to risk flags
    [NotMapped]
    public List<string>? RiskFlags
    {
        get => string.IsNullOrEmpty(RiskFlagsJson)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(RiskFlagsJson);
        set => RiskFlagsJson = value == null
            ? null
            : System.Text.Json.JsonSerializer.Serialize(value);
    }

    // Computed property for convenient access to recommendations
    [NotMapped]
    public List<string>? Recommendations
    {
        get => string.IsNullOrEmpty(RecommendationsJson)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(RecommendationsJson);
        set => RecommendationsJson = value == null
            ? null
            : System.Text.Json.JsonSerializer.Serialize(value);
    }
}
