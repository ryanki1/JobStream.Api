namespace JobStream.Api.Configuration;

/// <summary>
/// Configuration settings for Polygon blockchain integration
/// </summary>
public class BlockchainSettings
{
    /// <summary>
    /// Wallet address for blockchain transactions
    /// </summary>
    public string WalletAddress { get; set; } = string.Empty;

    /// <summary>
    /// Private key for signing transactions (should be stored securely)
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// RPC URL for connecting to Polygon network
    /// </summary>
    public string RpcUrl { get; set; } = "https://rpc-amoy.polygon.technology";

    /// <summary>
    /// Deployed smart contract address
    /// </summary>
    public string ContractAddress { get; set; } = string.Empty;

    /// <summary>
    /// Chain ID (80002 for Amoy Testnet, 137 for Polygon Mainnet)
    /// </summary>
    public int ChainId { get; set; } = 80002;

    /// <summary>
    /// Gas limit for transactions
    /// </summary>
    public int GasLimit { get; set; } = 500000;

    /// <summary>
    /// Whether to use the mock blockchain service instead of real blockchain
    /// </summary>
    public bool UseMockService { get; set; } = true;
}
