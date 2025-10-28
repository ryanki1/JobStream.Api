namespace JobStream.Api.Models;

/// <summary>
/// Status of a job posting in its lifecycle
/// </summary>
public enum JobPostingStatus
{
    /// <summary>
    /// Draft state - visible only to company and admin
    /// </summary>
    Draft,

    /// <summary>
    /// Published state - visible to all freelancers
    /// </summary>
    Published,

    /// <summary>
    /// Closed/archived - no longer accepting applications
    /// </summary>
    Closed
}
