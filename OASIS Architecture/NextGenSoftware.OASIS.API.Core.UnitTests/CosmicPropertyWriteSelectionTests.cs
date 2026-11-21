using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicPropertyWriteSelectionTests
{
    private static Holon Stored() => new() { Id = Guid.NewGuid(), Name = "native name", Description = "native description",
        CreatedByAvatarId = Guid.NewGuid(), Version = 7, VersionId = Guid.NewGuid(), IsActive = true,
        MetaData = new() { ["long"] = 9007199254740993L, ["nullable"] = null!,
            ["branch"] = new Dictionary<string, object> { ["selected"] = 1L, ["sibling"] = 2L },
            ["bytes"] = new byte[] { 1, 2, 3 }, ["array"] = new[] { 1, 2 } } };

    private static IHolon Apply(Holon stored, Holon submitted, params string[] paths)
    {
        var result = new HolonPropertyWriteSelection(paths).Apply(stored, submitted);
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        return result.Result;
    }

    [Fact]
    public void OnlySelectedValuesAreCombinedWithNativeRecordAndInputsRemainUntouched()
    {
        var stored = Stored(); var submitted = StoredRecordJson.Project<Holon>(stored);
        submitted.Name = "local unsaved name"; submitted.Description = "chosen description";
        submitted.CreatedByAvatarId = Guid.NewGuid(); submitted.Version = 100;
        var before = StoredRecordJson.CaptureRuntimeToken(stored).ToString();
        var input = StoredRecordJson.CaptureRuntimeToken(submitted).ToString();
        var merged = Apply(stored, submitted, "/Description");
        merged.Name.Should().Be(stored.Name); merged.Description.Should().Be(submitted.Description);
        merged.CreatedByAvatarId.Should().Be(stored.CreatedByAvatarId); merged.Version.Should().Be(7);
        merged.VersionId.Should().Be(stored.VersionId);
        StoredRecordJson.CaptureRuntimeToken(stored).ToString().Should().Be(before);
        StoredRecordJson.CaptureRuntimeToken(submitted).ToString().Should().Be(input);
    }

    [Fact]
    public void NestedSelectionPreservesUnselectedNativeSiblings()
    {
        var stored = Stored(); var submitted = StoredRecordJson.Project<Holon>(stored);
        ((Dictionary<string, object>)submitted.MetaData["branch"])["selected"] = 9L;
        ((Dictionary<string, object>)submitted.MetaData["branch"])["sibling"] = 99L;
        var merged = Apply(stored, submitted, "/MetaData/branch/selected");
        var branch = (Dictionary<string, object>)merged.MetaData["branch"];
        branch["selected"].Should().Be(9L); branch["sibling"].Should().Be(2L);
    }

    [Fact]
    public void MissingAndNullValuesRemainDistinct()
    {
        var stored = Stored(); var submitted = StoredRecordJson.Project<Holon>(stored);
        submitted.MetaData.Remove("nullable"); submitted.MetaData["long"] = null!;
        var merged = Apply(stored, submitted, "/MetaData/nullable", "/MetaData/long");
        merged.MetaData.ContainsKey("nullable").Should().BeFalse();
        merged.MetaData.ContainsKey("long").Should().BeTrue(); merged.MetaData["long"].Should().BeNull();
        stored.MetaData.ContainsKey("nullable").Should().BeTrue();
    }

    [Fact]
    public void WholeArraysAndBytesAreDetachedWithoutLosingRuntimeTypesOrPrecision()
    {
        var stored = Stored(); var submitted = StoredRecordJson.Project<Holon>(stored);
        var bytes = new byte[] { 4, 5 }; var array = new[] { 8, 9 };
        submitted.MetaData["bytes"] = bytes; submitted.MetaData["array"] = array;
        submitted.MetaData["long"] = 9007199254740994L;
        var merged = Apply(stored, submitted, "/MetaData/bytes", "/MetaData/array", "/MetaData/long");
        merged.MetaData["bytes"].Should().BeOfType<byte[]>().Which.Should().Equal(4, 5);
        merged.MetaData["array"].Should().BeOfType<int[]>().Which.Should().Equal(8, 9);
        merged.MetaData["long"].Should().Be(9007199254740994L);
        bytes[0] = 0; array[0] = 0;
        ((byte[])merged.MetaData["bytes"])[0].Should().Be(4);
        ((int[])merged.MetaData["array"])[0].Should().Be(8);
    }

    [Fact]
    public void NewTypedAncestorsComeFromProposalWithoutCopyingTheirUnselectedSiblings()
    {
        var stored = Stored(); var submitted = StoredRecordJson.Project<Holon>(stored);
        submitted.MetaData["new"] = new Dictionary<string, object> { ["selected"] = 5L, ["unselected"] = 6L };
        var merged = Apply(stored, submitted, "/MetaData/new/selected");
        var branch = (Dictionary<string, object>)merged.MetaData["new"];
        branch.Should().ContainSingle().Which.Key.Should().Be("selected");
        branch["selected"].Should().Be(5L);
    }

    [Theory]
    [InlineData("/Id")] [InlineData("/CreatedByAvatarId")] [InlineData("/Version")]
    [InlineData("/ProviderMetaData")] [InlineData("/ParentHolonId")] [InlineData("/DeletedDate")]
    public void NativeIdentityAuditRevisionAndRelationshipFieldsAreRejected(string path)
    {
        var stored = Stored(); var submitted = StoredRecordJson.Project<Holon>(stored);
        var result = new HolonPropertyWriteSelection(new[] { path }).Apply(stored, submitted);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("HOLON_PROPERTY_SELECTION_INVALID");
    }

    [Theory]
    [InlineData("/MetaData/array/0")] [InlineData("/MetaData/long/child")]
    [InlineData("/MetaData/nullable/child")] [InlineData("/MetaData/missing")]
    public void ArrayElementsScalarAncestorsNullAncestorsAndAbsentValuesAreRejected(string path)
    {
        var stored = Stored(); var submitted = StoredRecordJson.Project<Holon>(stored);
        new HolonPropertyWriteSelection(new[] { path }).Apply(stored, submitted).IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData("Name")] [InlineData("/Name~2")] [InlineData("/MetaData/$type")]
    [InlineData("/MetaData/$values/0")] [InlineData("/MetaData/$ref")]
    public void InvalidPointersAndSerializerStructureAreRejectedAtCapture(string path)
    {
        Action capture = () => new HolonPropertyWriteSelection(new[] { path });
        capture.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SelectionIsBoundedImmutableOrdinalAndRejectsNonAdjacentOverlap()
    {
        var input = new[] { "/Name", "/Description" };
        var selection = new HolonPropertyWriteSelection(input); input[0] = "/Version";
        selection.Paths.Should().Equal("/Description", "/Name");
        Action mutate = () => ((IList<string>)selection.Paths)[0] = "/Id";
        mutate.Should().Throw<NotSupportedException>();
        Action empty = () => new HolonPropertyWriteSelection(Array.Empty<string>());
        Action duplicate = () => new HolonPropertyWriteSelection(new[] { "/Name", "/Name" });
        Action overlap = () => new HolonPropertyWriteSelection(new[] { "/MetaData/a", "/MetaData/a-", "/MetaData/a/x" });
        Action excessive = () => new HolonPropertyWriteSelection(Enumerable.Repeat("/Name", 257));
        empty.Should().Throw<ArgumentException>(); duplicate.Should().Throw<ArgumentException>();
        overlap.Should().Throw<ArgumentException>(); excessive.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DifferentOrAbsentNativeIdentitiesCannotBeCombined()
    {
        var stored = Stored(); var submitted = Stored();
        var selection = new HolonPropertyWriteSelection(new[] { "/Name" });
        selection.Apply(stored, submitted).IsError.Should().BeTrue();
        submitted.Id = stored.Id = Guid.Empty;
        selection.Apply(stored, submitted).IsError.Should().BeTrue();
        selection.Apply(null!, submitted).IsError.Should().BeTrue();
    }

    [Fact]
    public void EscapedMetadataKeysAreSelectedExactlyWithoutChangingDifferentCaseKeys()
    {
        var stored = Stored(); stored.MetaData["a/b~c"] = 1L; stored.MetaData["A/b~c"] = 2L;
        var submitted = StoredRecordJson.Project<Holon>(stored);
        submitted.MetaData["a/b~c"] = 3L; submitted.MetaData["A/b~c"] = 4L;
        var merged = Apply(stored, submitted, "/MetaData/a~1b~0c");
        merged.MetaData["a/b~c"].Should().Be(3L); merged.MetaData["A/b~c"].Should().Be(2L);
    }
}
