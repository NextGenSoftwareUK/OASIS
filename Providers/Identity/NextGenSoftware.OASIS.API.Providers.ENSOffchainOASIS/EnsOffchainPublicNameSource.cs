using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ENSOffchainOASIS;

/// <summary>Read-only CQL projection of explicitly enrolled ENS names resolved through the
/// public web3.bio name-service endpoint. No wallet, signing, writes or name search.</summary>
public sealed class EnsOffchainPublicNameSource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string NameMetadata = "ENSOffchainName";
    public const string AddressMetadata = "ENSOffchainAddress";
    public const string DisplayNameMetadata = "ENSOffchainDisplayName";
    public static readonly Uri Origin = new("https://api.web3.bio/");

    private readonly NameBackend backend;
    private bool disposed;
    private HttpClient? ownedClient;

    public EnsOffchainPublicNameSource(HttpClient http, Uri origin, Guid owner, IEnumerable<string> names)
        : this(new NameBackend(http, origin, owner, names)) { }

    private EnsOffchainPublicNameSource(NameBackend backend) : base(backend, NameBackend.Prefix)
    {
        this.backend = backend;
        ProviderType = new EnumValue<ProviderType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Read-only ENS off-chain name profiles";
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
    }

    /// <summary>Exact schema-1 configuration: {"schema":1,"origin":"https://api.web3.bio/","sourceOwner":guid,"names":[...]}.</summary>
    public static EnsOffchainPublicNameSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded ENS off-chain configuration required.", nameof(json));
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        var fields = new HashSet<string>(new[] { "schema", "origin", "sourceOwner", "names" }, StringComparer.Ordinal);
        if (root.ValueKind != JsonValueKind.Object ||
            root.EnumerateObject().Any(property => !fields.Remove(property.Name)) || fields.Count != 0 ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1)
            throw new ArgumentException("Exact ENS off-chain configuration required; credentials are not accepted.", nameof(json));
        var origin = new Uri(root.GetProperty("origin").GetString()!, UriKind.Absolute);
        var owner = root.GetProperty("sourceOwner").GetGuid();
        var names = root.GetProperty("names").EnumerateArray().Select(item => item.GetString()!).Take(9).ToArray();
        var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler());
        try
        {
            return new EnsOffchainPublicNameSource(client, origin, owner, names) { ownedClient = client };
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public bool IsQuerySourceActivated => IsProviderActivated && !disposed;

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            foreach (var name in backend.Keys)
                await backend.Client.ReadAsync(name, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "ENS off-chain activation failed.", Exception = error };
        }
    }

    public void Dispose()
    {
        disposed = true;
        IsProviderActivated = false;
        ownedClient?.Dispose();
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.ENSOffchainOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; } =
        CosmicQueryCapabilities.ForStringMetadataKeyLookup(NameMetadata, 68, 8);

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
            return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit ENS off-chain source is required.");
        if (maximumResults is < 1 or > 10000)
            return Failure("CQL_QUERY_LIMIT", "A bounded result budget is required.");
        if (disposed || !IsProviderActivated)
            return Failure("CQL_PROVIDER_INACTIVE", "Activate the ENS off-chain source first.");
        var admitted = QueryCapabilities.AdmitPlan(plan);
        if (admitted.IsError)
            return Failure(admitted.ErrorCode, admitted.Message);

        IReadOnlyList<string> keys;
        if (QueryCapabilities.StringKeySetLookup.IsKeySetQuery(plan.Query))
        {
            var selected = QueryCapabilities.StringKeySetLookup.SelectKeys(plan.Query);
            if (selected.IsError) return Failure(selected.ErrorCode, selected.Message);
            keys = selected.Result;
        }
        else
        {
            var selected = QueryCapabilities.StringPointLookup.SelectKey(plan.Query);
            if (selected.IsError) return Failure(selected.ErrorCode, selected.Message);
            keys = new[] { selected.Result };
        }
        if (keys.Any(key => !backend.Keys.Contains(key, StringComparer.Ordinal)))
            return Failure("CQL_INVALID_SOURCE_KEY", "Select names from the enrolled ENS off-chain catalogue.");

        try
        {
            var rows = new List<IHolon>();
            foreach (var name in keys.OrderBy(key => key, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                rows.Add(backend.Map(await backend.Client.ReadAsync(name, cancellationToken)));
            }
            var page = rows.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            cancellationToken.ThrowIfCancellationRequested();
            return page.Length > maximumResults
                ? Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.")
                : new(Array.AsReadOnly(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException error)
        {
            return Failure("CQL_QUERY_CANCELLED", "ENS off-chain query cancelled.", error);
        }
        catch (Exception error)
        {
            return Failure("CQL_QUERY_FAILED", "ENS off-chain query failed.", error);
        }
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception? error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    internal sealed record NameProfile(string Name, string Address, string DisplayName);

    /// <summary>One bounded GET per name; the response must describe exactly that ENS identity.</summary>
    internal sealed class NameClient(HttpClient http, Uri origin)
    {
        private const int MaximumBodyBytes = 16 * 1024;
        private static readonly Regex Address = new(@"\A0x[0-9a-f]{40}\z", RegexOptions.CultureInvariant);

        public async Task<NameProfile> ReadAsync(string name, CancellationToken cancellationToken)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            using var response = await http.GetAsync(new Uri(origin, "ns/ens/" + name),
                HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if ((int)response.StatusCode != 200)
                throw new InvalidDataException("ENS off-chain endpoint returned HTTP " + (int)response.StatusCode + ".");
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            var buffer = new byte[MaximumBodyBytes + 1];
            var size = await stream.ReadAtLeastAsync(buffer, buffer.Length, false, deadline.Token);
            if (size > MaximumBodyBytes)
                throw new InvalidDataException("ENS off-chain response exceeded its budget.");
            using var json = JsonDocument.Parse(buffer.AsMemory(0, size), new JsonDocumentOptions { MaxDepth = 6 });
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("identity", out var identity) || identity.GetString() != name ||
                !root.TryGetProperty("platform", out var platform) || platform.GetString() != "ens" ||
                !root.TryGetProperty("address", out var address) || address.ValueKind != JsonValueKind.String ||
                !Address.IsMatch(address.GetString()!))
                throw new InvalidDataException("ENS off-chain response does not describe the requested name.");
            var display = root.TryGetProperty("displayName", out var shown) && shown.ValueKind == JsonValueKind.String
                ? shown.GetString()! : name;
            return new NameProfile(name, address.GetString()!, display.Length > 128 ? display[..128] : display);
        }
    }

    private sealed class NameBackend : IKeyValueBackend
    {
        public const string Prefix = "ens-offchain-names/";
        private static readonly Regex EnsName = new(@"\A[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.eth\z", RegexOptions.CultureInvariant);
        private readonly Guid owner;
        private readonly string sourceName;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> Keys { get; }
        public NameClient Client { get; }

        public NameBackend(HttpClient http, Uri origin, Guid owner, IEnumerable<string> names)
        {
            ArgumentNullException.ThrowIfNull(http);
            ArgumentNullException.ThrowIfNull(origin);
            ArgumentNullException.ThrowIfNull(names);
            if (origin != Origin || owner == Guid.Empty)
                throw new ArgumentException("The public web3.bio origin and a source owner are required.");
            var enrolled = names.Take(9).ToArray();
            if (enrolled.Length is < 1 or > 8 || enrolled.Any(name => name == null || !EnsName.IsMatch(name)) ||
                enrolled.Distinct(StringComparer.Ordinal).Count() != enrolled.Length)
                throw new ArgumentException("One to eight distinct lowercase .eth names are required.");
            Keys = Array.AsReadOnly(enrolled);
            this.owner = owner;
            sourceName = "ens-offchain:" + origin.AbsoluteUri;
            Client = new(http, origin);
            records = Keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, Core.Enums.ProviderType.ENSOffchainOASIS, sourceName, key).ToString("N"),
                key => key, StringComparer.Ordinal);
        }

        public IHolon Map(NameProfile profile)
        {
            var fields = new Dictionary<string, object> {
                ["name"] = profile.Name, ["description"] = "Public ENS name profile via web3.bio; no wallet or writes.",
                [NameMetadata] = profile.Name, [AddressMetadata] = profile.Address, [DisplayNameMetadata] = profile.DisplayName };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, Core.Enums.ProviderType.ENSOffchainOASIS,
                new NativeRecord(sourceName, profile.Name, fields), NativeDataMapping.ForSource(sourceName));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[Core.Enums.ProviderType.ENSOffchainOASIS] = profile.Name;
            return row;
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            foreach (var name in Keys) await Client.ReadAsync(name, cancellationToken);
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var name)) return null!;
            return StoredRecordJson.Serialize<IHolon>(Map(await Client.ReadAsync(name, cancellationToken)));
        }

        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray());
        }

        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("ENS off-chain names are read-only."));

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("ENS off-chain names are read-only."));
    }
}
