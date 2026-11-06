using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.FirebaseOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlFirebaseHostedConfigurationTests
{
    private const string Origin = "https://teaching-default-rtdb.firebaseio.com";
    private const string Project = "teaching-demo";
    private const string Id = "11111111-1111-4111-8111-111111111111";
    private const string Token = "controlled-reader";
    private const string Query = "?orderBy=%22%24key%22&equalTo=%22" + Id + "%22&auth=" + Token;

    private static string Jwt(string project) => "header." + Convert.ToBase64String(
        JsonSerializer.SerializeToUtf8Bytes(new { aud = project, iss = "https://securetoken.google.com/" + project, sub = "reader" }))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".signature";

    [Fact]
    public void EnrollmentIsExplicitAndProjectBound()
    {
        var services = new ServiceCollection();
        services.AddFirebaseReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?>
        {
            ["CQL_FIREBASE_QUERY_ORIGIN"] = Origin,
            ["CQL_FIREBASE_QUERY_PROJECT_ID"] = Project,
            ["CQL_FIREBASE_QUERY_READER_TOKEN"] = Jwt("foreign-project"),
            ["CQL_FIREBASE_QUERY_PROBE_ID"] = Id
        };
        IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddFirebaseReadSource(Configuration()));
        settings["CQL_FIREBASE_QUERY_READER_TOKEN"] = Jwt(Project);
        settings["CQL_FIREBASE_QUERY_ORIGIN"] = "https://firebaseio.com.unreviewed.test";
        Assert.Throws<InvalidOperationException>(() => services.AddFirebaseReadSource(Configuration()));
        Assert.Empty(services);
        settings["CQL_FIREBASE_QUERY_ORIGIN"] = Origin;
        services.AddFirebaseReadSource(Configuration());
        using var container = services.BuildServiceProvider();
        var source = Assert.Single(container.GetServices<CqlReadSourceProfile>()).Storage;
        Assert.Equal(ProviderType.FirebaseOASIS, source.ProviderType.Value);
        Assert.False(source.IsProviderActivated);
    }

    [Theory]
    [InlineData("POST", Origin + "/oasis_holons.json" + Query)]
    [InlineData("GET", Origin + "/oasis_avatars.json" + Query)]
    [InlineData("GET", Origin + "/oasis_holons.json?auth=controlled-reader")]
    [InlineData("GET", Origin + "/oasis_holons.json" + Query + "&ns=foreign")]
    [InlineData("GET", "https://other.firebaseio.com/oasis_holons.json" + Query)]
    public async Task UnadmittedRoutesNeverReachTheTransport(string method, string uri)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlFirebaseReadSourceRegistration.ExactReadHandler(new Uri(Origin), Token, transport));
        using var request = new HttpRequestMessage(new HttpMethod(method), uri);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        Assert.Equal(0, transport.Requests);
    }

    [Fact]
    public async Task NativeProbePassesTheGuardWithTheExactKeyAndCredential()
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlFirebaseReadSourceRegistration.ExactReadHandler(new Uri(Origin), Token, transport));
        var source = new FirebaseQuerySource(Origin, Token, client, Guid.Parse(Id));
        Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        Assert.Equal(1, transport.Requests);
    }

    private sealed class ObservedTransport : HttpMessageHandler
    {
        internal int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.Equal(Query, request.RequestUri!.Query);
            var body = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                [Id] = new { id = Id, name = "Controlled public probe", isPublic = true }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
