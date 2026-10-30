using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition;
using Holon = NextGenSoftware.OASIS.API.Core.Holons.Holon;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class ScrollCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids, string Contract);
    private const string Rpc = "http://127.0.0.1:18547";
    // Public Ganache deterministic development identity; local test funds only.
    private const string Key = "0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d";

    [TestMethod]
    public async Task NativeScrollFiltersThreeRecordsToTwoWithoutNewWritesOnReuse()
    {
        var root = Environment.GetEnvironmentVariable("OASIS_EVM_CQL_STATE")
            ?? throw new InvalidOperationException("Use the retained Ethereum CQL runner.");
        var manifest = Path.Combine(root, "demo-seed.json");
        var pending = Path.Combine(root, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Original unresolved setup must be reconciled, never replayed or reseeded.");
        var account = new Account(Key, 31338);
        var web3 = new Web3(account, Rpc);
        Assert.AreEqual(new BigInteger(31338), (await web3.Eth.ChainId.SendRequestAsync()).Value);
        var created = !File.Exists(manifest);
        Seed seed;
        if (!File.Exists(manifest))
        {
            var planned = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray(), "");
            // Preserve the original chain and intent if deployment or a save has an
            // uncertain outcome. Never auto-create a fresh fixture after failure.
            using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, planned);
                file.Flush(true);
            }
            using var artifact = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "..", "..", "..", "..", "TestResults", "EthereumCqlCurrent", "web3-storage.json")));
            var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                artifact.RootElement.GetProperty("abi").GetRawText(),
                artifact.RootElement.GetProperty("bytecode").GetString(), account.Address,
                new Nethereum.Hex.HexTypes.HexBigInteger(10000000));
            Assert.AreEqual(BigInteger.One, receipt.Status.Value);
            seed = planned with { Contract = receipt.ContractAddress };
            using (var file = new FileStream(Path.Combine(root, "deployment.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, new { seed.Contract, receipt.TransactionHash, Block = receipt.BlockNumber.Value.ToString() });
                file.Flush(true);
            }
            var writer = new NextGenSoftware.OASIS.API.Providers.ScrollOASIS.ScrollOASIS(Rpc, Key, seed.Contract, 31338);
            Assert.IsFalse((await writer.ActivateProviderAsync()).IsError);
            var names = new[] { "Espresso", "Latte", "Tea" };
            try
            {
                for (var index = 0; index < seed.Ids.Length; index++)
                {
                    var saved = await writer.SaveHolonsAsync(new IHolon[] { new Holon {
                        Id = seed.Ids[index], Name = names[index], CreatedByAvatarId = seed.Actor,
                        IsNewHolon = true } }, saveChildren: false, continueOnError: false);
                    Assert.IsFalse(saved.IsError, saved.Exception?.ToString() ?? saved.Message);
                    Assert.IsTrue(saved.IsSaved);
                    using var acknowledgement = new FileStream(Path.Combine(root, $"write-{index}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    JsonSerializer.Serialize(acknowledgement, new { Id = seed.Ids[index], saved.IsSaved });
                    acknowledgement.Flush(true);
                }
            }
            finally { await writer.DeActivateProviderAsync(); }
            using (var file = new FileStream(manifest, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { JsonSerializer.Serialize(file, seed); file.Flush(true); }
            File.Delete(pending);
        }
        else seed = JsonSerializer.Deserialize<Seed>(File.ReadAllText(manifest))!;

        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        var provider = new NextGenSoftware.OASIS.API.Providers.ScrollOASIS.ScrollOASIS(Rpc, Key, seed.Contract, 31338);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        try
        {
            async Task<Dictionary<string, string>> NativeHashes()
            {
                using var contractArtifact = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "..", "..", "..", "..", "TestResults", "EthereumCqlCurrent", "web3-storage.json")));
                var function = web3.Eth.GetContract(contractArtifact.RootElement.GetProperty("abi").GetRawText(), seed.Contract).GetFunction("lookupHolon");
                var hashes = new Dictionary<string, string>();
                foreach (var id in seed.Ids)
                {
                    var data = function.GetData((uint)NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(id.ToString()));
                    var raw = await web3.Eth.Transactions.Call.SendRequestAsync(new Nethereum.RPC.Eth.DTOs.CallInput(data, seed.Contract));
                    hashes.Add(id.ToString("D"), Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw))));
                }
                return hashes;
            }
            var snapshotPath = Path.Combine(root, "native-record-hashes.json");
            var hashes = await NativeHashes();
            if (created)
            {
                using var file = new FileStream(snapshotPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                JsonSerializer.Serialize(file, hashes); file.Flush(true);
            }
            else
            {
                Assert.IsTrue(File.Exists(snapshotPath), "Original source baseline missing; do not bless a new snapshot.");
                var original = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(snapshotPath))!;
                CollectionAssert.AreEquivalent(original.ToArray(), hashes.ToArray());
            }
            var before = (await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)).Value;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ScrollOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.ScrollOASIS, seed.Ids).Result).Result;
            async Task<IReadOnlyList<IHolon>> Read(CosmicQuery<Holon> selection)
            {
                var plan = CosmicQueryPlanner.Plan(selection, ProviderType.ScrollOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.ScrollOASIS, ((ICosmicQueryStorageProvider)provider).QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await ((ICosmicQueryStorageProvider)provider).ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                return result.Result;
            }
            var all = await Read(query);
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte", "Tea" }, all.Select(h => h.Name).ToArray());
            var coffee = seed.Ids.Take(2).ToArray();
            var filtered = await Read(query.Where(h => coffee.Contains(h.Id)).Result);
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte" }, filtered.Select(h => h.Name).ToArray());
            CollectionAssert.AreEquivalent(hashes.ToArray(), (await NativeHashes()).ToArray());
            Assert.AreEqual(before, (await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)).Value,
                "CQL reads must not submit any native transaction.");
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
