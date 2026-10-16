using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosMutationResultTests
{
    [DataTestMethod]
    [DataRow(false,"holon")] [DataRow(true,"holon")]
    [DataRow(false,"avatar-hard")] [DataRow(true,"avatar-hard")]
    [DataRow(false,"avatar-soft")] [DataRow(true,"avatar-soft")]
    public async Task AcknowledgedDeletionPublishesDeletionFlags(bool synchronous,string kind)
    {
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new List<string>());
        provider.IsProviderActivated=true;var id=Guid.NewGuid();
        var avatars=new Mock<IAvatarRepository>();var details=new Mock<IAvatarDetailRepository>();var holons=new Mock<IHolonRepository>();
        avatars.Setup(r=>r.GetByIdAsync(id.ToString())).ReturnsAsync(new Avatar {Id=id,IsActive=true});
        details.Setup(r=>r.GetByIdAsync(id.ToString())).ReturnsAsync(new AvatarDetail {Id=id,IsActive=true});
        holons.Setup(r=>r.GetByIdAsync(id.ToString())).ReturnsAsync(new Holon {Id=id,IsActive=true});
        avatars.Setup(r=>r.UpsertAsync(It.IsAny<IAvatar>())).ReturnsAsync((IAvatar value)=>value);
        details.Setup(r=>r.UpsertAsync(It.IsAny<IAvatarDetail>())).ReturnsAsync((IAvatarDetail value)=>value);
        avatars.Setup(r=>r.DeleteAsync(id.ToString())).Returns(Task.CompletedTask);
        details.Setup(r=>r.DeleteAsync(id.ToString())).Returns(Task.CompletedTask);
        holons.Setup(r=>r.DeleteAsync(id.ToString())).Returns(Task.CompletedTask);
        foreach(var pair in new Dictionary<string,object> {{"avatarRepository",avatars.Object},{"avatarDetailRepository",details.Object},{"holonRepository",holons.Object}})
            typeof(AzureCosmosDBOASIS).GetField(pair.Key,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,pair.Value);
        void Check<T>(OASISResult<T> result)
        {
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);
            Assert.IsTrue(result.IsDeleted);Assert.AreEqual(1,result.DeletedCount);
        }
        if(kind=="holon") Check(synchronous ? provider.DeleteHolon(id):await provider.DeleteHolonAsync(id));
        else Check(synchronous ? provider.DeleteAvatar(id,kind=="avatar-soft"):await provider.DeleteAvatarAsync(id,kind=="avatar-soft"));
    }

    [TestMethod]
    public async Task SyncAndAsyncSaveWrappersRetainExactUncertainCause()
    {
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new List<string>());
        provider.IsProviderActivated=true;
        var cause=new CosmosMutationReconciliationException("owned-id",new IOException("owned lost reply"));
        var avatar=new Mock<IAvatarRepository>();var detail=new Mock<IAvatarDetailRepository>();var holon=new Mock<IHolonRepository>();
        avatar.Setup(r=>r.UpsertAsync(It.IsAny<IAvatar>())).ThrowsAsync(cause);
        detail.Setup(r=>r.UpsertAsync(It.IsAny<IAvatarDetail>())).ThrowsAsync(cause);
        holon.Setup(r=>r.UpsertAsync(It.IsAny<IHolon>())).ThrowsAsync(cause);
        foreach(var pair in new Dictionary<string,object> {{"avatarRepository",avatar.Object},{"avatarDetailRepository",detail.Object},{"holonRepository",holon.Object}})
            typeof(AzureCosmosDBOASIS).GetField(pair.Key,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,pair.Value);
        void Check<T>(OASISResult<T> result) {Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreSame(cause,result.Exception);}
        Check(provider.SaveAvatar(new Avatar()));Check(await provider.SaveAvatarAsync(new Avatar()));
        Check(provider.SaveAvatarDetail(new AvatarDetail()));Check(await provider.SaveAvatarDetailAsync(new AvatarDetail()));
        Check(provider.SaveHolon(new Holon()));Check(await provider.SaveHolonAsync(new Holon()));
    }
}
