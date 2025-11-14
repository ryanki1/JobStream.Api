using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobStream.Api.Data;
using JobStream.Api.Models;
using JobStream.Api.Services;

namespace JobStream.Api.Controllers;

/// <summary>
/// Admin endpoints for managing company registrations
/// </summary>
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly JobStreamDbContext _context;
    private readonly IMLVerificationService _mlVerificationService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        JobStreamDbContext context,
        IMLVerificationService mlVerificationService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _mlVerificationService = mlVerificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get pending company registrations for admin review
    /// </summary>
    [HttpGet("registrations/pending")]
    [ProducesResponseType(typeof(List<CompanyRegistration>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingRegistrations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.CompanyRegistrations
            .Include(r => r.Documents)
            .Where(r => r.Status == RegistrationStatus.PendingReview)
            .OrderBy(r => r.CreatedAt);

        var total = await query.CountAsync();
        var registrations = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            data = registrations,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling((double)total / pageSize)
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get full registration details by ID
    /// </summary>
    [HttpGet("registrations/{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRegistrationById(Guid id)
    {
        var registration = await _context.CompanyRegistrations
            .Include(r => r.Documents)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
        {
            return NotFound(new { error = "Registration not found" });
        }

        // Get ML verification results
        var mlResults = await _context.MLVerificationResults
            .Where(v => v.RegistrationId == id)
            .OrderByDescending(v => v.VerifiedAt)
            .ToListAsync();

        var response = new
        {
            registration,
            mlVerifications = mlResults
        };

        return Ok(response);
    }

    /// <summary>
    /// Trigger ML verification for a company registration
    /// </summary>
    [HttpPost("registrations/{id}/verify-ml")]
    [ProducesResponseType(typeof(MLVerificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> VerifyWithML(Guid id)
    {
        var registration = await _context.CompanyRegistrations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
        {
            return NotFound(new { error = "Registration not found" });
        }

        if (registration.Status != RegistrationStatus.PendingReview)
        {
            return BadRequest(new
            {
                error = "Only pending registrations can be verified",
                currentStatus = registration.Status.ToString()
            });
        }

        try
        {
            _logger.LogInformation("Admin triggered ML verification for registration {RegistrationId}", id);

            var result = await _mlVerificationService.VerifyCompanyAsync(registration);

            _logger.LogInformation(
                "ML verification completed for registration {RegistrationId}. Risk: {RiskLevel} ({RiskScore})",
                id,
                result.RiskLevel,
                result.OverallRiskScore
            );

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("circuit is now open"))
        {
            _logger.LogWarning(ex, "Circuit breaker open for registration {RegistrationId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "ML verification service temporarily unavailable",
                message = "The service is currently experiencing issues. Please try again in 30 seconds.",
                suggestion = "The circuit breaker will reset automatically"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "ML service error for registration {RegistrationId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "ML verification service unavailable",
                message = ex.Message,
                suggestion = "Please try again later or verify manually"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during ML verification for registration {RegistrationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "An error occurred during ML verification",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get ML verification history for a registration
    /// </summary>
    [HttpGet("registrations/{id}/ml-history")]
    [ProducesResponseType(typeof(List<MLVerificationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMLVerificationHistory(Guid id)
    {
        var registration = await _context.CompanyRegistrations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
        {
            return NotFound(new { error = "Registration not found" });
        }

        var history = await _mlVerificationService.GetVerificationHistoryAsync(id);

        return Ok(new
        {
            registrationId = id,
            verificationCount = history.Count,
            verifications = history
        });
    }

    /// <summary>
    /// Get admin dashboard statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        var totalRegistrations = await _context.CompanyRegistrations.CountAsync();
        var pendingCount = await _context.CompanyRegistrations
            .CountAsync(r => r.Status == RegistrationStatus.PendingReview);
        var approvedCount = await _context.CompanyRegistrations
            .CountAsync(r => r.Status == RegistrationStatus.Approved);
        var rejectedCount = await _context.CompanyRegistrations
            .CountAsync(r => r.Status == RegistrationStatus.Rejected);
        var emailVerifiedCount = await _context.CompanyRegistrations
            .CountAsync(r => r.Status == RegistrationStatus.EmailVerified);

        // Calculate average review time for completed registrations
        // Review time = ReviewedAt - SubmittedAt (time from submission to admin decision)
        var completedRegistrations = await _context.CompanyRegistrations
            .Where(r => (r.Status == RegistrationStatus.Approved || r.Status == RegistrationStatus.Rejected)
                && r.SubmittedAt.HasValue
                && r.ReviewedAt.HasValue)
            .ToListAsync();

        double? averageReviewTimeHours = null;
        if (completedRegistrations.Any())
        {
            var reviewTimes = completedRegistrations
                .Select(r => (r.ReviewedAt!.Value - r.SubmittedAt!.Value).TotalHours)
                .ToList();
            averageReviewTimeHours = reviewTimes.Average();
        }

        var stats = new
        {
            totalRegistrations,
            pendingCount,
            approvedCount,
            rejectedCount,
            emailVerifiedCount,
            averageReviewTimeHours = averageReviewTimeHours.HasValue
                ? Math.Round(averageReviewTimeHours.Value, 2)
                : (double?)null
        };

        return Ok(stats);
    }

    /// <summary>
    /// Approve a company registration
    /// </summary>
    [HttpPost("registrations/{id}/approve")]
    [ProducesResponseType(typeof(CompanyRegistration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveRegistration(Guid id, [FromBody] ApproveRegistrationRequest? request)
    {
        var registration = await _context.CompanyRegistrations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
        {
            return NotFound(new { error = "Registration not found" });
        }

        if (registration.Status != RegistrationStatus.PendingReview)
        {
            return BadRequest(new
            {
                error = "Only pending registrations can be approved",
                currentStatus = registration.Status.ToString()
            });
        }

        // Update registration status
        registration.Status = RegistrationStatus.Approved;
        registration.ReviewedAt = DateTime.UtcNow;
        registration.ReviewedBy = "admin"; // TODO: Replace with actual admin user from JWT claims
        registration.ReviewNotes = request?.Notes;
        registration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Registration {RegistrationId} approved by {ReviewedBy}. Company: {LegalName}",
            id,
            registration.ReviewedBy,
            registration.LegalName
        );

        // TODO: Send approval email to company

        return Ok(registration);
    }

    /// <summary>
    /// Reject a company registration
    /// </summary>
    [HttpPost("registrations/{id}/reject")]
    [ProducesResponseType(typeof(CompanyRegistration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectRegistration(Guid id, [FromBody] RejectRegistrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { error = "Rejection reason is required" });
        }

        var registration = await _context.CompanyRegistrations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
        {
            return NotFound(new { error = "Registration not found" });
        }

        if (registration.Status != RegistrationStatus.PendingReview)
        {
            return BadRequest(new
            {
                error = "Only pending registrations can be rejected",
                currentStatus = registration.Status.ToString()
            });
        }

        // Update registration status
        registration.Status = RegistrationStatus.Rejected;
        registration.ReviewedAt = DateTime.UtcNow;
        registration.ReviewedBy = "admin"; // TODO: Replace with actual admin user from JWT claims
        registration.ReviewNotes = request.Reason;
        registration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Registration {RegistrationId} rejected by {ReviewedBy}. Company: {LegalName}. Reason: {Reason}",
            id,
            registration.ReviewedBy,
            registration.LegalName,
            request.Reason
        );

        // TODO: Send rejection email to company with reason

        return Ok(registration);
    }
}

// Request DTOs
public class ApproveRegistrationRequest
{
    public string? Notes { get; set; }
}

public class RejectRegistrationRequest
{
    [Required]
    [MinLength(10, ErrorMessage = "Rejection reason must be at least 10 characters")]
    public string Reason { get; set; } = string.Empty;
}
