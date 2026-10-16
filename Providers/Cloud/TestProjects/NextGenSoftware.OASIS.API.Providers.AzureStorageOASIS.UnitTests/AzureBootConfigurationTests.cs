using System.Reflection;
using Azure.Storage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests;

[TestClass]
public sealed class AzureBootConfigurationTests
{
    [TestMethod]
    public void ProductionFactoryUsesSeparateDnaContainersAndConnectionOverride()
    {
        var settings=new AzureStorageOASISProviderSettings{ConnectionString="invalid-not-used",HolonContainer="owned-holons",AvatarContainer="owned-avatars",AvatarDetailContainer="owned-details"};
        var restored=JsonConvert.DeserializeObject<AzureStorageOASISProviderSettings>(JsonConvert.SerializeObject(settings))!;
        var provider=CloudProviderFactory.CreateAzureStorage(restored,"UseDevelopmentStorage=true");
        Assert.IsFalse(provider.IsProviderActivated);
        object Field(string name)=>typeof(AzureStorageOASIS).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
        Assert.AreEqual(settings.HolonContainer,Field("_holonContainer"));Assert.AreEqual(settings.AvatarContainer,Field("_avatarContainer"));Assert.AreEqual(settings.AvatarDetailContainer,Field("_detailContainer"));
        Assert.AreEqual("127.0.0.1",((BlobServiceClient)Field("_serviceClient")).Uri.Host);
    }

    [DataTestMethod]
    [DataRow("connection")] [DataRow("detail-avatar")] [DataRow("detail-holon")] [DataRow("holon-avatar")]
    public void MissingConnectionOrAmbiguousContainerCannotCreateProvider(string mode)
    {
        var settings=new AzureStorageOASISProviderSettings{ConnectionString=mode=="connection" ? "":"UseDevelopmentStorage=true"};
        if(mode=="detail-avatar") settings.AvatarDetailContainer=settings.AvatarContainer;
        if(mode=="detail-holon") settings.AvatarDetailContainer=settings.HolonContainer;
        if(mode=="holon-avatar") settings.HolonContainer=settings.AvatarContainer;
        Assert.ThrowsException<ArgumentException>(()=>CloudProviderFactory.CreateAzureStorage(settings,null!));
    }
}
