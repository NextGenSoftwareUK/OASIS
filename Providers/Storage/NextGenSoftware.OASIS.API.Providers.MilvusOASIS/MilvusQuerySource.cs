using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;

namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS;

/// <summary>Read access to a reviewed public Milvus teaching collection.</summary>
public sealed class MilvusQuerySource : MilvusOASIS, IDisposable
{
    public MilvusQuerySource(Uri origin, string token, Guid probeId)
        : base(origin?.AbsoluteUri ?? throw new ArgumentNullException(nameof(origin)), token, probeId,
            CreateTransport(origin, token, probeId)) { }

    public static bool IsValidConfiguration(Uri origin, string token, Guid probeId) =>
        origin is { IsAbsoluteUri: true, Scheme: "https", HostNameType: UriHostNameType.Dns, IsDefaultPort: true } &&
        origin.Host.Length <= 253 && origin.Host.Contains('.') && origin.OriginalString == origin.GetLeftPart(UriPartial.Authority) &&
        origin.Host.Split('.').All(label => label.Length is >= 1 and <= 63 && label[0] != '-' && label[^1] != '-' &&
            label.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-')) &&
        char.IsAsciiLetterLower(origin.Host.Split('.')[^1][0]) &&
        origin.AbsolutePath == "/" && origin.UserInfo.Length == 0 && origin.Query.Length == 0 && origin.Fragment.Length == 0 &&
        token is { Length: > 0 and <= 1024 } && token.All(c => c > ' ' && c <= '~') && probeId != Guid.Empty;

    private static HttpMessageHandler CreateTransport(Uri origin, string token, Guid probeId)
    {
        if (!IsValidConfiguration(origin, token, probeId))
            throw new ArgumentException("A reviewed Milvus HTTPS origin, restricted reader and public probe are required.");
        var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        transport.UseProxy = false;
        transport.UseCookies = false;
        return new ExactReadHandler(origin, transport);
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Post || request.Headers.Host != null || uri is not { IsAbsoluteUri: true } ||
                uri.GetLeftPart(UriPartial.Authority) != origin.GetLeftPart(UriPartial.Authority) ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || uri.PathAndQuery != "/v2/vectordb/entities/get" ||
                request.Content?.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidOperationException("Milvus CQL only admits exact entity reads at the reviewed origin.");

            await request.Content.LoadIntoBufferAsync(1024, cancellationToken);
            using var document = JsonDocument.Parse(await request.Content.ReadAsByteArrayAsync(cancellationToken));
            var body = document.RootElement;
            if (body.ValueKind != JsonValueKind.Object || body.EnumerateObject().Count() != 4 ||
                body.GetProperty("collectionName").GetString() != "oasis_holons" ||
                body.GetProperty("consistencyLevel").GetString() != "Strong" ||
                !body.GetProperty("outputFields").EnumerateArray().Select(field => field.GetString())
                    .SequenceEqual(new[] { "id", "doc" }) ||
                body.GetProperty("id") is not { ValueKind: JsonValueKind.Array } keys || keys.GetArrayLength() != 1 ||
                keys[0].GetString() is not string key || !Guid.TryParseExact(key, "D", out var id) ||
                id == Guid.Empty || key != id.ToString("D"))
                throw new InvalidOperationException("Milvus CQL requires one canonical key in the reviewed teaching collection.");

            return await base.SendAsync(request, cancellationToken);
        }
    }

    public void Dispose() => DisposeQueryTransport();
}
