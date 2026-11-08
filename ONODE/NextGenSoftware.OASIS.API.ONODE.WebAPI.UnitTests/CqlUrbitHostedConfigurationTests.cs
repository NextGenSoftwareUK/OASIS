using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.UrbitOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlUrbitHostedConfigurationTests
{
    private const string Origin = "https://ship.example.test";
    private const string Point = "/~/scry/contacts/v1/book/id/0v1.json";
    private const string Owner = "d5cc4384-ff72-4e7d-a780-1595d56c3265";
    private const string Connection = "eaa5679e-9250-4abb-bb5e-d415dcf1f117";
    private static Dictionary<string, string?> Values() => new() {
        ["CQL_URBIT_QUERY_ORIGIN"] = Origin, ["CQL_URBIT_QUERY_SHIP"] = "~zod",
        ["CQL_URBIT_QUERY_SESSION_TOKEN"] = "0v1", ["CQL_URBIT_QUERY_OWNER"] = Owner,
        ["CQL_URBIT_QUERY_CONNECTION_ID"] = Connection, ["CQL_URBIT_QUERY_PAGE_IDS"] = "[\"0v1\",\"0v2\",\"0v3\"]"
    };
    private static IConfiguration Settings(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void MissingConfigurationDoesNotEnrollAnything()
    {
        var services = new ServiceCollection();
        services.AddUrbitReadSource(Settings(new()));
        Assert.Empty(services);
    }

    [Fact]
    public void ExplicitProfileStaysInactiveAndPolicyCapturesOnlyItsCatalogue()
    {
        var services = new ServiceCollection();
        services.AddUrbitReadSource(Settings(Values()));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.UrbitOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
        var policy = container.GetRequiredKeyedService<ICqlReadAccessPolicy>(ProviderType.UrbitOASIS);
        var access = policy.Authorize(Guid.Parse(Owner), ProviderType.UrbitOASIS);
        Assert.False(access.IsError, access.Message);
        Assert.Equal(Guid.Parse(Owner), access.Result.ActorId);
        Assert.Equal(ProviderType.UrbitOASIS, access.Result.Authority);
        Assert.True(access.Result.CanRead(UrbitContactBookQuerySource.LogicalId(Guid.Parse(Owner), Guid.Parse(Connection), "0v1"), false, Guid.Parse(Owner)));
        Assert.False(access.Result.CanRead(Guid.NewGuid(), true, Guid.Parse(Owner)));
    }

    [Fact]
    public void PrivateRegistrationCannotReplaceAnExistingProviderPolicy()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ICqlReadAccessPolicy>(ProviderType.UrbitOASIS,
            new CqlPublicReadCatalogue(ProviderType.UrbitOASIS, Array.Empty<Guid>()));
        Assert.Throws<InvalidOperationException>(() => services.AddUrbitReadSource(Settings(Values())));
        Assert.Single(services);
    }

    [Theory]
    [InlineData("ORIGIN", null)]
    [InlineData("ORIGIN", "http://ship.example.test")]
    [InlineData("ORIGIN", "https://localhost")]
    [InlineData("ORIGIN", "https://user@ship.example.test")]
    [InlineData("ORIGIN", Origin + "/book")]
    [InlineData("ORIGIN", Origin + "?other=1")]
    [InlineData("SHIP", "~zod/foreign")]
    [InlineData("SESSION_TOKEN", "0v1; foreign=1")]
    [InlineData("OWNER", "00000000-0000-0000-0000-000000000000")]
    [InlineData("CONNECTION_ID", "invalid")]
    [InlineData("PAGE_IDS", "null")]
    [InlineData("PAGE_IDS", "[\"0v1\",\"0v1\"]")]
    [InlineData("PAGE_IDS", "[\"../foreign\"]")]
    [InlineData("PAGE_IDS", "[null]")]
    public void InvalidPrivateConfigurationIsRejectedBeforeRegistration(string name, string? value)
    {
        var values = Values(); values["CQL_URBIT_QUERY_" + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddUrbitReadSource(Settings(values)));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("372a7b25-32cf-4316-88bc-ea1b56a24b37", ProviderType.UrbitOASIS)]
    [InlineData(Owner, ProviderType.MongoDBOASIS)]
    public void OtherActorOrAuthorityCannotObtainThePrivateCatalogue(string actor, ProviderType authority)
    {
        var policy = new CqlUrbitReadSourceRegistration.OwnerPolicy(Guid.Parse(Owner), new[] { Guid.NewGuid() });
        var result = policy.Authorize(Guid.Parse(actor), authority);
        Assert.True(result.IsError);
        Assert.Equal("CQL_ACCESS_FORBIDDEN", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData("POST", Origin + Point)]
    [InlineData("GET", Origin + "/~/scry/contacts/v1/book.json")]
    [InlineData("GET", Origin + "/~/scry/contacts/v1/book/id/0v4.json")]
    [InlineData("GET", "https://foreign.example.test" + Point)]
    [InlineData("GET", Origin + Point + "?other=1")]
    [InlineData("GET", Origin + "/~/login")]
    public async Task UnadmittedRequestNeverReachesTheTransport(string method, string url)
    {
        var transport = new Observed();
        using var client = new HttpClient(new CqlUrbitReadSourceRegistration.ExactReadHandler(
            new Uri(Origin), new[] { "0v1" }, "urbauth-~zod=0v1", transport));
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        Assert.Equal(0, transport.Calls);
        Assert.False(request.Headers.Contains("Cookie"));
    }

    [Fact]
    public async Task CallerCannotOverrideTheSourceSession()
    {
        var transport = new Observed();
        using var client = new HttpClient(new CqlUrbitReadSourceRegistration.ExactReadHandler(
            new Uri(Origin), new[] { "0v1" }, "urbauth-~zod=0v1", transport));
        using var request = new HttpRequestMessage(HttpMethod.Get, Origin + Point);
        request.Headers.Add("Cookie", "foreign=0v2");
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        Assert.Equal(0, transport.Calls);
    }

    [Fact]
    public async Task ExactSelectedReadCarriesOnlyTheConfiguredSession()
    {
        var transport = new Observed();
        using var client = new HttpClient(new CqlUrbitReadSourceRegistration.ExactReadHandler(
            new Uri(Origin), new[] { "0v1" }, "urbauth-~zod=0v1", transport));
        using var response = await client.GetAsync(Origin + Point);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(1, transport.Calls);
        Assert.Equal("urbauth-~zod=0v1", transport.Cookie);
    }

    [Fact]
    public void UrbitIsAReadProfileWithoutBroadeningCompositionProviders()
    {
        Assert.Equal(ProviderType.UrbitOASIS, CqlV1Compiler.RequestedProvider("UrbitOASIS", true));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.RequestedProvider("UrbitOASIS"));
    }

    [Theory]
    [InlineData("None", true)]
    [InlineData("Name", false)]
    public void CompiledApiQueryMustMatchTheRealPointSourceCapabilities(string orderBy, bool supported)
    {
        var services = new ServiceCollection();
        services.AddUrbitReadSource(Settings(Values()));
        using var container = services.BuildServiceProvider();
        var adapter = (ICosmicQueryStorageProvider)Assert.Single(container.GetServices<CqlReadSourceProfile>()).Storage;
        var access = container.GetRequiredKeyedService<ICqlReadAccessPolicy>(ProviderType.UrbitOASIS)
            .Authorize(Guid.Parse(Owner), ProviderType.UrbitOASIS);
        Assert.False(access.IsError, access.Message);
        var query = CqlV1Compiler.Compile(new CqlV1Request { Provider = "UrbitOASIS", OrderBy = orderBy },
            Guid.Parse(Owner), ProviderType.UrbitOASIS, access.Result);
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.UrbitOASIS,
            new[] { new CosmicQueryTarget(ProviderType.UrbitOASIS, adapter.QueryCapabilities) });
        Assert.Equal(supported, !plan.IsError);
    }

    private sealed class Observed : HttpMessageHandler
    {
        internal int Calls;
        internal string? Cookie;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; Cookie = request.Headers.GetValues("Cookie").Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
        }
    }
}
