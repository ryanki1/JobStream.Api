using JobStream.Api.Models;

namespace JobStream.Api.Services;

/// <summary>
/// Service for verifying company registrations using ML-powered analysis
/// </summary>
public interface IMLVerificationService
{
    /// <summary>
    /// Verifies a company registration using the ML service
    /// </summary>
    /// <param name="registration">The company registration to verify</param>
    /// <returns>ML verification result with risk assessment</returns>
    Task<MLVerificationResult> VerifyCompanyAsync(CompanyRegistration registration);

    /// <summary>
    /// Gets the verification history for a company registration
    /// </summary>
    /// <param name="registrationId">The registration ID</param>
    /// <returns>List of verification results, newest first</returns>
    Task<List<MLVerificationResult>> GetVerificationHistoryAsync(Guid registrationId);
}
