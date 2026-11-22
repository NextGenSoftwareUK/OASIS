using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicPropertyPreparationTests
{
    public class Probe : Holon
    {
        public static int Constructors;
        public Probe() { Constructors++; }
    }
    private sealed class Resolver : IHolonMetadataKeyResolver
    {
        public int Reads;
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
        {
            Reads++;
            return new(new HolonMetadataKeyMaterial(Enumerable.Repeat((byte)83, 32).ToArray()));
        }
    }
    private static Type? Admit(string type, string assembly) => type == typeof(Probe).FullName
        && assembly == typeof(Probe).Assembly.GetName().Name ? typeof(Probe) : null;
    private sealed class Fixture
    {
        public Probe Value = new() { Id = Guid.NewGuid(), Name = "unselected-local", Description = "selected-secret" };
        public Resolver Keys = new();
        public HolonMetadataEncryptionPolicy Policy;
        public ConditionalHolonWrite Write;
        public Dictionary<Guid, IReadOnlyList<string>> Selection;
        public HolonConditionalPreparation Preparation;
        public Fixture(ProviderType authority = ProviderType.SQLLiteDBOASIS)
        {
            Value.MetaData["exact"] = 9007199254740993L;
            Policy = new("property-preparation", "key", Keys);
            Write = new(Guid.NewGuid(), Guid.NewGuid(), authority,
                new[] { new HolonWriteCondition(Value.Id, Guid.Empty) });
            Selection = new() { [Value.Id] = new[] { "/MetaData/exact", "/Description" } };
            Preparation = Plan(Write, Selection).Seal();
        }
        public HolonConditionalPreparation Plan(ConditionalHolonWrite write,
            IReadOnlyDictionary<Guid, IReadOnlyList<string>>? selection)
            => new(new[] { Value }, write, false, false, 0, _ => Policy, ConditionalHolonOperation.Save, selection);
        public OASISResult<ConditionalHolonPropertyWriteRequest> Recover(HolonWritePreparation stored)
            => HolonConditionalPreparation.RecoverOriginalPropertyForStatus(stored,
                Write.OperationId, Write.ActorId, Write.Authority, _ => Policy, Admit);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public void ProtectedSelectedIntentRoundTripsWithoutRefreshOrWholeRecordDowngrade(ProviderType authority)
    {
        var fixture = new Fixture(authority);
        var expected = new ConditionalHolonPropertyWriteRequest(new ConditionalHolonWriteRequest(fixture.Write,
            ConditionalHolonOperation.Save, new[] { fixture.Value }, false, false, 0), fixture.Selection);
        fixture.Selection[fixture.Value.Id] = new[] { "/Name" };
        Probe.Constructors = 0;
        var restored = fixture.Recover(fixture.Preparation.Candidate);
        Assert.False(restored.IsError, restored.Message + restored.Exception);
        Assert.Equal(1, Probe.Constructors);
        Assert.Equal(expected.CanonicalSelectionJson, restored.Result.CanonicalSelectionJson);
        Assert.Equal(Guid.Empty, Assert.Single(restored.Result.WriteRequest.Write.Conditions).ExpectedRevisionId);
        Assert.Equal(authority, restored.Result.WriteRequest.Write.Authority);
        Assert.Equal(fixture.Write.ActorId, restored.Result.WriteRequest.Write.ActorId);
        Assert.Equal(fixture.Write.OperationId, restored.Result.WriteRequest.Write.OperationId);
        var value = Assert.Single(restored.Result.WriteRequest.Roots);
        Assert.Equal("selected-secret", value.Description);
        Assert.Equal(9007199254740993L, value.MetaData["exact"]);
        Assert.All(fixture.Preparation.Candidate.Records, row => Assert.DoesNotContain("selected-secret", row.Envelope));
        var sameWinner = fixture.Preparation.RecoverProperty(fixture.Preparation.Candidate);
        Assert.False(sameWinner.IsError, sameWinner.Message + sameWinner.Exception);
        Assert.Equal(expected.CanonicalSelectionJson, sameWinner.Result.CanonicalSelectionJson);
    }

    [Theory]
    [InlineData("mask")]
    [InlineData("whole")]
    [InlineData("value")]
    [InlineData("revision")]
    public void WinningPreparationCannotBeReusedForDifferentFieldIntent(string change)
    {
        var fixture = new Fixture();
        if (change == "value") fixture.Value.Description = "different";
        var write = change == "revision" ? new ConditionalHolonWrite(fixture.Write.OperationId,
            fixture.Write.ActorId, fixture.Write.Authority,
            new[] { new HolonWriteCondition(fixture.Value.Id, Guid.NewGuid()) }) : fixture.Write;
        IReadOnlyDictionary<Guid, IReadOnlyList<string>>? selection = change == "whole" ? null
            : change == "mask" ? new Dictionary<Guid, IReadOnlyList<string>> { [fixture.Value.Id] = new[] { "/Name" } }
            : fixture.Selection;
        var retry = fixture.Plan(write, selection).Seal();
        Probe.Constructors = 0;
        if (change == "whole")
        {
            var refused = retry.Recover(fixture.Preparation.Candidate);
            Assert.True(refused.IsError);
            Assert.Null(refused.Result);
        }
        else
        {
            var refused = retry.RecoverProperty(fixture.Preparation.Candidate);
            Assert.True(refused.IsError);
            Assert.Null(refused.Result);
            Assert.Equal("COSMIC_OPERATION_ID_REUSED", refused.ErrorCode);
        }
        Assert.Equal(0, Probe.Constructors);
    }

    [Theory]
    [InlineData("duplicate-path")]
    [InlineData("overlap")]
    [InlineData("noncanonical")]
    [InlineData("unknown-id")]
    [InlineData("create")]
    [InlineData("missing")]
    [InlineData("format")]
    [InlineData("extra")]
    [InlineData("operation")]
    [InlineData("duplicate-id")]
    public void InvalidAuthenticatedSelectionIsRefusedBeforeEntityConstruction(string change)
    {
        var fixture = new Fixture();
        var record = Assert.Single(fixture.Preparation.Candidate.Records);
        var clear = JObject.Parse(HolonPreparationCipher.Decrypt(record.HolonId, fixture.Write.OperationId,
            fixture.Write.ActorId, fixture.Write.Authority, record.Envelope, fixture.Policy));
        var graph = JObject.Parse(clear.Value<string>("graph")!);
        var selected = (JObject)graph["propertySelection"]!;
        var rows = (JArray)selected["selections"]!;
        if (change == "duplicate-path") rows[0]["paths"] = new JArray("/Description", "/Description");
        if (change == "overlap") rows[0]["paths"] = new JArray("/MetaData", "/MetaData/exact");
        if (change == "noncanonical") rows[0]["paths"] = new JArray("/MetaData/exact", "/Description");
        if (change == "unknown-id") rows[0]["id"] = Guid.NewGuid().ToString("D");
        if (change == "create") graph["conditions"]![0]!["revision"] = JValue.CreateNull();
        if (change == "missing") graph.Remove("propertySelection");
        if (change == "format") graph["format"] = 1;
        if (change == "extra") selected["extra"] = true;
        if (change == "operation") graph["operation"] = ConditionalHolonOperation.SoftDelete.ToString();
        if (change == "duplicate-id") rows.Add(rows[0].DeepClone());
        clear["graph"] = graph.ToString(Formatting.None);
        var changed = new HolonWritePreparation(fixture.Write.OperationId, fixture.Write.ActorId,
            fixture.Write.Authority, new[] { new ProtectedHolonPreparation(record.HolonId,
                HolonPreparationCipher.Encrypt(record.HolonId, fixture.Write.OperationId, fixture.Write.ActorId,
                    fixture.Write.Authority, clear.ToString(Formatting.None), fixture.Policy)) });
        Probe.Constructors = 0;
        var refused = fixture.Recover(changed);
        Assert.True(refused.IsError);
        Assert.Null(refused.Result);
        Assert.Equal(0, Probe.Constructors);
    }

    [Fact]
    public void SelectedPreparationCannotEnterWholeRecoveryAndWholeCannotEnterSelectedRecovery()
    {
        var fixture = new Fixture();
        var whole = fixture.Plan(fixture.Write, null).Seal();
        Probe.Constructors = 0;
        var ordinary = fixture.Preparation.Recover(fixture.Preparation.Candidate);
        Assert.True(ordinary.IsError);
        Assert.Null(ordinary.Result);
        Assert.Equal("COSMIC_PREPARATION_SELECTION_REQUIRED", ordinary.ErrorCode);
        var status = HolonConditionalPreparation.RecoverOriginalForStatus(fixture.Preparation.Candidate,
            fixture.Write.OperationId, fixture.Write.ActorId, fixture.Write.Authority, _ => fixture.Policy, Admit);
        Assert.True(status.IsError);
        Assert.Null(status.Result);
        var selected = fixture.Recover(whole.Candidate);
        Assert.True(selected.IsError);
        Assert.Null(selected.Result);
        Assert.Equal(0, Probe.Constructors);
    }

    [Fact]
    public void AuthenticatedActorBindingIsCheckedBeforeResolvingAnyKey()
    {
        var fixture = new Fixture();
        var before = fixture.Keys.Reads;
        Probe.Constructors = 0;
        var refused = HolonConditionalPreparation.RecoverOriginalPropertyForStatus(fixture.Preparation.Candidate,
            fixture.Write.OperationId, Guid.NewGuid(), fixture.Write.Authority, _ => fixture.Policy, Admit);
        Assert.True(refused.IsError);
        Assert.Null(refused.Result);
        Assert.Equal(before, fixture.Keys.Reads);
        Assert.Equal(0, Probe.Constructors);
    }
}
