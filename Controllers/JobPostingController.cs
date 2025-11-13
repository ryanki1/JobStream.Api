using JobStream.Api.DTOs;
using JobStream.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobStream.Api.Controllers;

/// <summary>
/// Controller for managing job postings in the JobStream platform
/// </summary>
[ApiController]
[Route("api/v1/jobpostings")]
public class JobPostingController : ControllerBase
{
    private readonly IJobPostingService _jobPostingService;
    private readonly ILogger<JobPostingController> _logger;

    public JobPostingController(
        IJobPostingService jobPostingService,
        ILogger<JobPostingController> logger)
    {
        _jobPostingService = jobPostingService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new draft job posting
    /// </summary>
    /// <param name="request">Job posting details</param>
    /// <returns>Created draft posting response</returns>
    [HttpPost("draft")]
    [ProducesResponseType(typeof(JobPostingActionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JobPostingActionResponse>> CreateDraft([FromBody] CreateJobPostingRequest request)
    {
        try
        {
            _logger.LogInformation("POST /api/v1/jobpostings/draft called for company: {CompanyId}", request.CompanyId);

            var response = await _jobPostingService.CreateDraftAsync(request);

            return CreatedAtAction(nameof(GetById), new { postingId = response.PostingId }, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create draft job posting for company: {CompanyId}", request.CompanyId);

            return BadRequest(new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating draft job posting for company: {CompanyId}", request.CompanyId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An unexpected error occurred while creating the job posting"
            });
        }
    }

    /// <summary>
    /// Publish a draft job posting to the blockchain
    /// </summary>
    /// <param name="postingId">ID of the draft posting to publish</param>
    /// <returns>Published posting response</returns>
    [HttpPut("{postingId:guid}/publish")]
    [ProducesResponseType(typeof(JobPostingActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JobPostingActionResponse>> Publish(Guid postingId)
    {
        try
        {
            // TODO: Replace with authenticated company ID from JWT token when auth is implemented
            const string companyId = "197c8ae3-aa7b-41f0-be6e-e60e13f63232";

            _logger.LogInformation("PUT /api/v1/jobpostings/{PostingId}/publish called by company: {CompanyId}", postingId, companyId);

            var response = await _jobPostingService.PublishAsync(postingId, companyId);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Job posting not found: {PostingId}", postingId);

            return NotFound(new ErrorResponse
            {
                Code = "POSTING_NOT_FOUND",
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to publish job posting: {PostingId}", postingId);

            return BadRequest(new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing job posting: {PostingId}", postingId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An unexpected error occurred while publishing the job posting"
            });
        }
    }

    /// <summary>
    /// Update a draft job posting
    /// </summary>
    /// <param name="postingId">ID of the posting to update</param>
    /// <param name="request">Updated job posting details</param>
    /// <returns>Updated posting response</returns>
    [HttpPut("{postingId:guid}")]
    [ProducesResponseType(typeof(JobPostingActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JobPostingActionResponse>> UpdateDraft(Guid postingId, [FromBody] UpdateJobPostingRequest request)
    {
        try
        {
            // TODO: Replace with authenticated company ID from JWT token when auth is implemented
            const string companyId = "197c8ae3-aa7b-41f0-be6e-e60e13f63232";

            _logger.LogInformation("PUT /api/v1/jobpostings/{PostingId} called by company: {CompanyId}", postingId, companyId);

            var response = await _jobPostingService.UpdateDraftAsync(postingId, request, companyId);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Job posting not found: {PostingId}", postingId);

            return NotFound(new ErrorResponse
            {
                Code = "POSTING_NOT_FOUND",
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update job posting: {PostingId}", postingId);

            return BadRequest(new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating job posting: {PostingId}", postingId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An unexpected error occurred while updating the job posting"
            });
        }
    }

    /// <summary>
    /// Get all live job postings
    /// </summary>
    /// <returns>List of published job postings</returns>
    [HttpGet("live")]
    [ProducesResponseType(typeof(List<JobPostingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<JobPostingResponse>>> GetLivePostings()
    {
        try
        {
            _logger.LogInformation("GET /api/v1/jobpostings/live called");

            var postings = await _jobPostingService.GetLivePostingsAsync();

            return Ok(postings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving live job postings");

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An unexpected error occurred while retrieving live job postings"
            });
        }
    }

    /// <summary>
    /// Get all job postings for a specific company
    /// </summary>
    /// <param name="companyId">Company ID to filter by</param>
    /// <returns>List of job postings for the company</returns>
    [HttpGet("company/{companyId}")]
    [ProducesResponseType(typeof(List<JobPostingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<JobPostingResponse>>> GetByCompany(string companyId)
    {
        try
        {
            _logger.LogInformation("GET /api/v1/jobpostings/company/{CompanyId} called", companyId);

            var postings = await _jobPostingService.GetByCompanyIdAsync(companyId);

            return Ok(postings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving job postings for company: {CompanyId}", companyId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An unexpected error occurred while retrieving company job postings"
            });
        }
    }

    /// <summary>
    /// Get a specific job posting by ID
    /// </summary>
    /// <param name="postingId">ID of the job posting</param>
    /// <returns>Job posting details</returns>
    [HttpGet("{postingId:guid}")]
    [ProducesResponseType(typeof(JobPostingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JobPostingResponse>> GetById(Guid postingId)
    {
        try
        {
            // TODO: Replace with authenticated company ID from JWT token when auth is implemented
            // For now, use null to allow admin access or public viewing of published postings
            const string? companyId = null;

            _logger.LogInformation("GET /api/v1/jobpostings/{PostingId} called", postingId);

            var posting = await _jobPostingService.GetByIdAsync(postingId, companyId);

            if (posting == null)
            {
                return NotFound(new ErrorResponse
                {
                    Code = "POSTING_NOT_FOUND",
                    Message = $"Job posting with ID {postingId} not found"
                });
            }

            return Ok(posting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving job posting: {PostingId}", postingId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An unexpected error occurred while retrieving the job posting"
            });
        }
    }

    /// <summary>
    /// Get all draft job postings for the authenticated company
    /// </summary>
    /// <returns>List of draft job postings</returns>
    [HttpGet("drafts")]
    [ProducesResponseType(typeof(List<JobPostingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<JobPostingResponse>>> GetDrafts()
    {
        try
        {
            // TODO: Replace with authenticated company ID from JWT token when auth is implemented
            const string companyId = "197c8ae3-aa7b-41f0-be6e-e60e13f63232";

            _logger.LogInformation("GET /api/v1/jobpostings/drafts called by company: {CompanyId}", companyId);

            var postings = await _jobPostingService.GetDraftsByCompanyIdAsync(companyId);

            return Ok(postings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving draft job postings");

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An unexpected error occurred while retrieving draft job postings"
            });
        }
    }
}
