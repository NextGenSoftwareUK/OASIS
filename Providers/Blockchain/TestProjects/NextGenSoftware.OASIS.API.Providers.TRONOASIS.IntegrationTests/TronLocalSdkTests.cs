using TronNet;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests;

/// <summary>Requires the single reusable, loopback-only pinned oasis-tron-tre instance.</summary>
public sealed class TronLocalSdkTests
{
    private const string Genesis = "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f";
    // Public local fixture keys. Never send these to a public network.
    private static readonly string SenderKey = new string('0', 63) + "1";
    private static readonly string RecipientKey = new string('0', 63) + "2";

    [Fact]
    public async Task NftEscrowPreservesDepositorAndRejectsUnauthorizedWithdrawalOnTvm()
    {
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(110));
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), Genesis, contractFeeLimitSun: 1000000000);
        var client = runtime.NativeTransfers;
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/compile-tron-store.cjs"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var artifacts = Path.Combine(directory!.FullName, "TestResults/TronRuntime");
        var owner = new TronECKey(SenderKey, TronNetwork.MainNet).GetPublicAddress();
        var other = new TronECKey(RecipientKey, TronNetwork.MainNet).GetPublicAddress();
        async Task<string> Deployment(string name, string bytecode)
        {
            var file = Path.Combine(artifacts, name);
            if (File.Exists(file)) return (await File.ReadAllTextAsync(file, bounded.Token)).Trim();
            var deployed = await client.DeployBytecodeAsync(owner, Convert.FromHexString((await File.ReadAllTextAsync(Path.Combine(artifacts, bytecode), bounded.Token)).Trim()), SenderKey, bounded.Token);
            await File.WriteAllTextAsync(file, deployed.ContractAddress, bounded.Token);
            return deployed.ContractAddress;
        }
        // A distinct fixed fixture is necessary: the older deployed NFT has no approval/transferFrom ABI.
        var nft = await Deployment("local-escrow-nft-address.txt", "nft-bytecode.txt");
        var escrow = await Deployment("local-escrow-address.txt", "escrow-bytecode.txt");
        var id = new System.Numerics.BigInteger(42);
        byte[] Call(string selector, params byte[][] words)
        {
            var data = new byte[4 + words.Length * 32]; Convert.FromHexString(selector).CopyTo(data, 0);
            for (int i = 0; i < words.Length; i++) words[i].CopyTo(data, 4 + (i + 1) * 32 - words[i].Length);
            return data;
        }
        byte[] Address(string address) => NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(address).AsSpan(1).ToArray();
        var token = id.ToByteArray(true, true);
        async Task<System.Numerics.BigInteger> CurrentOwner() => new(await client.QueryContractAsync(owner, nft, Call("6352211e", token), bounded.Token), true, true);
        var ownerValue = new System.Numerics.BigInteger(Address(owner), true, true);
        var escrowValue = new System.Numerics.BigInteger(Address(escrow), true, true);
        var current = await CurrentOwner();
        if (current == escrowValue) await client.ExecuteNftEscrowAsync(owner, escrow, nft + "/42", SenderKey, true, bounded.Token);
        else Assert.True(current == 0 || current == ownerValue, "Preserve any unrelated account's NFT.");
        if (await CurrentOwner() == 0) await client.MintNftAsync(owner, owner, nft, id, "https://example.invalid/escrow.json", SenderKey, "", bounded.Token);
        try
        {
            // Revoke any approval left by an interrupted owned run, then prove failed deposit rolls back.
            await client.ExecuteSignedContractAsync(owner, nft, Call("095ea7b3", new byte[20], token), SenderKey, bounded.Token);
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteNftEscrowAsync(owner, escrow, nft + "/42", SenderKey, false, bounded.Token));
            Assert.Equal(ownerValue, await CurrentOwner());
            await client.ExecuteSignedContractAsync(owner, nft, Call("095ea7b3", Address(escrow), token), SenderKey, bounded.Token);
            await client.ExecuteNftEscrowAsync(owner, escrow, nft + "/42", SenderKey, false, bounded.Token);
            Assert.Equal(escrowValue, await CurrentOwner());
            await client.SendAsync(owner, other, 50m, SenderKey, "local escrow rejection energy", bounded.Token);
            var rejected = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteNftEscrowAsync(other, escrow, nft + "/42", RecipientKey, true, bounded.Token));
            var rejectedId = System.Text.RegularExpressions.Regex.Match(rejected.Message, "[0-9a-f]{64}");
            Assert.True(rejectedId.Success, "Failed signed execution must retain its original native transaction ID.");
            var failedHistory = Assert.Single(await client.ReadWalletTransactionsAsync(other, new[] { rejectedId.Value }, bounded.Token));
            Assert.Equal(rejectedId.Value, failedHistory.NativeTransactionId);
            Assert.Equal(((TronNet.Protocol.TransactionInfo.Types.code)1).ToString(), failedHistory.NativeExecutionResult);
            Assert.Equal(escrowValue, await CurrentOwner());
            await client.ExecuteNftEscrowAsync(owner, escrow, nft + "/42", SenderKey, true, bounded.Token);
            Assert.Equal(ownerValue, await CurrentOwner());
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteNftEscrowAsync(owner, escrow, nft + "/42", SenderKey, true, bounded.Token));
            Assert.Equal(ownerValue, await CurrentOwner());
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(40));
            var actual = new System.Numerics.BigInteger(await client.QueryContractAsync(owner, nft, Call("6352211e", token), cleanup.Token), true, true);
            if (actual == escrowValue) await client.ExecuteNftEscrowAsync(owner, escrow, nft + "/42", SenderKey, true, cleanup.Token);
            else Assert.True(actual == ownerValue || actual == 0, "Preserve unrelated ownership during cleanup.");
            if (actual != 0) await client.BurnNftAsync(nft, id, SenderKey, cleanup.Token);
        }
        Assert.Equal(System.Numerics.BigInteger.Zero, await CurrentOwner());
    }

    [Fact]
    public async Task NonEnumerableNftMintTransferDiscoveryAndBurnExecuteOnTronTvm()
    {
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(55));
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), Genesis);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/compile-tron-store.cjs"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var artifacts = Path.Combine(directory!.FullName, "TestResults/TronRuntime");
        var deployment = Path.Combine(artifacts, "local-nft-address.txt");
        var owner = new TronECKey(SenderKey, TronNetwork.MainNet).GetPublicAddress();
        var recipient = new TronECKey(RecipientKey, TronNetwork.MainNet).GetPublicAddress();
        string contract;
        if (File.Exists(deployment)) contract = (await File.ReadAllTextAsync(deployment, bounded.Token)).Trim();
        else
        {
            var deployed = await runtime.NativeTransfers.DeployBytecodeAsync(owner,
                Convert.FromHexString((await File.ReadAllTextAsync(Path.Combine(artifacts, "nft-bytecode.txt"), bounded.Token)).Trim()), SenderKey, bounded.Token);
            contract = deployed.ContractAddress;
            await File.WriteAllTextAsync(deployment, contract, bounded.Token);
        }
        var id = System.Numerics.BigInteger.Parse("9007199254740993");
        await runtime.NativeTransfers.SendAsync(owner, recipient, 50m, SenderKey, "local NFT energy budget", bounded.Token);
        // Reconcile this fixed fixture after an interrupted run rather than redeploying or choosing a new ID.
        var ownerCall = new byte[36]; Convert.FromHexString("6352211e").CopyTo(ownerCall, 0);
        var idBytes = id.ToByteArray(true, true); idBytes.CopyTo(ownerCall, ownerCall.Length - idBytes.Length);
        var existingOwner = new System.Numerics.BigInteger(await runtime.NativeTransfers.QueryContractAsync(owner, contract, ownerCall, bounded.Token), true, true);
        if (existingOwner != 0)
        {
            var senderValue = new System.Numerics.BigInteger(NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(owner).AsSpan(1), true, true);
            var recipientValue = new System.Numerics.BigInteger(NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(recipient).AsSpan(1), true, true);
            Assert.True(existingOwner == senderValue || existingOwner == recipientValue, "Preserve NFTs owned by any unrelated account.");
            await runtime.NativeTransfers.BurnNftAsync(contract, id, existingOwner == senderValue ? SenderKey : RecipientKey, bounded.Token);
        }
        var uri = "https://example.invalid/local-tron-fixture.json";
        var mint = await runtime.NativeTransfers.MintNftAsync(owner, owner, contract, id, uri, SenderKey, "local test", bounded.Token);
        var currentKey = SenderKey;
        string burn;
        try
        {
            Assert.Equal(uri, await runtime.NativeTransfers.GetNftMetadataUriAsync(contract, id, bounded.Token));
            Assert.Equal(contract + "/" + id, Assert.Single(await runtime.NativeTransfers.GetIndexedOwnedNftAddressesAsync(owner, new[] { mint }, bounded.Token)));
            var transfer = await runtime.NativeTransfers.SendNftAsync(owner, recipient, contract, id, SenderKey, cancellationToken: bounded.Token);
            currentKey = RecipientKey;
            Assert.Empty(await runtime.NativeTransfers.GetIndexedOwnedNftAddressesAsync(owner, new[] { transfer, mint }, bounded.Token));
            Assert.Equal(contract + "/" + id, Assert.Single(await runtime.NativeTransfers.GetIndexedOwnedNftAddressesAsync(recipient, new[] { transfer }, bounded.Token)));
        }
        finally { burn = await runtime.NativeTransfers.BurnNftAsync(contract, id, currentKey, bounded.Token); }
        Assert.Empty(await runtime.NativeTransfers.GetIndexedOwnedNftAddressesAsync(recipient, new[] { burn }, bounded.Token));
    }

    [Fact]
    public async Task ProviderHolonSurvivesDeactivateAndReopenOnSameLedger()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), Genesis, contractFeeLimitSun: 1000000000);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/compile-tron-store.cjs"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var contract = (await File.ReadAllTextAsync(Path.Combine(directory!.FullName, "TestResults/TronRuntime/local-store-address.txt"))).Trim();
        var owner = new TronECKey(SenderKey, TronNetwork.MainNet).GetPublicAddress();
        byte[] Key() => System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("public reusable local TRON encryption fixture"));
        using var storage = new TronPrivateStorageBackend(runtime.NativeTransfers, owner, contract, () => SenderKey, Key);
        var initial = new TRONOASIS(rpcEndpoint: "http://127.0.0.1:9090", nativeTransfers: runtime.NativeTransfers, storageBackend: storage);
        var activation = await initial.ActivateProviderAsync();
        Assert.False(activation.IsError, activation.Message);
        var id = Guid.Parse("f1fa8b24-3219-48fa-9db2-1bcdab720986");
        var holon = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = id, Name = "Reusable TRON lifecycle 🌳" };
        try
        {
            var saved = await initial.SaveHolonAsync(holon);
            Assert.False(saved.IsError, saved.Message);
            Assert.True(saved.IsSaved);
            Assert.False((await initial.DeActivateProviderAsync()).IsError);
            using var reopenedStorage = new TronPrivateStorageBackend(runtime.NativeTransfers, owner, contract, () => SenderKey, Key);
            var reopened = new TRONOASIS(rpcEndpoint: "http://127.0.0.1:9090", nativeTransfers: runtime.NativeTransfers, storageBackend: reopenedStorage);
            Assert.False((await reopened.ActivateProviderAsync()).IsError);
            try
            {
                var loaded = await reopened.LoadHolonAsync(id);
                Assert.False(loaded.IsError, loaded.Message);
                Assert.Equal(id, loaded.Result.Id);
                Assert.Equal(holon.Name, loaded.Result.Name);
                var removed = await reopened.DeleteHolonAsync(id);
                Assert.False(removed.IsError, removed.Message);
                Assert.True(removed.IsDeleted);
                Assert.True((await reopened.LoadHolonAsync(id)).IsError);
            }
            finally { Assert.False((await reopened.DeActivateProviderAsync()).IsError); }
        }
        finally
        {
            if (!initial.IsProviderActivated) await initial.ActivateProviderAsync();
            var remaining = await initial.LoadHolonAsync(id);
            if (!remaining.IsError) Assert.False((await initial.DeleteHolonAsync(id)).IsError);
            Assert.False((await initial.DeActivateProviderAsync()).IsError);
        }
    }

    [Fact]
    public async Task EncryptedStorageReopensSameContractWithoutPlaintextKeysOrValues()
    {
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(55));
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), Genesis);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/compile-tron-store.cjs"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var contract = (await File.ReadAllTextAsync(Path.Combine(directory!.FullName, "TestResults/TronRuntime/local-store-address.txt"), bounded.Token)).Trim();
        var owner = new TronECKey(SenderKey, TronNetwork.MainNet).GetPublicAddress();
        byte[] Key() => System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("public reusable local TRON encryption fixture"));
        const string logicalKey = "private-reusable-test";
        const string value = "encrypted local TRON 🌳 storage";
        var raw = new TronStorageBackend(runtime.NativeTransfers, owner, contract, () => SenderKey);
        var indexKey = System.Security.Cryptography.HMACSHA256.HashData(Key(), System.Text.Encoding.UTF8.GetBytes("OASIS private storage index v1"));
        var physical = "oasis/private/v1/" + Convert.ToHexString(System.Security.Cryptography.HMACSHA256.HashData(indexKey, System.Text.Encoding.UTF8.GetBytes(logicalKey))).ToLowerInvariant();
        try
        {
            using (var initial = new TronPrivateStorageBackend(runtime.NativeTransfers, owner, contract, () => SenderKey, Key))
            {
                await initial.VerifyAsync(bounded.Token);
                await initial.PutAsync(logicalKey, value, bounded.Token);
                Assert.Equal(value, await initial.GetAsync(logicalKey, bounded.Token));
            }
            Assert.Null(await raw.GetAsync(logicalKey, bounded.Token));
            var ciphertext = await raw.GetAsync(physical, bounded.Token);
            Assert.NotNull(ciphertext);
            Assert.DoesNotContain(logicalKey, ciphertext);
            Assert.DoesNotContain(value, ciphertext);
            using var reopened = new TronPrivateStorageBackend(runtime.NativeTransfers, owner, contract, () => SenderKey, Key);
            await reopened.VerifyAsync(bounded.Token);
            Assert.Equal(value, await reopened.GetAsync(logicalKey, bounded.Token));
            Assert.Contains(logicalKey, await reopened.ListKeysAsync("private-", bounded.Token));
        }
        finally
        {
            using var cleanup = new TronPrivateStorageBackend(runtime.NativeTransfers, owner, contract, () => SenderKey, Key);
            await cleanup.DeleteAsync(logicalKey, bounded.Token);
        }
        Assert.Null(await raw.GetAsync(physical, bounded.Token));
    }

    [Fact]
    public async Task SignedContractStorageCommitsOnReusableLocalTronNode()
    {
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(55));
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), Genesis);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/compile-tron-store.cjs"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var artifacts = Path.Combine(directory!.FullName, "TestResults/TronRuntime");
        var deployment = Path.Combine(artifacts, "local-store-address.txt");
        string contract;
        if (File.Exists(deployment)) contract = (await File.ReadAllTextAsync(deployment, bounded.Token)).Trim();
        else
        {
            var bytecode = Convert.FromHexString((await File.ReadAllTextAsync(Path.Combine(artifacts, "store-bytecode.txt"), bounded.Token)).Trim());
            var deployed = await runtime.NativeTransfers.DeployBytecodeAsync(new TronECKey(SenderKey, TronNetwork.MainNet).GetPublicAddress(), bytecode, SenderKey, bounded.Token);
            contract = deployed.ContractAddress;
            await File.WriteAllTextAsync(deployment, contract, bounded.Token);
        }
        var store = new TronStorageBackend(runtime.NativeTransfers, new TronECKey(SenderKey, TronNetwork.MainNet).GetPublicAddress(), contract, () => SenderKey);
        await store.VerifyAsync(bounded.Token);
        const string key = "reusable-sdk-storage-test";
        try
        {
            await store.PutAsync(key, "TRON 🌳 exact storage", bounded.Token);
            Assert.Equal("TRON 🌳 exact storage", await store.GetAsync(key, bounded.Token));
            Assert.Contains(key, await store.ListKeysAsync("reusable-", bounded.Token));
        }
        finally { await store.DeleteAsync(key, bounded.Token); }
        Assert.Null(await store.GetAsync(key, bounded.Token));
    }

    [Fact]
    public async Task NativeTransferUsesSdkSigningAndSolidifiedReceiptOnPinnedLocalChain()
    {
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(50));
        using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:9090/"), Timeout = TimeSpan.FromSeconds(5) };
        using var node = System.Text.Json.JsonDocument.Parse(await http.GetStringAsync("wallet/getnodeinfo", bounded.Token));
        Assert.Equal("4.8.2", node.RootElement.GetProperty("configNodeInfo").GetProperty("codeVersion").GetString());
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), Genesis);
        var sender = new TronECKey(SenderKey, TronNetwork.MainNet).GetPublicAddress();
        var recipient = new TronECKey(RecipientKey, TronNetwork.MainNet).GetPublicAddress();
        var before = await runtime.NativeTransfers.GetNativeBalanceAsync(sender, bounded.Token);
        Assert.True(before > 1m);
        var hash = await runtime.NativeTransfers.SendAsync(sender, recipient, 1m, SenderKey, "OASIS reusable local SDK test", bounded.Token);
        Assert.Matches("\\A[0-9a-fA-F]{64}\\z", hash);
        var history = Assert.Single(await runtime.NativeTransfers.ReadWalletTransactionsAsync(sender, new[] { hash }, bounded.Token));
        Assert.Equal(hash, history.NativeTransactionId);
        Assert.Equal("1000000", history.RawAmount);
        Assert.Equal("SUN", history.AmountUnit);
        Assert.Equal(Guid.Empty, history.TransactionId);
        Assert.Equal(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,
            await runtime.NativeTransfers.GetTransactionStatusAsync(hash, bounded.Token));
        Assert.True(await runtime.NativeTransfers.GetNativeBalanceAsync(sender, bounded.Token) <= before - 1m);
        Assert.True(await runtime.NativeTransfers.GetNativeBalanceAsync(recipient, bounded.Token) >= 1m);
    }
}
