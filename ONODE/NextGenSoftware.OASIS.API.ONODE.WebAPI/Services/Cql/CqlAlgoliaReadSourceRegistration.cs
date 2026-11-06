using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.AlgoliaOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlAlgoliaReadSourceRegistration
{
    private const string ClientKey = "cql-algolia-reader";
    private const string ApplicationHeader = "X-Algolia-Application-Id";

    public static void AddAlgoliaReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_ALGOLIA_QUERY_";
        var application = configuration[prefix + "APP_ID"];
        var key = configuration[prefix + "READER_KEY"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (application == null && key == null && probe == null) return;
        if (application is not { Length: > 0 and <= 50 } || application.Any(c => !char.IsAsciiLetterOrDigit(c)) ||
            key is not { Length: > 0 and <= 1024 } || key.Any(c => c <= ' ' || c > '~') ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty || probe != id.ToString("D"))
            throw new InvalidOperationException("Algolia CQL requires an application ID, restricted reader key and canonical probe.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) =>
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            return new HttpClient(new ExactReadHandler(application, transport));
        });
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new AlgoliaQuerySource(
            application, key, id, provider.GetRequiredKeyedService<HttpClient>(ClientKey))));
    }

    internal sealed class ExactReadHandler(string application, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        private readonly Uri origin = new($"https://{application}-dsn.algolia.net");

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            var content = request.Content;
            if (request.Method != HttpMethod.Post || request.Headers.Host != null ||
                uri is not { IsAbsoluteUri: true } || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
                uri.GetLeftPart(UriPartial.Authority) != origin.GetLeftPart(UriPartial.Authority) ||
                uri.AbsolutePath != "/1/indexes/*/objects" ||
                !request.Headers.TryGetValues(ApplicationHeader, out var applications) ||
                !applications.SequenceEqual(new[] { application }, StringComparer.Ordinal) ||
                content?.Headers.ContentType?.MediaType != "application/json" || content.Headers.ContentLength is not (> 0 and <= 8192))
                throw new InvalidOperationException("Algolia CQL admits only bounded native getObjects requests at the reviewed application.");

            using var document = JsonDocument.Parse(await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
            var body = document.RootElement;
            if (body.ValueKind != JsonValueKind.Object || body.EnumerateObject().Count() != 1 ||
                !body.TryGetProperty("requests", out var requests) || requests.ValueKind != JsonValueKind.Array ||
                requests.GetArrayLength() is < 1 or > 100)
                throw new InvalidOperationException("Algolia CQL requires a bounded getObjects batch.");

            var ids = new HashSet<Guid>();
            foreach (var item in requests.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Count() != 2 ||
                    !item.TryGetProperty("indexName", out var index) || index.ValueKind != JsonValueKind.String ||
                    index.GetString() != "oasis_holons" ||
                    !item.TryGetProperty("objectID", out var objectId) || objectId.ValueKind != JsonValueKind.String ||
                    !Guid.TryParseExact(objectId.GetString(), "D", out var id) || id == Guid.Empty ||
                    objectId.GetString() != id.ToString("D") || !ids.Add(id))
                    throw new InvalidOperationException("Algolia CQL requires distinct canonical UUIDs in the teaching index.");
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
