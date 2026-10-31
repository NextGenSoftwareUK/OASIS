using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public sealed class CosmosDeleteObservationTests
{
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task PublicDeletePassesOriginalObservationAndDoesNotPublishConflictedSuccess(bool sync,int kind)
    {
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new());provider.IsProviderActivated=true;
        var id=Guid.NewGuid();var original=new Avatar{Id=id,IsActive=true};var detail=new AvatarDetail{Id=id,IsActive=true};var holon=new Holon{Id=id,IsActive=true};
        var avatars=new Mock<IAvatarRepository>(MockBehavior.Strict);var details=new Mock<IAvatarDetailRepository>(MockBehavior.Strict);var holons=new Mock<IHolonRepository>(MockBehavior.Strict);
        avatars.Setup(repository=>repository.GetByIdAsync(id.ToString())).ReturnsAsync(original);
        details.Setup(repository=>repository.GetByIdAsync(id.ToString())).ReturnsAsync(detail);
        holons.Setup(repository=>repository.GetByIdAsync(id.ToString())).ReturnsAsync(holon);
        var conflict=new StorageLifecycleConflictException("owned competing record");
        details.Setup(repository=>repository.DeleteIfUnchangedAsync(detail)).Returns(()=>kind==1?Task.FromException(conflict):Task.CompletedTask);
        avatars.Setup(repository=>repository.DeleteIfUnchangedAsync(original)).ThrowsAsync(conflict);
        holons.Setup(repository=>repository.DeleteIfUnchangedAsync(holon)).ThrowsAsync(conflict);
        foreach(var pair in new Dictionary<string,object>{{"avatarRepository",avatars.Object},{"avatarDetailRepository",details.Object},{"holonRepository",holons.Object}})
            typeof(AzureCosmosDBOASIS).GetField(pair.Key,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,pair.Value);
        if(kind==0){var result=sync?provider.DeleteHolon(id):await provider.DeleteHolonAsync(id);Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsDeleted);Assert.AreSame(conflict,result.Exception);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);}
        else {var result=sync?provider.DeleteAvatar(id,false):await provider.DeleteAvatarAsync(id,false);Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
            if(kind==1)Assert.AreSame(conflict,result.Exception);else {Assert.IsInstanceOfType<CosmosAvatarLifecycleException>(result.Exception);Assert.AreSame(conflict,result.Exception.InnerException);}}
        if(kind==0)holons.Verify(repository=>repository.DeleteIfUnchangedAsync(holon),Times.Once());
        else {details.Verify(repository=>repository.DeleteIfUnchangedAsync(detail),Times.Once());avatars.Verify(repository=>repository.DeleteIfUnchangedAsync(original),kind==1?Times.Never():Times.Once());}
    }
}
