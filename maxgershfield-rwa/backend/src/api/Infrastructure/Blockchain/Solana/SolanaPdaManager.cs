using Microsoft.Extensions.Configuration;
using Solnet.Wallet;

namespace Infrastructure.Blockchain.Solana;

/// <summary>
/// Manages Program Derived Addresses (PDAs) for Solana funding rate accounts
/// </summary>
public class SolanaPdaManager
{
    private readonly PublicKey _programId;

    public SolanaPdaManager(IConfiguration configuration)
    {
        var programIdString = configuration["Blockchain:Solana:FundingRateProgramId"] 
            ?? "Fg6PaFpoGXkYsidMpWTK6W2BeZ7FEfcYkg476zPFsLnS";
        _programId = new PublicKey(programIdString);
    }

    /// <summary>
    /// Derive PDA for a funding rate account
    /// </summary>
    public (PublicKey, byte) DeriveFundingRatePda(string symbol)
    {
        var symbolBytes = System.Text.Encoding.UTF8.GetBytes(symbol);
        var seeds = new List<byte[]>
        {
            System.Text.Encoding.UTF8.GetBytes("funding_rate"),
            symbolBytes
        };

        return PublicKey.TryFindProgramAddress(seeds, _programId);
    }

    /// <summary>
    /// Get or create PDA for a funding rate account (async wrapper for consistency)
    /// </summary>
    public Task<(PublicKey, byte)> GetOrCreateFundingRatePdaAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DeriveFundingRatePda(symbol));
    }
}

