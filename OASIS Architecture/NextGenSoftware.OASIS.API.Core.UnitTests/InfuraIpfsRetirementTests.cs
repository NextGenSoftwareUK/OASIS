using System.Net;
using System.Net.Http;
using System.Threading;
using NextGenSoftware.OASIS.API.Providers.InfuraOASIS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;
using InfuraProvider = NextGenSoftware.OASIS.API.Providers.InfuraOASIS.InfuraOASIS;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class InfuraIpfsRetirementTests
{
    [Fact]
    public void RetiredStorageIsNotAdvertised()
    {
        var provider = new InfuraProvider();
        Assert.Equal(ProviderType.InfuraOASIS, provider.ProviderType.Value);
        Assert.Equal(ProviderCategory.Network, provider.ProviderCategory.Value);
        Assert.Contains(provider.ProviderCapabilities, category => category.Value == ProviderCategory.Blockchain);
        Assert.DoesNotContain(provider.ProviderCapabilities, category => category.Value == ProviderCategory.Storage);
    }

    [Fact]
    public async Task RetiredStorageWritesPreserveRetainedCidAndInput()
    {
        var provider = new InfuraProvider();
        var holon = new Holon { Id = Guid.NewGuid(), Name = "retained Infura record" };
        const string cid = "retained-cid";
        holon.MetaData["infura_ipfs_cid"] = cid;
        var originalId = holon.Id;
        Rejected(await provider.SaveHolonAsync(holon));
        Rejected(provider.SaveHolon(holon));
        Rejected(await provider.SaveHolonsAsync(new[] { holon }));
        Rejected(await provider.DeleteHolonAsync(cid));
        Rejected(provider.DeleteHolon(originalId));
        Rejected(await provider.ImportAsync(new[] { holon }));
        Assert.Equal(originalId, holon.Id);
        Assert.Equal("retained Infura record", holon.Name);
        Assert.Equal(cid, holon.MetaData["infura_ipfs_cid"]);
    }

    [Fact]
    public async Task RetiredStorageReadsRejectInsteadOfReturningEmptySuccess()
    {
        var provider = new InfuraProvider();
        const string cid = "retained-cid";
        Rejected(await provider.LoadHolonAsync(cid));
        Rejected(provider.LoadHolon(Guid.NewGuid()));
        Rejected(await provider.LoadHolonsForParentAsync(cid));
        Rejected(await provider.LoadHolonsForParentAsync(Guid.NewGuid()));
        Rejected(await provider.LoadAllHolonsAsync());
        Rejected(await provider.LoadHolonsByMetaDataAsync("infura_ipfs_cid", cid));
        Rejected(await provider.LoadHolonsByMetaDataAsync(new Dictionary<string, string>(), default));
        Rejected(await provider.SearchAsync(null!));
    }

    [Fact]
    public void QueryProfileRequiresExplicitServerConfiguration()
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddInfuraReadSource(settings);
        Assert.Empty(services);

        settings["CQL_INFURA_QUERY_SOURCE_OWNER"] = Guid.NewGuid().ToString("D");
        Assert.Throws<InvalidOperationException>(() => services.AddInfuraReadSource(settings));
        settings["CQL_INFURA_API_KEY"] = "controlledkey";
        settings["CQL_INFURA_API_SECRET"] = "controlledsecret";
        services.AddInfuraReadSource(settings);
        using var container = services.BuildServiceProvider();
        var profile = container.GetRequiredService<CqlReadSourceProfile>();
        Assert.Equal(ProviderType.InfuraOASIS, profile.Storage.ProviderType.Value);
        _ = new StorageQueryTariff(ProviderType.InfuraOASIS, 0m, "test-v1");
    }

    [Fact]
    public async Task RpcCredentialsReachOnlyThePinnedTransportAndAreRemovedFromErrors()
    {
        using var invoker = new HttpMessageInvoker(
            new InfuraPublicAccountMetadataSource.CredentialHandler(
                "controlledkey", "controlledsecret", new FailingTransport()));

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://mainnet.infura.io/");
        var error = await Assert.ThrowsAsync<HttpRequestException>(
            () => invoker.SendAsync(request, CancellationToken.None));
        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.DoesNotContain("controlledkey", error.ToString());
        Assert.DoesNotContain("controlledsecret", error.ToString());
        Assert.Equal(new Uri("https://mainnet.infura.io/"), request.RequestUri);
        Assert.Null(request.Headers.Authorization);

        using var foreign = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/");
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => invoker.SendAsync(foreign, CancellationToken.None));
        Assert.Null(foreign.Headers.Authorization);
    }

    private sealed class FailingTransport : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal(new Uri("https://mainnet.infura.io/v3/controlledkey"), request.RequestUri);
            Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
            Assert.Equal("OmNvbnRyb2xsZWRzZWNyZXQ=", request.Headers.Authorization?.Parameter);
            throw new HttpRequestException("controlledkey controlledsecret", null, HttpStatusCode.Unauthorized);
        }
    }

    private static void Rejected<T>(OASISResult<T> result)
    {
        Assert.True(result.IsError);
        Assert.Equal(1, result.ErrorCount);
        Assert.Equal("INFURA_IPFS_RETIRED", result.ErrorCode);
        Assert.Equal(default(T), result.Result);
    }
}
