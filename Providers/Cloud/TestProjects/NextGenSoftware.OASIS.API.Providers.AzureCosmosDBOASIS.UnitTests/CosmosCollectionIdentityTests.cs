using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosCollectionIdentityTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach(var kind in new[]{"avatar","detail","holon"})foreach(var sync in new[]{false,true})
        foreach(var version in new[]{0,1,3})foreach(var repeated in new[]{false,true})yield return new object[]{kind,sync,version,repeated};
    }

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task CollectionRejectsRepeatedCanonicalIdentityBeforeHistorySelection(string kind,bool sync,int version,bool repeated)
    {
        var client=new Mock<ICosmosDbClient>();var factory=new Mock<ICosmosDbClientFactory>();
        factory.Setup(value=>value.GetClient(It.IsAny<string>())).Returns(client.Object);
        async Task Check<T>(T prototype,CosmosDbRepository<T> repository,Func<string,T> decode) where T:class,IHolonBase
        {
            async Task<JObject> Record(Guid id)
            {
                var value=decode(OasisJson.Serialize(prototype));value.Id=id;value.Version=0;value.MetaData=new(){{"marker","historical"}};
                var prior=await CosmosEmbeddedHistory.StageAsync(value,null!,decode);
                value.MetaData["marker"]="current";return await CosmosEmbeddedHistory.StageAsync(value,prior,decode);
            }
            var first=await Record(Guid.NewGuid());var other=repeated?first:await Record(Guid.NewGuid());
            client.Setup(value=>value.ReadAllDocumentsAsync()).ReturnsAsync(new List<string>{first.ToString(),other.ToString()});
            async Task<List<T>> Read()=>sync?repository.GetList(version):await repository.GetListAsync(version);
            if(repeated)await Assert.ThrowsExceptionAsync<InvalidDataException>(async()=>{await Read();});
            else {var result=await Read();Assert.AreEqual(version==3?0:2,result.Count);Assert.AreEqual(result.Count,result.Select(value=>value.Id).Distinct().Count());}
            client.Verify(value=>value.ReadAllDocumentsAsync(),Times.Once);
        }
        if(kind=="avatar")await Check<IAvatar>(new Avatar(),new AvatarRepository(factory.Object),json=>OasisJson.DeserializeStored<Avatar>(json));
        else if(kind=="detail")await Check<IAvatarDetail>(new AvatarDetail{XP=321},new AvatarDetailRepository(factory.Object),json=>OasisJson.DeserializeStored<AvatarDetail>(json));
        else await Check<IHolon>(new Holon{IsActive=true},new HolonRepository(factory.Object),json=>OasisJson.DeserializeStored<Holon>(json));
    }
}
