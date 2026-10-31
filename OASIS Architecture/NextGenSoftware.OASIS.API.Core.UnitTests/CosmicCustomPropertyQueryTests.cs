using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicCustomPropertyQueryTests
{
    public class Row:Holon
    {
        public static int Reads;
        [CustomOASISProperty] public virtual int Rating {get{Reads++;throw new InvalidOperationException("Query compilation must not execute a getter.");}set{}}
        [CustomOASISProperty] public string Label {get;set;}="";
        [CustomOASISProperty] public bool Enabled {get;set;}
        [CustomOASISProperty] public int? Optional {get;set;}
        [CustomOASISProperty(StoreAsJsonString=true)] public int JsonRating {get;set;}
        [CustomOASISProperty] public int ReadOnly=>7;
        public int Unmarked {get;set;}
    }
    public class Derived:Row { }
    public class Overridden:Row {public override int Rating {get=>throw new InvalidOperationException();set{}}}
    public class Shadow:Holon {[CustomOASISProperty] public new string Name {get;set;}="";}
    public class FakeCanonical:Holon {[CustomOASISProperty] public new int CreatedDate {get;set;}}
    public class DangerousMarker:CustomOASISProperty
    {
        public static int Calls;
        public DangerousMarker(){Calls++;throw new InvalidOperationException("Query compilation must not instantiate custom attributes.");}
    }
    public class Untrusted:Holon {[DangerousMarker] public int Rating {get;set;}}

    [Theory]
    [InlineData("comparison")] [InlineData("string")] [InlineData("boolean")]
    [InlineData("nullable")] [InlineData("membership")]
    public void AnnotatedScalarPropertiesBecomeLiteralMetadataPathsWithoutReadingValues(string form)
    {
        Row.Reads=0;var ratings=new[]{7,8};
        Expression<Func<Row,bool>> expression=form switch{
            "comparison"=>row=>row.Rating>=7,
            "string"=>row=>row.Label.Contains("coffee"),
            "boolean"=>row=>row.Enabled,
            "nullable"=>row=>row.Optional==null,
            _=>row=>ratings.Contains(row.Rating)};
        var compiled=CosmicQueryExpression.Compile(expression);Assert.False(compiled.IsError,compiled.Message);
        var path=compiled.Result switch{
            CosmicQueryComparison comparison=>comparison.Path,
            CosmicQueryStringMatch match=>match.Path,
            CosmicQueryMembership membership=>membership.Path,
            _=>throw new InvalidOperationException("Expected one scalar path.")};
        Assert.True(path.IsMetadata);Assert.Single(path.Segments);
        Assert.Equal(form=="string"?"Label":form=="boolean"?"Enabled":form=="nullable"?"Optional":"Rating",path.Segments[0]);
        Assert.Equal(0,Row.Reads);
    }
    [Fact]
    public void InheritedPropertyUsesItsDeclaredPersistentKey()
    {
        var compiled=CosmicQueryExpression.Compile<Derived>(row=>row.Rating>3);
        Assert.False(compiled.IsError,compiled.Message);Assert.True(Assert.IsType<CosmicQueryComparison>(compiled.Result).Path.IsMetadata);
    }
    [Fact]
    public void OverriddenPropertyInheritsItsTrustedPersistenceMarker()
    {
        var compiled=CosmicQueryExpression.Compile<Overridden>(row=>row.Rating>3);
        Assert.False(compiled.IsError,compiled.Message);Assert.Equal("Rating",Assert.IsType<CosmicQueryComparison>(compiled.Result).Path.Segments.Single());
    }
    [Theory]
    [InlineData("json-string")] [InlineData("read-only")] [InlineData("unmarked")]
    public void PropertiesWithoutNativeScalarPersistenceAreNotInferred(string form)
    {
        Expression<Func<Row,bool>> expression=form switch{
            "json-string"=>row=>row.JsonRating>0,"read-only"=>row=>row.ReadOnly>0,_=>row=>row.Unmarked>0};
        Assert.True(CosmicQueryExpression.Compile(expression).IsError);
    }
    [Fact]
    public void CustomMarkerCannotReinterpretCanonicalPropertiesAsMetadata()
    {
        Assert.True(CosmicQueryExpression.Compile<Shadow>(row=>row.Name=="forged").IsError);
        Assert.True(CosmicQueryExpression.Compile<FakeCanonical>(row=>row.CreatedDate==7).IsError);
    }
    [Fact]
    public void AttributeSubclassConstructorsAreNeverExecuted()
    {
        DangerousMarker.Calls=0;Assert.True(CosmicQueryExpression.Compile<Untrusted>(row=>row.Rating==7).IsError);
        Assert.Equal(0,DangerousMarker.Calls);
    }
}
