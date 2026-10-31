using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicProjectionCompositionExecutorTests
{
    public static CosmicQueryCapabilities Sources => new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
    public static CosmicProjectionCapabilities Columns => new(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
    public static CosmicProjectionCompositionCapabilities Profile => new(CosmicSelectedStageFeatures.Paging,
        CosmicQueryFeatures.None, Array.Empty<CosmicQueryMetadataCapability>(), Array.Empty<Type>());
    public static CosmicProjectionCompositionCapabilities Denied => new(CosmicSelectedStageFeatures.None,
        CosmicQueryFeatures.None, Array.Empty<CosmicQueryMetadataCapability>(), Array.Empty<Type>());
    private static readonly Guid Id = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public sealed class Dto { public static int Calls; public Dto() { Calls++; } public string Label { get; set; } = ""; }
    private class RowOnly(ProviderType provider) : ICosmicQueryStorageProvider
    {
        public virtual ProviderType QueryProviderType => provider;
        public CosmicQueryCapabilities QueryCapabilities => Sources;
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => throw new InvalidOperationException("Composition must not hydrate entities.");
    }
    private sealed class Native(ProviderType provider) : RowOnly(provider), ICosmicQueryProjectionCompositionStorageProvider
    {
        public ProviderType Identity = provider;
        public override ProviderType QueryProviderType => Identity;
        public CosmicProjectionCapabilities ProjectionCapabilities => Columns;
        public CosmicProjectionCompositionCapabilities CompositionCapabilities { get; set; } = Profile;
        public int Calls; public bool Loaded = true; public bool NullResult; public string? Failure; public Action? DuringRead;
        public CosmicProjectionCompositionExecutionLimits? Limits;
        public CosmicProjectionCompositionPacket Data = Packet();
        public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T, TResult>(
            CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
            CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++; Limits = limits; DuringRead?.Invoke(); Assert.Single(plan.Targets); Assert.Single(plan.SourcePlan.Targets);
            return Task.FromResult(NullResult ? null! : new OASISResult<CosmicProjectionCompositionPacket>(Data)
            { IsLoaded = Loaded, IsError = Failure != null, ErrorCode = Failure! });
        }
    }
    public static CosmicProjectionCompositionPacket Packet(bool invalid = false, bool empty = false)
        => empty ? new(Array.Empty<CosmicProjectionInputRow>(), new[] { new CosmicProjectionStageProof(Array.Empty<Guid>()) }, Array.Empty<CosmicProjectionCompositionRow>(), 0, 0)
        : new(new[] { new CosmicProjectionInputRow(Id, true, Guid.Empty, new object[] { "coffee" }, Array.Empty<object>()) },
            new[] { new CosmicProjectionStageProof(new[] { Id }) }, new[] { new CosmicProjectionCompositionRow(Id, invalid ? "changed" : "coffee") }, 1, 1);
    private static CosmicProjectionCompositionPlan<Holon, Dto> Plan(CosmicProjectionCompositionOptions? options = null, params ProviderType[] providers)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(providers).Result).Select(h => new Dto { Label = h.Name }).Take(1).Result;
        return CosmicProjectionCompositionPlanner.Plan(query, providers[0], providers.Select(provider => new CosmicProjectionCompositionTarget(provider, Sources, Columns, Profile)).ToArray(), options).Result;
    }

    [Fact]
    public async Task AllTargetsValidateBeforeFirstIoAndEveryBufferBeforeFirstDto()
    {
        var first = new Native(ProviderType.MongoDBOASIS); var second = new Native(ProviderType.Neo4jOASIS) { Data = Packet(invalid: true) };
        Dto.Calls = 0; first.DuringRead = second.DuringRead = () => Assert.Equal(0, Dto.Calls);
        var result = await CosmicProjectionCompositionExecutor.ExecuteAsync(Plan(null, first.Identity, second.Identity), new[] { first, second });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode); Assert.Null(result.Result); Assert.Equal(0, Dto.Calls);
        Assert.Equal(1, first.Calls); Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task SuccessPreservesProviderProvenanceAndConstructsDetachedRows()
    {
        var first = new Native(ProviderType.MongoDBOASIS); var second = new Native(ProviderType.SQLLiteDBOASIS);
        Dto.Calls = 0; first.DuringRead = second.DuringRead = () => Assert.Equal(0, Dto.Calls);
        var result = await CosmicProjectionCompositionExecutor.ExecuteAsync(Plan(null, first.Identity, second.Identity), new[] { first, second });
        Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded); Assert.Equal(2, Dto.Calls);
        Assert.Equal(new[] { first.Identity, second.Identity }, result.Result.Providers.Select(value => value.Provider));
        Assert.Equal("coffee", result.Result.Providers[0].Rows.Single().Label);
        Assert.NotSame(result.Result.Providers[0].Rows.Single(), result.Result.Providers[1].Rows.Single());
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task AllSixBudgetsAreConsumedAcrossProvidersWithoutSilentTruncation(int budget)
    {
        var options = budget switch {
            0 => new CosmicProjectionCompositionOptions(maximumInputRows: 1),
            1 => new CosmicProjectionCompositionOptions(maximumInputPayloadBytes: 70),
            2 => new CosmicProjectionCompositionOptions(maximumStageRows: 1),
            3 => new CosmicProjectionCompositionOptions(maximumProofPayloadBytes: 16),
            _ => CosmicProjectionCompositionOptions.Default };
        var first = new Native(ProviderType.MongoDBOASIS); var second = new Native(ProviderType.Neo4jOASIS);
        Dto.Calls = 0;
        var result = await CosmicProjectionCompositionExecutor.ExecuteAsync(Plan(options, first.Identity, second.Identity), new[] { first, second },
            maximumResults: budget == 4 ? 1 : 100, maximumPayloadBytes: budget == 5 ? 38 : 1048576);
        Assert.Equal("CQL_RESULT_LIMIT", result.ErrorCode); Assert.Null(result.Result); Assert.Equal(0, Dto.Calls);
        var remaining = second.Limits!;
        Assert.Equal(0, budget switch { 0 => remaining.MaximumInputRows, 1 => remaining.MaximumInputPayloadBytes,
            2 => remaining.MaximumStageRows, 3 => remaining.MaximumProofPayloadBytes, 4 => remaining.MaximumResults, _ => remaining.MaximumPayloadBytes });
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task ExhaustedInputCanStillAcceptAnExplicitlyEmptySecondProviderProof()
    {
        var first = new Native(ProviderType.MongoDBOASIS); var second = new Native(ProviderType.Neo4jOASIS) { Data = Packet(empty: true) };
        var result = await CosmicProjectionCompositionExecutor.ExecuteAsync(Plan(new CosmicProjectionCompositionOptions(1, 70, 1, 16), first.Identity, second.Identity),
            new[] { first, second }, maximumResults: 1, maximumPayloadBytes: 38);
        Assert.False(result.IsError, result.Message); Assert.Empty(result.Result.Providers[1].Rows);
        Assert.Equal(0, second.Limits!.MaximumInputRows); Assert.Equal(0, second.Limits.MaximumPayloadBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MissingContractOrCapabilityRejectsAllTargetsBeforeIo(bool missingContract)
    {
        var first = new Native(ProviderType.MongoDBOASIS);
        ICosmicQueryStorageProvider second = missingContract ? new RowOnly(ProviderType.Neo4jOASIS)
            : new Native(ProviderType.Neo4jOASIS) { CompositionCapabilities = Denied };
        var result = await CosmicProjectionCompositionExecutor.ExecuteAsync(Plan(null, first.Identity, ProviderType.Neo4jOASIS), new[] { first, second });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode); Assert.Equal(0, first.Calls);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DispatchReadmissionRejectsChangedIdentityOrCapabilities(bool identity)
    {
        var first = new Native(ProviderType.MongoDBOASIS); var second = new Native(ProviderType.Neo4jOASIS);
        first.DuringRead = () => { if (identity) second.Identity = ProviderType.SQLLiteDBOASIS; else second.CompositionCapabilities = Denied; };
        Dto.Calls = 0;
        var result = await CosmicProjectionCompositionExecutor.ExecuteAsync(Plan(null, first.Identity, second.Identity), new[] { first, second });
        Assert.True(result.IsError); Assert.Equal(0, second.Calls); Assert.Equal(0, Dto.Calls); Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UnacknowledgedOrMissingPacketCannotPublish(bool missing)
    {
        var native = new Native(ProviderType.MongoDBOASIS) { Loaded = false, NullResult = missing };
        var result = await CosmicProjectionCompositionExecutor.ExecuteAsync(Plan(null, native.Identity), new[] { native });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode); Assert.Null(result.Result);
    }

    [Fact]
    public async Task ProviderFailureAndCancellationNeverFallBackToRows()
    {
        var native = new Native(ProviderType.MongoDBOASIS) { Failure = "NATIVE_FAILED" };
        Assert.Equal("NATIVE_FAILED", (await CosmicProjectionCompositionExecutor.ExecuteAsync(Plan(null, native.Identity), new[] { native })).ErrorCode);
        using var cancellation = new CancellationTokenSource(); native.Failure = null; native.DuringRead = cancellation.Cancel;
        Dto.Calls = 0;
        Assert.Equal("CQL_QUERY_CANCELLED", (await CosmicProjectionCompositionExecutor.ExecuteAsync(Plan(null, native.Identity), new[] { native }, cancellationToken: cancellation.Token)).ErrorCode);
        Assert.Equal(0, Dto.Calls);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public async Task NamedRuntimeRecordsUseTheSameGenericOwner(ProviderType provider)
    {
        var native = new Native(provider);
        var query = CosmicQuery<Holon>.Create().SelectColumns(new[] { CosmicProjectionSelection<Holon>.Create("label", h => h.Name).Result }).Take(1).Result;
        var plan = CosmicProjectionCompositionPlanner.Plan(query, provider, new[] { new CosmicProjectionCompositionTarget(provider, Sources, Columns, Profile) }).Result;
        var result = await CosmicProjectionCompositionExecutor.ExecuteAsync(plan, new[] { native });
        Assert.False(result.IsError, result.Message); Assert.Equal("coffee", result.Result.Providers.Single().Rows.Single()["label"]);
    }

    [Theory]
    [InlineData(NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2, true)]
    [InlineData(NextGenSoftware.OASIS.API.DNA.HyperDriveModes.Legacy, false)]
    public async Task ManagerExecutesOnlyThroughV2(string mode, bool supported)
    {
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = mode } };
        var manager = new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null, dna, new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null, dna));
        var native = new Native(ProviderType.MongoDBOASIS);
        var result = await manager.ExecuteProjectionCompositionQueryAsync(Plan(null, native.Identity), new[] { native });
        Assert.Equal(!supported, result.IsError); Assert.Equal(supported ? 1 : 0, native.Calls);
        if (!supported) Assert.Equal("CQL_V2_REQUIRED", result.ErrorCode);
    }

    [Fact]
    public async Task MissingAndDuplicateAdapterIdentitiesRejectBeforeDispatch()
    {
        var native = new Native(ProviderType.MongoDBOASIS); var plan = Plan(null, native.Identity);
        Assert.Equal("CQL_PLAN_TARGET_UNAVAILABLE", (await CosmicProjectionCompositionExecutor.ExecuteAsync(plan, Array.Empty<ICosmicQueryStorageProvider>())).ErrorCode);
        Assert.Equal("CQL_EXECUTION_INVALID", (await CosmicProjectionCompositionExecutor.ExecuteAsync(plan, new[] { native, native })).ErrorCode);
        Assert.Equal(0, native.Calls);
    }

    [Fact]
    public void RemainingWorkCannotUpgradeThePlan()
    {
        var options = new CosmicProjectionCompositionOptions(1, 70, 1, 16);
        Assert.True(new CosmicProjectionCompositionExecutionLimits(2, 70, 1, 16, 1, 38).Admit(options).IsError);
        Assert.True(new CosmicProjectionCompositionExecutionLimits(1, 71, 1, 16, 1, 38).Admit(options).IsError);
        Assert.True(new CosmicProjectionCompositionExecutionLimits(1, 70, 2, 16, 1, 38).Admit(options).IsError);
        Assert.True(new CosmicProjectionCompositionExecutionLimits(1, 70, 1, 17, 1, 38).Admit(options).IsError);
        Assert.False(new CosmicProjectionCompositionExecutionLimits(0, 0, 0, 0, 0, 0).Admit(options).IsError);
    }
}
