using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.WeaviateOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlWeaviateReadSourceRegistration
{
    private const string ClientKey = "cql-weaviate-source";

    public static void AddWeaviateReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_WEAVIATE_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var reader = configuration[prefix + "READER_API_KEY"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (endpoint == null && reader == null && probe == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Weaviate CQL requires an explicit credential-free HTTPS gRPC origin.");
        if (reader is not { Length: > 0 and <= 4096 } ||
            reader.Any(character => character is < '!' or > '~') ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty)
            throw new InvalidOperationException("Weaviate CQL requires a restricted reader API key and existing probe.");

        services.AddKeyedSingleton<GrpcChannel>(ClientKey, (_, _) => GrpcChannel.ForAddress(origin,
            new GrpcChannelOptions
            {
                HttpHandler = new ExactReadHandler(origin, CreateTransport()),
                DisposeHttpClient = true,
                MaxReceiveMessageSize = 4 * 1024 * 1024
            }));
        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) => new HttpClient(new DenyRestHandler()));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new WeaviateQuerySource(
            provider.GetRequiredKeyedService<HttpClient>(ClientKey), origin.AbsoluteUri,
            provider.GetRequiredKeyedService<GrpcChannel>(ClientKey).CreateCallInvoker(), reader, id)));
    }

    private static HttpMessageHandler CreateTransport()
    {
        var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        transport.UseProxy = false;
        transport.UseCookies = false;
        return transport;
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Post || request.Version.Major != 2 ||
                uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || uri.Query.Length != 0 ||
                request.Headers.Host != null || uri.AbsolutePath != "/weaviate.v1.Weaviate/Search" ||
                request.Content?.Headers.ContentType?.MediaType != "application/grpc")
                throw new InvalidOperationException("Weaviate CQL transport admits only native gRPC searches at its configured origin.");
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class DenyRestHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Weaviate CQL transport has no REST operations.");
    }
}
