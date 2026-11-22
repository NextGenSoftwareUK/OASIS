using System;
using System.Security.Cryptography;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.AlgorandOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

public static partial class OASISBootLoader
{
    private static AlgorandOASIS CreateAlgorandProvider(AlgorandOASISProviderSettings settings, string overrideConnectionString)
    {
        if (settings == null)
            throw new InvalidOperationException("Configure StorageProviders.AlgorandOASIS in OASIS DNA.");
        if (overrideConnectionString != null && !string.Equals(overrideConnectionString, settings.AlgodUrl, StringComparison.Ordinal))
            throw new ArgumentException("Algorand registration requires the configured Algod URL.", nameof(overrideConnectionString));

        var algodUrl = RequireAlgorandEndpoint(settings.AlgodUrl, "Algod");
        var indexerUrl = RequireAlgorandEndpoint(settings.IndexerUrl, "Indexer");
        var algodToken = ReadAlgorandSecret(settings.AlgodTokenEnvironmentVariable, "Algod token");
        var indexerToken = ReadAlgorandSecret(settings.IndexerTokenEnvironmentVariable, "Indexer token");

        return new AlgorandOASIS(algodUrl, indexerUrl, algodToken, indexerToken,
            CreateAlgorandRecordStorage(settings.RecordStorage));
    }

    private static AlgorandRecordBoxSettings CreateAlgorandRecordStorage(AlgorandRecordStorageSettings settings)
    {
        if (settings == null) return null;

        var encodedKey = ReadAlgorandSecret(settings.StorageKeyEnvironmentVariable, "record storage key")
            ?? throw new InvalidOperationException("Specify the Algorand record storage key environment variable.");
        var encodedSeed = ReadAlgorandSecret(settings.WriterSeedEnvironmentVariable, "record writer seed");
        var storageKey = Convert.FromBase64String(encodedKey);
        byte[] writerSeed = null;
        try
        {
            if (encodedSeed != null) writerSeed = Convert.FromBase64String(encodedSeed);
            return new AlgorandRecordBoxSettings(settings.ApplicationId, settings.GenesisHash,
                settings.WriterAddress, settings.ApprovalProgramSha256, storageKey, writerSeed, settings.MaximumFee);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(storageKey);
            if (writerSeed != null) CryptographicOperations.ZeroMemory(writerSeed);
        }
    }

    private static string ReadAlgorandSecret(string variableName, string purpose)
    {
        if (string.IsNullOrWhiteSpace(variableName)) return null;
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"The configured Algorand {purpose} environment variable is empty or missing.");
        return value;
    }

    private static string RequireAlgorandEndpoint(string value, string name)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttps && !(endpoint.Scheme == Uri.UriSchemeHttp && endpoint.IsLoopback)) ||
            endpoint.AbsolutePath != "/" || endpoint.UserInfo.Length != 0 || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0)
            throw new InvalidOperationException($"Configure an HTTPS Algorand {name} origin or a loopback HTTP origin without embedded credentials.");
        return endpoint.AbsoluteUri;
    }
}
