using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.ChainflipOASIS;

/// <summary>
/// Anonymous completed historical swap observations over a bounded ID catalogue.
/// Never quotes, creates deposit addresses, signs or dispatches swaps.
/// </summary>
public sealed class ChainflipPublicSwapMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string AddressMetadata = "ChainflipSwapId";
    private readonly SwapsBackend swaps;
    private bool disposed;

    public ChainflipPublicSwapMetadataSource(HttpClient client, Uri rest,
        IEnumerable<string> addresses, Guid sourceOwner, bool ownsClient = false)
        : this(new SwapsBackend(client, rest, addresses, sourceOwner, ownsClient)) { }

    private ChainflipPublicSwapMetadataSource(SwapsBackend backend) : base(backend, backend.Prefix)
    {
        swaps = backend;
        ProviderName = PType.ChainflipOASIS.ToString();
        ProviderDescription = "Anonymous completed historical Chainflip swap observations";
        ProviderType = new EnumValue<PType>(PType.ChainflipOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        QueryCapabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup(AddressMetadata, 20, 8);
    }

    public PType QueryProviderType => PType.ChainflipOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; }

    /// <summary>Separate host query-profile configuration, never the existing
    /// cross-chain tracker settings. Reject unknown fields rather than ignore keys.</summary>
    public static ChainflipPublicSwapMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 8192)
            throw new ArgumentException("Bounded explicit Chainflip public source required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        Unique(document.RootElement);
        var root = document.RootElement;
        var fields = new[] { "schema", "apiUrl", "swapIds", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            root.EnumerateObject().Any(property => !fields.Contains(property.Name, StringComparer.Ordinal)) ||
            root.GetProperty("schema").ValueKind != JsonValueKind.Number || root.GetProperty("schema").GetInt32() != 1 ||
            root.GetProperty("apiUrl").ValueKind != JsonValueKind.String ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String || root.GetProperty("swapIds").ValueKind != JsonValueKind.Array ||
            root.GetProperty("swapIds").GetArrayLength() is < 1 or > 8)
            throw new ArgumentException("Exact anonymous Chainflip source schema required; signer/billing fields forbidden.");
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            return new ChainflipPublicSwapMetadataSource(client, new Uri(root.GetProperty("apiUrl").GetString(), UriKind.Absolute),
                root.GetProperty("swapIds").EnumerateArray().Select(value => value.GetString()),
                root.GetProperty("sourceOwner").GetGuid(), true);
        }
        catch { client.Dispose(); throw; }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await swaps.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Chainflip swap source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Chainflip source is required.");
            if (maximumResults is < 1 or > 10000) return Failure("CQL_QUERY_LIMIT", "A bounded result budget is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Failure(admitted.ErrorCode, admitted.Message);
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
            if (keys.Any(key => !swaps.Addresses.Contains(key, StringComparer.Ordinal)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select swapIds from the configured swap catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Activate the configured public source first.");
            var native = await swaps.ReadAsync(keys, cancellationToken);
            var rows = native.Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? native.Count).ToArray();
            if (rows.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Chainflip query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Chainflip swap query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; IsProviderActivated = false; swaps.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!seen.Add(property.Name)) throw new InvalidDataException("Duplicate native/configuration property.");
                Unique(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var entry in value.EnumerateArray()) Unique(entry);
    }



    private static void SwapId(string value)
    {
        if (value == null || value.Length > 20 ||
            !ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ||
            id.ToString(CultureInfo.InvariantCulture) != value)
            throw new ArgumentException("Canonical uint64 swap identity required.");
    }

    private static string Amount(JsonElement value)
    {
        string text = Text(value, 78);
        if (!Regex.IsMatch(text, @"\A(?:0|[1-9][0-9]{0,77})\z"))
            throw new InvalidDataException("Bounded canonical decimal quantity text required.");
        return text;
    }

    private static string Milliseconds(JsonElement value)
    {
        long reported = value.GetInt64();
        if (reported < 0) throw new InvalidDataException("Nonnegative reported millisecond integer required.");
        return reported.ToString(CultureInfo.InvariantCulture);
    }

    private static Dictionary<string, object> Decode(JsonElement root, string id)
    {
        if (Text(root.GetProperty("swapId"), 20) != id || Text(root.GetProperty("state"), 64) != "COMPLETED")
            throw new InvalidDataException("Exact completed historical swap observation required.");
        string sourceChain = Text(root.GetProperty("srcChain"), 64), sourceAsset = Text(root.GetProperty("srcAsset"), 64);
        string destinationChain = Text(root.GetProperty("destChain"), 64), destinationAsset = Text(root.GetProperty("destAsset"), 64);
        var swap = root.GetProperty("swap");
        var fields = new Dictionary<string, object> {
            [AddressMetadata] = id, ["ChainflipSwapState"] = "COMPLETED",
            ["ChainflipSourceChain"] = sourceChain, ["ChainflipSourceAsset"] = sourceAsset,
            ["ChainflipDestinationChain"] = destinationChain, ["ChainflipDestinationAsset"] = destinationAsset,
            ["ChainflipOriginalInputAmount"] = Amount(swap.GetProperty("originalInputAmount")),
            ["ChainflipRemainingInputAmount"] = Amount(swap.GetProperty("remainingInputAmount")),
            ["ChainflipSwappedInputAmount"] = Amount(swap.GetProperty("swappedInputAmount")),
            ["ChainflipSwappedIntermediateAmount"] = Amount(swap.GetProperty("swappedIntermediateAmount")),
            ["ChainflipSwappedOutputAmount"] = Amount(swap.GetProperty("swappedOutputAmount")),
            ["ChainflipLastStatechainUpdateAtMilliseconds"] = Milliseconds(root.GetProperty("lastStatechainUpdateAt")),
            ["ChainflipObservationPolicy"] = "indexer-reported-swap-state", ["name"] = id,
            ["description"] = $"Indexed Chainflip swap {sourceAsset}/{sourceChain} to {destinationAsset}/{destinationChain} (COMPLETED)"
        };
        // A protocol swap can omit an external deposit. Model absence explicitly;
        // never substitute a swap quantity or fabricated zero as a deposit amount.
        bool depositPresent = root.TryGetProperty("deposit", out var deposit);
        fields["ChainflipExternalDepositObserved"] = depositPresent ? "true" : "false";
        if (depositPresent)
        {
            if (deposit.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Present deposit must be complete.");
            fields["ChainflipDepositAmount"] = Amount(deposit.GetProperty("amount"));
            fields["ChainflipDepositTxRef"] = Text(deposit.GetProperty("txRef"), 256);
            fields["ChainflipDepositWitnessedAtMilliseconds"] = Milliseconds(deposit.GetProperty("witnessedAt"));
        }
        bool refundPresent = root.TryGetProperty("refundEgress", out var refund);
        fields["ChainflipRefundObserved"] = refundPresent ? "true" : "false";
        if (refundPresent)
        {
            if (refund.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Present refund must be complete.");
            fields["ChainflipRefundAmount"] = Amount(refund.GetProperty("amount"));
        }
        return fields;
    }

    private static string Text(JsonElement value, int maximumBytes)
    {
        if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException("Native string required.");
        string text = value.GetString();
        if (text.Length == 0 || Encoding.UTF8.GetByteCount(text) > maximumBytes || text.Any(c => c < 33 || c > 126)) throw new InvalidDataException("Native text exceeds its bound.");
        return text;
    }

    private sealed class SwapsBackend : IKeyValueBackend, IDisposable
    {
        private const string SourceName = "chainflip-public-completed-swaps";
        private readonly HttpClient client;
        private readonly Uri api;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        public IReadOnlyList<string> Addresses { get; }
        public string Prefix => "chainflip-public-metadata/";

        public SwapsBackend(HttpClient client, Uri api, IEnumerable<string> swapIds, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (api == null || api.Scheme != Uri.UriSchemeHttps || api.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) ||
                client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie") ||
                owner == Guid.Empty)
                throw new ArgumentException("Explicit anonymous HTTPS swap source required.");
            var keys = swapIds?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(swapIds));
            if (keys.Length is < 1 or > 8 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("One to eight unique swaps required.");
            foreach (string key in keys) SwapId(key);
            this.client = client; this.api = api; this.owner = owner; this.ownsClient = ownsClient;
            Addresses = Array.AsReadOnly(keys);
            records = keys.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, PType.ChainflipOASIS, SourceName, key).ToString("N"), key => key);
        }

        public async Task<IReadOnlyList<IHolon>> ReadAsync(IReadOnlyList<string> keys, CancellationToken token)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var rows = new List<IHolon>();
            long totalBytes = 0;
            foreach (string swapId in keys)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    new Uri(api, "v2/swaps/" + swapId));
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
                using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
                while ((count = await stream.ReadAsync(buffer.AsMemory(), deadline.Token)) != 0)
                {
                    totalBytes += count;
                    if (body.Length + count > 262144 || totalBytes > 1048576)
                        throw new InvalidDataException("Native response byte budget exceeded.");
                    body.Write(buffer, 0, count);
                }
                using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
                var root = document.RootElement;
                Unique(root);
                if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("error", out _) || root.TryGetProperty("message", out _))
                    throw new InvalidDataException("Complete swap response required.");
                var fields = Decode(root, swapId);
                var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.ChainflipOASIS,
                    new NativeRecord(SourceName, swapId, fields), NativeDataMapping.ForSource(SourceName));
                if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
                var row = mapped.Result;
                row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[PType.ChainflipOASIS] = swapId;
                rows.Add(row);
            }
            deadline.Token.ThrowIfCancellationRequested();
            return Array.AsReadOnly(rows.ToArray());
        }

        public async Task VerifyAsync(CancellationToken token = default)
        {
            var rows = await ReadAsync(Addresses, token);
            if (rows.Count != Addresses.Count) throw new InvalidDataException("Configured swapIds are missing.");
        }
        public async Task<string> GetAsync(string key, CancellationToken token = default)
        {
            if (!records.TryGetValue(key, out var did)) return null;
            var rows = await ReadAsync(new[] { did }, token);
            return StoredRecordJson.Serialize<IHolon>(rows[0]);
        }
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }
        public Task PutAsync(string key, string value, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Chainflip swap metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken token = default)
            => Task.FromException(new NotSupportedException("Public Chainflip swap metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
