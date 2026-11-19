using System;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Mongo = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SQLite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using Neo = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedDistinctProviderDomainTests
{
    private static CosmicGroupedCapabilities Capabilities(string provider) => provider switch
    {
        "Mongo" => Mongo.CqlGroupedCapabilities,
        "SQLite" => SQLite.CqlGroupedCapabilities,
        "Neo" => Neo.CqlGroupedCapabilities,
        _ => throw new ArgumentException(nameof(provider))
    };

    [Theory]
    [InlineData("Mongo")] [InlineData("SQLite")] [InlineData("Neo")]
    public void DistinctPrimitiveDomainsAreExplicitlyAdmitted(string provider)
    {
        var capabilities = Capabilities(provider);
        var group = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        void Check<T>(System.Linq.Expressions.Expression<Func<Holon, T>> selector)
        {
            var binding = CosmicGroupedBinding<Holon, string>.CountValues(group, "Unique", selector, true);
            Assert.False(binding.IsError, binding.Message);
            var selected = group.SelectColumns(new[] { binding.Result });
            Assert.False(selected.IsError, selected.Message);
            var admission = capabilities.Admit(selected.Result);
            Assert.False(admission.IsError, admission.Message);
        }
        Check(h => h.Description);
        Check(h => h.Version);
        Check(h => CosmicQueryMetadata.Value<string>(h, "value"));
        Check(h => CosmicQueryMetadata.Value<bool?>(h, "value"));
        Check(h => CosmicQueryMetadata.Value<int?>(h, "value"));
        Check(h => CosmicQueryMetadata.Value<long?>(h, "value"));
        Check(h => CosmicQueryMetadata.Value<float?>(h, "value"));
        Check(h => CosmicQueryMetadata.Value<double?>(h, "value"));
    }

    [Theory]
    [InlineData("Mongo")] [InlineData("SQLite")] [InlineData("Neo")]
    public void UnenrolledNormalizationDomainsRemainRefused(string provider)
    {
        var capabilities = Capabilities(provider);
        var group = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        void Refuse<T>(System.Linq.Expressions.Expression<Func<Holon, T>> selector)
        {
            var binding = CosmicGroupedBinding<Holon, string>.CountValues(group, "Unique", selector, true);
            Assert.False(binding.IsError, binding.Message);
            var selected = group.SelectColumns(new[] { binding.Result });
            Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", capabilities.Admit(selected.Result).ErrorCode);
        }
        Refuse(h => h.Id);
        Refuse(h => CosmicQueryMetadata.Value<decimal?>(h, "value"));
        Refuse(h => CosmicQueryMetadata.Value<ulong?>(h, "value"));
        Refuse(h => CosmicQueryMetadata.Value<DateTime?>(h, "value"));
    }
}
