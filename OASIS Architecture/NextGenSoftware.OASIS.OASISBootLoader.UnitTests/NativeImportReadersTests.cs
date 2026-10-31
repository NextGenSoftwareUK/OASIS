using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;

namespace NextGenSoftware.OASIS.OASISBootLoader.UnitTests
{
    /// <summary>The stateless readers WEB4 uses for a caller-supplied connection exist for every supported type and never touch the network on their own.</summary>
    public class NativeImportReadersTests
    {
        [Fact]
        public void EverySupportedTypeCreatesAReaderOfThatType()
        {
            Assert.Equal(8, NativeImportReaders.SupportedTypes.Count);
            foreach (var type in NativeImportReaders.SupportedTypes)
            {
                var reader = NativeImportReaders.Create(type);
                Assert.NotNull(reader);
                Assert.Equal(type, reader.NativeDataProviderType);
            }
        }

        [Fact]
        public void UnsupportedTypesGetNoReader()
        {
            Assert.Null(NativeImportReaders.Create(ProviderType.EthereumOASIS));
            Assert.Null(NativeImportReaders.Create(ProviderType.None));
        }

        [Fact]
        public async Task EveryReaderRefusesAnInternalAddressItWasHandedWithoutConnecting()
        {
            var connections = new (ProviderType, NativeConnection)[]
            {
                (ProviderType.MongoDBOASIS, new NativeConnection { ["uri"] = "mongodb://localhost", ["database"] = "d" }),
                (ProviderType.Neo4jOASIS, new NativeConnection { ["uri"] = "bolt://10.0.0.5", ["username"] = "u", ["password"] = "p" }),
                (ProviderType.AzureCosmosDBOASIS, new NativeConnection { ["endpoint"] = "https://localhost:8081/", ["key"] = "k", ["database"] = "d" }),
                (ProviderType.SOLIDOASIS, new NativeConnection { ["podUrl"] = "https://localhost/", ["containers"] = "x" }),
                (ProviderType.ActivityPubOASIS, new NativeConnection { ["instanceUrl"] = "https://localhost", ["accessToken"] = "t" }),
                (ProviderType.IPFSOASIS, new NativeConnection { ["apiUrl"] = "https://localhost:5001/", ["paths"] = "/ipfs/Qm" }),
                (ProviderType.SolanaOASIS, new NativeConnection { ["rpcUrl"] = "https://localhost/", ["programs"] = new Solnet.Wallet.Account().PublicKey.Key }),
            };

            foreach (var (type, connection) in connections)
            {
                var result = await NativeImportReaders.Create(type).ListNativeDataSourcesAsync(connection);
                Assert.True(result.IsError, $"{type} accepted an internal address");
                Assert.True(result.Message.Contains("internal name") || result.Message.Contains("not a public address"), $"{type}: {result.Message}");
            }
        }
    }
}
