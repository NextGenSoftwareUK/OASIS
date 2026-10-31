using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class GoogleProviderSurfaceTests
{
    [TestMethod]
    public void ObsoleteHolonIdentityOnlyOverloadsAreRemoved()
    {
        var type=typeof(GoogleCloudOASIS);
        foreach(var name in new[]{"LoadHolonByEmail","LoadHolonByEmailAsync","DeleteHolonByEmail","DeleteHolonByEmailAsync"})
            Assert.IsFalse(type.GetMethods(BindingFlags.Public|BindingFlags.Instance).Any(method=>method.Name==name),name);
        foreach(var name in new[]{"DeleteHolon","DeleteHolonAsync"})
            Assert.IsNull(type.GetMethod(name,new[]{typeof(string),typeof(bool)}),name);
    }

    [TestMethod]
    public void SharedAvatarIdentityAndHolonKeyReadsRemainImplemented()
    {
        var type=typeof(GoogleCloudOASIS);
        foreach(var asynchronous in new[]{false,true}){
            var suffix=asynchronous?"Async":"";
            foreach(var name in new[]{"LoadAvatar","LoadAvatarDetail"})Require(name+suffix,new[]{typeof(Guid),typeof(int)});
            foreach(var name in new[]{"LoadAvatarByProviderKey","LoadAvatarByUsername","LoadAvatarByEmail","LoadAvatarDetailByUsername","LoadAvatarDetailByEmail"})
                Require(name+suffix,new[]{typeof(string),typeof(int)});
            foreach(var key in new[]{typeof(Guid),typeof(string)})
                Require("LoadHolon"+suffix,new[]{key,typeof(bool),typeof(bool),typeof(int),typeof(bool),typeof(bool),typeof(int)});
        }
        void Require(string name,Type[] arguments){
            var method=type.GetMethod(name,arguments);Assert.IsNotNull(method,name);
            Assert.AreEqual(type,method.DeclaringType,"Shared lookup must retain its concrete provider implementation: "+name);
        }
    }
}
