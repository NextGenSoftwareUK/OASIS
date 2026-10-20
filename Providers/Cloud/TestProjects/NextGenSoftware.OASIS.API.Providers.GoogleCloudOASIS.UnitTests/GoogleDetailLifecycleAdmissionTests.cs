using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public sealed class GoogleDetailLifecycleAdmissionTests
{
    public static IEnumerable<object[]> Cases(){for(var method=0;method<6;method++)foreach(var soft in new[]{false,true})yield return new object[]{method,soft};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task InvalidStandaloneDetailSelectorsFailBeforeActivation(int method,bool soft)
    {
        var concrete=new GoogleCloudOASIS(projectId:"owned-no-io",enableStorage:false,enableFirestore:false,enableBigQuery:false);
        IOASISStorageProvider provider=concrete;
        var result=method switch {
            0=>await provider.DeleteAvatarDetailAsync(Guid.Empty,soft),1=>provider.DeleteAvatarDetail(Guid.Empty,soft),
            2=>await provider.DeleteAvatarDetailByEmailAsync(" ",soft),3=>provider.DeleteAvatarDetailByEmail("",soft),
            4=>await provider.DeleteAvatarDetailByUsernameAsync("",soft),_=>provider.DeleteAvatarDetailByUsername(" ",soft)
        };
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.IsFalse(result.IsSaved);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.IsInstanceOfType<ArgumentException>(result.Exception);
        Assert.IsFalse(concrete.IsProviderActivated);
    }
}
