using FluentAssertions;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicPropertyWriteRequestTests
{
    private static (Holon First, Holon Second, ConditionalHolonWriteRequest Request) Base(
        bool createSecond = false, ConditionalHolonOperation operation = ConditionalHolonOperation.Save)
    {
        var first = new Holon { Id = Guid.NewGuid(), Name = "first native", Description = "native description" };
        var second = new Holon { Id = Guid.NewGuid(), Name = "second native" };
        var write = new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS,
            new[] { new HolonWriteCondition(first.Id, Guid.Empty),
                new HolonWriteCondition(second.Id, createSecond ? null : Guid.NewGuid()) });
        return (first, second, new ConditionalHolonWriteRequest(write, operation, new[] { first, second }, false, false, 0));
    }

    private static Dictionary<Guid, IReadOnlyList<string>> Select(Guid id, params string[] paths)
        => new() { [id] = paths };

    [Fact]
    public void ExistingLegacyRevisionIsValidAndUnselectedCreateRetainsBaseSemantics()
    {
        var f = Base(createSecond: true);
        var request = new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.First.Id, "/Name"));
        request.WriteRequest.Should().BeSameAs(f.Request);
        request.WriteRequest.Write.Conditions[0].ExpectedRevisionId.Should().Be(Guid.Empty);
        request.WriteRequest.Write.Conditions[1].ExpectedRevisionId.Should().BeNull();
        request.Selections.Keys.Should().Equal(f.First.Id);
    }

    [Fact]
    public void OperationIdentityActorAuthorityAndConditionsAreNeverReplacedBySelection()
    {
        var f = Base();
        var request = new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.First.Id, "/Description"));
        request.WriteRequest.Write.Should().BeSameAs(f.Request.Write);
        request.WriteRequest.Operation.Should().Be(ConditionalHolonOperation.Save);
        request.WriteRequest.SaveChildren.Should().BeFalse(); request.WriteRequest.Recursive.Should().BeFalse();
        request.WriteRequest.Write.Conditions.Should().Equal(f.Request.Write.Conditions);
    }

    [Fact]
    public void CallerDictionariesListsAndPublishedViewsCannotChangeCapturedSelection()
    {
        var f = Base(); var paths = new List<string> { "/Name", "/Description" };
        var input = new Dictionary<Guid, IReadOnlyList<string>> { [f.First.Id] = paths };
        var request = new ConditionalHolonPropertyWriteRequest(f.Request, input);
        var captured = request.CanonicalSelectionJson;
        paths[0] = "/Id"; input.Clear();
        request.Selections[f.First.Id].Should().Equal("/Description", "/Name");
        Action mutateMap = () => ((IDictionary<Guid, IReadOnlyList<string>>)request.Selections).Clear();
        Action mutatePaths = () => ((IList<string>)request.Selections[f.First.Id])[0] = "/Id";
        mutateMap.Should().Throw<NotSupportedException>(); mutatePaths.Should().Throw<NotSupportedException>();
        request.CanonicalSelectionJson.Should().Be(captured);
    }

    [Fact]
    public void CanonicalIntentIsVersionedDeterministicOrdinalAndChangesWhenFieldIntentChanges()
    {
        var f = Base();
        var left = new ConditionalHolonPropertyWriteRequest(f.Request, new Dictionary<Guid, IReadOnlyList<string>> {
            [f.First.Id] = new[] { "/Name", "/Description" }, [f.Second.Id] = new[] { "/Name" } });
        var right = new ConditionalHolonPropertyWriteRequest(f.Request, new Dictionary<Guid, IReadOnlyList<string>> {
            [f.Second.Id] = new[] { "/Name" }, [f.First.Id] = new[] { "/Description", "/Name" } });
        left.CanonicalSelectionJson.Should().Be(right.CanonicalSelectionJson);
        var parsed = JObject.Parse(left.CanonicalSelectionJson);
        parsed.Value<int>("format").Should().Be(1); ((JArray)parsed["selections"]!).Count.Should().Be(2);
        var changed = new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.First.Id, "/Name"));
        changed.CanonicalSelectionJson.Should().NotBe(left.CanonicalSelectionJson);
    }

    [Fact]
    public void CombinationUsesTheNativeUnselectedValuesAndDoesNotAcceptATrackingBaseline()
    {
        var f = Base(); f.First.AcceptChanges();
        var request = new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.First.Id, "/Description"));
        var submitted = StoredRecordJson.Project<Holon>(f.First);
        submitted.Name = "do not save"; submitted.Description = "save this";
        var result = request.ApplyToAuthoritative(f.First, submitted);
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Name.Should().Be("first native"); result.Result.Description.Should().Be("save this");
        HolonChangeTracking.GetPropertyValues(f.First, "/Description").Result.OriginalJson.Should().Be("\"native description\"");
        submitted.Name.Should().Be("do not save"); f.First.Description.Should().Be("native description");
    }

    [Theory]
    [InlineData(ConditionalHolonOperation.SoftDelete)] [InlineData(ConditionalHolonOperation.HardDelete)]
    public void SelectedIntentCannotBeUsedForDeletes(ConditionalHolonOperation operation)
    {
        var f = Base(operation: operation);
        Action create = () => new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.First.Id, "/Name"));
        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SelectedCreateOnlyAndOutOfGraphIdentitiesAreRejected()
    {
        var f = Base(createSecond: true);
        Action createOnly = () => new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.Second.Id, "/Name"));
        Action outside = () => new ConditionalHolonPropertyWriteRequest(f.Request, Select(Guid.NewGuid(), "/Name"));
        createOnly.Should().Throw<ArgumentException>(); outside.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NullBaseOrSelectionCannotAcquirePatchSemantics(bool missingBase)
    {
        var f = Base();
        Action create = () => new ConditionalHolonPropertyWriteRequest(missingBase ? null! : f.Request,
            missingBase ? Select(f.First.Id, "/Name") : null!);
        create.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("Name")] [InlineData("/MetaData/$type")] [InlineData("/Name~2")]
    public void MalformedAndSerializerPathsCannotBeCaptured(string path)
    {
        var f = Base();
        Action create = () => new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.First.Id, path));
        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EmptyAndOverlappingPathSetsAreRejected()
    {
        var f = Base();
        Action noRecords = () => new ConditionalHolonPropertyWriteRequest(f.Request, new Dictionary<Guid, IReadOnlyList<string>>());
        Action noPaths = () => new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.First.Id));
        Action overlapping = () => new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.First.Id, "/MetaData", "/MetaData/x"));
        noRecords.Should().Throw<ArgumentException>(); noPaths.Should().Throw<ArgumentException>();
        overlapping.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OperationWidePathBoundCannotBeBypassedWithManyIndividuallyValidRecords()
    {
        var records = Enumerable.Range(0, 17).Select(_ => new Holon { Id = Guid.NewGuid() }).ToArray();
        var write = new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS,
            records.Select(record => new HolonWriteCondition(record.Id, Guid.Empty)));
        var basis = new ConditionalHolonWriteRequest(write, ConditionalHolonOperation.Save, records, false, false, 0);
        var paths = Enumerable.Range(0, 256).Select(index => "/MetaData/value" + index).ToArray();
        var maximum = records.Take(16).ToDictionary(record => record.Id, _ => (IReadOnlyList<string>)paths);
        var accepted = new ConditionalHolonPropertyWriteRequest(basis, maximum);
        accepted.Selections.Values.Sum(value => value.Count).Should().Be(4096);
        maximum.Add(records[16].Id, paths);
        Action excessive = () => new ConditionalHolonPropertyWriteRequest(basis, maximum);
        excessive.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ARecordWithoutSelectedIntentIsRefusedInsteadOfReceivingAWholeRecordWrite()
    {
        var f = Base(); var request = new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.First.Id, "/Name"));
        var result = request.ApplyToAuthoritative(f.Second, f.Second);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("COSMIC_PROPERTY_SELECTION_MISSING");
        result.Result.Should().BeNull();
    }

    [Fact]
    public void ProtectedFieldsAndWrongNativeIdentitiesRemainExplicitFailures()
    {
        var f = Base(); var request = new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.First.Id, "/Version"));
        request.ApplyToAuthoritative(f.First, f.First).IsError.Should().BeTrue();
        var name = new ConditionalHolonPropertyWriteRequest(f.Request, Select(f.First.Id, "/Name"));
        name.ApplyToAuthoritative(f.Second, f.First).IsError.Should().BeTrue();
        name.ApplyToAuthoritative(null!, f.First).IsError.Should().BeTrue();
        name.ApplyToAuthoritative(f.First, null!).IsError.Should().BeTrue();
    }
}
