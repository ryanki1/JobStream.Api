using JobStream.Api.DTOs;
using JobStream.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobStream.Api.Controllers;

[ApiController]
[Route("api/company/register")]
public class CompanyRegistrationController : ControllerBase
{
    private readonly ICompanyRegistrationService _registrationService;
    private readonly ILogger<CompanyRegistrationController> _logger;

    public CompanyRegistrationController(
        ICompanyRegistrationService registrationService,
        ILogger<CompanyRegistrationController> logger)
    {
        _registrationService = registrationService;
        _logger = logger;
    }

    /// <summary>
    /// Initialize registration session
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(StartRegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StartRegistrationResponse>> StartRegistration([FromBody] StartRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("POST /api/company/register/start called for email: {Email}", request.CompanyEmail);

            var registration = await _registrationService.StartRegistrationAsync(request);

            var response = new StartRegistrationResponse
            {
                RegistrationId = registration.Id,
                Status = registration.Status.ToString().ToLower(),
                ExpiresAt = registration.ExpiresAt ?? DateTime.UtcNow.AddDays(7)
            };

            return CreatedAtAction(nameof(GetRegistrationStatus), new { registrationId = registration.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration start failed for email: {Email}", request.CompanyEmail);

            var errorResponse = new ErrorResponse
            {
                Code = ex.Message.Contains("already registered") ? "EMAIL_ALREADY_REGISTERED" : "VALIDATION_ERROR",
                Message = ex.Message
            };

            return ex.Message.Contains("already registered")
                ? Conflict(errorResponse)
                : BadRequest(errorResponse);
        }
    }

    /// <summary>
    /// Verify company email domain
    /// </summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VerifyEmailResponse>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            _logger.LogInformation("POST /api/company/register/verify-email called for registration: {RegistrationId}", request.RegistrationId);

            var verified = await _registrationService.VerifyEmailAsync(request.RegistrationId, request.VerificationToken);

            return Ok(new VerifyEmailResponse
            {
                Verified = verified,
                NextStep = "company-details"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Email verification failed for registration: {RegistrationId}", request.RegistrationId);

            var errorResponse = new ErrorResponse
            {
                Code = ex.Message.Contains("not found") ? "REGISTRATION_NOT_FOUND" : "VALIDATION_ERROR",
                Message = ex.Message
            };

            return ex.Message.Contains("not found")
                ? NotFound(errorResponse)
                : BadRequest(errorResponse);
        }
    }

    /// <summary>
    /// Save company information
    /// </summary>
    [HttpPut("{registrationId}/company-details")]
    [ProducesResponseType(typeof(UpdateCompanyDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateCompanyDetailsResponse>> UpdateCompanyDetails(
        Guid registrationId,
        [FromBody] UpdateCompanyDetailsRequest request)
    {
        try
        {
            _logger.LogInformation("PUT /api/company/register/{RegistrationId}/company-details called", registrationId);

            var registration = await _registrationService.UpdateCompanyDetailsAsync(registrationId, request);

            return Ok(new UpdateCompanyDetailsResponse
            {
                Saved = true,
                ValidationErrors = new List<string>(),
                NextStep = "documents"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Company details update failed for registration: {RegistrationId}", registrationId);

            var errorResponse = new ErrorResponse
            {
                Code = ex.Message.Contains("not found") ? "REGISTRATION_NOT_FOUND" : "VALIDATION_ERROR",
                Message = ex.Message
            };

            return ex.Message.Contains("not found")
                ? NotFound(errorResponse)
                : BadRequest(errorResponse);
        }
    }

    /// <summary>
    /// Upload verification documents
    /// </summary>
    [HttpPost("{registrationId}/documents")]
    [ProducesResponseType(typeof(UploadDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UploadDocumentResponse>> UploadDocument(
        Guid registrationId,
        [FromForm] IFormFile file,
        [FromForm] string documentType)
    {
        try
        {
            _logger.LogInformation("POST /api/company/register/{RegistrationId}/documents called. File: {FileName}, Type: {DocumentType}",
                registrationId, file.FileName, documentType);

            if (file == null || file.Length == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "File is required"
                });
            }

            // Check file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge, new ErrorResponse
                {
                    Code = "FILE_TOO_LARGE",
                    Message = "File size must not exceed 10MB"
                });
            }

            using var stream = file.OpenReadStream();
            var document = await _registrationService.UploadDocumentAsync(
                registrationId,
                stream,
                file.FileName,
                file.ContentType,
                documentType);

            return CreatedAtAction(nameof(GetRegistrationStatus), new { registrationId }, new UploadDocumentResponse
            {
                DocumentId = document.Id,
                FileName = document.FileName,
                UploadedAt = document.UploadedAt,
                Status = document.Status
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document upload failed for registration: {RegistrationId}", registrationId);

            var errorResponse = new ErrorResponse
            {
                Code = ex.Message.Contains("not found") ? "REGISTRATION_NOT_FOUND" : "VALIDATION_ERROR",
                Message = ex.Message
            };

            return ex.Message.Contains("not found")
                ? NotFound(errorResponse)
                : BadRequest(errorResponse);
        }
    }

    /// <summary>
    /// Submit financial verification data
    /// </summary>
    [HttpPost("{registrationId}/financial-verification")]
    [ProducesResponseType(typeof(FinancialVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FinancialVerificationResponse>> SubmitFinancialVerification(
        Guid registrationId,
        [FromBody] FinancialVerificationRequest request)
    {
        try
        {
            _logger.LogInformation("POST /api/company/register/{RegistrationId}/financial-verification called", registrationId);

            var registration = await _registrationService.SubmitFinancialVerificationAsync(registrationId, request);

            return Ok(new FinancialVerificationResponse
            {
                Verified = false,
                Status = "pending_manual_review",
                EstimatedReviewTime = "2-3 business days"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Financial verification failed for registration: {RegistrationId}", registrationId);

            var errorResponse = new ErrorResponse
            {
                Code = ex.Message.Contains("not found") ? "REGISTRATION_NOT_FOUND" : "VALIDATION_ERROR",
                Message = ex.Message
            };

            return ex.Message.Contains("not found")
                ? NotFound(errorResponse)
                : BadRequest(errorResponse);
        }
    }

    /// <summary>
    /// Get current registration progress
    /// </summary>
    [HttpGet("{registrationId}/status")]
    [ProducesResponseType(typeof(RegistrationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationStatusResponse>> GetRegistrationStatus(Guid registrationId)
    {
        try
        {
            _logger.LogInformation("GET /api/company/register/{RegistrationId}/status called", registrationId);

            var status = await _registrationService.GetRegistrationStatusAsync(registrationId);

            return Ok(status);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Get registration status failed for: {RegistrationId}", registrationId);

            return NotFound(new ErrorResponse
            {
                Code = "REGISTRATION_NOT_FOUND",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Final submission for review
    /// </summary>
    [HttpPost("{registrationId}/submit")]
    [ProducesResponseType(typeof(SubmitRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmitRegistrationResponse>> SubmitRegistration(
        Guid registrationId,
        [FromBody] SubmitRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("POST /api/company/register/{RegistrationId}/submit called", registrationId);

            var registration = await _registrationService.SubmitForReviewAsync(registrationId, request);

            return Ok(new SubmitRegistrationResponse
            {
                Submitted = true,
                ReviewQueuePosition = registration.ReviewQueuePosition ?? 0,
                EstimatedReviewTime = "24-48 hours",
                SmartContractAddress = registration.SmartContractAddress,
                NextSteps = "Wait for admin approval and stake deposit confirmation"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration submission failed for: {RegistrationId}", registrationId);

            var errorResponse = new ErrorResponse
            {
                Code = ex.Message.Contains("not found") ? "REGISTRATION_NOT_FOUND" : "VALIDATION_ERROR",
                Message = ex.Message
            };

            return ex.Message.Contains("not found")
                ? NotFound(errorResponse)
                : BadRequest(errorResponse);
        }
    }
}
