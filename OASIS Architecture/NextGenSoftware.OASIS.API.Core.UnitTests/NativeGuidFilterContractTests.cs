using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class NativeGuidFilterContractTests
{
    [Theory]
    [InlineData(CosmicQueryComparisonOperator.Equal)]
    [InlineData(CosmicQueryComparisonOperator.NotEqual)]
    public void GuidPredicateRetainsExactDomainAcrossFieldMapping(CosmicQueryComparisonOperator operation)
    {
        var value = Guid.Parse("AB8079E8-5E72-4FF9-81B7-8FECE973C50A");
        var filter = new NativeRecordFilter("alias", operation, value);
        var mapped = filter.WithField("identity");
        Assert.Equal(typeof(Guid), mapped.InputType);
        Assert.Equal(value, Assert.IsType<Guid>(mapped.Value));
        Assert.Equal(operation, mapped.Operator);
        Assert.Equal("alias", filter.Field);
        Assert.Equal("identity", mapped.Field);
    }
    [Theory]
    [InlineData(CosmicQueryComparisonOperator.GreaterThan)]
    [InlineData(CosmicQueryComparisonOperator.GreaterThanOrEqual)]
    [InlineData(CosmicQueryComparisonOperator.LessThan)]
    [InlineData(CosmicQueryComparisonOperator.LessThanOrEqual)]
    public void GuidPredicateRefusesUndeclaredRelationalSemantics(CosmicQueryComparisonOperator operation)
        => Assert.Throws<ArgumentException>(() => new NativeRecordFilter("identity", operation, Guid.NewGuid()));
    [Fact]
    public void EqualityCapturePreservesUuidInsteadOfConvertingToText()
    {
        var value = Guid.NewGuid();
        var matches = new List<KeyValuePair<string, object>> { new("identity", value) };
        var captured = NativeRecordFilter.CaptureEqualities(matches);
        matches.Clear();
        Assert.Equal(value, Assert.IsType<Guid>(Assert.Single(captured).Value));
    }
}
