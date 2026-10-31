using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

[TestClass]
public sealed class AwsStorageInterfaceMappingTests
{
    public static IEnumerable<object[]> Contracts()
    {
        yield return new object[] { typeof(IOASISStorageProvider) };
        yield return new object[] { typeof(IOASISDBStorageProvider) };
        yield return new object[] { typeof(IOASISNETProvider) };
    }

    // This guards dispatch ownership, not native execution or hosted AWS acceptance.
    // In particular, credential methods must not silently inherit interface defaults.
    [DataTestMethod]
    [DynamicData(nameof(Contracts), DynamicDataSourceType.Method)]
    public void AdvertisedStorageContractsDispatchToTheRealDynamoImplementation(Type contract)
    {
        var map = typeof(AWSOASIS).GetInterfaceMap(contract);
        Assert.IsTrue(map.InterfaceMethods.Length > 0);
        for (var index = 0; index < map.InterfaceMethods.Length; index++)
        {
            var target = map.TargetMethods[index];
            // Visibility overloads intentionally share post-filtering of the
            // provider's native reads. Do not copy that policy into every provider.
            if (target.DeclaringType == typeof(IOASISStorageProvider)
                && target.GetParameters().Any(parameter => parameter.Name == "avatarId")
                && target.Name is "LoadAllHolons" or "LoadAllHolonsAsync" or "LoadHolonsForParent" or "LoadHolonsForParentAsync" or "LoadHolonsByMetaData" or "LoadHolonsByMetaDataAsync")
                continue;
            // These four are deliberately provider-independent domain operations on
            // IAvatarDetail, not storage defaults. Keep that shared owner intact.
            var sharedKarma = target.Name is "AddKarmaToAvatar" or "AddKarmaToAvatarAsync"
                or "RemoveKarmaFromAvatar" or "RemoveKarmaFromAvatarAsync";
            if (sharedKarma || target.Name is "add_OnStorageProviderError" or "remove_OnStorageProviderError")
            {
                Assert.AreEqual(typeof(Core.OASISStorageProviderBase), target.DeclaringType);
                continue;
            }
            Assert.AreEqual(typeof(DynamoDBOASIS.DynamoDBOASIS), target.DeclaringType,
                $"{map.InterfaceMethods[index]} dispatches to {target.DeclaringType}, not the native DynamoDB implementation.");
            Assert.IsFalse(target.IsAbstract);
        }
    }
}
