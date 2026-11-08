#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS;

/// <summary>
/// Public versioned metadata from an explicitly owned co-located OpenVINO model server.
/// Queries read metadata through the existing SDK; inference remains a separate native operation.
/// </summary>
public sealed class OpenVinoVersionQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "openvino/owned-models/cosmic_dummy";
    private readonly ModelBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    public OpenVinoVersionQuerySource(Guid owner, IEnumerable<string> versions)
        : this(owner, versions, null) { }

    public static OpenVinoVersionQuerySource ForReviewedTeachingCatalogue(Guid owner) =>
        new(owner, new[] { "1", "2", "3" });

    internal OpenVinoVersionQuerySource(Guid owner, IEnumerable<string> versions, HttpMessageHandler? handler)
        : this(new ModelBackend(owner, versions, handler)) { }

    private OpenVinoVersionQuerySource(ModelBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Owned OpenVINO versioned model metadata";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.AI);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3,
            admittedRecordIds: backend.RecordIds);
    }

    public PType QueryProviderType => PType.IntelOpenVINOOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static Guid LogicalId(Guid owner, string version)
    {
        if (owner == Guid.Empty || version is not ("1" or "2" or "3"))
            throw new ArgumentException("A source owner and reviewed public model version are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.IntelOpenVINOOASIS, Source,
            version);
    }

    public const string ModelName = "cosmic_dummy";
    public const string Endpoint = "http://127.0.0.1:28000/";
    private static readonly JsonElement Pins = LoadPins();
    public static string ServerVersion => Pins.GetProperty("serverVersion").GetString()!;

    private static JsonElement LoadPins()
    {
        using var stream = typeof(OpenVinoVersionQuerySource).Assembly.GetManifestResourceStream("OASIS.Cql.OpenVino.Pins.json")
            ?? throw new InvalidDataException("The reviewed model-server pins are absent.");
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        if (root.GetProperty("modelName").GetString() != ModelName ||
            !root.GetProperty("versions").EnumerateArray().Select(x => x.GetString()).SequenceEqual(new[] { "1", "2", "3" }))
            throw new InvalidDataException("The embedded catalogue differs from its stable source identity.");
        return root.Clone();
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(backend.Probe, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose() { IsProviderActivated = false; backend.Dispose(); }

    private sealed class ModelBackend : IKeyValueBackend, IDisposable
    {
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> versions;
        private readonly HttpMessageHandler handler;
        private readonly IntelOpenVINOOASIS client;
        public Guid Probe { get; }
        public IReadOnlySet<Guid> RecordIds => versions.Keys.ToHashSet();

        public ModelBackend(Guid owner, IEnumerable<string> configured, HttpMessageHandler? handler)
        {
            if (configured is null) throw new ArgumentNullException(nameof(configured));
            var selected = configured.Take(4).ToArray();
            if (selected.Length is < 1 or > 3 || selected.Distinct().Count() != selected.Length)
                throw new ArgumentException("One to three distinct reviewed model versions are required.");
            versions = selected.ToDictionary(version => LogicalId(owner, version));
            this.owner = owner; Probe = LogicalId(owner, selected[0]);
            _ = ServerVersion;
            this.handler = handler ?? new SocketsHttpHandler
                { AllowAutoRedirect = false, UseProxy = false, UseCookies = false, ConnectTimeout = TimeSpan.FromSeconds(5) };
            client = new IntelOpenVINOOASIS(Endpoint, this.handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!versions.TryGetValue(id, out var version))
                throw new InvalidDataException("The ID is outside the owned OpenVINO version catalogue.");
            var server = Required(await client.GetServerMetadataAsync(token));
            if (server.GetProperty("name").GetString() != "OpenVINO Model Server" || server.GetProperty("version").GetString() != ServerVersion)
                throw new InvalidDataException("The native model server identity differs from the reviewed build.");
            var ready = await client.IsModelReadyAsync(ModelName, version, token);
            if (ready.IsError || !ready.Result) throw new InvalidDataException("The selected native model version is not ready.");
            var model = Required(await client.GetModelMetadataAsync(ModelName, version, token));
            if (model.GetProperty("name").GetString() != ModelName || model.GetProperty("platform").GetString() != "OpenVINO")
                throw new InvalidDataException("The native model identity differs.");
            var advertised = model.GetProperty("versions");
            if (advertised.ValueKind != JsonValueKind.Array || advertised.GetArrayLength() is < 1 or > 32)
                throw new InvalidDataException("A bounded native version list is required.");
            var all = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in advertised.EnumerateArray())
            {
                var key = item.GetString();
                if (key is null || !long.TryParse(key, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out var number) || number <= 0 ||
                    number.ToString(System.Globalization.CultureInfo.InvariantCulture) != key || !all.Add(key))
                    throw new InvalidDataException("Distinct canonical native model versions are required.");
            }
            if (!all.Contains(version)) throw new InvalidDataException("The requested version is absent from the native metadata.");
            ValidatePort(model.GetProperty("inputs"), Pins.GetProperty("input"));
            ValidatePort(model.GetProperty("outputs"), Pins.GetProperty("output"));
            var compact = JsonSerializer.Serialize(model);
            var fields = new Dictionary<string, object>
            {
                ["name"] = "OpenVINO " + ModelName + " version " + version,
                ["OpenVinoModelName"] = model.GetProperty("name").GetString()!,
                ["OpenVinoRequestedVersion"] = version,
                ["OpenVinoAdvertisedVersionsJson"] = JsonSerializer.Serialize(advertised),
                ["OpenVinoServerName"] = server.GetProperty("name").GetString()!,
                ["OpenVinoServerVersion"] = server.GetProperty("version").GetString()!,
                ["OpenVinoPlatform"] = model.GetProperty("platform").GetString()!,
                ["OpenVinoMetadataJson"] = compact,
                ["OpenVinoMetadataJsonSha256"] = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(compact)))
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.IntelOpenVINOOASIS,
                new NativeRecord(Source, version, fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner; row.IsPublic = true; row.IsActive = true; row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.IntelOpenVINOOASIS] = version;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        private static JsonElement Required(OASISResult<JsonElement> result)
        {
            if (result.IsError || result.Result.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Native SDK metadata failed: " + result.Message);
            return result.Result;
        }

        private static void ValidatePort(JsonElement ports, JsonElement expected)
        {
            if (ports.ValueKind != JsonValueKind.Array || ports.GetArrayLength() != 1)
                throw new InvalidDataException("The reviewed native tensor port is required.");
            var port = ports[0];
            if (port.GetProperty("name").GetString() != expected.GetProperty("name").GetString() ||
                port.GetProperty("datatype").GetString() != expected.GetProperty("datatype").GetString() ||
                port.GetProperty("shape").ValueKind != JsonValueKind.Array ||
                !port.GetProperty("shape").EnumerateArray().Select(x => x.GetInt64()).SequenceEqual(
                    expected.GetProperty("shape").EnumerateArray().Select(x => x.GetInt64())))
                throw new InvalidDataException("Native tensor names, datatypes or shapes differ from the reviewed model.");
        }
        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact OpenVINO version CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Model scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Model metadata observations are read-only."));
        public void Dispose() { client.Dispose(); handler.Dispose(); }
    }
}
