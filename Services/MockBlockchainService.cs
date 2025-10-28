namespace JobStream.Api.Services;

/// <summary>
/// Mock implementation of blockchain service for development
/// Simulates blockchain interactions without actual Polygon integration
/// </summary>
public class MockBlockchainService : IBlockchainService
{
    private readonly ILogger<MockBlockchainService> _logger;
    private static long _nextPostingId = 1000; // Start from 1000 for mock IDs

    public MockBlockchainService(ILogger<MockBlockchainService> logger)
    {
        _logger = logger;
    }

    public async Task<(long postingId, string transactionHash)> CreateDraftPostingAsync(
        string companyId,
        string title,
        string description,
        string walletAddress)
    {
        // Simulate blockchain delay
        await Task.Delay(100);

        var postingId = Interlocked.Increment(ref _nextPostingId);
        var txHash = GenerateMockTransactionHash();

        _logger.LogInformation(
            "Mock Blockchain: Created draft posting {PostingId} for company {CompanyId}. TX: {TxHash}",
            postingId, companyId, txHash);

        return (postingId, txHash);
    }

    public async Task<string> PublishPostingAsync(long blockchainPostingId, string walletAddress)
    {
        // Simulate blockchain delay
        await Task.Delay(100);

        var txHash = GenerateMockTransactionHash();

        _logger.LogInformation(
            "Mock Blockchain: Published posting {PostingId}. TX: {TxHash}",
            blockchainPostingId, txHash);

        return txHash;
    }

    public async Task<string> UpdatePostingAsync(long blockchainPostingId, string walletAddress)
    {
        // Simulate blockchain delay
        await Task.Delay(100);

        var txHash = GenerateMockTransactionHash();

        _logger.LogInformation(
            "Mock Blockchain: Updated posting {PostingId}. TX: {TxHash}",
            blockchainPostingId, txHash);

        return txHash;
    }

    public async Task<bool> VerifyPostingExistsAsync(long blockchainPostingId)
    {
        // Simulate blockchain delay
        await Task.Delay(50);

        // In mock, all posting IDs >= 1000 are considered valid
        return blockchainPostingId >= 1000;
    }

    private static string GenerateMockTransactionHash()
    {
        // Generate a mock transaction hash that looks like a real Ethereum/Polygon hash
        return "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 32);
    }
}
