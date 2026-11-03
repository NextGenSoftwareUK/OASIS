using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass, DoNotParallelize]
public class Web3OwnedCqlRpcTests
{
    [DataTestMethod]
    [DataRow("valid")]
    [DataRow("tampered")]
    [DataRow("identity")]
    [DataRow("transport")]
    [DataRow("missing")]
    [DataRow("deleted")]
    public async Task ShippingV2QueryUsesAbiDecodeAndAuthenticatedCanonicalRecords(string mode)
    {
        const string address = "0x0000000000000000000000000000000000000001";
        var previous = Environment.GetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY", Convert.ToBase64String(key));
            var provider = (Web3CoreOASISBaseProvider)EvmProviderFactory.CreateArbitrum(
                new ArbitrumOASISProviderSettings
                {
                    ConnectionString = "http://127.0.0.1:1", ChainPrivateKey = "0x" + new string('1', 64),
                    ContractAddress = address, ChainId = 31337
                }, HyperDriveModes.V2, null!);

            // Replace only transport. Production query, native ABI decoder,
            // context-bound crypto and canonical graph projection all execute.
            var client = DispatchProxy.Create<IClient, Web3ReadCountControlledTests.ReadRpc>();
            var rpc = (Web3ReadCountControlledTests.ReadRpc)client;
            var web3 = new Web3(client);
            var sdk = new Web3CoreOASIS("0x" + new string('1', 64), "http://127.0.0.1:1",
                address, Web3CoreOASISBaseProviderHelper.Abi, 31337);
            typeof(Web3CoreOASIS).GetField("_web3", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sdk, web3);
            typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, sdk);
            provider.IsProviderActivated = true;
            var function = web3.Eth.GetContract(Web3CoreOASISBaseProviderHelper.Abi, address).GetFunction("lookupHolon");
            rpc.Selectors.Add(function.GetData(0).Substring(0, 10), ("holon", false));
            rpc.Records["holon"] = new();
            var ids = Enumerable.Range(1, 3).Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D12}")).ToArray();
            foreach (var id in ids)
            {
                if (mode == "missing" && id == ids[1]) continue;
                var row = new Holon { Id = mode == "identity" && id == ids[1] ? Guid.NewGuid() : id,
                    Name = "encrypted controlled coffee", IsActive = true,
                    DeletedDate = mode == "deleted" && id == ids[1] ? DateTime.UtcNow : DateTime.MinValue };
                var info = Encoding.UTF8.GetBytes(provider.EncodePrivateRecord("holon", row));
                if (mode == "tampered" && id == ids[1]) info[info.Length / 2] ^= 1;
                rpc.Records["holon"].Add(new EntityOASIS
                {
                    EntityId = (uint)HashUtility.GetNumericHash(id.ToString()),
                    ExternalId = Web3CoreOASISBaseProvider.PrivateExternalId(id), Info = info
                });
            }
            rpc.Fail = mode == "transport";
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ArbitrumOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.ArbitrumOASIS, ids).Result).Result;
            var planned = CosmicQueryPlanner.Plan(query, ProviderType.ArbitrumOASIS,
                new[] { new CosmicQueryTarget(ProviderType.ArbitrumOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(planned.IsError, planned.Message);
            var result = await provider.ExecuteCosmicQueryAsync(planned.Result);
            if (mode is "tampered" or "identity" or "transport")
            {
                Assert.IsTrue(result.IsError);
                Assert.IsNull(result.Result);
                Assert.IsFalse(result.IsLoaded);
                Assert.AreEqual(0, result.LoadedCount);
            }
            else
            {
                Assert.IsFalse(result.IsError, result.Message);
                var expected = mode == "valid" ? ids : new[] { ids[0], ids[2] };
                CollectionAssert.AreEqual(expected, result.Result.Select(x => x.Id).ToArray());
                Assert.IsTrue(result.Result.All(x => !x.Children.Any()));
            }
            Assert.IsTrue(rpc.Calls > 0);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY", previous);
            CryptographicOperations.ZeroMemory(key);
        }
    }
}
