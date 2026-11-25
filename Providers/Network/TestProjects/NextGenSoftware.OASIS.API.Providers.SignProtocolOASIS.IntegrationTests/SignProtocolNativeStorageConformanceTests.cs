using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.SignProtocolOASIS.IntegrationTests;

/// <summary>
/// Runs the shared core storage suite against the real Sign Protocol SP contract (pinned sign-protocol-evm commit) on a
/// disposable local Hardhat chain started by fixture/start-sign-protocol.sh. Each mode uses its own freshly funded wallet
/// (attester) and registers its own storage schema.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class SignProtocolNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    private static string RequireFixture()
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable local Sign Protocol CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        // The fixture chain is local Hardhat only; never point this suite at a funded network.
        Assert.AreEqual("31337", Env("OASIS_SIGN_CHAIN_ID"));
        StringAssert.StartsWith(Env("OASIS_SIGN_RPC"), "http://127.0.0.1:");
        return directory;
    }

    private static async Task<SignProtocolConnection> ConnectionForNewSchemaAsync(string walletKeyVariable)
    {
        var unbound = new SignProtocolConnection(new Uri(Env("OASIS_SIGN_RPC")), 31337, Env("OASIS_SIGN_SP_ADDRESS"), Env(walletKeyVariable), 0, 0);
        var schema = await SignProtocolKeyValueBackend.RegisterSchemaAsync(unbound);
        return new SignProtocolConnection(unbound.RpcUrl, 31337, unbound.SpAddress, Env(walletKeyVariable), schema.SchemaId, schema.RegisteredBlock);
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstSignProtocol(bool synchronous)
    {
        var directory = RequireFixture();
        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var connection = await ConnectionForNewSchemaAsync(synchronous ? "OASIS_SIGN_SYNC_KEY" : "OASIS_SIGN_ASYNC_KEY");
        var provider = new SignProtocolOASIS(connection, RandomNumberGenerator.GetBytes(32));
        var report = await CoreStorageConformance.RunAsync(provider, provider.ProviderName, "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            });

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }

    /// <summary>
    /// A fresh provider instance rebuilds the store from chain state alone; an attestation by another wallet on the same
    /// schema and indexing key is not accepted as a record; a revocation is seen by later instances.
    /// </summary>
    [TestMethod]
    public async Task FreshProviderReadsRecordsFromChainAndIgnoresOtherAttesters()
    {
        RequireFixture();
        var connection = await ConnectionForNewSchemaAsync("OASIS_SIGN_ASYNC_KEY");
        var key = RandomNumberGenerator.GetBytes(32);
        var holon = new Holon { Id = Guid.NewGuid(), Name = "sign-persistence-" + Guid.NewGuid().ToString("N"), CreatedByAvatarId = Guid.NewGuid() };

        var writer = new SignProtocolOASIS(connection, key);
        Assert.IsFalse((await writer.ActivateProviderAsync()).IsError);
        var saved = await writer.SaveHolonAsync(holon, saveChildren: false);
        Assert.IsFalse(saved.IsError, saved.Message);

        // Another funded wallet attests on this schema with the same indexing key (the contract lets anyone attest on a
        // hook-less schema); the mined forgery must not replace the owner's record.
        var physicalKeys = await new SignProtocolKeyValueBackend(connection).ListKeysAsync("");
        Assert.IsTrue(physicalKeys.Count >= 1);
        var intruder = new SignProtocolKeyValueBackend(new SignProtocolConnection(connection.RpcUrl, 31337, connection.SpAddress,
            Env("OASIS_SIGN_SYNC_KEY"), connection.SchemaId, connection.FromBlock));
        foreach (var physicalKey in physicalKeys) await intruder.PutAsync(physicalKey, "forged");

        var reader = new SignProtocolOASIS(connection, key);
        Assert.IsFalse((await reader.ActivateProviderAsync()).IsError);
        var loaded = await reader.LoadHolonAsync(holon.Id, loadChildren: false);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(holon.Name, loaded.Result?.Name);

        var deleted = await reader.DeleteHolonAsync(holon.Id);
        Assert.IsTrue(!deleted.IsError && deleted.IsDeleted, deleted.Message);
        var later = new SignProtocolOASIS(connection, key);
        Assert.IsFalse((await later.ActivateProviderAsync()).IsError);
        Assert.IsNull((await later.LoadHolonAsync(holon.Id, loadChildren: false)).Result);
        Assert.AreEqual(0, (await new SignProtocolKeyValueBackend(connection).ListKeysAsync("")).Count);
    }
}
