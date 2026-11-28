using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EclipseOASIS.IntegrationTests;

/// <summary>
/// Shared core storage suite for Eclipse (SVM) against the disposable Agave test validator that the Solana workflow
/// starts. Eclipse speaks the Solana JSON-RPC and runs the same programs, so a local SVM ledger is its native backend.
/// The workflow supplies a separate airdrop-funded keypair so Eclipse's records never share Solana's signer.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class EclipseNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstLocalSvmValidator(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable Agave validator CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var rpc = Env("OASIS_ECLIPSE_RPC");
        Assert.IsTrue(new Uri(rpc).IsLoopback);

        await using var file = File.OpenRead(Env("OASIS_ECLIPSE_KEYPAIR"));
        var keypair = await JsonSerializer.DeserializeAsync<int[]>(file);
        Assert.IsNotNull(keypair);
        Assert.AreEqual(64, keypair.Length);
        var secret = Array.ConvertAll(keypair, value => checked((byte)value));
        EclipseOASIS provider;
        try { provider = new EclipseOASIS(rpc, Encoders.Base58.EncodeData(secret), Encoders.Base58.EncodeData(secret[32..])); }
        finally { Array.Clear(keypair); CryptographicOperations.ZeroMemory(secret); }

        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        var report = await CoreStorageConformance.RunAsync(provider, provider.ProviderName, "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            });
        if (!report.Succeeded)
        {
            var activation = await provider.ActivateProviderAsync();
            Console.WriteLine($"Diagnose(activate): {activation.Message} | {activation.Exception}");
        }
        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        await DelegatedStorageIdentityConformance.RunAsync(provider,
            NextGenSoftware.OASIS.API.Core.Enums.ProviderType.EclipseOASIS,
            NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SolanaOASIS, synchronous);
    }
}
