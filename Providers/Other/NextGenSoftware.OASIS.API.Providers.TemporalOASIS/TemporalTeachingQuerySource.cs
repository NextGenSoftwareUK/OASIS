using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using Temporalio.Client;
using Temporalio.Exceptions;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.TemporalOASIS;

public sealed record TemporalTeachingRun(string WorkflowId, Guid RunId);

/// <summary>Read-only metadata for reviewed workflow runs in a dedicated Temporal namespace.</summary>
public sealed class TemporalTeachingQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation
{
    private const string Source = "temporal/workflow-description";
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public TemporalTeachingQuerySource(ITemporalClient client, Guid owner, IEnumerable<TemporalTeachingRun> runs, TemporalTeachingRun probe)
        : this(new WorkflowBackend(client, owner, runs, probe), probe) { }

    public static TemporalTeachingQuerySource ForCloud(string workflowNamespace, string apiKey, Guid owner,
        IEnumerable<TemporalTeachingRun> runs, TemporalTeachingRun probe)
    {
        if (!IsNamespace(workflowNamespace) || !workflowNamespace.Contains('.') ||
            apiKey is not { Length: >= 1 and <= 4096 } || apiKey.Any(c => c is < '!' or > '~' || c == ','))
            throw new ArgumentException("A reviewed Temporal Cloud namespace and protected API key are required.");

        var client = TemporalClient.CreateLazy(new TemporalClientConnectOptions(workflowNamespace + ".tmprl.cloud:7233")
        {
            Namespace = workflowNamespace,
            ApiKey = apiKey,
            Tls = new TlsOptions()
        });
        return new TemporalTeachingQuerySource(client, owner, runs, probe);
    }

    private TemporalTeachingQuerySource(WorkflowBackend backend, TemporalTeachingRun probe)
        : base(backend, "temporal-teaching:")
    {
        probeId = backend.Id(probe);
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed Temporal teaching workflow-run metadata";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Network);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public static Guid LogicalId(Guid owner, string workflowNamespace, TemporalTeachingRun run)
    {
        if (owner == Guid.Empty || !IsNamespace(workflowNamespace) || !IsTeachingRun(run))
            throw new ArgumentException("A source owner, bounded namespace and exact teaching run are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.TemporalOASIS, Source, NativeKey(workflowNamespace, run));
    }

    private static bool IsSafeKey(string key, int limit) =>
        key is { Length: > 0 } && key.Length <= limit &&
        key.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');

    private static bool IsNamespace(string value)
    {
        if (value is not { Length: >= 1 and <= 64 }) return false;
        var labels = value.Split('.');
        return labels.Length <= 2 && labels.All(label =>
            IsSafeKey(label, 63) && label[0] != '-' && label[^1] != '-');
    }

    private static bool IsTeachingRun(TemporalTeachingRun run) =>
        run is not null && run.RunId != Guid.Empty && IsSafeKey(run.WorkflowId, 55) &&
        run.WorkflowId.StartsWith("oasis-teaching-", StringComparison.Ordinal);

    private static string NativeKey(string workflowNamespace, TemporalTeachingRun run) =>
        $"{workflowNamespace}/{run.WorkflowId}/{run.RunId:D}";

    public PType QueryProviderType => PType.TemporalOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(probeId, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    private sealed class WorkflowBackend : IKeyValueBackend
    {
        private readonly ITemporalClient client;
        private readonly Guid owner;
        private readonly string workflowNamespace;
        private readonly Dictionary<Guid, TemporalTeachingRun> runs;

        public WorkflowBackend(ITemporalClient client, Guid owner, IEnumerable<TemporalTeachingRun> selected, TemporalTeachingRun probe)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.owner = owner;
            workflowNamespace = client.Options.Namespace;
            var catalogue = selected?.Take(9).ToArray() ?? Array.Empty<TemporalTeachingRun>();
            if (owner == Guid.Empty || !IsNamespace(workflowNamespace) || catalogue.Length is < 1 or > 8 ||
                catalogue.Any(run => !IsTeachingRun(run)) || catalogue.Distinct().Count() != catalogue.Length ||
                !catalogue.Contains(probe))
                throw new ArgumentException("Use a bounded set of unique reviewed Temporal teaching runs and an enrolled probe.");
            runs = catalogue.ToDictionary(Id);
        }

        public Guid Id(TemporalTeachingRun run) => LogicalId(owner, workflowNamespace, run);

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!runs.TryGetValue(id, out var run))
                throw new InvalidDataException("The selected ID is outside the Temporal teaching catalogue.");
            WorkflowExecutionDescription description;
            try
            {
                description = await client.GetWorkflowHandle(run.WorkflowId, run.RunId.ToString("D")).DescribeAsync(
                    new WorkflowDescribeOptions { Rpc = new RpcOptions { Retry = false, CancellationToken = token } });
            }
            catch (RpcException error) when (error.Code == RpcException.StatusCode.NotFound)
            {
                return null;
            }
            if (description.Id != run.WorkflowId || description.RunId != run.RunId.ToString("D") ||
                description.StartTime == default || (int)description.Status == 0 || !Enum.IsDefined(description.Status))
                throw new InvalidDataException("The Temporal description must match the exact teaching workflow run.");

            var nativeKey = NativeKey(workflowNamespace, run);
            var observation = new NativeRecord(Source, nativeKey, new Dictionary<string, object>
            {
                ["name"] = run.WorkflowId,
                ["TemporalNamespace"] = workflowNamespace,
                ["TemporalRunId"] = run.RunId.ToString("D"),
                ["TemporalStatus"] = description.Status.ToString(),
                ["TemporalStartTimeUtc"] = description.StartTime
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.TemporalOASIS, observation, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var holon = mapped.Result;
            holon.CreatedByAvatarId = owner;
            holon.IsPublic = true;
            holon.IsActive = true;
            holon.Children = Array.Empty<IHolon>();
            holon.ProviderUniqueStorageKey[PType.TemporalOASIS] = nativeKey;
            return StoredRecordJson.Serialize<IHolon>(holon);
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted Temporal teaching CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Temporal teaching scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Temporal teaching CQL exposes only read operations."));
    }
}
