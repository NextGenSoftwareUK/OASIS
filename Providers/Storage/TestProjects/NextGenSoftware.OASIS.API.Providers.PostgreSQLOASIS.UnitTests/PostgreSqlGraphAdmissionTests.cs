using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests;

[TestClass]
public class PostgreSqlGraphAdmissionTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(3,false)][DataRow(4,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)][DataRow(3,true)][DataRow(4,true)]
    public async Task InvalidSelectedGraphFailsBeforeNativeConnection(int kind,bool synchronous)
    {
        var provider=new PostgreSQLOASIS("UnsupportedConnectionKeyword=true");
        var root=new Holon();var child=new Holon();root.Children.Add(child);
        if(kind==0)child.Children.Add(root);
        if(kind==1){root.Id=Guid.NewGuid();child.Id=root.Id;}
        if(kind==2)child.ParentHolonId=Guid.NewGuid();
        if(kind==3)root.Children.Add(null!);
        var originalRoot=root.Id;var originalChild=child.Id;
        var result=synchronous?provider.SaveHolon(root,maxChildDepth:kind==4?-1:0):await provider.SaveHolonAsync(root,maxChildDepth:kind==4?-1:0);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);
        if(kind==4)Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
        else{Assert.IsInstanceOfType<ArgumentException>(result.Exception);StringAssert.Contains(result.Exception.Message,new[]{"cycle","duplicate","conflicting","null"}[kind]);}
        Assert.AreEqual(originalRoot,root.Id);Assert.AreEqual(originalChild,child.Id);
    }
}
