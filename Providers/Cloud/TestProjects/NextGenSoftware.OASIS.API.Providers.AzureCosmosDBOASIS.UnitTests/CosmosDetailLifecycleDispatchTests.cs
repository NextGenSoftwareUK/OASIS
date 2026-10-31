using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public sealed class CosmosDetailLifecycleDispatchTests
{
    [TestMethod]
    public void StandaloneDetailLifecycleMustNotInheritUnsupportedHardDeleteDefault()
    {
        var map=typeof(AzureCosmosDBOASIS).GetInterfaceMap(typeof(IOASISStorageProvider));
        var methods=map.InterfaceMethods.Select((method,index)=>(method,target:map.TargetMethods[index]))
            .Where(pair=>pair.method.Name.StartsWith("DeleteAvatarDetail",System.StringComparison.Ordinal)).ToArray();
        Assert.AreEqual(6,methods.Length);
        foreach(var pair in methods)Assert.AreEqual(typeof(AzureCosmosDBOASIS),pair.target.DeclaringType,pair.method.ToString());
    }
}
