namespace JobStream.Api.Services;

/// <summary>
/// Service for interacting with Polygon blockchain smart contracts
/// </summary>
public interface IBlockchainService
{
    /// <summary>
    /// Create a draft posting on the blockchain
    /// </summary>
    Task<(long postingId, string transactionHash)> CreateDraftPostingAsync(
        string companyId,
        string title,
        string description,
        string walletAddress);

    /// <summary>
    /// Publish a draft posting to make it live
    /// </summary>
    Task<string> PublishPostingAsync(long blockchainPostingId, string walletAddress);

    /// <summary>
    /// Update a draft posting
    /// </summary>
    Task<string> UpdatePostingAsync(long blockchainPostingId, string walletAddress);

    /// <summary>
    /// Get posting details from blockchain
    /// </summary>
    Task<bool> VerifyPostingExistsAsync(long blockchainPostingId);
}
