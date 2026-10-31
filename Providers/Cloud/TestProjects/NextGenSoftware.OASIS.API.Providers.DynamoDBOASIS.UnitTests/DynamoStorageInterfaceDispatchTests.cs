using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

// Structural dispatch evidence only; real SDK/native behavior has separate gates.
[TestClass]
public class DynamoStorageInterfaceDispatchTests
{
    [DataTestMethod][DataRow("storage")][DataRow("database")][DataRow("network")]
    public void AllRelevantInterfaceMembersHaveAnExplicitImplementationOwner(string family)
    {
        var contract=family switch{"storage"=>typeof(IOASISStorageProvider),"database"=>typeof(IOASISDBStorageProvider),_=>typeof(IOASISNETProvider)};
        var map=typeof(DynamoDBOASIS).GetInterfaceMap(contract);
        var owners=new Dictionary<string,int>(StringComparer.Ordinal);var defaults=0;
        for(var index=0;index<map.InterfaceMethods.Length;index++){
            var method=map.InterfaceMethods[index];var target=map.TargetMethods[index];
            if(!method.IsPublic)continue;
            Assert.IsFalse(target.IsAbstract,$"{method} has no concrete dispatch.");
            var owner=target.DeclaringType!;owners[owner.FullName!]=owners.GetValueOrDefault(owner.FullName!)+1;
            if(owner.IsInterface){
                Assert.AreEqual(typeof(IOASISStorageProvider),owner,$"Unexpected default owner: {method}");
                Assert.IsTrue(method.Name is "LoadAllHolons" or "LoadAllHolonsAsync" or "LoadHolonsForParent" or "LoadHolonsForParentAsync" or "LoadHolonsByMetaData" or "LoadHolonsByMetaDataAsync",
                    $"Unsupported/unreviewed default dispatch: {method}");
                Assert.IsTrue(method.GetParameters().Any(parameter=>parameter.Name=="avatarId"&&parameter.ParameterType==typeof(Guid)),
                    $"Only the existing reviewed visibility-scoped defaults are admitted: {method}");
                defaults++;
            }
            else if(owner!=typeof(DynamoDBOASIS)){
                Assert.AreEqual(typeof(OASISStorageProviderBase),owner,$"Unexpected implementation owner: {method}");
                Assert.IsTrue(method.Name is "AddKarmaToAvatar" or "AddKarmaToAvatarAsync" or "RemoveKarmaFromAvatar" or "RemoveKarmaFromAvatarAsync"
                    or "add_OnStorageProviderError" or "remove_OnStorageProviderError",$"Unreviewed base dispatch: {method}");
            }
            Console.WriteLine($"{method.Name}({string.Join(',',method.GetParameters().Select(parameter=>parameter.ParameterType.Name))}) -> {owner.FullName}");
        }
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{family,members=owners.Values.Sum(),visibilityDefaults=defaults,owners}));
        Assert.IsTrue(owners.Values.Sum()>0);
    }
}
