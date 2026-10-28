using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

/// <summary>The Solana native reader's snapshot paging, record shape and program allowlist, with account data supplied by the test.</summary>
[TestClass]
public class SolanaNativeDataImportTests
{
    private static readonly string Program = new Account().PublicKey.Key;

    private static SolanaOASIS Provider(string? programs, out int fetches, params string[] addresses)
    {
        var account = new Account();
        var count = 0;
        var accounts = addresses.Select((address, index) =>
            new SolanaOASIS.NativeSolanaAccount(address, (ulong)(index + 1) * 1000, Program, false, 361, "AQID", 3)).ToList();
        var provider = new SolanaOASIS("http://127.0.0.1:1", account.PrivateKey.Key, account.PublicKey.Key)
        {
            NativeImportPrograms = programs,
            NativeProgramAccounts = (_, _) => { count++; return Task.FromResult<IReadOnlyList<SolanaOASIS.NativeSolanaAccount>>(accounts); }
        };
        fetches = 0;
        _fetches = () => count;
        return provider;
    }

    private static Func<int> _fetches = () => 0;

    [TestMethod]
    public async Task ListsConfiguredProgramsWithAccountCounts()
    {
        var sources = await Provider(Program, out _, "b", "a", "c").ListNativeDataSourcesAsync();
        Assert.IsFalse(sources.IsError, sources.Message);
        var source = sources.Result.Single();
        Assert.AreEqual(Program, source.Name);
        Assert.AreEqual(3, source.EstimatedRecordCount);
    }

    [TestMethod]
    public async Task PagesAccountsInAddressOrderFromOneSnapshot()
    {
        var provider = Provider(Program, out _, "c", "a", "b");

        var first = await provider.ReadNativeRecordsAsync(Program, null, 2);
        Assert.IsFalse(first.IsError, first.Message);
        CollectionAssert.AreEqual(new[] { "a", "b" }, first.Result.Records.Select(record => record.NativeKey).ToArray());
        Assert.AreEqual("b", first.Result.ContinuationToken);
        var fields = first.Result.Records[0].Fields;
        Assert.AreEqual(2000L, fields["lamports"]); // "a" was the second address supplied, so it holds (index 1 + 1) * 1000
        Assert.AreEqual(Program, fields["owner"]);
        Assert.AreEqual(false, fields["executable"]);
        Assert.AreEqual("361", fields["rentEpoch"]);
        Assert.AreEqual(3L, fields["dataLength"]);
        Assert.AreEqual("AQID", fields["data"]);

        var second = await provider.ReadNativeRecordsAsync(Program, first.Result.ContinuationToken, 2);
        Assert.AreEqual("c", second.Result.Records.Single().NativeKey);
        Assert.IsNull(second.Result.ContinuationToken);
        Assert.AreEqual(1, _fetches(), "both pages must come from the same RPC fetch");
    }

    [TestMethod]
    public async Task UnconfiguredInvalidAndUnlistedProgramsAreRefused()
    {
        Assert.AreEqual("NATIVE_IMPORT_UNAVAILABLE", (await Provider(null, out _).ListNativeDataSourcesAsync()).ErrorCode);
        Assert.AreEqual("NATIVE_IMPORT_UNAVAILABLE", (await Provider("not-an-address", out _).ListNativeDataSourcesAsync()).ErrorCode);
        Assert.AreEqual("NATIVE_IMPORT_READ_FAILED", (await Provider(Program, out _).ReadNativeRecordsAsync(new Account().PublicKey.Key, null, 5)).ErrorCode);
    }

    [TestMethod]
    public async Task DuplicateAddressesAreRefusedAsAnInconsistentSnapshot()
    {
        var result = await Provider(Program, out _, "a", "a").ReadNativeRecordsAsync(Program, null, 5);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("NATIVE_IMPORT_READ_FAILED", result.ErrorCode);
    }
}
