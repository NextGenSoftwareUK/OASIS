using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class ConditionalProviderContractTests
{
    public sealed class OwnedWorkflowState
    {
        public bool IsChanged { get; set; }
        public bool IsNewHolon { get; set; }
        public bool IsSaving { get; set; }
    }

    [Fact]
    public void CanonicalTrackingExclusionDoesNotEraseUnrelatedOwnedValueFields()
    {
        var record = new Holon { Id = Guid.NewGuid() };
        record.MetaData["workflow"] = new OwnedWorkflowState { IsChanged = true, IsNewHolon = true, IsSaving = true };
        var json = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(record);
        var document = Newtonsoft.Json.Linq.JObject.Parse(json);
        Assert.Null(document["IsChanged"]); Assert.Null(document["IsNewHolon"]); Assert.Null(document["IsSaving"]);
        var workflow = document["MetaData"]!["workflow"]!;
        Assert.Equal(true, workflow["IsChanged"]?.Value<bool>());
        Assert.Equal(true, workflow["IsNewHolon"]?.Value<bool>());
        Assert.Equal(true, workflow["IsSaving"]?.Value<bool>());
    }
    [Theory]
    [InlineData(ProviderType.Default)]
    [InlineData(ProviderType.None)]
    [InlineData((ProviderType)int.MaxValue)]
    public void RevisionObservationsRequireExplicitDefinedAuthority(ProviderType authority)
    {
        var record = new Holon();
        Assert.Throws<ArgumentException>(() => CosmicConcurrency.GetProviderRevisionId(record, authority));
        Assert.Throws<ArgumentException>(() => CosmicConcurrency.SetProviderRevisionId(record, authority, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), authority,
            new[] { new HolonWriteCondition(Guid.NewGuid(), null) }));
    }

    [Fact]
    public void MissingLegacyAndMalformedRevisionsHaveDistinctOutcomes()
    {
        var record = new Holon(); var authority = ProviderType.OracleDBOASIS;
        Assert.Null(CosmicConcurrency.GetProviderRevisionId(record, authority));
        CosmicConcurrency.SetProviderRevisionId(record, authority, Guid.Empty);
        Assert.Equal(Guid.Empty, CosmicConcurrency.GetProviderRevisionId(record, authority));
        record.ProviderMetaData[authority][CosmicConcurrency.ProviderRevisionMetadataKey] = "not-a-revision";
        Assert.Throws<InvalidDataException>(() => CosmicConcurrency.GetProviderRevisionId(record, authority));
    }

    [Fact]
    public void ConditionsAndExpectedRevisionOptionsDoNotAliasCallerCollections()
    {
        var id = Guid.NewGuid(); var revision = Guid.NewGuid();
        var conditions = new List<HolonWriteCondition> { new(id, revision) };
        var write = new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), ProviderType.OracleDBOASIS, conditions);
        conditions.Clear(); Assert.Single(write.Conditions); Assert.Equal(revision, write.Conditions[0].ExpectedRevisionId);
        var observations = new Dictionary<Guid, Guid?> { [id] = revision };
        var options = new CosmicWriteOptions(Guid.NewGuid(), observations);
        observations[id] = null; Assert.Equal(revision, options.ExpectedRevisions[id]);
    }

    [Fact]
    public void OperationIdentityAndUniqueConditionsAreMandatory()
    {
        var id = Guid.NewGuid(); var condition = new HolonWriteCondition(id, null);
        Assert.Throws<ArgumentException>(() => new CosmicWriteOptions(Guid.Empty));
        Assert.Throws<ArgumentException>(() => new HolonWriteCondition(Guid.Empty, null));
        Assert.Throws<ArgumentException>(() => new ConditionalHolonWrite(Guid.Empty, Guid.NewGuid(), ProviderType.OracleDBOASIS, new[] { condition }));
        Assert.Throws<ArgumentException>(() => new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), ProviderType.OracleDBOASIS, new[] { condition, condition }));
    }
}
