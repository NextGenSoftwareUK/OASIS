using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

// Concrete dispatch is a necessary implementation gate, not native-service proof.
[TestClass]
public class OracleInterfaceDispatchTests
{
    [DataTestMethod][DataRow("storage")][DataRow("database")]
    public void RelevantInterfacesDoNotFallThroughToUnreviewedBaseImplementations(string family)
    {
        var contract=family=="storage"?typeof(IOASISStorageProvider):typeof(IOASISDBStorageProvider);
        var map=typeof(OracleDBOASIS).GetInterfaceMap(contract);
        var members=0;
        for(var index=0;index<map.InterfaceMethods.Length;index++){
            var method=map.InterfaceMethods[index];if(!method.IsPublic)continue;
            var target=map.TargetMethods[index];var owner=target.DeclaringType!;members++;
            Console.WriteLine($"{method} -> {owner.FullName}");
            Assert.IsFalse(target.IsAbstract,$"No concrete implementation: {method}");
            if(owner.IsInterface){
                Assert.AreEqual(typeof(IOASISStorageProvider),owner);
                Assert.IsTrue(method.Name is "LoadAllHolons" or "LoadAllHolonsAsync" or "LoadHolonsForParent" or "LoadHolonsForParentAsync" or "LoadHolonsByMetaData" or "LoadHolonsByMetaDataAsync",$"Unreviewed interface default: {method}");
                Assert.IsTrue(method.GetParameters().Any(parameter=>parameter.Name=="avatarId"&&parameter.ParameterType==typeof(Guid)),$"Only visibility-scoped defaults are reviewed: {method}");
            }else if(owner!=typeof(OracleDBOASIS)){
                Assert.AreEqual(typeof(OASISStorageProviderBase),owner,$"Unexpected implementation owner: {method}");
                Assert.IsTrue(method.Name is "AddKarmaToAvatar" or "AddKarmaToAvatarAsync" or "RemoveKarmaFromAvatar" or "RemoveKarmaFromAvatarAsync" or "add_OnStorageProviderError" or "remove_OnStorageProviderError",$"Unreviewed base implementation: {method}");
            }
        }
        Assert.IsTrue(members>0);Console.WriteLine($"{family}: {members} public entries");
    }
}
