using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleCollectionAdmissionTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
    [DataRow(4,false)][DataRow(4,true)][DataRow(5,false)][DataRow(5,true)][DataRow(6,false)][DataRow(6,true)][DataRow(7,false)][DataRow(7,true)]
    [DataRow(8,false)][DataRow(8,true)][DataRow(9,false)][DataRow(9,true)][DataRow(10,false)][DataRow(10,true)]
    public async Task NegativeCollectionVersionRejectsBeforeNativeConnection(int selector,bool synchronous)
    {
        var provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");var id=Guid.NewGuid();
        switch(selector){
            case 0:Check(synchronous?provider.LoadAllAvatars(-1):await provider.LoadAllAvatarsAsync(-1));break;
            case 1:Check(synchronous?provider.LoadAllAvatarDetails(-1):await provider.LoadAllAvatarDetailsAsync(-1));break;
            case 2:Check(synchronous?provider.LoadAllHolons(version:-1):await provider.LoadAllHolonsAsync(version:-1));break;
            case 3:Check(synchronous?provider.LoadHolonsForParent(id,version:-1):await provider.LoadHolonsForParentAsync(id,version:-1));break;
            case 4:Check(synchronous?provider.LoadHolonsByMetaData("owned","value",version:-1):await provider.LoadHolonsByMetaDataAsync("owned","value",version:-1));break;
            case 5:Check(synchronous?provider.LoadHolonsByMetaData(new Dictionary<string,string>{{"owned","value"}},MetaKeyValuePairMatchMode.All,version:-1):await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string>{{"owned","value"}},MetaKeyValuePairMatchMode.All,version:-1));break;
            case 6:Check(synchronous?provider.ExportAll(-1):await provider.ExportAllAsync(-1));break;
            case 7:Check(synchronous?provider.ExportAllDataForAvatarById(id,-1):await provider.ExportAllDataForAvatarByIdAsync(id,-1));break;
            case 8:Check(synchronous?provider.LoadAllHolonsLegacy(version:-1):await provider.LoadAllHolonsAsyncLegacy(version:-1));break;
            case 9:Check(synchronous?provider.LoadHolonsForParentLegacy(id,version:-1):await provider.LoadHolonsForParentAsyncLegacy(id,version:-1));break;
            default:Check(synchronous?provider.LoadHolonsForParentLegacy(id.ToString(),version:-1):await provider.LoadHolonsForParentAsyncLegacy(id.ToString(),version:-1));break;
        }
        static void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);}
    }
}
