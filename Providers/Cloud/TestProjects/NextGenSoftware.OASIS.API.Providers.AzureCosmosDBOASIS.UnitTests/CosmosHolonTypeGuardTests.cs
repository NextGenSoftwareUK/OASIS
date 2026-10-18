using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosHolonTypeGuardTests
{
    [DataTestMethod]
    [DataRow(false,"all")][DataRow(true,"all")]
    [DataRow(false,"parent")][DataRow(true,"parent")]
    [DataRow(false,"metadata")][DataRow(true,"metadata")]
    public async Task UndefinedHolonTypeRejectsBeforeRepositoryIo(bool synchronous,string operation)
    {
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new());
        provider.IsProviderActivated=true;
        var repository=new Mock<IHolonRepository>(MockBehavior.Strict);
        typeof(AzureCosmosDBOASIS).GetField("holonRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,repository.Object);
        var invalid=(HolonType)int.MaxValue;var parent=Guid.NewGuid();
        var result=operation switch {
            "all"=>synchronous?provider.LoadAllHolons(invalid):await provider.LoadAllHolonsAsync(invalid),
            "parent"=>synchronous?provider.LoadHolonsForParent(parent,invalid):await provider.LoadHolonsForParentAsync(parent,invalid),
            _=>synchronous?provider.LoadHolonsByMetaData("key","value",invalid):await provider.LoadHolonsByMetaDataAsync("key","value",invalid)};
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
        repository.VerifyNoOtherCalls();
    }
}
