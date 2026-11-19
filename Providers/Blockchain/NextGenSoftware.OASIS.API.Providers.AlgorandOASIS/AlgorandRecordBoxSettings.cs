using System;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS;

public sealed class AlgorandRecordBoxSettings
{
    public AlgorandRecordBoxSettings(ulong applicationId, string genesisHash, string writerAddress,
        string approvalProgramSha256, byte[] storageKey, byte[]? writerSeed = null, ulong maximumFee = 1000)
    {
        if (applicationId == 0)
            throw new ArgumentOutOfRangeException(nameof(applicationId));
        if (Convert.FromBase64String(genesisHash).Length != 32)
            throw new ArgumentException("An Algorand genesis hash is required.", nameof(genesisHash));
        ArgumentException.ThrowIfNullOrWhiteSpace(writerAddress);
        var programDigest = Convert.FromHexString(approvalProgramSha256);
        if (programDigest.Length != 32)
            throw new ArgumentException("A SHA-256 program digest is required.", nameof(approvalProgramSha256));
        if (storageKey is not { Length: 32 })
            throw new ArgumentException("An externally managed 32-byte key is required.", nameof(storageKey));
        if (writerSeed != null && (writerSeed.Length != 32 || maximumFee == 0))
            throw new ArgumentException("Signing requires a 32-byte seed and a positive fee ceiling.", nameof(writerSeed));

        ApplicationId = applicationId;
        GenesisHash = genesisHash;
        WriterAddress = writerAddress;
        ProgramDigest = programDigest;
        StorageKey = (byte[])storageKey.Clone();
        WriterSeed = writerSeed == null ? null : (byte[])writerSeed.Clone();
        MaximumFee = maximumFee;
    }

    public ulong ApplicationId { get; }
    public string GenesisHash { get; }
    public string WriterAddress { get; }
    internal byte[] ProgramDigest { get; }
    internal byte[] StorageKey { get; }
    internal byte[]? WriterSeed { get; }
    internal ulong MaximumFee { get; }
}
