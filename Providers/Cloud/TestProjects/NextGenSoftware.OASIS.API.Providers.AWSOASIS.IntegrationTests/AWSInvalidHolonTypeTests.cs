using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public async Task NativeInvalidTypeCannotBecomeSuccessfulEmptyResult(int selector,bool synchronous)
    {
        var id=Guid.NewGuid();var type=(HolonType)int.MaxValue;
        var result=selector switch
        {
            0=>synchronous?_provider.LoadAllHolons(type,loadChildren:false):await _provider.LoadAllHolonsAsync(type,loadChildren:false),
            1=>synchronous?_provider.LoadHolonsForParent(id,type,loadChildren:false):await _provider.LoadHolonsForParentAsync(id,type,loadChildren:false),
            _=>synchronous?_provider.LoadHolonsForParent(id.ToString(),type,loadChildren:false):await _provider.LoadHolonsForParentAsync(id.ToString(),type,loadChildren:false)
        };
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
    }
}
