using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS;

/// <summary>Bounded public queries over a caller-owned Couchbase Query Service HTTP transport.</summary>
public sealed class CouchbaseQuerySource : CouchbaseOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly HttpClient _transport;
    private readonly Uri _endpoint;
    private readonly AuthenticationHeaderValue _authentication;
    private readonly string _statement;
    private readonly Guid _probe;

    public CouchbaseQuerySource(Uri origin, string bucket, string username, string password, HttpClient transport, Guid probe)
        : base(bucket)
    {
        if (origin is not { IsAbsoluteUri: true } || origin.Scheme != Uri.UriSchemeHttps || origin.UserInfo.Length != 0 ||
            origin.AbsolutePath != "/" || origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new ArgumentException("Explicit HTTPS Couchbase Query Service origin required.", nameof(origin));
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (username.Contains(':')) throw new ArgumentException("Invalid Basic authentication username.", nameof(username));
        if (probe == Guid.Empty) throw new ArgumentException("An existing teaching probe is required.", nameof(probe));
        _endpoint = new Uri(origin, "/query/service");
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _authentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password)));
        _statement = CouchbaseCqlCompiler.NativeIdentityStatement(bucket);
        _probe = probe;
    }

    protected override bool RequiresPublicCqlRecords => true;

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            var verified = false;
            await foreach (var record in ReadNativeCqlAsync(new[] { _probe }, cancellationToken))
            {
                if (verified || record.Key != _probe.ToString("D") || record.Id != record.Key)
                    throw new InvalidDataException("Couchbase did not return exactly the configured probe.");
                var row = ReadQueryRecord(record, _probe);
                if (record.IsDeleted || row.IsDeleted || !row.IsPublic)
                    throw new InvalidDataException("The teaching probe must be an existing public record.");
                verified = true;
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (!verified) throw new InvalidDataException("The teaching probe was missing.");
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception cause)
        {
            return new()
            {
                IsError = true, ErrorCount = 1,
                ErrorCode = cause is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "The native Couchbase query source did not verify its teaching probe.",
                Exception = cause
            };
        }
    }

    protected override async IAsyncEnumerable<NativeCqlDocument> ReadNativeCqlAsync(
        IReadOnlyList<Guid> ids, [EnumeratorCancellation] CancellationToken cancellation)
    {
        if (ids.Count is < 1 or > 100 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Count)
            throw new ArgumentException("One to one hundred distinct canonical Couchbase identities are required.", nameof(ids));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["statement"] = _statement,
                ["readonly"] = true,
                ["timeout"] = "15s",
                ["$keys"] = ids.Select(id => id.ToString("D")).ToArray()
            }), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = _authentication;
        using var response = await _transport.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        response.EnsureSuccessStatusCode();
        await response.Content.LoadIntoBufferAsync(8 * 1024 * 1024, deadline.Token);
        await using var body = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: deadline.Token);
        var root = json.RootElement;
        if (root.GetProperty("status").GetString() != "success" ||
            (root.TryGetProperty("errors", out var errors) && errors.GetArrayLength() != 0))
            throw new InvalidDataException("Native Couchbase query did not complete successfully.");
        var rows = root.GetProperty("results");
        if (rows.GetArrayLength() > ids.Count)
            throw new InvalidDataException("Native Couchbase returned more records than requested.");
        foreach (var item in rows.EnumerateArray())
        {
            deadline.Token.ThrowIfCancellationRequested();
            yield return new NativeCqlDocument
            {
                Key = item.GetProperty("Key").GetString() ?? throw new InvalidDataException("Missing Couchbase document key."),
                Id = item.GetProperty("Id").GetString() ?? throw new InvalidDataException("Missing Couchbase payload identity."),
                IsDeleted = item.GetProperty("IsDeleted").GetBoolean(),
                DataJson = item.GetProperty("DataJson").GetString() ?? throw new InvalidDataException("Missing Couchbase payload.")
            };
        }
    }
}
