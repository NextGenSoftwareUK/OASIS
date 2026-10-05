namespace ScGen.Lib.Shared.Options;

/// <summary>
/// Configuration for x402 payment protocol
/// </summary>
public class X402Options
{
    /// <summary>
    /// Solana RPC URL for payment verification
    /// </summary>
    public string SolanaRpcUrl { get; set; } = "https://api.devnet.solana.com";

    /// <summary>
    /// Treasury wallet address that receives payments
    /// </summary>
    public string TreasuryAddress { get; set; } = string.Empty;

    /// <summary>
    /// Enable/disable payment requirement
    /// </summary>
    public bool RequirePayment { get; set; } = true;

    /// <summary>
    /// Free tier limit (operations before payment required)
    /// </summary>
    public int FreeTierLimit { get; set; } = 3;

    /// <summary>
    /// JWT secret for payment tokens
    /// </summary>
    public string JwtSecret { get; set; } = string.Empty;

    /// <summary>
    /// Payment token expiration in hours
    /// </summary>
    public int PaymentTokenExpirationHours { get; set; } = 24;

    /// <summary>
    /// Webhook URL for x402 distribution (OASIS)
    /// </summary>
    public string DistributionWebhookUrl { get; set; } = string.Empty;
}

/// <summary>
/// Pricing configuration for operations
/// </summary>
public class PricingConfig
{
    public const decimal GenerateSolidity = 0.01m;
    public const decimal GenerateRust = 0.02m;
    public const decimal GenerateScrypto = 0.015m;

    public const decimal CompileSolidity = 0.05m;
    public const decimal CompileRust = 0.15m;
    public const decimal CompileScrypto = 0.08m;

    public const decimal DeploySolidity = 0.10m;
    public const decimal DeployRust = 0.10m;
    public const decimal DeployScrypto = 0.10m;

    public static decimal GetPrice(string operation, string blockchain)
    {
        return (operation.ToLower(), blockchain.ToLower()) switch
        {
            ("generate", "solidity") => GenerateSolidity,
            ("generate", "rust") => GenerateRust,
            ("generate", "scrypto") => GenerateScrypto,

            ("compile", "solidity") => CompileSolidity,
            ("compile", "rust") => CompileRust,
            ("compile", "scrypto") => CompileScrypto,

            ("deploy", "solidity") => DeploySolidity,
            ("deploy", "rust") => DeployRust,
            ("deploy", "scrypto") => DeployScrypto,

            _ => 0.01m // Default
        };
    }
}

