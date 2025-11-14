using System.Collections.Concurrent;
using System.Text.Json;
using ScGen.Lib.Shared.DTOs.Requests;

namespace ScGen.Lib.Shared.Services.X402;

/// <summary>
/// Service for managing user credits
/// </summary>
public interface ICreditsService
{
    Task<int> GetCreditsAsync(string walletAddress, CancellationToken ct = default);
    Task<bool> DeductCreditsAsync(string walletAddress, int amount, CancellationToken ct = default);
    Task<bool> AddCreditsAsync(string walletAddress, int amount, CancellationToken ct = default);
    Task<CreditPurchaseResult> PurchaseCreditPackAsync(string walletAddress, string transactionSignature, int packSize, CancellationToken ct = default);
}

public class CreditsService : ICreditsService
{
    private readonly ILogger<CreditsService> _logger;
    private readonly IX402PaymentService _paymentService;
    
    // In-memory storage (in production, use a database)
    private static readonly ConcurrentDictionary<string, int> _creditsStore = new();
    
    // Persistent storage file
    private readonly string _storageFile;

    public CreditsService(
        ILogger<CreditsService> logger,
        IX402PaymentService paymentService)
    {
        _logger = logger;
        _paymentService = paymentService;
        
        // Store credits in temp directory (in production, use database)
        _storageFile = Path.Combine(Path.GetTempPath(), "assetrail-credits.json");
        LoadCreditsFromStorage();
    }

    public Task<int> GetCreditsAsync(string walletAddress, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
            return Task.FromResult(0);

        _creditsStore.TryGetValue(walletAddress, out int credits);
        return Task.FromResult(credits);
    }

    public async Task<bool> DeductCreditsAsync(string walletAddress, int amount, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress) || amount <= 0)
            return false;

        bool success = _creditsStore.AddOrUpdate(
            walletAddress,
            key => 0, // If doesn't exist, return 0 (insufficient)
            (key, current) => current >= amount ? current - amount : current
        ) >= amount;

        if (success)
        {
            _logger.LogInformation("Deducted {Amount} credits from {Wallet}. Remaining: {Remaining}", 
                amount, walletAddress, _creditsStore[walletAddress]);
            await SaveCreditsToStorageAsync();
        }
        else
        {
            _logger.LogWarning("Insufficient credits for {Wallet}. Has: {Current}, Needs: {Amount}",
                walletAddress, _creditsStore.GetValueOrDefault(walletAddress, 0), amount);
        }

        return success;
    }

    public async Task<bool> AddCreditsAsync(string walletAddress, int amount, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress) || amount <= 0)
            return false;

        _creditsStore.AddOrUpdate(walletAddress, amount, (key, current) => current + amount);
        
        _logger.LogInformation("Added {Amount} credits to {Wallet}. New balance: {Balance}",
            amount, walletAddress, _creditsStore[walletAddress]);
        
        await SaveCreditsToStorageAsync();
        return true;
    }

    public async Task<CreditPurchaseResult> PurchaseCreditPackAsync(
        string walletAddress, 
        string transactionSignature, 
        int packSize, 
        CancellationToken ct = default)
    {
        try
        {
            // Get pack pricing
            var pack = GetCreditPack(packSize);
            if (pack == null)
            {
                return new CreditPurchaseResult
                {
                    Success = false,
                    Error = $"Invalid pack size: {packSize}"
                };
            }

            // Verify payment on blockchain
            bool isValid = await VerifyPackPurchaseTransaction(
                transactionSignature, 
                pack.PriceSOL, 
                ct);

            if (!isValid)
            {
                return new CreditPurchaseResult
                {
                    Success = false,
                    Error = "Transaction verification failed"
                };
            }

            // Add credits to user's account
            await AddCreditsAsync(walletAddress, pack.Credits, ct);

            _logger.LogInformation(
                "Credit pack purchased: {Wallet} bought {Credits} credits for {Price} SOL (tx: {Signature})",
                walletAddress, pack.Credits, pack.PriceSOL, transactionSignature);

            return new CreditPurchaseResult
            {
                Success = true,
                CreditsAdded = pack.Credits,
                NewBalance = await GetCreditsAsync(walletAddress, ct),
                TransactionSignature = transactionSignature,
                PackName = pack.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purchasing credit pack for {Wallet}", walletAddress);
            return new CreditPurchaseResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private async Task<bool> VerifyPackPurchaseTransaction(
        string signature, 
        decimal expectedAmount, 
        CancellationToken ct)
    {
        // Reuse the payment verification logic
        var request = new VerifyPaymentRequest
        {
            Signature = signature,
            Operation = "purchase-credits",
            Blockchain = "Solana",
            Amount = expectedAmount
        };

        // Verify transaction exists on chain (we don't need a payment token for credits)
        try
        {
            var result = await _paymentService.VerifyPaymentAsync(request, ct);
            return result.IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    private void LoadCreditsFromStorage()
    {
        try
        {
            if (File.Exists(_storageFile))
            {
                string json = File.ReadAllText(_storageFile);
                var credits = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                
                if (credits != null)
                {
                    foreach (var kvp in credits)
                    {
                        _creditsStore[kvp.Key] = kvp.Value;
                    }
                    
                    _logger.LogInformation("Loaded {Count} credit accounts from storage", credits.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load credits from storage, starting fresh");
        }
    }

    private async Task SaveCreditsToStorageAsync()
    {
        try
        {
            var credits = _creditsStore.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            string json = JsonSerializer.Serialize(credits, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            await File.WriteAllTextAsync(_storageFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save credits to storage");
        }
    }

    private static CreditPack? GetCreditPack(int packSize)
    {
        return CREDIT_PACKS.FirstOrDefault(p => p.Credits == packSize);
    }

    public static readonly CreditPack[] CREDIT_PACKS = 
    {
        new() { Name = "Starter", Credits = 10, PriceSOL = 0.15m, Discount = 0.25m },
        new() { Name = "Developer", Credits = 50, PriceSOL = 0.60m, Discount = 0.40m },
        new() { Name = "Professional", Credits = 100, PriceSOL = 1.00m, Discount = 0.50m },
        new() { Name = "Enterprise", Credits = 500, PriceSOL = 4.00m, Discount = 0.60m }
    };
}

public record CreditPack
{
    public required string Name { get; init; }
    public required int Credits { get; init; }
    public required decimal PriceSOL { get; init; }
    public decimal Discount { get; init; } // Percentage discount vs pay-per-use
}

public record CreditPurchaseResult
{
    public bool Success { get; init; }
    public int CreditsAdded { get; init; }
    public int NewBalance { get; init; }
    public string TransactionSignature { get; init; } = string.Empty;
    public string PackName { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
}

/// <summary>
/// Credit costs for each operation
/// </summary>
public static class CreditCosts
{
    public const int GenerateSolidity = 1;
    public const int GenerateRust = 2;
    public const int GenerateScrypto = 2;

    public const int CompileSolidity = 5;
    public const int CompileRust = 15;
    public const int CompileScrypto = 8;

    public const int DeploySolidity = 10;
    public const int DeployRust = 10;
    public const int DeployScrypto = 10;

    public static int GetCost(string operation, string blockchain)
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

            _ => 1 // Default
        };
    }
}

