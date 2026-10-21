using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicIntegerFieldPromotionTests
{
    [Fact]
    public void LosslessIntegerFieldPromotionsRetainBothOriginalDomains()
    {
        Expression<Func<Holon,bool>>[] expressions={
            h=>CosmicQueryMetadata.Value<int>(h,"left")<CosmicQueryMetadata.Value<long>(h,"right"),
            h=>CosmicQueryMetadata.Value<uint>(h,"left")<CosmicQueryMetadata.Value<int>(h,"right"),
            h=>CosmicQueryMetadata.Value<int?>(h,"left")==CosmicQueryMetadata.Value<long?>(h,"right")};
        Type[] left={typeof(int),typeof(uint),typeof(int?)};
        Type[] right={typeof(long),typeof(int),typeof(long?)};
        for(var index=0;index<expressions.Length;index++)
        {
            var result=CosmicQueryExpression.Compile(expressions[index]);
            Assert.False(result.IsError,result.Message);
            var fields=Assert.IsType<CosmicQueryFieldComparison>(result.Result);
            Assert.Equal(left[index],fields.Left.ValueType);Assert.Equal(right[index],fields.Right.ValueType);
        }
    }

    [Fact]
    public void PromotedFieldAdmissionStillRequiresEachExactProviderDomain()
    {
        var query=CosmicQuery<Holon>.Create().Where(h=>CosmicQueryMetadata.Value<int>(h,"left")<CosmicQueryMetadata.Value<long>(h,"right"));
        Assert.False(query.IsError,query.Message);
        var narrow=new CosmicQueryMetadataCapability(typeof(int),new[]{CosmicQueryComparisonOperator.LessThan});
        var wide=new CosmicQueryMetadataCapability(typeof(long),new[]{CosmicQueryComparisonOperator.LessThan});
        var features=CosmicQueryFeatures.Metadata|CosmicQueryFeatures.FieldComparison;
        Assert.True(new CosmicQueryCapabilities(features,Array.Empty<CosmicQueryFieldCapability>(),new[]{narrow}).Admit(query.Result).IsError);
        Assert.False(new CosmicQueryCapabilities(features,Array.Empty<CosmicQueryFieldCapability>(),new[]{narrow,wide}).Admit(query.Result).IsError);
    }

    [Fact]
    public void NarrowingNullableErasureAndLossyFloatFieldConversionsStayRejected()
    {
        Assert.True(CosmicQueryExpression.Compile<Holon>(h=>(int)CosmicQueryMetadata.Value<long>(h,"left")==h.Version).IsError);
        Assert.True(CosmicQueryExpression.Compile<Holon>(h=>(int)CosmicQueryMetadata.Value<int?>(h,"left")==h.Version).IsError);
        Assert.True(CosmicQueryExpression.Compile<Holon>(h=>(double)CosmicQueryMetadata.Value<long>(h,"left")==CosmicQueryMetadata.Value<double>(h,"right")).IsError);
    }
}
