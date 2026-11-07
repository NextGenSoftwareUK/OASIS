using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client.Core;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NATSJetStreamOASIS;

/// <summary>Explicit public records in an existing JetStream bucket.</summary>
public sealed class NatsPublicCqlProfile : NATSJetStreamOASIS, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string OwnedEndpoint = "tls://127.0.0.1:24222";
    public const string OwnedBucket = "cosmic_cql_public";
    public const string OwnedReader = "cql_reader";
    public const string OwnedCaFile = "/data/COSMIC/SandboxNATS/ca.pem";
    private readonly HashSet<Guid> catalogue;
    private readonly Guid owner;
    private bool disposed;

    public NatsPublicCqlProfile(string url, string bucket, Guid owner, IEnumerable<Guid> ids)
        : this(url, bucket, owner, ids, null) { }

    private NatsPublicCqlProfile(string url, string bucket, Guid owner, IEnumerable<Guid> ids, NatsAuthOpts? auth,
        NatsTlsOpts? tls = null)
        : base(url, bucket, readOnly: true, auth, tls)
    {
        var selected = ids.Take(101).ToArray();
        catalogue = new(selected);
        if (owner == Guid.Empty || selected.Length is < 1 or > 100 || catalogue.Contains(Guid.Empty) ||
            catalogue.Count != selected.Length)
            throw new ArgumentException("One to 100 distinct public NATS record identities and an owner are required.");
        this.owner = owner;
    }

    public static NatsPublicCqlProfile FromJson(string json, string? username = null, string? password = null)
        => Parse(json, username, password, ownedSandbox: false);

    public static NatsPublicCqlProfile FromOwnedSandboxJson(string json, string username, string password, bool sandboxEnabled)
    {
        if (!sandboxEnabled)
            throw new InvalidOperationException("The owned NATS source requires the enabled OASIS sandbox.");
        return Parse(json, username, password, ownedSandbox: true);
    }

    private static NatsPublicCqlProfile Parse(string json, string? username, string? password, bool ownedSandbox)
    {
        if (Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded NATS profile configuration required.", nameof(json));
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        var fields = new HashSet<string>(new[] { "schema", "endpoint", "bucket", "sourceOwner", "ids" }, StringComparer.Ordinal);
        if (root.ValueKind != JsonValueKind.Object ||
            root.EnumerateObject().Any(property => !fields.Remove(property.Name)) || fields.Count != 0 ||
            root.GetProperty("schema").GetInt32() != 1)
            throw new ArgumentException("Exact NATS profile configuration required.", nameof(json));

        var endpoint = root.GetProperty("endpoint").GetString();
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != "tls" ||
            uri.Host.Length == 0 || (ownedSandbox ? endpoint != OwnedEndpoint : uri.IsLoopback) || uri.UserInfo.Length != 0 ||
            uri.AbsolutePath is not ("" or "/") || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            endpoint!.Contains(','))
            throw new ArgumentException("One explicit TLS NATS endpoint is required.", nameof(json));
        var bucket = root.GetProperty("bucket").GetString();
        if (string.IsNullOrEmpty(bucket) || bucket.Length > 64 ||
            bucket.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-')) ||
            ownedSandbox && bucket != OwnedBucket)
            throw new ArgumentException("An explicit NATS key-value bucket is required.", nameof(json));
        var ids = root.GetProperty("ids").EnumerateArray().Take(101).Select(item => item.GetGuid()).ToArray();
        if ((username == null) != (password == null) ||
            username != null && (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)))
            throw new ArgumentException("NATS source credentials require both a username and password.");
        if (ownedSandbox && (username != OwnedReader || password == null || password.Length != 43 ||
                password.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-'))))
            throw new ArgumentException("The owned NATS source requires its explicit bounded reader credentials.");
        var auth = username == null ? null : new NatsAuthOpts { Username = username, Password = password };
        var tls = ownedSandbox ? new NatsTlsOpts { Mode = TlsMode.Require, CaFile = OwnedCaFile } : null;
        return new NatsPublicCqlProfile(endpoint, bucket, root.GetProperty("sourceOwner").GetGuid(), ids, auth, tls);
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await Backend.VerifyAsync(cancellationToken);
            foreach (var id in catalogue)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = await Backend.GetAsync(RecordKey("holon", id), cancellationToken);
                var row = json == null ? null : OasisJson.DeserializeStored<Holon>(json);
                if (row == null || row.Id != id || !IsPublicRecord(row))
                    throw new InvalidOperationException("An enrolled NATS row is missing or has changed identity or visibility.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            await DeActivateProviderAsync();
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "NATS public profile activation failed.", Exception = error };
        }
    }

    public override Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100, CancellationToken cancellationToken = default)
    {
        var selected = BoundedIdentityCqlCompiler.Compile(plan?.Query, QueryProviderType, QueryProviderType);
        if (selected.IsError)
            return Task.FromResult(Failure(selected.ErrorCode, selected.Message));
        if (selected.Result.Any(id => !catalogue.Contains(id)))
            return Task.FromResult(Failure("CQL_INVALID_SOURCE_KEY", "Only enrolled NATS identities are available."));
        return base.ExecuteCosmicQueryAsync(plan, maximumResults, cancellationToken);
    }

    protected override bool IsCqlRecordVisible(IHolon row) => IsPublicRecord(row);

    private bool IsPublicRecord(IHolon row) => row.IsPublic && !row.IsDeleted && row.CreatedByAvatarId == owner;

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        DeActivateProviderAsync().GetAwaiter().GetResult();
    }
}
