using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.InfuraOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class InfuraNativeFactAttribute : FactAttribute
{
    public InfuraNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_INFURA_VERIFY_NATIVE") != "1")
            Skip = "Explicit authorized Infura native qualification required.";
    }
}

public sealed class InfuraNativeCqlTests
{
    [InfuraNativeFact]
    public async Task RegisteredInfuraProfileReadsThreeAndTwoFinalizedAccounts()
    {
        const ProviderType provider = ProviderType.InfuraOASIS;
        var owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
        var key = Environment.GetEnvironmentVariable("CQL_INFURA_API_KEY");
        var secret = Environment.GetEnvironmentVariable("CQL_INFURA_API_SECRET");
        Assert.True(!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(secret),
            "Authorized Infura key and secret are required.");
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CQL_INFURA_QUERY_SOURCE_OWNER"] = owner.ToString("D"),
            ["CQL_INFURA_API_KEY"] = key,
            ["CQL_INFURA_API_SECRET"] = secret
        }).Build();
        var services = new ServiceCollection();
        services.AddInfuraReadSource(settings);
        using var container = services.BuildServiceProvider();
        var source = Assert.IsType<InfuraPublicAccountMetadataSource>(
            container.GetRequiredService<CqlReadSourceProfile>().Storage);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        Assert.False((await source.ActivateQuerySourceAsync(timeout.Token)).IsError,
            "Authorized Infura activation failed.");

        string[] addresses =
        [
            "0x0000000000000000000000000000000000000000",
            "0x000000000000000000000000000000000000dead",
            "0x00000000219ab540356cbb839cbe05303d7705fa"
        ];
        Guid[] ids =
        [
            Guid.Parse("ef429e7f-cebb-8538-87b2-5a57b6884bf2"),
            Guid.Parse("8c2689e4-73e9-8faa-9d18-b70d4e4486fa"),
            Guid.Parse("a4669a44-3d6d-826b-91aa-1dae1e62fc03")
        ];
        await Verify(addresses, ids);
        await Verify(addresses[..2], ids[..2]);

        async Task Verify(string[] selected, Guid[] expectedIds)
        {
            const string metadata = InfuraPublicAccountMetadataSource.AddressMetadata;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
                .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(owner).Result).Result
                .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, metadata)));
            Assert.False(query.IsError);
            var plan = CosmicQueryPlanner.Plan(query.Result, provider,
                new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
            Assert.False(plan.IsError);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: timeout.Token);
            Assert.False(result.IsError, "Authorized Infura CQL read failed.");
            Assert.Equal(expectedIds.OrderBy(id => id), result.Result.Select(row => row.Id).OrderBy(id => id));
            Assert.Equal(selected.OrderBy(address => address),
                result.Result.Select(row => row.ProviderUniqueStorageKey[provider]).OrderBy(address => address));
            Assert.Single(result.Result.Select(row => row.MetaData["InfuraEthereumBlockHash"]).Distinct());
            Assert.All(result.Result, row =>
            {
                Assert.True(row.IsPublic && row.CreatedByAvatarId == owner);
                Assert.Equal("0x1", row.MetaData["InfuraEthereumChainId"].ToString());
                Assert.Equal("finalized", row.MetaData["InfuraEthereumCommitment"].ToString());
            });
        }
    }
}
