using System;
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

/// <summary>Immutable explicit contract identity; signing key remains the provider's
/// configured key and encryption material is separately persisted/resolved.</summary>
public sealed class PolkadotStorageConfiguration
{
    public string ContractAccountId { get; }
    public string CodeHash { get; }
    public string OwnerAccountId { get; }
    internal Func<byte[]> EncryptionKeyResolver { get; }
    public PolkadotStorageConfiguration(string contractAccountId, string codeHash, string ownerAccountId,
        Func<byte[]> encryptionKeyResolver = null)
    {
        foreach (var value in new[] { contractAccountId, codeHash, ownerAccountId })
            if (value == null || !Regex.IsMatch(value, "^0x[0-9a-fA-F]{64}$", RegexOptions.CultureInvariant))
                throw new ArgumentException("Explicit canonical32byte contract/code/owner identities required.");
        ContractAccountId = contractAccountId.ToLowerInvariant(); CodeHash = codeHash.ToLowerInvariant();
        OwnerAccountId = ownerAccountId.ToLowerInvariant();
        EncryptionKeyResolver = encryptionKeyResolver ?? (() => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_POLKADOT_STORAGE_ENCRYPTION_KEY"));
    }
    internal static PolkadotStorageConfiguration FromEnvironment()
    {
        var contract = Environment.GetEnvironmentVariable("OASIS_POLKADOT_STORAGE_CONTRACT_ACCOUNT_ID");
        var code = Environment.GetEnvironmentVariable("OASIS_POLKADOT_STORAGE_CODE_HASH");
        var owner = Environment.GetEnvironmentVariable("OASIS_POLKADOT_STORAGE_OWNER_ACCOUNT_ID");
        if (contract == null && code == null && owner == null) return null;
        return new PolkadotStorageConfiguration(contract, code, owner);
    }
}
