using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryExpressionTests
{
    [Fact]
    public void IntegerPromotionsRequireWholeDomainContainmentAndRetainOriginalPathTypes()
    {
        Expression<Func<Holon,bool>>[] allowed={
            h=>CosmicQueryMetadata.Value<byte>(h,"x")>=0,
            h=>CosmicQueryMetadata.Value<sbyte>(h,"x")>=0,
            h=>CosmicQueryMetadata.Value<short>(h,"x")>=0,
            h=>CosmicQueryMetadata.Value<ushort>(h,"x")>=0,
            h=>(ushort)CosmicQueryMetadata.Value<byte>(h,"x")>=0,
            h=>(short)CosmicQueryMetadata.Value<sbyte>(h,"x")>=0,
            h=>(float)CosmicQueryMetadata.Value<ushort>(h,"x")>=0f,
            h=>(double)CosmicQueryMetadata.Value<uint>(h,"x")>=0d};
        Type[] types={typeof(byte),typeof(sbyte),typeof(short),typeof(ushort),typeof(byte),typeof(sbyte),typeof(ushort),typeof(uint)};
        for(var index=0;index<allowed.Length;index++)
        {
            var result=CosmicQueryExpression.Compile(allowed[index]);Assert.False(result.IsError,result.Message);
            Assert.Equal(types[index],Assert.IsType<CosmicQueryComparison>(result.Result).Path.ValueType);
        }
        Expression<Func<Holon,bool>>[] rejected={
            h=>(byte)CosmicQueryMetadata.Value<sbyte>(h,"x")>=0,
            h=>(sbyte)CosmicQueryMetadata.Value<byte>(h,"x")>=0,
            h=>(int)CosmicQueryMetadata.Value<uint>(h,"x")>=0,
            h=>(ushort)CosmicQueryMetadata.Value<short>(h,"x")>=0,
            h=>(float)CosmicQueryMetadata.Value<int>(h,"x")>=0f,
            h=>(double)CosmicQueryMetadata.Value<long>(h,"x")>=0d,
            h=>(int)CosmicQueryMetadata.Value<short?>(h,"x")>=0};
        foreach(var expression in rejected)Assert.True(CosmicQueryExpression.Compile(expression).IsError);
    }

    [Fact]
    public void BuiltInNullableLiteralLiftsPreserveBooleanGuidAndUtcValues()
    {
        var id = Guid.NewGuid(); var utc = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        var flag = CosmicQueryExpression.Compile<Holon>(h => CosmicQueryMetadata.Value<bool?>(h, "flag") != true);
        Assert.False(flag.IsError, flag.Message);
        Assert.Equal(true, Assert.IsType<CosmicQueryComparison>(flag.Result).Literal.Value);
        var guid = CosmicQueryExpression.Compile<Holon>(h => CosmicQueryMetadata.Value<Guid?>(h, "id") == id);
        Assert.False(guid.IsError, guid.Message); Assert.Equal(id, Assert.IsType<CosmicQueryComparison>(guid.Result).Literal.Value);
        var date = CosmicQueryExpression.Compile<Holon>(h => CosmicQueryMetadata.Value<DateTime?>(h, "at") == utc);
        Assert.False(date.IsError, date.Message); Assert.Equal(utc, Assert.IsType<CosmicQueryComparison>(date.Result).Literal.Value);
    }

    [Fact]
    public void MetadataPresenceAndNullMarkersCaptureLiteralSegmentsWithoutExecutingAccessors()
    {
        var key = "a.b\"'; --";
        var result = CosmicQueryExpression.Compile<Holon>(h => CosmicQueryMetadata.Exists(h, "nested", key) && !CosmicQueryMetadata.IsNull(h, "status"));
        Assert.False(result.IsError, result.Message); key = "changed";
        var logical = Assert.IsType<CosmicQueryLogical>(result.Result);
        var exists = Assert.IsType<CosmicQueryMetadataTest>(logical.Left);
        Assert.Equal(CosmicQueryMetadataTestKind.Exists, exists.Kind); Assert.Equal(new[] { "nested", "a.b\"'; --" }, exists.Segments);
        var nullTest = Assert.IsType<CosmicQueryMetadataTest>(Assert.IsType<CosmicQueryNot>(logical.Right).Operand);
        Assert.Equal(CosmicQueryMetadataTestKind.IsNull, nullTest.Kind);
        Assert.Throws<InvalidOperationException>(() => CosmicQueryMetadata.Exists(new Holon(), "x"));
    }

    [Fact]
    public void MetadataMarkerAdmissionRejectsUnboundedPathsAndForeignOwners()
    {
        var foreign = new Holon();
        Assert.True(CosmicQueryExpression.Compile<Holon>(h => CosmicQueryMetadata.Exists(foreign, "x")).IsError);
        var empty = CosmicQueryExpression.Compile<Holon>(h => CosmicQueryMetadata.IsNull(h, ""));
        Assert.True(empty.IsError); Assert.Equal("CQL_EXPRESSION_INVALID", empty.ErrorCode);
        Assert.True(CosmicQueryExpression.Compile<Holon>(h => CosmicQueryMetadata.Exists(h)).IsError);
    }

    [Fact]
    public void CapturedEnumsAreFrozenAndUndefinedEnumValuesAreRejected()
    {
        var selected=HolonType.Quest;
        var result=CosmicQueryExpression.Compile<Holon>(h=>h.HolonType==selected);
        Assert.False(result.IsError,result.Message);selected=HolonType.Avatar;
        Assert.Equal(Convert.ToInt64(HolonType.Quest),Assert.IsType<CosmicQueryComparison>(result.Result).Literal.Value);
        selected=(HolonType)int.MaxValue;
        var invalid=CosmicQueryExpression.Compile<Holon>(h=>h.HolonType==selected);
        Assert.True(invalid.IsError);Assert.Equal("CQL_EXPRESSION_INVALID",invalid.ErrorCode);
    }

    [Fact]
    public void InterfaceRootsAndNullableMetadataNullTestsUseTheCanonicalContract()
    {
        var nullable=CosmicQueryExpression.Compile<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(h=>CosmicQueryMetadata.Value<int?>(h,"score")!=null);
        Assert.False(nullable.IsError,nullable.Message);
        var comparison=Assert.IsType<CosmicQueryComparison>(nullable.Result);
        Assert.True(comparison.Path.IsMetadata);Assert.Equal(CosmicQueryScalarKind.Null,comparison.Literal.Kind);
        Assert.Equal(typeof(int?),comparison.Path.ValueType);
        var boolean=CosmicQueryExpression.Compile<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(h=>h.IsActive);
        Assert.False(boolean.IsError,boolean.Message);Assert.Equal(true,Assert.IsType<CosmicQueryComparison>(boolean.Result).Literal.Value);
    }

    [Fact]
    public void CompoundPredicateFreezesCapturedScalarsWithoutInterpolatingText()
    {
        var minimum=7;var text="x'; DROP TABLE Holon; --";
        var result=CosmicQueryExpression.Compile<Holon>(h=>h.Version>=minimum && (h.Name==text || !h.IsDeleted));
        Assert.False(result.IsError,result.Message);minimum=999;text="changed";
        var root=Assert.IsType<CosmicQueryLogical>(result.Result);Assert.True(root.IsAnd);
        var version=Assert.IsType<CosmicQueryComparison>(root.Left);
        Assert.Equal(CosmicQueryComparisonOperator.GreaterThanOrEqual,version.Operator);Assert.Equal(7L,version.Literal.Value);
        var disjunction=Assert.IsType<CosmicQueryLogical>(root.Right);Assert.False(disjunction.IsAnd);
        Assert.Equal("x'; DROP TABLE Holon; --",Assert.IsType<CosmicQueryComparison>(disjunction.Left).Literal.Value);
        Assert.IsType<CosmicQueryNot>(disjunction.Right);
    }

    [Fact]
    public void TypedMetadataPathAndDecimalRemainExplicitAndImmutable()
    {
        var result=CosmicQueryExpression.Compile<Holon>(h=>CosmicQueryMetadata.Value<decimal>(h,"stats","rating")>10.5m);
        Assert.False(result.IsError,result.Message);var comparison=Assert.IsType<CosmicQueryComparison>(result.Result);
        Assert.True(comparison.Path.IsMetadata);Assert.Equal(new[]{"stats","rating"},comparison.Path.Segments);
        Assert.Equal(CosmicQueryScalarKind.Decimal,comparison.Path.ScalarKind);Assert.Equal(10.5m,comparison.Literal.Value);
        Assert.Equal(typeof(decimal),comparison.Path.ValueType);
        Assert.Throws<NotSupportedException>(()=>((IList<string>)comparison.Path.Segments)[0]="mutated");
    }

    [Fact]
    public void ReversedComparisonNullBooleanAndEnumAreNormalized()
    {
        var reverse=CosmicQueryExpression.Compile<Holon>(h=>100<h.Version);
        Assert.False(reverse.IsError,reverse.Message);
        Assert.Equal(CosmicQueryComparisonOperator.GreaterThan,Assert.IsType<CosmicQueryComparison>(reverse.Result).Operator);
        var nullTest=CosmicQueryExpression.Compile<Holon>(h=>h.Description==null);
        Assert.False(nullTest.IsError,nullTest.Message);Assert.Equal(CosmicQueryScalarKind.Null,Assert.IsType<CosmicQueryComparison>(nullTest.Result).Literal.Kind);
        var constant=CosmicQueryExpression.Compile<Holon>(h=>true);
        Assert.True(Assert.IsType<CosmicQueryBoolean>(constant.Result).Value);
        var typed=CosmicQueryExpression.Compile<Holon>(h=>h.HolonType==HolonType.Quest);
        Assert.False(typed.IsError,typed.Message);Assert.Equal(Convert.ToInt64(HolonType.Quest),Assert.IsType<CosmicQueryComparison>(typed.Result).Literal.Value);
    }

    private sealed class GetterValue {public int Calls;public string Text {get{Calls++;return "never invoked";}}}
    private sealed class ShadowHolon:Holon {public static int Calls;public new string Name {get{Calls++;return "shadow";}}}

    [Fact]
    public void GettersMethodsAndShadowPropertiesAreNotExecutedOrSilentlyTranslated()
    {
        var getter=new GetterValue();
        var property=CosmicQueryExpression.Compile<Holon>(h=>h.Name==getter.Text);
        Assert.True(property.IsError);Assert.Equal("CQL_EXPRESSION_UNSUPPORTED",property.ErrorCode);Assert.Equal(0,getter.Calls);
        var method=CosmicQueryExpression.Compile<Holon>(h=>h.Name.StartsWith("prefix"));
        Assert.True(method.IsError);Assert.Equal("CQL_EXPRESSION_UNSUPPORTED",method.ErrorCode);
        ShadowHolon.Calls=0;var shadow=CosmicQueryExpression.Compile<ShadowHolon>(h=>h.Name=="shadow");
        Assert.True(shadow.IsError);Assert.Equal(0,ShadowHolon.Calls);
    }

    [Fact]
    public void LossyFieldConversionsAreRefusedInsteadOfChangingPredicateMeaning()
    {
        var lossy=CosmicQueryExpression.Compile<Holon>(h=>(float)h.Version==16777216f);
        Assert.True(lossy.IsError);Assert.Equal("CQL_EXPRESSION_UNSUPPORTED",lossy.ErrorCode);
        var widening=CosmicQueryExpression.Compile<Holon>(h=>(long)h.Version>7L);
        Assert.False(widening.IsError,widening.Message);
    }

    [Fact]
    public void LiteralKindsAndPathBoundsFailExplicitly()
    {
        var date=new DateTime(2026,1,1,0,0,0,DateTimeKind.Unspecified);
        var invalidDate=CosmicQueryExpression.Compile<Holon>(h=>h.CreatedDate>date);
        Assert.True(invalidDate.IsError);Assert.Equal("CQL_EXPRESSION_INVALID",invalidDate.ErrorCode);
        var nonfinite=CosmicQueryExpression.Compile<Holon>(h=>CosmicQueryMetadata.Value<double>(h,"score")==double.NaN);
        Assert.True(nonfinite.IsError);Assert.Equal("CQL_EXPRESSION_INVALID",nonfinite.ErrorCode);
        var emptyPath=CosmicQueryExpression.Compile<Holon>(h=>CosmicQueryMetadata.Value<int>(h)>0);
        Assert.True(emptyPath.IsError);
        var text=new string('x',CosmicQueryExpression.MaximumStringLength+1);
        Assert.True(CosmicQueryExpression.Compile<Holon>(h=>h.Name==text).IsError);
    }

    [Fact]
    public void DeepExpressionsAreBoundedBeforeAnyClientExecution()
    {
        var parameter=Expression.Parameter(typeof(Holon),"h");Expression body=Expression.Constant(true);
        for(var index=0;index<CosmicQueryExpression.MaximumDepth+2;index++)
            body=Expression.AndAlso(body,Expression.GreaterThan(Expression.Property(parameter,nameof(Holon.Version)),Expression.Constant(index)));
        var result=CosmicQueryExpression.Compile(Expression.Lambda<Func<Holon,bool>>(body,parameter));
        Assert.True(result.IsError);Assert.Equal("CQL_EXPRESSION_INVALID",result.ErrorCode);Assert.Null(result.Result);
    }
}
