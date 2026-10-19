using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearCollectionContractTests
{
    [DataTestMethod]
    [DataRow(0,false)] [DataRow(0,true)] [DataRow(1,false)] [DataRow(1,true)]
    [DataRow(2,false)] [DataRow(2,true)] [DataRow(3,false)] [DataRow(3,true)] [DataRow(4,false)] [DataRow(4,true)]
    public async Task SharedInterfaceVersionAndDepthAreValidatedBeforeActivation(int kind,bool synchronous)
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","fixture","different-chain","unused");
        IOASISStorageProvider storage=provider;
        async Task<OASISResult<IEnumerable<IHolon>>> Read(int depth,int version) => kind switch {
            0 => synchronous ? storage.LoadAllHolons(HolonType.All,curentChildDepth:depth,version:version)
                :await storage.LoadAllHolonsAsync(HolonType.All,curentChildDepth:depth,version:version),
            1 => synchronous ? storage.LoadHolonsForParent(Guid.NewGuid(),HolonType.All,curentChildDepth:depth,version:version)
                :await storage.LoadHolonsForParentAsync(Guid.NewGuid(),HolonType.All,curentChildDepth:depth,version:version),
            2 => synchronous ? storage.LoadHolonsForParent("owned-parent",HolonType.All,curentChildDepth:depth,version:version)
                :await storage.LoadHolonsForParentAsync("owned-parent",HolonType.All,curentChildDepth:depth,version:version),
            3 => synchronous ? storage.LoadHolonsByMetaData("owned","value",HolonType.All,curentChildDepth:depth,version:version)
                :await storage.LoadHolonsByMetaDataAsync("owned","value",HolonType.All,curentChildDepth:depth,version:version),
            _ => synchronous ? storage.LoadHolonsByMetaData(new() {{"owned","value"}},MetaKeyValuePairMatchMode.All,HolonType.All,curentChildDepth:depth,version:version)
                :await storage.LoadHolonsByMetaDataAsync(new() {{"owned","value"}},MetaKeyValuePairMatchMode.All,HolonType.All,curentChildDepth:depth,version:version)
        };
        foreach(var invalid in new[] {await Read(0,-1),await Read(-1,0)}) {
            Assert.IsTrue(invalid.IsError);Assert.IsNull(invalid.Result);
            Assert.IsInstanceOfType(invalid.Exception,typeof(ArgumentOutOfRangeException));
            Assert.IsFalse(provider.IsProviderActivated,"Invalid collection arguments must not reach native activation.");
        }
    }

    [DataTestMethod]
    [DataRow("LoadAllHolons")] [DataRow("LoadAllHolonsAsync")]
    [DataRow("LoadHolonsForParent")] [DataRow("LoadHolonsForParentAsync")]
    [DataRow("LoadHolonsByMetaData")] [DataRow("LoadHolonsByMetaDataAsync")]
    [DataRow("LoadHolon")] [DataRow("LoadHolonAsync")]
    public void CollectionOverridesKeepSharedVersionDepthAndProviderChildRoles(string name)
    {
        var methods=typeof(NEAROASIS).GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).Where(method=>method.Name==name).ToList();
        Assert.IsTrue(methods.Count>0);
        foreach(var method in methods) {
            var actual=method.GetParameters();
            var contract=typeof(IOASISStorageProvider).GetMethod(name,actual.Select(parameter=>parameter.ParameterType).ToArray())!;
            Assert.IsNotNull(contract);
            var expected=contract.GetParameters();
            foreach(var role in new[] {"curentChildDepth","loadChildrenFromProvider","version"}) {
                var index=Array.FindIndex(expected,parameter=>parameter.Name==role);
                if(index<0) continue; // Single-record loads do not have a current-depth argument.
                Assert.AreEqual(role,actual[index].Name,$"{method}: shared argument {index} must retain its meaning.");
                Assert.AreEqual(expected[index].DefaultValue,actual[index].DefaultValue,$"{method}: default {role} differs from shared interface.");
            }
        }
    }
}
