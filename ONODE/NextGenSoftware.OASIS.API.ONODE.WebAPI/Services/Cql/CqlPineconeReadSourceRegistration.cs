using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.PineconeOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlPineconeReadSourceRegistration
{
    private const string ClientKey = "cql-pinecone-reader";
    private const string Uuid = "[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}";
    private static readonly Regex FetchQuery = new(
        @"\A\?namespace=holons(?:&ids=" + Uuid + @"){1,100}\z",
        RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static void AddPineconeReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_PINECONE_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var key = configuration[prefix + "READER_KEY"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (endpoint == null && key == null && probe == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.HostNameType != UriHostNameType.Dns || origin.Host.Length > 253 ||
            !origin.Host.Contains(".svc.", StringComparison.Ordinal) || !origin.Host.EndsWith(".pinecone.io", StringComparison.Ordinal) ||
            origin.Host.Split('.').Any(label => label.Length is < 1 or > 63 || label[0] == '-' || label[^1] == '-' ||
                label.Any(c => !(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))) ||
            origin.AbsolutePath != "/" || !origin.IsDefaultPort || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0 || endpoint != origin.GetLeftPart(UriPartial.Authority) ||
            key is not { Length: > 0 and <= 1024 } || key.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)) ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty || probe != id.ToString("D"))
            throw new InvalidOperationException("Pinecone CQL requires a canonical index origin, restricted reader key and probe.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) =>
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            return new HttpClient(new ExactReadHandler(origin, transport));
        });
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new PineconeQuerySource(
            provider.GetRequiredKeyedService<HttpClient>(ClientKey), origin, key, id)));
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || request.Content != null || request.Headers.Host != null ||
                uri is not { IsAbsoluteUri: true } || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.AbsolutePath != "/vectors/fetch" || uri.Query.Length > 4200 || !FetchQuery.IsMatch(uri.Query))
                throw new InvalidOperationException("Pinecone CQL admits only bounded native fetches at the reviewed index.");
            var keys = uri.Query.Split('&').Skip(1).Select(pair => Guid.ParseExact(pair.AsSpan(4), "D")).ToArray();
            if (keys.Contains(Guid.Empty) || keys.Distinct().Count() != keys.Length)
                throw new InvalidOperationException("Pinecone CQL requires distinct nonempty UUIDs.");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
