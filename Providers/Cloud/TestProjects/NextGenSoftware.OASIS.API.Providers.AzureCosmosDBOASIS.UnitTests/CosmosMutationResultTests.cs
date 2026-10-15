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
    [TestMethod]
    public async Task SyncAndAsyncSaveWrappersRetainExactUncertainCause()
    {
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new List<string>());
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
