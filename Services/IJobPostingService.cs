using JobStream.Api.DTOs;
using JobStream.Api.Models;

namespace JobStream.Api.Services;

public interface IJobPostingService
{
    Task<JobPostingActionResponse> CreateDraftAsync(CreateJobPostingRequest request);
    Task<JobPostingActionResponse> PublishAsync(Guid postingId, string companyId);
    Task<JobPostingActionResponse> UpdateDraftAsync(Guid postingId, UpdateJobPostingRequest request, string companyId);
    Task<JobPostingResponse?> GetByIdAsync(Guid postingId, string? companyId = null);
    Task<List<JobPostingResponse>> GetLivePostingsAsync();
    Task<List<JobPostingResponse>> GetByCompanyIdAsync(string companyId);
    Task<List<JobPostingResponse>> GetDraftsByCompanyIdAsync(string companyId);
}
