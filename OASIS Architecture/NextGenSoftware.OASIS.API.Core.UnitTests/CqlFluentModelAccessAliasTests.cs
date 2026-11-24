using System;
using Xunit;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlFluentModelAccessAliasTests
{
    public sealed class Scalar:Holon
    {
        public static int GetterCalls;
        public int Value {get{GetterCalls++;return 1;}set{}}
    }
    public sealed class AnnotatedAccessScalar:Holon
    {
        public static int GetterCalls;
        [CustomOASISProperty(MetadataKey=HolonPermissions.MetadataKey)]
        public int Value {get{GetterCalls++;return 1;}set{}}
    }

    [Fact]
    public void AccessMetadataAliasesFailBeforeApplicationGettersForBothMappingInputs()
    {
        Scalar.GetterCalls=0;AnnotatedAccessScalar.GetterCalls=0;
        var fluent=new CosmicModelBuilder();
        Assert.Throws<ArgumentException>(()=>fluent.Entity<Scalar>()
            .Property(row=>row.Value).HasMetadataKey(HolonPermissions.MetadataKey));
        var annotated=CosmicQuery<AnnotatedAccessScalar>.Create().Where(row=>row.Value==1);
        Assert.True(annotated.IsError);Assert.Null(annotated.Result);
        Assert.Equal(0,Scalar.GetterCalls);Assert.Equal(0,AnnotatedAccessScalar.GetterCalls);
        var ordinary=new CosmicModelBuilder();
        ordinary.Entity<Scalar>().Property(row=>row.Value).HasMetadataKey("score_value");
        Assert.False(ordinary.Build().IsError);
        Assert.Equal(0,Scalar.GetterCalls);
    }
}
