using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicLinqProjectionElementTests
{
    public sealed class NumericDto
    {
        public static int Constructions;
        public NumericDto() => Constructions++;
        public int Revision { get; set; }
    }

    [Fact]
    public void StagedIntegerReductionRequiresIndependentEnrollmentAndConstructsNoDto()
    {
        var native = new Native { NumericValues = new object?[] { 1, 2, 3 } };
        var selected = Root(native).Select(row => new NumericDto { Revision = row.Version }).Skip(1).Take(2);
        NumericDto.Constructions = 0;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Assert.Throws<CosmicLinqException>(() => selected.Sum(row => row.Revision)).Code);
        Assert.Equal(0, native.Calls);
        native.NumericEnabled = true;
        Assert.Equal(5, selected.Sum(row => row.Revision));
        Assert.Equal(2.5d, selected.Average(row => row.Revision));
        Assert.Equal(0, NumericDto.Constructions);
        Assert.Equal(2, native.Calls);
    }

    [Fact]
    public void MissingParticipationAndWordSchemaFailuresCannotPublishNumericValues()
    {
        var native = new Native { NumericEnabled = true, NumericValues = new object?[] { 1, 2 }, MissingNumeric = true };
        var selected = Root(native).Select(row => row.Version).Take(2);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Assert.Throws<CosmicLinqException>(() => selected.Sum()).Code);
        native.MissingNumeric = false;
        native.NumericCountOverride = 0;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Assert.Throws<CosmicLinqException>(() => selected.Sum()).Code);
        native.NumericCountOverride = null;
        native.NumericPartsOverride = new[] { 1L };
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Assert.Throws<CosmicLinqException>(() => selected.Sum()).Code);
        native.NumericPartsOverride = null;
        native.NumericPrefixOverride = false;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Assert.Throws<CosmicLinqException>(() => selected.Average()).Code);
    }

    [Fact]
    public void EmptyAndNullableSelectedMeansKeepTheirResultDomains()
    {
        var native = new Native { NumericEnabled = true, NumericValues = new object?[] { null, 7 } };
        var selected = Root(native).Select(row => CosmicQueryMetadata.Value<int?>(row, "optional")).Take(2);
        Assert.Equal((int?)7, selected.Sum());
        Assert.Equal((double?)7, selected.Average());
        Assert.Equal((int?)0, selected.Take(0).Sum());
        Assert.Null(selected.Take(0).Average());
        native.NumericValues = new object?[] { 7 };
        var required = Root(native).Select(row => row.Version).Take(0);
        Assert.Equal("CQL_EMPTY_SEQUENCE", Assert.Throws<CosmicLinqException>(() => required.Average()).Code);
    }

    [Fact]
    public void OverflowingIntegerSumCanStillHaveAValidDoubleMean()
    {
        var native = new Native { NumericEnabled = true, NumericValues = new object?[] { int.MaxValue, int.MaxValue } };
        var selected = Root(native).Select(row => row.Version).Take(2);
        Assert.Equal("CQL_SCALAR_OVERFLOW", Assert.Throws<CosmicLinqException>(() => selected.Sum()).Code);
        Assert.Equal((double)int.MaxValue, selected.Average());
    }
}
