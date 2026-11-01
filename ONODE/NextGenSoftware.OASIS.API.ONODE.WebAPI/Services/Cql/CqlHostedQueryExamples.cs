using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public sealed record CqlHostedQueryExample(string Id, string Label, JsonElement Request,
    string Operation = "Rows", bool Available = true, string ErrorCode = null);

public sealed record CqlDemoMappedIdentity(Guid Id, string NativeKey, IReadOnlyDictionary<string, string> Metadata);
public sealed record CqlDemoIdentityResult(Guid OwnerId, string Provider, IReadOnlyList<CqlDemoMappedIdentity> Records);

/// <summary>Pure owner-scoped identity mapping. No source reads, storage writes,
/// permission grants or client copy of the Core native-import identity algorithm.</summary>
public static class CqlHostedDemoIdentities
{
    public const string SourceName = "cosmic-cql-demo";

    public static CqlDemoIdentityResult Create(CqlDemoIdentityRequest request, Guid actor)
    {
        var provider = CqlV1Compiler.RequestedProvider(request?.Provider, true)
            ?? throw new ArgumentException("A concrete configured-source profile is required.");
        // Azure dataset blob names can exceed 128 characters. Match the source's
        // declared bound without expanding other providers' identity contracts.
        var maximumKeyLength = provider == ProviderType.AzureStorageOASIS ? 512 : 128;
        if (actor == Guid.Empty || request?.Keys == null || request.Keys.Length is < 1 or > 2
            || request.Keys.Any(key => string.IsNullOrEmpty(key) || key.Length > maximumKeyLength || key.Any(c => c < 33 || c > 126))
            || request.Keys.Distinct(StringComparer.Ordinal).Count() != request.Keys.Length)
            throw new ArgumentException("One or two bounded unique native keys and an authenticated owner are required.");
        return new(actor, provider.ToString(), Array.AsReadOnly(request.Keys.Select(key => new CqlDemoMappedIdentity(
            NativeRecordHolonMapper.StableHolonId(actor, provider, SourceName, key), key,
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                [NativeRecordHolonMapper.ProviderMetaDataKey] = provider.ToString(),
                [NativeRecordHolonMapper.SourceMetaDataKey] = SourceName,
                [NativeRecordHolonMapper.KeyMetaDataKey] = key
            }))).ToArray()));
    }
}

/// <summary>Public demo requests, never authorization or credentials. Captured server
/// configuration owns examples; each must still pass native capability planning.</summary>
public sealed class CqlHostedQueryExamples
{
    public const string ConfigurationKey = "CQL_HOSTED_QUERY_EXAMPLES_JSON";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly Guid ExampleActor = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Dictionary<ProviderType, IReadOnlyList<CqlHostedQueryExample>> examples = new();

    // Partition large catalogues by provider so each deployment variable remains
    // bounded. The existing shared catalogue and named sections compose strictly:
    // conflicting ownership is an error, never an override or fallback.
    public const string ProviderConfigurationSection = "CQL_HOSTED_QUERY_EXAMPLES";

    public static CqlHostedQueryExamples FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var shared = configuration[ConfigurationKey];
        var result = FromJson(shared);
        var children = configuration.GetSection(ProviderConfigurationSection).GetChildren().Take(129).ToArray();
        if (children.Length > 128)
            throw new ArgumentException("A bounded provider example catalogue is required.");
        long total = shared?.Length ?? 0;
        foreach (var child in children)
        {
            if (child.Value == null || child.GetChildren().Any())
                throw new ArgumentException("Each provider example section must contain one JSON request array.");
            total += child.Key.Length + child.Value.Length;
            if (total > 262144)
                throw new ArgumentException("Combined CQL example configuration exceeds its work budget.");
            var partition = FromJson("{" + JsonSerializer.Serialize(child.Key) + ":" + child.Value + "}");
            foreach (var pair in partition.examples)
                if (!result.examples.TryAdd(pair.Key, pair.Value))
                    throw new ArgumentException("A provider's CQL examples must have exactly one configuration owner.");
        }
        return result;
    }

    public static CqlHostedQueryExamples FromJson(string json)
    {
        var result = new CqlHostedQueryExamples();
        if (json == null) return result;
        if (json.Length is < 2 or > 262144) throw new ArgumentException("Bounded public CQL example configuration required.");
        using var document = JsonDocument.Parse(json);
        Unique(document.RootElement);
        if (document.RootElement.ValueKind != JsonValueKind.Object || document.RootElement.EnumerateObject().Count() > 128)
            throw new ArgumentException("Examples require a bounded provider object.");
        foreach (var entry in document.RootElement.EnumerateObject())
        {
            if (!Enum.TryParse<ProviderType>(entry.Name, false, out var type) || type.ToString() != entry.Name
                || !CqlV1Compiler.SupportedReadProfile(type) || entry.Value.ValueKind != JsonValueKind.Array
                || entry.Value.GetArrayLength() is < 1 or > 8)
                throw new ArgumentException("Examples require an explicit supported source and1–8 requests.");
            var rows = new List<CqlHostedQueryExample>();
            foreach (var example in entry.Value.EnumerateArray())
            {
                var hasOperation = example.ValueKind == JsonValueKind.Object && example.TryGetProperty("operation", out _);
                if (example.ValueKind != JsonValueKind.Object || example.EnumerateObject().Count() != (hasOperation ? 4 : 3)
                    || !example.TryGetProperty("id", out var id) || !example.TryGetProperty("label", out var label)
                    || !example.TryGetProperty("request", out var request))
                    throw new ArgumentException("Example fields must be id, label, request and optional operation.");
                var operation = hasOperation ? example.GetProperty("operation").GetString() : "Rows";
                if (operation is not ("Rows" or "Federation"))
                    throw new ArgumentException("Examples support only Rows or Federation operations.");
                var key = id.GetString(); var title = label.GetString();
                if (string.IsNullOrWhiteSpace(key) || key.Length > 40 || key.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')
                    || string.IsNullOrWhiteSpace(title) || title.Length > 100 || title.Any(char.IsControl)
                    || rows.Any(row => row.Id == key) || request.GetRawText().Length > 32768)
                    throw new ArgumentException("Unique bounded public example identities required.");
                if (operation == "Rows")
                {
                    // Exercise the owning HTTP snapshot before advertising a preset.
                    var typed = CqlRequestCapture.Capture(request.Deserialize<CqlV1Request>(Json));
                    if (typed?.Provider != entry.Name || typed.Graph != null)
                        throw new ArgumentException("Public leaf examples cannot change source ownership or request graphs.");
                    _ = CqlV1Compiler.Compile(typed, ExampleActor, type);
                    rows.Add(new(key, title, JsonSerializer.SerializeToElement(typed, Json)));
                }
                else
                {
                    var typed = Federation(request, type);
                    rows.Add(new(key, title, JsonSerializer.SerializeToElement(typed, Json), operation));
                }
            }
            result.examples.Add(type, rows.AsReadOnly());
        }
        return result;
    }

    public IReadOnlyList<CqlHostedQueryExample> For(ProviderType type, CosmicQueryCapabilities capabilities,
        Func<ProviderType, CosmicQueryCapabilities> resolveCapabilities = null,
        Func<ProviderType, Guid, CosmicQueryReadAccess> resolveReadAccess = null)
    {
        if (!examples.TryGetValue(type, out var rows)) return Array.Empty<CqlHostedQueryExample>();
        CosmicQueryReadAccess Access(ProviderType target)
        {
            var access = resolveReadAccess == null ? CosmicQueryReadAccess.OwnerOrPublic(ExampleActor).Result
                : resolveReadAccess(target, ExampleActor);
            if (access == null || access.ActorId != ExampleActor || access.Authority.HasValue && access.Authority != target)
                throw new ArgumentException("Example access must bind the same actor and target authority.");
            return access;
        }
        var selected = new List<CqlHostedQueryExample>();
        foreach (var example in rows)
        {
            if (example.Operation == "Federation")
            {
                var request = Federation(example.Request, type);
                var targets = request.Providers.Select(Target).ToArray();
                var actual = targets.Select(target => (Provider: target,
                    Capabilities: target == type ? capabilities : resolveCapabilities?.Invoke(target))).ToArray();
                if (actual.Any(target => target.Capabilities == null))
                {
                    selected.Add(example with { Available = false, ErrorCode = "CQL_DEMO_TARGET_UNAVAILABLE" });
                    continue;
                }
                var source = CqlV1Compiler.CompileFederationSource(request.Source, ExampleActor)
                    .WithScope(CosmicQueryScope.Named(targets).Result);
                var planned = CosmicFederationPlanner.Plan(source, FederationOptions(request), targets[0],
                    actual.Select(target => new CosmicQueryTarget(target.Provider, target.Capabilities)).ToArray(),
                    targets.Select(target => new CosmicFederationReadPolicy(target,
                        Access(target))).ToArray());
                selected.Add(planned.IsError ? example with { Available = false, ErrorCode = "CQL_DEMO_EXAMPLE_UNSUPPORTED" } : example);
                continue;
            }
            var query = CqlV1Compiler.Compile(example.Request.Deserialize<CqlV1Request>(Json),
                ExampleActor, type, Access(type));
            var plan = CosmicQueryPlanner.Plan(query, type, new[] { new CosmicQueryTarget(type, capabilities) });
            if (plan.IsError) throw new ArgumentException("CQL_DEMO_EXAMPLE_UNSUPPORTED");
            selected.Add(example);
        }
        return selected.AsReadOnly();
    }

    private static CqlFederationV1Request Federation(JsonElement json, ProviderType owner)
    {
        var request = CqlRequestCapture.CaptureAndValidateFederation(json.Deserialize<CqlFederationV1Request>(Json));
        var targets = request.Providers.Select(Target).ToArray();
        if (request.Scope != "Named" || targets.Length < 2 || targets.Distinct().Count() != targets.Length
            || !targets.Contains(owner) || request.IdentityMode != "PreserveCopies" || request.ProviderPriority.Length != 0
            || request.EnableContinuation || request.Cursor != null || request.MaximumProviderAttempts < targets.Length)
            throw new ArgumentException("Federation examples require an explicit bounded owner-containing read set without continuation.");
        _ = CqlV1Compiler.CompileFederationSource(request.Source, ExampleActor);
        _ = FederationOptions(request);
        return request;
    }

    private static ProviderType Target(string name) => CqlV1Compiler.RequestedProvider(name, true)
        ?? throw new ArgumentException("Federation examples require concrete providers.");

    private static CosmicFederationOptions FederationOptions(CqlFederationV1Request request)
    {
        var options = CosmicFederationOptions.Create(skip: request.Skip, take: request.Take,
            maximumInputRows: request.MaximumInputRows, maximumRowsPerProvider: request.MaximumRowsPerProvider,
            maximumInputBytes: request.MaximumInputBytes, maximumOutputBytes: request.MaximumOutputBytes,
            maximumWork: request.MaximumWork, maximumProviderAttempts: request.MaximumProviderAttempts,
            maximumDurationMilliseconds: request.MaximumDurationMilliseconds);
        if (options.IsError) throw new ArgumentException(options.Message);
        return options.Result;
    }

    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new ArgumentException("Duplicate CQL example property.");
                Unique(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) Unique(item);
    }
}
