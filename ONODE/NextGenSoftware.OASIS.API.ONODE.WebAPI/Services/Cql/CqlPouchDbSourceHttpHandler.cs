using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Every read belongs to the configured owned native PouchDB dataset.</summary>
public sealed class CqlPouchDbSourceHttpHandler : DelegatingHandler
{
    private readonly string digest;
    internal static bool IsDigest(string? value) => value is { Length: 64 } && value.All(char.IsAsciiHexDigitLower);
    public CqlPouchDbSourceHttpHandler(string digest, HttpMessageHandler transport) : base(transport)
    {
        if (!IsDigest(digest))
            throw new ArgumentException("Explicit PouchDB source digest required.", nameof(digest));
        this.digest = digest;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        bool Exact(string name, string expected) => response.Headers.TryGetValues(name, out var values)
            && values.SequenceEqual(new[] { expected });
        if (!Exact("X-OASIS-PouchDB-Source", digest) || !Exact("X-OASIS-PouchDB-Version", "9.0.0"))
        {
            response.Dispose();
            throw new InvalidDataException("Native PouchDB response does not belong to the configured source/version.");
        }
        return response;
    }
}
