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
public class AvalancheCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids, string Contract);
    private const string Rpc = "http://127.0.0.1:18547";
    // Public Ganache deterministic development identity; local test funds only.
    private const string Key = "0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d";

    [TestMethod]
    public async Task NativeAvalancheFiltersThreeRecordsToTwoWithoutNewWritesOnReuse()
    {
        var root = Environment.GetEnvironmentVariable("OASIS_EVM_CQL_STATE")
            ?? throw new InvalidOperationException("Use the retained Ethereum CQL runner.");
        var manifest = Path.Combine(root, "demo-seed.json");
        var pending = Path.Combine(root, "demo-seed.pending.json");
        var continuing = Environment.GetEnvironmentVariable("OASIS_EVM_CQL_CONTINUE_EMPTY_SEED") == "1";
        Assert.IsFalse(File.Exists(pending) && !continuing, "Original unresolved setup must be reconciled, never replayed or reseeded.");
        Assert.IsFalse(continuing && File.Exists(manifest), "Completed sources must use normal read-only reuse.");
        var account = new Account(Key, 31338);
        var web3 = new Web3(account, Rpc);
        Assert.AreEqual(new BigInteger(31338), (await web3.Eth.ChainId.SendRequestAsync()).Value);
        Seed seed;
        if (!File.Exists(manifest))
        {
          using var artifact = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "..", "..", "..", "..", "TestResults", "EthereumCqlCurrent", "web3-storage.json")));
          if (continuing)
          {
            // Continue only the original known pre-write admission failure. This
            // never retries a transaction, replaces IDs or deploys another contract.
            var original = await File.ReadAllBytesAsync(pending);
            seed = JsonSerializer.Deserialize<Seed>(original)!;
            Assert.AreNotEqual(Guid.Empty, seed.Actor);
            Assert.AreEqual(3, seed.Ids.Distinct().Count());
            Assert.IsFalse(seed.Ids.Contains(Guid.Empty));
            var code = await web3.Eth.GetCode.SendRequestAsync(seed.Contract);
            Assert.AreEqual(artifact.RootElement.GetProperty("deployedBytecode").GetString()!.ToLowerInvariant(), code.ToLowerInvariant());
            var contract = web3.Eth.GetContract(artifact.RootElement.GetProperty("abi").GetRawText(), seed.Contract);
            Assert.AreEqual(account.Address.ToLowerInvariant(), (await contract.GetFunction("owner").CallAsync<string>()).ToLowerInvariant());
            foreach (var counter in new[] { "getHolonsCount", "getAvatarsCount", "getAvatarDetailsCount" })
                Assert.AreEqual(0u, await contract.GetFunction(counter).CallAsync<uint>(), "Any persisted source data blocks empty-source continuation.");
            using var admission = new FileStream(Path.Combine(root, "continued-empty-original.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(admission, new {
                seed.Contract, seed.Actor, seed.Ids,
                originalIntentSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(original)),
                nativeCodeSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Convert.FromHexString(code[2..]))),
                owner = account.Address, holons = 0, avatars = 0, details = 0,
                action = "First source writes after verified empty original pre-write failure; no replay or redeployment"
            });
            admission.Flush(true);
          }
          else
          {
            var planned = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray(), "");
            // Preserve the original chain and intent if deployment or a save has an
            // uncertain outcome. Never auto-create a fresh fixture after failure.
            using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, planned);
                file.Flush(true);
            }
            var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                artifact.RootElement.GetProperty("abi").GetRawText(),
                artifact.RootElement.GetProperty("bytecode").GetString(), account.Address,
                new Nethereum.Hex.HexTypes.HexBigInteger(10000000));
            Assert.AreEqual(BigInteger.One, receipt.Status.Value);
            seed = planned with { Contract = receipt.ContractAddress };
            File.WriteAllText(pending, JsonSerializer.Serialize(seed));
          }
            var writer = EvmCqlDemoProviders.CreateAvalanche(Rpc, Key, seed.Contract);
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
                }
            }
            finally { await writer.DeActivateProviderAsync(); }
            File.Move(pending, manifest);
        }
        else seed = JsonSerializer.Deserialize<Seed>(File.ReadAllText(manifest))!;

        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        var provider = EvmCqlDemoProviders.CreateAvalanche(Rpc, Key, seed.Contract);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        try
        {
            var before = (await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)).Value;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AvalancheOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.AvalancheOASIS, seed.Ids).Result).Result;
            async Task<IReadOnlyList<IHolon>> Read(CosmicQuery<Holon> selection)
            {
                var plan = CosmicQueryPlanner.Plan(selection, ProviderType.AvalancheOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.AvalancheOASIS, ((ICosmicQueryStorageProvider)provider).QueryCapabilities) });
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
            Assert.AreEqual(before, (await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)).Value,
                "CQL reads must not submit any native transaction.");
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
