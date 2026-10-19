using Microsoft.Azure.Cosmos;
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
public class CosmosHistoryReadTests
{
    [DataTestMethod][DataRow("avatar")][DataRow("detail")][DataRow("holon")]
    public async Task RepositoryReadsExactHistoricalRecordsAndSelectors(string kind)
    {
        var client=new Mock<ICosmosDbClient>();
        var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(f=>f.GetClient(It.IsAny<string>())).Returns(client.Object);
        async Task Check<T>(T input,CosmosDbRepository<T> repository,Func<string,T> decode) where T:class,IHolonBase
        {
            input.Id=Guid.NewGuid();input.MetaData=new Dictionary<string,object>{{"history_marker","first"}};
            var first=await CosmosEmbeddedHistory.StageAsync(input,null!,decode);
            input.MetaData["history_marker"]="second";
            var second=await CosmosEmbeddedHistory.StageAsync(input,first,decode);
            client.Setup(c=>c.ReadDocumentAsync(input.Id.ToString(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).ReturnsAsync(second.ToString());
            client.Setup(c=>c.ReadAllDocuments()).Returns(new List<string>{second.ToString()});
            var previous=await repository.GetByIdAsync(input.Id.ToString(),1);
            Assert.AreEqual("first",previous.MetaData["history_marker"].ToString());Assert.AreEqual(1,previous.Version);
            Assert.AreEqual(2,(await repository.GetByIdAsync(input.Id.ToString())).Version);
            Assert.IsNull(await repository.GetByIdAsync(input.Id.ToString(),3));
            Assert.AreEqual(1,repository.GetList(1).Count);Assert.AreEqual(0,repository.GetList(3).Count);
            Assert.AreEqual(input.Id,repository.GetByField("MetaData.history_marker","first",1).Id);
            second[CosmosEmbeddedHistory.PropertyName]!["schema"]=2;
            client.Setup(c=>c.ReadDocumentAsync(input.Id.ToString(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).ReturnsAsync(second.ToString());
            await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>repository.GetByIdAsync(input.Id.ToString()));
        }
        if(kind=="avatar")await Check<IAvatar>(new Avatar(),new AvatarRepository(factory.Object),json=>OasisJson.DeserializeStored<Avatar>(json));
        else if(kind=="detail")await Check<IAvatarDetail>(new AvatarDetail{XP=321},new AvatarDetailRepository(factory.Object),json=>OasisJson.DeserializeStored<AvatarDetail>(json));
        else await Check<IHolon>(new Holon(),new HolonRepository(factory.Object),json=>OasisJson.DeserializeStored<Holon>(json));
    }

    [TestMethod]
    public async Task HistoricalRootProjectionLoadsIndependentlyVersionedCurrentChildren()
    {
        var root=new Holon{Id=Guid.NewGuid(),IsActive=true,Name="historic",Version=1};
        var current=new Holon{Id=root.Id,IsActive=true,Name="current",Version=3};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,IsActive=true,Name="current child",Version=5};
        var repository=new Mock<IHolonRepository>();
        repository.Setup(r=>r.GetByIdAsync(root.Id.ToString(),1)).ReturnsAsync(root);
        repository.Setup(r=>r.GetList(1)).Returns(new List<IHolon>{root});
        repository.Setup(r=>r.GetList()).Returns(new List<IHolon>{current,child});
        var provider=new AzureCosmosDBOASIS(new Uri("https://owned.invalid"),"owned","owned",new(){"holonItems"}){IsProviderActivated=true};
        typeof(AzureCosmosDBOASIS).GetField("holonRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,repository.Object);
        var single=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true,version:1);
        var listed=await provider.LoadAllHolonsAsync(loadChildrenFromProvider:true,version:1);
        Assert.IsFalse(single.IsError,single.Exception?.ToString());Assert.IsFalse(listed.IsError,listed.Exception?.ToString());
        foreach(var selected in new[]{single.Result,listed.Result.Single()}) {
            Assert.AreEqual(1,selected.Version);Assert.AreEqual("historic",selected.Name);
            Assert.AreEqual(5,selected.Children.Single().Version);Assert.AreEqual("current child",selected.Children.Single().Name);
        }
    }
}
