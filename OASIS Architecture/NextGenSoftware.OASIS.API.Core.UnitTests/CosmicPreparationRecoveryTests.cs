using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicPreparationRecoveryTests
{
    public class HydrationProbe : Holon
    {
        public static int Constructors;
        public HydrationProbe() { Constructors++; }
    }
    private sealed class Resolver : IHolonMetadataKeyResolver
    {
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
            => new(new HolonMetadataKeyMaterial(Enumerable.Repeat((byte)41, 32).ToArray()));
    }

    [Fact]
    public void EverySelectedEnvelopeMustAuthenticateBeforeAnyRecoveredEntityIsConstructed()
    {
        var root = new HydrationProbe { Id = Guid.NewGuid() }; var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id };
        root.Children.Add(child); var op = Guid.NewGuid(); var actor = Guid.NewGuid();
        var write = new ConditionalHolonWrite(op, actor, ProviderType.SQLLiteDBOASIS,
            new[] { new HolonWriteCondition(root.Id, null), new HolonWriteCondition(child.Id, null) });
        var policy = new HolonMetadataEncryptionPolicy("scope", "key", new Resolver());
        var plan = new HolonConditionalPreparation(new[] { root }, write, true, true, 0, _ => policy).Seal();
        var second = plan.Candidate.Records[1]; var json = JObject.Parse(second.Envelope.Substring(HolonPreparationCipher.Prefix.Length));
        var tag = Convert.FromBase64String(json.Value<string>("tag")!); tag[0] ^= 1; json["tag"] = Convert.ToBase64String(tag);
        var altered = new HolonWritePreparation(op, actor, ProviderType.SQLLiteDBOASIS, new[] { plan.Candidate.Records[0],
            new ProtectedHolonPreparation(second.HolonId, HolonPreparationCipher.Prefix + json.ToString()) });
        HydrationProbe.Constructors = 0;
        var rejected = plan.Recover(altered);
        Assert.True(rejected.IsError); Assert.Null(rejected.Result); Assert.Equal("COSMIC_PREPARATION_INVALID", rejected.ErrorCode);
        Assert.Equal(0, HydrationProbe.Constructors);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void WinningPreparationBindsTheRequestedRuntimeSnapshotContract(bool reverse)
    {
        var id = Guid.NewGuid(); var op = Guid.NewGuid(); var actor = Guid.NewGuid();
        var policy = new HolonMetadataEncryptionPolicy("scope", "key", new Resolver());
        var write = new ConditionalHolonWrite(op, actor, ProviderType.SQLLiteDBOASIS, new[] { new HolonWriteCondition(id, null) });
        IHolon original = reverse ? new Holon { Id = id } : new HydrationProbe { Id = id };
        IHolon changed = reverse ? new HydrationProbe { Id = id } : new Holon { Id = id };
        var first = new HolonConditionalPreparation(new[] { original }, write, false, false, 0, _ => policy).Seal();
        var retry = new HolonConditionalPreparation(new[] { changed }, write, false, false, 0, _ => policy).Seal();
        var rejected = retry.Recover(first.Candidate);
        Assert.True(rejected.IsError); Assert.Equal("COSMIC_OPERATION_ID_REUSED", rejected.ErrorCode); Assert.Null(rejected.Result);
    }

    [Theory]
    [InlineData("children")] [InlineData("recursive")] [InlineData("depth")] [InlineData("replication")]
    public void WinningPreparationBindsAllOperationFlagsAndReplicationEnrollment(string field)
    {
        var id = Guid.NewGuid(); var op = Guid.NewGuid(); var actor = Guid.NewGuid();
        var policy = new HolonMetadataEncryptionPolicy("scope", "key", new Resolver());
        ConditionalHolonWrite Write(bool changed) => new(op, actor, ProviderType.SQLLiteDBOASIS, new[] { new HolonWriteCondition(id, null) },
            changed && field == "replication" ? new HolonReplicationEnrollment("source", new[] { "target" }) : null);
        var original = new HolonConditionalPreparation(new[] { new Holon { Id = id } }, Write(false), true, true, 0, _ => policy).Seal();
        var retry = new HolonConditionalPreparation(new[] { new Holon { Id = id } }, Write(true), field != "children", field != "recursive", field == "depth" ? 1 : 0, _ => policy).Seal();
        var rejected = retry.Recover(original.Candidate);
        Assert.True(rejected.IsError); Assert.Equal("COSMIC_OPERATION_ID_REUSED", rejected.ErrorCode); Assert.Null(rejected.Result);
    }
}
