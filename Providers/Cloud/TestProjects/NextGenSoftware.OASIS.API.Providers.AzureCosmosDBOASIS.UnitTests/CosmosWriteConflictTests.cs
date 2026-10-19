using System.Net;
using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosWriteConflictTests
{
    [DataTestMethod][DataRow(409)][DataRow(412)]
    public async Task KnownNativeWriteConflictPreservesCauseAndCannotFailOver(int status)
    {
        var input=new AvatarDetail{Id=Guid.NewGuid(),IsActive=true};
        var cause=new CosmosException("controlled native concurrency conflict",(HttpStatusCode)status,0,"owned",0);
        var repository=new Mock<IAvatarDetailRepository>();repository.Setup(r=>r.UpsertAsync(input)).ThrowsAsync(cause);
        var provider=new AzureCosmosDBOASIS(new Uri("https://owned.invalid"),"owned","owned",new(){"avatarDetailItems"}){IsProviderActivated=true};
        typeof(AzureCosmosDBOASIS).GetField("avatarDetailRepository",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,repository.Object);
        var result=await provider.SaveAvatarDetailAsync(input);
        Assert.IsTrue(result.IsError);Assert.AreSame(cause,result.Exception);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
        var decision=typeof(NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive)
            .GetMethod("ShouldFailOver",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(IAvatarDetail));
        var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest{Operation="SaveAvatarDetail",Payload=input};
        Assert.AreEqual(false,decision.Invoke(null,new object[]{request,result}));
    }
}
