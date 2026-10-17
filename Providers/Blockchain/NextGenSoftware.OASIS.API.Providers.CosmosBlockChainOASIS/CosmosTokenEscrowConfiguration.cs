using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

/// <summary>Explicit reviewed custom-asset contract, not a CW20 or native-bank escrow.</summary>
public sealed record CosmosTokenEscrowConfiguration(string ContractAddress, string CodeSha256)
{
    public void Validate(string assetContract)
    {
        if (string.IsNullOrWhiteSpace(ContractAddress) || ContractAddress != assetContract
            || CodeSha256?.Length != 64)
            throw new ArgumentException("Escrow requires the configured asset contract and its lowercase SHA256 code checksum.");
        foreach (var c in CodeSha256)
            if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f'))
                throw new ArgumentException("Escrow code checksum must be canonical lowercase SHA256.");
    }
}

public sealed class CosmosIdBoundEscrowResponse : ITransactionResponse
{
    public string TransactionResult { get; set; }
    public Guid LockId { get; }
    public IReadOnlyList<string> NativeTransactions { get; }
    internal CosmosIdBoundEscrowResponse(Guid id, string transaction)
    {
        LockId = id; TransactionResult = transaction;
        NativeTransactions = Array.AsReadOnly(new[] { transaction });
    }
}
