using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicStoredPreparationStatusTests
{
    public class Probe : Holon
    {
        public static int Constructors;
        public Probe() { Constructors++; }
    }
    public class OtherProbe : Probe { }
    private sealed class Resolver : IHolonMetadataKeyResolver
    {
        public Guid? Denied;
        public List<HolonMetadataKeyRequest> Requests = new();
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
        {
            Requests.Add(request);
            return request.RecordId == Denied ? new() { IsError = true, ErrorCode = "STATUS_KEY_DENIED" }
                : new(new HolonMetadataKeyMaterial(Enumerable.Repeat((byte)71, 32).ToArray()));
        }
    }
    private static (HolonWritePreparation Stored, ConditionalHolonWrite Write, Probe Root, Probe Child,
        Dictionary<Guid, HolonMetadataEncryptionPolicy> Policies) Fixture(ConditionalHolonOperation kind)
    {
        var root = new Probe { Id = Guid.NewGuid(), Name = "secret-root" };
        var child = new Probe { Id = Guid.NewGuid(), Name = "secret-child", ParentHolonId = root.Id };
        root.Children.Add(child);
        root.MetaData["exactLong"] = 9007199254740993L;
        root.MetaData["exactDecimal"] = 1.0000000000000000000000000001m;
        var write = new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS,
            new[] { new HolonWriteCondition(root.Id, kind == ConditionalHolonOperation.Save ? null : Guid.NewGuid()),
                new HolonWriteCondition(child.Id, kind == ConditionalHolonOperation.Save ? null : Guid.Empty) },
            new HolonReplicationEnrollment("source", new[] { "second", "first" }));
        var resolver = new Resolver();
        var policies = new Dictionary<Guid, HolonMetadataEncryptionPolicy>
        {
            [root.Id] = new("context-a", "key-a", resolver), [child.Id] = new("context-b", "key-b", resolver)
        };
        var preparation = new HolonConditionalPreparation(new[] { root }, write, true, true, 1,
            value => policies[value.Id], kind).Seal();
        return (preparation.Candidate, write, root, child, policies);
    }
    private static Type? Admit(string type, string assembly) => type == typeof(Probe).FullName
        && assembly == typeof(Probe).Assembly.GetName().Name ? typeof(Probe) : null;

    [Theory]
    [InlineData(ConditionalHolonOperation.Save)]
    [InlineData(ConditionalHolonOperation.SoftDelete)]
    [InlineData(ConditionalHolonOperation.HardDelete)]
    public void FreshStatusRecoveryNeedsOnlyProtectedRecordsAndAuthorizedPolicies(ConditionalHolonOperation kind)
    {
        var fixture = Fixture(kind);
        var resolver = new Resolver();
        HolonMetadataEncryptionPolicy Policy(Guid id) => new(fixture.Policies[id].ContextId, "new-write-key", resolver);
        Probe.Constructors = 0;
        var result = HolonConditionalPreparation.RecoverOriginalForStatus(fixture.Stored,
            fixture.Write.OperationId, fixture.Write.ActorId, fixture.Write.Authority, Policy, Admit);
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.Equal(2, Probe.Constructors);
        Assert.Equal(kind, result.Result.Operation);
        Assert.Equal(fixture.Write.OperationId, result.Result.Write.OperationId);
        Assert.Equal(fixture.Write.ActorId, result.Result.Write.ActorId);
        Assert.Equal(fixture.Write.Authority, result.Result.Write.Authority);
        Assert.True(result.Result.SaveChildren);
        Assert.True(result.Result.Recursive);
        Assert.Equal(1, result.Result.MaxChildDepth);
        Assert.Equal(fixture.Write.Conditions.OrderBy(row => row.HolonId).Select(row => (row.HolonId, row.ExpectedRevisionId)),
            result.Result.Write.Conditions.OrderBy(row => row.HolonId).Select(row => (row.HolonId, row.ExpectedRevisionId)));
        Assert.Equal(fixture.Write.ReplicationEnrollment.TargetIds, result.Result.Write.ReplicationEnrollment.TargetIds);
        var root = Assert.Single(result.Result.Roots);
        Assert.Equal(fixture.Root.Id, root.Id);
        Assert.Equal(fixture.Child.Id, Assert.Single(root.Children).Id);
        Assert.True(StoredRecordValueEquality.Equivalent(StoredRecordJson.CaptureRuntimeToken(fixture.Root), StoredRecordJson.CaptureRuntimeToken(root)));
        Assert.All(resolver.Requests, request => Assert.Equal(HolonMetadataKeyPurpose.DecryptPreparation, request.Purpose));
        Assert.Contains(resolver.Requests, request => request.KeyId == "key-a");
        Assert.Contains(resolver.Requests, request => request.KeyId == "key-b");
        Assert.All(fixture.Stored.Records, row => Assert.DoesNotContain("secret-", row.Envelope));
    }

    [Theory]
    [InlineData("operation")]
    [InlineData("actor")]
    [InlineData("authority")]
    [InlineData("denied-child")]
    [InlineData("missing-envelope")]
    public void BindingAndAllKeyAuthorizationPrecedeEntityConstruction(string mode)
    {
        var fixture = Fixture(ConditionalHolonOperation.Save);
        var resolver = new Resolver { Denied = mode == "denied-child" ? fixture.Child.Id : null };
        var stored = mode == "missing-envelope" ? new HolonWritePreparation(fixture.Write.OperationId, fixture.Write.ActorId,
            fixture.Write.Authority, new[] { fixture.Stored.Records[0] }) : fixture.Stored;
        Probe.Constructors = 0;
        var result = HolonConditionalPreparation.RecoverOriginalForStatus(stored,
            mode == "operation" ? Guid.NewGuid() : fixture.Write.OperationId,
            mode == "actor" ? Guid.NewGuid() : fixture.Write.ActorId,
            mode == "authority" ? ProviderType.MongoDBOASIS : fixture.Write.Authority,
            id => new HolonMetadataEncryptionPolicy(fixture.Policies[id].ContextId, "key", resolver), Admit);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.Equal(0, Probe.Constructors);
        if (mode is "operation" or "actor" or "authority") Assert.Empty(resolver.Requests);
    }

    [Theory]
    [InlineData("cycle")]
    [InlineData("unreachable")]
    [InlineData("conditions")]
    [InlineData("extra-field")]
    [InlineData("numeric-kind")]
    [InlineData("runtime-contract")]
    [InlineData("prepared-id")]
    [InlineData("prepared-type")]
    public void AuthenticatedButInvalidGraphOrRuntimePayloadRejectsBeforeConstruction(string mode)
    {
        var fixture = Fixture(ConditionalHolonOperation.Save);
        var records = fixture.Stored.Records.Select(record =>
        {
            var policy = fixture.Policies[record.HolonId];
            var clear = JObject.Parse(HolonPreparationCipher.Decrypt(record.HolonId, fixture.Write.OperationId,
                fixture.Write.ActorId, fixture.Write.Authority, record.Envelope, policy));
            var graph = JObject.Parse(clear.Value<string>("graph")!);
            var nodes = (JArray)graph["nodes"]!;
            if (mode == "cycle") nodes[1]["children"] = new JArray(fixture.Root.Id.ToString("D"));
            if (mode == "unreachable") nodes[0]["children"] = new JArray();
            if (mode == "conditions") ((JArray)graph["conditions"]!).RemoveAt(1);
            if (mode == "extra-field") graph["extra"] = true;
            if (mode == "numeric-kind") graph["operation"] = "0";
            if (mode == "runtime-contract") nodes[1]["type"] = typeof(OtherProbe).FullName;
            clear["graph"] = graph.ToString(Formatting.None);
            if (record.HolonId == fixture.Root.Id && mode is "prepared-id" or "prepared-type")
            {
                var prepared = JObject.Parse(clear.Value<string>("prepared")!);
                if (mode == "prepared-id") prepared["Id"] = Guid.NewGuid().ToString("D");
                else prepared["$type"] = typeof(OtherProbe).AssemblyQualifiedName;
                clear["prepared"] = prepared.ToString(Formatting.None);
            }
            return new ProtectedHolonPreparation(record.HolonId, HolonPreparationCipher.Encrypt(record.HolonId,
                fixture.Write.OperationId, fixture.Write.ActorId, fixture.Write.Authority, clear.ToString(Formatting.None), policy));
        }).ToArray();
        var altered = new HolonWritePreparation(fixture.Write.OperationId, fixture.Write.ActorId, fixture.Write.Authority, records);
        Probe.Constructors = 0;
        var result = HolonConditionalPreparation.RecoverOriginalForStatus(altered, fixture.Write.OperationId,
            fixture.Write.ActorId, fixture.Write.Authority, id => fixture.Policies[id], Admit);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.Equal(0, Probe.Constructors);
    }
}
