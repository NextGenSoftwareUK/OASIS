using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

/// <summary>The Solana reader reading through the caller's own RPC node and programs.</summary>
[TestClass]
public class SolanaNativeConnectionTests
{
    private static readonly string Program = new Account().PublicKey.Key;

    private static NativeConnection Connection(string rpc = "https://rpc.example.com/", string? programs = null) =>
        new() { ["rpcUrl"] = rpc, ["programs"] = programs ?? Program };

    private static SolanaOASIS Provider(Func<string, CancellationToken, Task<IReadOnlyList<SolanaOASIS.NativeSolanaAccount>>>? fetch = null)
    {
        var account = new Account();
        return new SolanaOASIS("http://127.0.0.1:1", account.PrivateKey.Key, account.PublicKey.Key) { NativeProgramAccounts = fetch, NativeImportPrograms = Program };
    }

    private static SolanaOASIS.NativeSolanaAccount Account(string address) => new(address, 1000, Program, false, 361, "AQID", 3);

    [TestMethod]
    public void IdentityIsTheRpcHostNeverAnyKeyInTheUrl()
    {
        var identity = Provider().DescribeNativeConnection(Connection("https://Rpc.Example.com/?api-key=secret123"));
        Assert.AreEqual("solana://rpc.example.com", identity);
        Assert.IsFalse(identity.Contains("secret123"));
        Assert.IsNull(Provider().DescribeNativeConnection(null));
    }

    [TestMethod]
    public async Task BadConnectionsAreRefusedBeforeAnyRead()
    {
        var provider = Provider((_, _) => throw new AssertFailedException("nothing may be fetched"));

        StringAssert.Contains((await provider.ListNativeDataSourcesAsync(Connection("http://rpc.example.com"))).Message, "https");
        StringAssert.Contains((await provider.ListNativeDataSourcesAsync(Connection("https://user:pw@rpc.example.com"))).Message, "https");
        StringAssert.Contains((await provider.ListNativeDataSourcesAsync(new NativeConnection { ["rpcUrl"] = "https://rpc.example.com" })).Message, "programs");
        StringAssert.Contains((await provider.ListNativeDataSourcesAsync(Connection(programs: "not-an-address"))).Message, "not a Solana program address");
        StringAssert.Contains((await provider.ListNativeDataSourcesAsync(new NativeConnection { ["rpcUrl"] = "https://rpc.example.com", ["programs"] = Program, ["rpc"] = "x" })).Message, "rpc");
        Assert.AreEqual("NATIVE_IMPORT_READ_FAILED", (await provider.ReadNativeRecordsAsync(new Account().PublicKey.Key, null, 5, Connection())).ErrorCode);
    }

    [TestMethod]
    public async Task EachNodeGetsItsOwnSnapshotAndPagesComeFromOneFetch()
    {
        var fetches = 0;
        var provider = Provider((_, _) => { fetches++; return Task.FromResult<IReadOnlyList<SolanaOASIS.NativeSolanaAccount>>(new[] { Account("b"), Account("a") }); });

        var first = await provider.ReadNativeRecordsAsync(Program, null, 1, Connection("https://one.example.com/"));
        Assert.IsFalse(first.IsError, first.Message);
        Assert.AreEqual("a", first.Result.Records.Single().NativeKey);
        var second = await provider.ReadNativeRecordsAsync(Program, first.Result.ContinuationToken, 1, Connection("https://one.example.com/"));
        Assert.AreEqual("b", second.Result.Records.Single().NativeKey);
        Assert.AreEqual(1, fetches, "both pages of one node come from one fetch");

        await provider.ReadNativeRecordsAsync(Program, null, 1, Connection("https://two.example.com/"));
        Assert.AreEqual(2, fetches, "a different node must not reuse the first node's snapshot");

        await provider.ReadNativeRecordsAsync(Program, null, 1);
        Assert.AreEqual(3, fetches, "the configured store has its own snapshot too");
    }

    [TestMethod]
    public async Task TheDefaultClientRefusesInternalNodesWithTheRealReason()
    {
        var result = await Provider().ListNativeDataSourcesAsync(Connection("https://localhost/"));

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "internal name");
    }
}
