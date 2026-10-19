using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleGraphAdmissionTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
    public async Task InvalidGraphDepthRejectsBeforeConnection(int route,bool synchronous)
    {
        var provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");var id=Guid.NewGuid();
        switch(route){
            case 0:Check(synchronous?provider.LoadHolon(id,maxChildDepth:-1):await provider.LoadHolonAsync(id,maxChildDepth:-1));break;
            case 1:Check(synchronous?provider.LoadAllHolons(maxChildDepth:-1):await provider.LoadAllHolonsAsync(maxChildDepth:-1));break;
            case 2:Check(synchronous?provider.LoadHolonsForParent(id,curentChildDepth:-1):await provider.LoadHolonsForParentAsync(id,curentChildDepth:-1));break;
            default:Check(synchronous?provider.LoadHolonsByMetaData("owned","value",maxChildDepth:-1):await provider.LoadHolonsByMetaDataAsync("owned","value",maxChildDepth:-1));break;
        }
        static void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);}
    }
}
