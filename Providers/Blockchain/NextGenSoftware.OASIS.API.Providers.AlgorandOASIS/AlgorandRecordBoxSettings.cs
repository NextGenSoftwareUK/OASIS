using System;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS;

public sealed class AlgorandRecordBoxSettings
{
    public AlgorandRecordBoxSettings(ulong applicationId, string genesisHash, string writerAddress,
        string approvalProgramSha256, byte[] storageKey)
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

        ApplicationId = applicationId;
        GenesisHash = genesisHash;
        WriterAddress = writerAddress;
        ProgramDigest = programDigest;
        StorageKey = (byte[])storageKey.Clone();
    }

    public ulong ApplicationId { get; }
    public string GenesisHash { get; }
    public string WriterAddress { get; }
    internal byte[] ProgramDigest { get; }
    internal byte[] StorageKey { get; }
}
