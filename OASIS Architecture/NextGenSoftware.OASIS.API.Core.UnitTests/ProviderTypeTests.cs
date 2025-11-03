using NextGenSoftware.OASIS.API.Core.Enums;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests
{
    public class ProviderTypeTests
    {
        [Fact]
        public void ProviderType_ShouldHaveExpectedValues()
        {
            // Assert
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.Default);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.HoloOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.EthereumOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.EOSIOOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.TRONOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.SEEDSOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.SQLLiteDBOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.MongoDBOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.Neo4jOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.AzureCosmosDBOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.LocalFileOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.IPFSOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.ActivityPubOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.SolanaOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.PolygonOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.RootstockOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.ArbitrumOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.CosmosBlockChainOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.HashgraphOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.PinataOASIS);
            Enum.GetValues<ProviderType>().Should().Contain(ProviderType.GoogleCloudOASIS);
        }

        [Fact]
        public void ProviderType_Default_ShouldBeTwo()
        {
            // Assert
            ((int)ProviderType.Default).Should().Be(2);
        }

        [Fact]
        public void ProviderType_ShouldBeEnum()
        {
            // Assert
            typeof(ProviderType).IsEnum.Should().BeTrue();
        }
    }
}
