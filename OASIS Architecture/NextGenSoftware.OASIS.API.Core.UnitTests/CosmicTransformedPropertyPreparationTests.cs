using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTransformedPropertyPreparationTests
{
    public class Probe : Holon
    {
        public static int Constructors;
        public Probe() { Constructors++; }
    }
    private sealed class Keys : IHolonMetadataKeyResolver
    {
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
            => new(new HolonMetadataKeyMaterial(Enumerable.Repeat((byte)89, 32).ToArray()));
    }
    private sealed class Fixture
    {
        public Probe Value = new() { Id = Guid.NewGuid(), Name = "ordinary-root" };
        public HolonMetadataEncryptionPolicy Policy = new("transformed-property", "key", new Keys());
        public ConditionalHolonWrite Write;
        public HolonConditionalPreparation Preparation;
        public string LogicalSnapshot, ClearMetadata;
        public Fixture(ProviderType authority = ProviderType.SQLLiteDBOASIS, bool transform = true)
        {
            Value.MetaData["selected"] = "selected-secret";
            Value.MetaData["sibling"] = "unselected-secret";
            Value.MetaData["long"] = 9007199254740993L;
            LogicalSnapshot = StoredRecordJson.SerializeRuntime(Value);
            ClearMetadata = StoredRecordJson.Serialize(Value.MetaData);
            Write = new(Guid.NewGuid(), Guid.NewGuid(), authority, new[] { new HolonWriteCondition(Value.Id, Guid.Empty) });
            Preparation = Capture(Value, Write, "/MetaData/selected", transform ? "/MetaData" : "/MetaData/selected", transform);
        }
        public Probe Logical() => (Probe)StoredRecordJson.Deserialize(LogicalSnapshot, typeof(Probe));
        public HolonConditionalPreparation Capture(Probe value, ConditionalHolonWrite write,
            string logicalPath, string storagePath, bool transform = true)
        {
            var selected = new Dictionary<Guid, IReadOnlyList<string>> { [value.Id] = new[] { logicalPath } };
            var preparation = new HolonConditionalPreparation(new[] { value }, write, false, false, 0,
                _ => Policy, ConditionalHolonOperation.Save, selected);
            if (transform)
            {
                var envelope = HolonMetadataCipher.Encrypt(value.Id, StoredRecordJson.Serialize(value.MetaData), Policy);
                value.MetaData = new() { [HolonSaveMetadataOwnership.EncryptionEnvelope] = envelope };
            }
            return preparation.Seal(new Dictionary<Guid, IReadOnlyList<string>> { [value.Id] = new[] { storagePath } });
        }
        public OASISResult<ConditionalHolonPropertyWriteRequest> Recover(HolonWritePreparation stored)
            => HolonConditionalPreparation.RecoverOriginalPropertyForStatus(stored, Write.OperationId,
                Write.ActorId, Write.Authority, _ => Policy,
                (name, assembly) => name == typeof(Probe).FullName && assembly == typeof(Probe).Assembly.GetName().Name ? typeof(Probe) : null!);
        public JObject Clear(ProtectedHolonPreparation record)
            => JObject.Parse(HolonPreparationCipher.Decrypt(record.HolonId, Write.OperationId, Write.ActorId, Write.Authority, record.Envelope, Policy));
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public void LogicalAndStoredSelectionsRoundTripWithRealAuthenticatedMetadata(ProviderType authority)
    {
        var f = new Fixture(authority);
        var record = Assert.Single(f.Preparation.Candidate.Records);
        var graph = JObject.Parse(f.Clear(record).Value<string>("graph")!);
        Assert.Equal(3, graph.Value<int>("format"));
        Assert.Equal("/MetaData/selected", graph["propertySelection"]!["selections"]![0]!["paths"]![0]!.Value<string>());
        Assert.Equal("/MetaData", graph["storagePropertySelection"]!["selections"]![0]!["paths"]![0]!.Value<string>());
        Assert.DoesNotContain("selected-secret", record.Envelope);
        Probe.Constructors = 0;
        var result = f.Recover(f.Preparation.Candidate);
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.Equal(1, Probe.Constructors);
        Assert.Equal("/MetaData", Assert.Single(result.Result.Selections[f.Value.Id]));
        Assert.Equal(Guid.Empty, Assert.Single(result.Result.WriteRequest.Write.Conditions).ExpectedRevisionId);
        var stored = Assert.Single(result.Result.WriteRequest.Roots);
        Assert.Single(stored.MetaData);
        Assert.Equal(f.ClearMetadata, HolonMetadataCipher.Decrypt(stored.Id,
            stored.MetaData[HolonSaveMetadataOwnership.EncryptionEnvelope].ToString()!, f.Policy));
        f.Preparation.ValidatePropertyRequest(result.Result);
    }

    [Fact]
    public void FirstWinnerRetainsItsExactPreparedCiphertextAcrossFreshEncryption()
    {
        var f = new Fixture(); var retryValue = f.Logical();
        var retry = f.Capture(retryValue, f.Write, "/MetaData/selected", "/MetaData");
        var firstCipher = f.Value.MetaData[HolonSaveMetadataOwnership.EncryptionEnvelope];
        Assert.NotEqual(firstCipher, retryValue.MetaData[HolonSaveMetadataOwnership.EncryptionEnvelope]);
        var recovered = retry.RecoverProperty(f.Preparation.Candidate);
        Assert.False(recovered.IsError, recovered.Message + recovered.Exception);
        Assert.Equal(firstCipher, Assert.Single(recovered.Result.WriteRequest.Roots).MetaData[HolonSaveMetadataOwnership.EncryptionEnvelope]);
        Assert.Equal("/MetaData", Assert.Single(recovered.Result.Selections[f.Value.Id]));
    }

    [Theory]
    [InlineData("logical")][InlineData("storage")][InlineData("conditions")]
    public void WinningPreparationBindsBothMasksAndOriginalNativeCondition(string change)
    {
        var f = new Fixture(); var retryValue = f.Logical();
        var write = change == "conditions" ? new ConditionalHolonWrite(f.Write.OperationId, f.Write.ActorId,
            f.Write.Authority, new[] { new HolonWriteCondition(f.Value.Id, Guid.NewGuid()) }) : f.Write;
        var retry = f.Capture(retryValue, write, change == "logical" ? "/MetaData/sibling" : "/MetaData/selected",
            change == "storage" ? "/Name" : "/MetaData");
        Probe.Constructors = 0;
        var denied = retry.RecoverProperty(f.Preparation.Candidate);
        Assert.True(denied.IsError); Assert.Null(denied.Result);
        Assert.Equal("COSMIC_OPERATION_ID_REUSED", denied.ErrorCode);
        Assert.Equal(0, Probe.Constructors);
    }

    [Theory]
    [InlineData("missing-storage")][InlineData("missing-logical")]
    [InlineData("storage-format")][InlineData("logical-format")]
    [InlineData("duplicate-storage")][InlineData("unknown-storage")][InlineData("redundant")]
    public void AuthenticatedInvalidTransformedMaskIsRejectedBeforeEntityConstruction(string change)
    {
        var f = new Fixture(); var original = Assert.Single(f.Preparation.Candidate.Records);
        var clear = f.Clear(original); var graph = JObject.Parse(clear.Value<string>("graph")!);
        if (change == "missing-storage") graph.Remove("storagePropertySelection");
        if (change == "missing-logical") graph.Remove("propertySelection");
        if (change == "storage-format") graph["storagePropertySelection"]!["format"] = 99;
        if (change == "logical-format") graph["propertySelection"]!["format"] = 99;
        if (change == "duplicate-storage") graph["storagePropertySelection"]!["selections"]![0]!["paths"] = new JArray("/MetaData", "/MetaData");
        if (change == "unknown-storage") graph["storagePropertySelection"]!["selections"]![0]!["id"] = Guid.NewGuid().ToString("D");
        if (change == "redundant") graph["storagePropertySelection"] = graph["propertySelection"]!.DeepClone();
        clear["graph"] = graph.ToString(Formatting.None);
        var stored = new HolonWritePreparation(f.Write.OperationId, f.Write.ActorId, f.Write.Authority,
            new[] { new ProtectedHolonPreparation(original.HolonId, HolonPreparationCipher.Encrypt(original.HolonId,
                f.Write.OperationId, f.Write.ActorId, f.Write.Authority, clear.ToString(Formatting.None), f.Policy)) });
        Probe.Constructors = 0;
        var denied = f.Recover(stored);
        Assert.True(denied.IsError); Assert.Null(denied.Result);
        Assert.Equal(0, Probe.Constructors);
    }

    [Theory]
    [InlineData("whole")][InlineData("foreign-record")][InlineData("empty")]
    public void StoredTransformationNeedsLogicalIntentAndExactRecordCoverage(string mode)
    {
        var f = new Fixture(); var value = f.Logical(); var foreign = Guid.NewGuid();
        var write = new ConditionalHolonWrite(Guid.NewGuid(), f.Write.ActorId, f.Write.Authority,
            new[] { new HolonWriteCondition(value.Id, Guid.Empty), new HolonWriteCondition(foreign, Guid.Empty) });
        var logical = mode == "whole" ? null : new Dictionary<Guid, IReadOnlyList<string>> { [value.Id] = new[] { "/MetaData/selected" } };
        var preparation = new HolonConditionalPreparation(new[] { value }, write, false, false, 0,
            _ => f.Policy, ConditionalHolonOperation.Save, logical);
        var stored = mode == "empty" ? new Dictionary<Guid, IReadOnlyList<string>>()
            : new Dictionary<Guid, IReadOnlyList<string>> { [mode == "foreign-record" ? foreign : value.Id] = new[] { "/MetaData" } };
        if (mode == "whole") Assert.Throws<InvalidOperationException>(() => preparation.Seal(stored));
        else if (mode == "empty") Assert.Throws<ArgumentException>(() => preparation.Seal(stored));
        else Assert.Throws<InvalidDataException>(() => preparation.Seal(stored));
        Assert.Null(preparation.Candidate);
    }

    [Fact]
    public void EqualMasksRetainFormat2AndWholeRecoveryNeverDiscardsTransformedIntent()
    {
        var same = new Fixture(transform: false);
        var sameGraph = JObject.Parse(same.Clear(Assert.Single(same.Preparation.Candidate.Records)).Value<string>("graph")!);
        Assert.Equal(2, sameGraph.Value<int>("format")); Assert.Null(sameGraph.Property("storagePropertySelection"));
        Assert.False(same.Recover(same.Preparation.Candidate).IsError);
        var transformed = new Fixture();
        Probe.Constructors = 0;
        var denied = HolonConditionalPreparation.RecoverOriginalForStatus(transformed.Preparation.Candidate,
            transformed.Write.OperationId, transformed.Write.ActorId, transformed.Write.Authority, _ => transformed.Policy,
            (_, _) => typeof(Probe));
        Assert.True(denied.IsError); Assert.Null(denied.Result); Assert.Equal(0, Probe.Constructors);
    }
}
