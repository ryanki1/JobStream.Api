using System.Diagnostics;
using System.Text.Json;
using JobStream.Api.Data;
using JobStream.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JobStream.Api.Services;

/// <summary>
/// ML-powered company verification service with resilience pipeline
/// </summary>
public class MLVerificationService : IMLVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly JobStreamDbContext _context;
    private readonly ILogger<MLVerificationService> _logger;
    private readonly string _mlServiceUrl;

    public MLVerificationService(
        HttpClient httpClient,
        JobStreamDbContext context,
        ILogger<MLVerificationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
        _mlServiceUrl = configuration["MLService:BaseUrl"] ?? "http://localhost:8000";
    }

    public async Task<MLVerificationResult> VerifyCompanyAsync(CompanyRegistration registration)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting ML verification for registration {RegistrationId}", registration.Id);

            // Prepare request for ML service
            var request = new
            {
                registration_id = registration.Id.ToString(),
                company_name = registration.LegalName,
                company_number = registration.RegistrationNumber,
                vat_number = registration.VatId,
                website_url = (string?)null, // TODO: Add WebsiteUrl field to CompanyRegistration model
                linkedin_url = registration.LinkedInUrl,
                business_description = registration.Description
            };

            // Call ML service with resilience pipeline
            var response = await _httpClient.PostAsJsonAsync(
                $"{_mlServiceUrl}/api/v1/verify-company",
                request
            );

            response.EnsureSuccessStatusCode();

            // Parse response
            var mlResponse = await response.Content.ReadFromJsonAsync<MLServiceResponse>();

            if (mlResponse == null)
            {
                throw new InvalidOperationException("ML service returned null response");
            }

            stopwatch.Stop();

            // Map to MLVerificationResult
            var result = new MLVerificationResult
            {
                RegistrationId = registration.Id,
                OverallRiskScore = mlResponse.OverallRiskScore,
                RiskLevel = ParseRiskLevel(mlResponse.RiskLevel),
                Confidence = mlResponse.Confidence,
                WebIntelligenceJson = JsonSerializer.Serialize(mlResponse.WebIntelligence),
                SentimentAnalysisJson = JsonSerializer.Serialize(mlResponse.SentimentAnalysis),
                RiskFlagsJson = JsonSerializer.Serialize(mlResponse.RiskFlags ?? new List<string>()),
                RecommendationsJson = JsonSerializer.Serialize(mlResponse.Recommendations ?? new List<string>()),
                VerifiedAt = DateTime.UtcNow,
                ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds
            };

            // Save to database
            _context.MLVerificationResults.Add(result);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "ML verification completed for registration {RegistrationId}. Risk score: {RiskScore}, Level: {RiskLevel}",
                registration.Id,
                result.OverallRiskScore,
                result.RiskLevel
            );

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling ML service for registration {RegistrationId}", registration.Id);
            throw new InvalidOperationException($"Failed to communicate with ML service: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ML verification for registration {RegistrationId}", registration.Id);
            throw;
        }
    }

    public async Task<List<MLVerificationResult>> GetVerificationHistoryAsync(Guid registrationId)
    {
        return await _context.MLVerificationResults
            .Where(v => v.RegistrationId == registrationId)
            .OrderByDescending(v => v.VerifiedAt)
            .ToListAsync();
    }

    private static RiskLevel ParseRiskLevel(string riskLevel)
    {
        return riskLevel.ToUpperInvariant() switch
        {
            "LOW" => RiskLevel.Low,
            "MEDIUM" => RiskLevel.Medium,
            "HIGH" => RiskLevel.High,
            _ => RiskLevel.Medium // Default to Medium if unknown
        };
    }

    // DTOs for ML service communication
    private class MLServiceResponse
    {
        public decimal OverallRiskScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public WebIntelligenceData? WebIntelligence { get; set; }
        public SentimentAnalysisData? SentimentAnalysis { get; set; }
        public List<string>? RiskFlags { get; set; }
        public List<string>? Recommendations { get; set; }
    }

    private class WebIntelligenceData
    {
        public bool CompaniesHouseMatch { get; set; }
        public bool WebsiteActive { get; set; }
        public bool LinkedInVerified { get; set; }
        public int NewsArticlesFound { get; set; }
    }

    private class SentimentAnalysisData
    {
        public string OverallSentiment { get; set; } = string.Empty;
        public decimal SentimentScore { get; set; }
    }
}
