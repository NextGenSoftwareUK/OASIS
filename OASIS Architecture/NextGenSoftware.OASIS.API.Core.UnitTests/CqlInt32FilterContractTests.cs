using System;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlInt32FilterContractTests
{
    private static NativeRecordFilter Compile(JsonElement value, CqlV1Scalar scalar = CqlV1Scalar.Int32)
        => Assert.Single(CqlV1Compiler.CompileVirtualMatches(new[] { new CqlV1Filter {
            MetadataPath = new[] { "stock" }, Scalar = scalar,
            Operator = CqlV1Operator.GreaterThanOrEqual, Value = value
        } }));
    [Theory]
    [InlineData(int.MinValue)] [InlineData(0)] [InlineData(int.MaxValue)]
    public void MappedInt32RetainsExactWidthAndBoundary(int value)
    {
        var filter = Compile(JsonSerializer.SerializeToElement(value));
        Assert.Equal(typeof(int), filter.InputType);
        Assert.Equal(value, Assert.IsType<int>(filter.Value));
        Assert.Equal(value, Assert.IsType<int>(filter.WithField("native_stock").Value));
    }
    [Theory]
    [InlineData(-2147483649L)] [InlineData(2147483648L)]
    public void MappedInt32RefusesOverflowBeforeNativeDispatch(long value)
        => Assert.Throws<ArgumentException>(() => Compile(JsonSerializer.SerializeToElement(value)));
    [Theory]
    [InlineData("\"5\"")] [InlineData("1.25")] [InlineData("null")]
    public void MappedInt32RefusesTextFractionAndNull(string raw)
    {
        using var json = JsonDocument.Parse(raw);
        Assert.Throws<ArgumentException>(() => Compile(json.RootElement));
    }
    [Fact]
    public void AddingInt32DoesNotRenumberExistingWireEnums()
    {
        Assert.Equal(1, (int)CqlV1Scalar.Integer);
        Assert.Equal(3, (int)CqlV1Scalar.Guid);
        Assert.Equal(8, (int)CqlV1Scalar.Single);
        Assert.Equal(9, (int)CqlV1Scalar.Int32);
    }
    [Fact]
    public void ExistingIntegerFilterRetainsItsInt64Width()
    {
        var filter = Compile(JsonSerializer.SerializeToElement(long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)), CqlV1Scalar.Integer);
        Assert.Equal(long.MaxValue, Assert.IsType<long>(filter.Value));
    }
}
