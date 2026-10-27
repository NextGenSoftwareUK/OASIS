using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicLinqProjectionElementTests
{
    public sealed class Dto
    {
        public static int Constructions;
        public Dto() => Constructions++;
        public string? Label { get; set; }
    }

    private sealed class Native : ICosmicQueryProjectionCompositionStorageProvider
    {
        public int Calls;
        public bool ReverseEnabled = true;
        public bool Corrupt;
        public bool CardinalityEnabled = true;
        public bool MissingCardinality;
        public long? CountOverride;
        public string? Failure;
        public string?[] Values = { "a", "b", "c" };
        public ProviderType QueryProviderType => ProviderType.SQLLiteDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
        public CosmicProjectionCapabilities ProjectionCapabilities => new(
            new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
        public CosmicProjectionCompositionCapabilities CompositionCapabilities => new(
            CosmicSelectedStageFeatures.Paging | CosmicSelectedStageFeatures.Ordering
                | (ReverseEnabled ? CosmicSelectedStageFeatures.Reverse : CosmicSelectedStageFeatures.None),
            CosmicQueryFeatures.None, Array.Empty<CosmicQueryMetadataCapability>(), new[] { typeof(string) }, 32, false, CardinalityEnabled);

        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => throw new InvalidOperationException("Selected elements cannot hydrate source holons.");

        public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T, TResult>(
            CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
            CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            if (Failure != null) return Task.FromResult(new OASISResult<CosmicProjectionCompositionPacket>
                { IsError = true, ErrorCount = 1, ErrorCode = Failure, Message = "Native test fault" });
            var input = Values.Select((value, index) => new CosmicProjectionInputRow(
                Guid.Parse($"10000000-0000-0000-0000-{index + 1:D12}"), true, Guid.Empty,
                new object[] { value! }, Array.Empty<object>())).ToArray();
            var current = input.ToArray();
            var proofs = new List<CosmicProjectionStageProof>();
            long work = 0;
            foreach (var stage in plan.Query.ResultStages)
            {
                work += current.Length;
                current = stage switch
                {
                    CosmicSelectedReverseStage => current.Reverse().ToArray(),
                    CosmicSelectedOrderStage => current.OrderBy(row => (string?)row.Values[0], StringComparer.Ordinal).ToArray(),
                    CosmicSelectedWindowStage window => (window.Kind == CosmicSelectedWindowKind.Skip
                        ? current.Skip(window.Count) : current.Take(window.Count)).ToArray(),
                    _ => throw new InvalidOperationException("Unexpected test stage")
                };
                proofs.Add(new CosmicProjectionStageProof(current.Select(row => row.Id).ToArray()));
            }
            var rows = current.Select(row => new CosmicProjectionCompositionRow(row.Id,
                Corrupt ? "forged value" : row.Values[0])).ToArray();
            return Task.FromResult(new OASISResult<CosmicProjectionCompositionPacket>(
                new CosmicProjectionCompositionPacket(input, proofs, rows, input.Length, work,
                    plan.RequestsCardinality && !MissingCardinality ? CountOverride ?? current.LongLength : null)) { IsLoaded = true });
        }
    }

    private static IQueryable<Holon> Root(Native native, string mode = HyperDriveModes.V2)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = mode } };
        var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result);
        return CosmicLinq.Create(manager, native, source).Result;
    }

    [Fact]
    public void AmbiguousSingleFailsBeforeAnyDtoConstructor()
    {
        var native = new Native();
        var selected = Root(native).Select(row => new Dto { Label = row.Name });
        Dto.Constructions = 0;
        Assert.Equal("CQL_SEQUENCE_MULTIPLE", Assert.Throws<CosmicLinqException>(() => selected.Single()).Code);
        Assert.Equal(0, Dto.Constructions);
        Assert.Equal(1, native.Calls);
    }

    [Fact]
    public void FirstAndTailConstructOnlyTheNativelySelectedDto()
    {
        var native = new Native();
        var selected = Root(native).Select(row => new Dto { Label = row.Name }).OrderBy(row => row.Label);
        Dto.Constructions = 0;
        Assert.Equal("a", selected.First().Label);
        Assert.Equal("c", selected.Last().Label);
        Assert.Equal("b", selected.Take(2).ElementAt(^1).Label);
        Assert.Equal("b", selected.ElementAt(1).Label);
        Assert.Equal(4, Dto.Constructions);
    }

    [Fact]
    public void PresentNullNeverBecomesTheCallersEmptyDefault()
    {
        var native = new Native { Values = new string?[] { null } };
        var selected = Root(native).Select(row => row.Name);
        Assert.Null(selected.FirstOrDefault("empty default"));
        native.Values = Array.Empty<string?>();
        Assert.Equal("empty default", selected.FirstOrDefault("empty default"));
        Assert.Equal("CQL_SEQUENCE_EMPTY", Assert.Throws<CosmicLinqException>(() => selected.First()).Code);
        Assert.Null(selected.ElementAtOrDefault(-1));
    }

    [Fact]
    public void ForgedProofAndNativeFailureCannotConstructOrReturnDefaults()
    {
        var native = new Native { Corrupt = true };
        var selected = Root(native).Select(row => new Dto { Label = row.Name });
        Dto.Constructions = 0;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Assert.Throws<CosmicLinqException>(() => selected.FirstOrDefault()).Code);
        Assert.Equal(0, Dto.Constructions);
        native.Corrupt = false;
        native.Failure = "CQL_QUERY_LIMIT";
        Assert.Equal("CQL_QUERY_LIMIT", Assert.Throws<CosmicLinqException>(() => selected.FirstOrDefault()).Code);
        Assert.Equal(0, Dto.Constructions);
    }

    [Fact]
    public void TailRequiresIndependentReverseEnrollmentAndV2Ownership()
    {
        var native = new Native { ReverseEnabled = false };
        var selected = Root(native).Select(row => row.Name).OrderBy(value => value);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Assert.Throws<CosmicLinqException>(() => selected.Last()).Code);
        Assert.Equal(0, native.Calls);
        Assert.Equal("a", selected.First());
        var legacy = Root(native, "legacy").Select(row => row.Name);
        Assert.Equal("CQL_V2_REQUIRED", Assert.Throws<CosmicLinqException>(() => legacy.First()).Code);
        Assert.Equal(1, native.Calls);
    }
}
