using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosCallerPublicationTests
{
    [DataTestMethod][DataRow(0,false)][DataRow(1,false)][DataRow(2,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)]
    public async Task PublicSavePreservesCallerEditAfterRepositoryAcknowledgement(int kind,bool synchronous)
    {
        IHolonBase source=kind==0?new Avatar{Id=Guid.NewGuid()}:kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon{Id=Guid.NewGuid()};
        var id=source.Id;var calls=0;
        async Task<T> Save<T>(T draft) where T:IHolonBase{calls++;await Task.Yield();draft.Version=2;source.Version=999;return draft;}
        var avatars=new Mock<IAvatarRepository>();var details=new Mock<IAvatarDetailRepository>();var holons=new Mock<IHolonRepository>();
        avatars.Setup(store=>store.UpsertAsync(It.IsAny<IAvatar>())).Returns((IAvatar row)=>Save(row));
        details.Setup(store=>store.UpsertAsync(It.IsAny<IAvatarDetail>())).Returns((IAvatarDetail row)=>Save(row));
        holons.Setup(store=>store.UpsertAsync(It.IsAny<IHolon>())).Returns((IHolon row)=>Save(row));
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new List<string>()){IsProviderActivated=true};
        foreach(var pair in new Dictionary<string,object>{{"avatarRepository",avatars.Object},{"avatarDetailRepository",details.Object},{"holonRepository",holons.Object}})
            typeof(AzureCosmosDBOASIS).GetField(pair.Key,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,pair.Value);
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
            var failure=result.Exception as CosmosMutationReconciliationException;Assert.IsNotNull(failure);Assert.IsTrue(failure.Acknowledged);Assert.AreEqual(id.ToString(),failure.DocumentId);
        }
        if(kind==0)Check(synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source));
        else if(kind==1)Check(synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source));
        else Check(synchronous?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false));
        Assert.AreEqual(1,calls);Assert.AreEqual(999,source.Version);Assert.AreEqual(id,source.Id);
    }
}
