using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class HolonCelestialParentIdsTests
{
    [Fact]
    public void EveryCelestialIdentityRoundTripsWithoutTouchingOtherRootFields()
    {
        var properties=typeof(IHolon).GetProperties().Where(p=>p.PropertyType==typeof(Guid)&&p.Name.StartsWith("Parent")).ToArray();
        Assert.Equal(16,properties.Length);Assert.Equal(properties.Select(p=>p.Name).Order(),typeof(HolonCelestialParentIds).GetProperties().Where(p=>p.PropertyType==typeof(Guid)).Select(p=>p.Name).Order());
        var source=new Holon{Id=Guid.NewGuid(),DimensionLevel=DimensionLevel.Twelfth,SubDimensionLevel=SubDimensionLevel.Fifth};
        foreach(var property in properties)property.SetValue(source,Guid.NewGuid());
        var ids=HolonCelestialParentIds.Capture(source);var target=new Holon{Id=Guid.NewGuid(),ParentHolonId=Guid.NewGuid(),Name="keep"};
        var rootId=target.Id;var parentId=target.ParentHolonId;ids.Apply(target);
        foreach(var property in properties)Assert.Equal(property.GetValue(source),property.GetValue(target));
        Assert.Equal(rootId,target.Id);Assert.Equal(parentId,target.ParentHolonId);Assert.Equal("keep",target.Name);
        Assert.Equal(source.DimensionLevel,target.DimensionLevel);Assert.Equal(source.SubDimensionLevel,target.SubDimensionLevel);
        ids.ParentStarId=Guid.Empty;Assert.NotEqual(Guid.Empty,source.ParentStarId);
    }
    [Fact]
    public void OnlyIdentityAndLevelGettersAreRead()
    {
        var source=System.Reflection.DispatchProxy.Create<IHolon,IdentityOnlyProxy>();
        var result=HolonCelestialParentIds.Capture(source);Assert.Equal(Guid.Empty,result.ParentStarId);
    }
    public class IdentityOnlyProxy:System.Reflection.DispatchProxy
    {
        protected override object? Invoke(System.Reflection.MethodInfo? method,object?[]? args)
        {
            if(method?.Name.StartsWith("get_",StringComparison.Ordinal)==true){
                var scalar=typeof(HolonCelestialParentIds).GetProperty(method.Name.Substring(4));
                if(scalar!=null)return Activator.CreateInstance(scalar.PropertyType);
            }
            throw new InvalidOperationException("Navigation or unrelated source member was accessed.");
        }
    }
    [Theory][InlineData(true)][InlineData(false)]
    public void InvalidLevelsAreRejectedBeforeApplyingAnyIdentity(bool dimension)
    {
        var value=new HolonCelestialParentIds{ParentStarId=Guid.NewGuid()};
        if(dimension)value.DimensionLevel=(DimensionLevel)999;else value.SubDimensionLevel=(SubDimensionLevel)999;
        var target=new Holon();Assert.Throws<ArgumentException>(()=>value.Apply(target));Assert.Equal(Guid.Empty,target.ParentStarId);
    }
}
