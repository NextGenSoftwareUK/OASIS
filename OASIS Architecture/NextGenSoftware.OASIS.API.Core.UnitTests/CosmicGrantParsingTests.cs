using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicGrantParsingTests
{
    private static readonly Guid Recipient = Guid.Parse("12345678-1234-1234-1234-123456789abc");

    public static IEnumerable<object[]> InvalidGrants()
    {
        var id = Recipient.ToString("D");
        yield return new object[] { $"[\"{id}\", broken]" };
        yield return new object[] { $"[\"{id}\", true]" };
        yield return new object[] { $"[\"{id}\", null]" };
        yield return new object[] { $"[\"{id}\", \"not-an-id\"]" };
        yield return new object[] { id + ", invalid" };
        yield return new object[] { id + ",," };
        yield return new object[] { $"[\"{id}\",\"{Guid.Empty:D}\"]" };
        yield return new object[] { JsonDocument.Parse($"[\"{id}\",true]").RootElement.Clone() };
        yield return new object[] { new SpoofedRecipientText() };
        yield return new object[] { Enumerable.Repeat(Recipient, 1025).ToArray() };
    }

    [Theory]
    [MemberData(nameof(InvalidGrants))]
    public void MalformedOrOversizedGrantCannotProvidePartialRecipientAccess(object raw)
    {
        var holon = new Holon { CreatedByAvatarId = Guid.NewGuid() };
        holon.MetaData[HolonAccess.SharedAvatarIdsMetaKey] = raw;

        HolonAccess.CanRead(holon, Recipient).Should().BeFalse();
        HolonAccess.GetSharedAvatarIds(holon).Should().BeEmpty();
        holon.MetaData[HolonAccess.SharedAvatarIdsMetaKey].Should().BeSameAs(raw);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyTextRequiresExplicitAdmissionAndCanonicalEncoding(bool commaList)
    {
        var other = Guid.NewGuid();
        var holon = new Holon();
        holon.MetaData[HolonReadGrants.MetadataKey] = commaList
            ? Recipient.ToString("D") + ", " + other.ToString("D") : Recipient.ToString("D");

        HolonReadGrants.Read(holon).ErrorCode.Should().Be(HolonReadGrants.InvalidCode);
        var admitted = HolonReadGrants.Read(holon, allowLegacyText: true);
        admitted.IsError.Should().BeFalse();
        var encoded = HolonReadGrants.Encode(admitted.Result);
        holon.MetaData[HolonReadGrants.MetadataKey] = encoded.Result;
        var canonical = HolonReadGrants.Read(holon);
        canonical.IsError.Should().BeFalse();
        canonical.Result.Should().BeEquivalentTo(commaList ? new[] { Recipient, other } : new[] { Recipient });
    }

    [Fact]
    public void ParsedRecipientsOwnTheirImmutableSnapshot()
    {
        var input = new List<Guid> { Recipient };
        var holon = new Holon();
        holon.MetaData[HolonReadGrants.MetadataKey] = input;
        var parsed = HolonReadGrants.Read(holon);

        input.Clear();

        parsed.Result.Should().Equal(Recipient);
        Action mutate = () => ((ICollection<Guid>)parsed.Result).Add(Guid.NewGuid());
        mutate.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void EncodingAnUnboundedSourceStopsAtTheDeclaredInputLimit()
    {
        var observed = 0;
        IEnumerable<Guid> Infinite()
        {
            while (true) { observed++; yield return Recipient; }
        }

        var encoded = HolonReadGrants.Encode(Infinite());

        encoded.ErrorCode.Should().Be(HolonReadGrants.InvalidCode);
        observed.Should().Be(HolonReadGrants.MaximumRecipients + 1);
    }

    [Fact]
    public void CanonicalGrantLimitIsInclusiveAndOversizedTextIsRejected()
    {
        var ids = Enumerable.Range(0, HolonReadGrants.MaximumRecipients).Select(_ => Guid.NewGuid()).ToArray();
        var encoded = HolonReadGrants.Encode(ids);
        encoded.IsError.Should().BeFalse();
        var holon = new Holon();
        holon.MetaData[HolonReadGrants.MetadataKey] = encoded.Result;
        HolonReadGrants.Read(holon).Result.Should().HaveCount(ids.Length);
        holon.MetaData[HolonReadGrants.MetadataKey] = new string(' ', HolonReadGrants.MaximumEncodedLength) + "[]";
        HolonReadGrants.Read(holon).ErrorCode.Should().Be(HolonReadGrants.InvalidCode);
    }

    [Fact]
    public void InvalidRecipientWritePreservesExistingGrantAndIndexMetadata()
    {
        var holon = new Holon();
        HolonAccess.RecordShares(holon, new[] { Recipient });
        var before = holon.MetaData.ToDictionary(pair => pair.Key, pair => pair.Value);

        Action invalid = () => HolonAccess.RecordShares(holon, Enumerable.Repeat(Recipient, 1025));

        invalid.Should().Throw<ArgumentException>();
        holon.MetaData.Should().BeEquivalentTo(before);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void NewtonsoftRepresentationsUseTheSameWholeGrantValidation(bool scalarWrapper, bool malformed)
    {
        var holon = new Holon();
        if (scalarWrapper)
            holon.MetaData[HolonReadGrants.MetadataKey] = new Newtonsoft.Json.Linq.JValue(malformed
                ? Recipient.ToString("D") + ", invalid" : Recipient.ToString("D"));
        else
            holon.MetaData[HolonReadGrants.MetadataKey] = malformed
                ? new Newtonsoft.Json.Linq.JArray(Recipient.ToString("D"), true)
                : new Newtonsoft.Json.Linq.JArray(Recipient.ToString("D"));

        var parsed = HolonReadGrants.Read(holon, allowLegacyText: true);

        parsed.IsError.Should().Be(malformed);
        if (malformed) parsed.Result.Should().BeNull();
        else parsed.Result.Should().Equal(Recipient);
    }

    private sealed class SpoofedRecipientText
    {
        public override string ToString() => Recipient.ToString("D");
    }
}
