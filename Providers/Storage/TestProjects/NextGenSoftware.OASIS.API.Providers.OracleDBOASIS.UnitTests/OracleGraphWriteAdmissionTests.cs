using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleGraphWriteAdmissionTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)][DataRow(4,false)][DataRow(4,true)]
    public async Task InvalidGraphIsRejectedBeforeConnectionAndCallerPublication(int kind,bool synchronous)
    {
        var provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");var root=new Holon();var child=new Holon();
        if(kind==0)root.Children.Add(root);
        if(kind==1){child.ParentHolonId=Guid.NewGuid();root.Children.Add(child);}
        if(kind==2){root.Id=Guid.NewGuid();child.Id=root.Id;root.Children.Add(child);}
        var original=root.Id;
        if(kind==3)Check(synchronous?provider.SaveHolons(Array.Empty<IHolon>(),curentChildDepth:0):await provider.SaveHolonsAsync(Array.Empty<IHolon>(),curentChildDepth:0));
        else Check(synchronous?provider.SaveHolon(root,maxChildDepth:kind==4?-1:0):await provider.SaveHolonAsync(root,maxChildDepth:kind==4?-1:0));
        Assert.AreEqual(original,root.Id);Assert.AreEqual(0,root.Version);Assert.AreEqual(0,root.ProviderUniqueStorageKey.Count);
        static void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentException>(result.Exception);Assert.IsFalse(result.Exception.Message.Contains("UnsupportedOracleOptionForOwnedTest",StringComparison.OrdinalIgnoreCase));}
    }
}
