using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosCurrentReadTests
{
    [TestMethod]
    public async Task HolonGraphReadsRespectChildrenAndProviderSource()
    {
        var root=new Holon {Id=Guid.NewGuid(),IsActive=true,HolonType=NextGenSoftware.OASIS.API.Core.Enums.HolonType.Park};
        var child=new Holon {Id=Guid.NewGuid(),IsActive=true,ParentHolonId=root.Id,HolonType=NextGenSoftware.OASIS.API.Core.Enums.HolonType.InventoryItem};
        var grandchild=new Holon {Id=Guid.NewGuid(),IsActive=true,ParentHolonId=child.Id};
        var repository=new Mock<IHolonRepository>();
        repository.Setup(r=>r.GetList()).Returns(new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> {root,child,grandchild});
        repository.Setup(r=>r.GetByIdAsync(root.Id.ToString())).ReturnsAsync(root);
        var provider=new Provider(false);
        typeof(AzureCosmosDBOASIS).GetField("holonRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,repository.Object);
        var all=await provider.LoadAllHolonsAsync(loadChildrenFromProvider:true);
        Assert.IsFalse(all.IsError,all.Message);Assert.AreEqual(3,all.Result.Count());
        var projected=all.Result.Single(r=>r.Id==root.Id);
        Assert.AreEqual(child.Id,projected.Children.Single().Id);Assert.AreEqual(grandchild.Id,projected.Children.Single().Children.Single().Id);
        var shallow=provider.LoadHolon(root.Id.ToString(),recursive:false,loadChildrenFromProvider:true);
        Assert.IsFalse(shallow.IsError,shallow.Message);Assert.AreEqual(1,shallow.Result.Children.Count());
        Assert.IsTrue(shallow.Result.Children.Single().Children==null || !shallow.Result.Children.Single().Children.Any());
        var flat=await provider.LoadHolonAsync(root.Id,loadChildren:false);
        Assert.IsFalse(flat.IsError,flat.Message);Assert.IsTrue(flat.Result.Children==null || !flat.Result.Children.Any());
        Assert.IsTrue(root.Children==null || !root.Children.Any());
        var typed=await provider.LoadAllHolonsAsync(type:NextGenSoftware.OASIS.API.Core.Enums.HolonType.Park,loadChildrenFromProvider:true);
        Assert.IsFalse(typed.IsError,typed.Message);Assert.AreEqual(1,typed.Result.Count());
        Assert.AreEqual(child.Id,typed.Result.Single().Children.Single().Id);
        Assert.IsInstanceOfType((await provider.LoadAllHolonsAsync(curentChildDepth:-1)).Exception,typeof(ArgumentOutOfRangeException));
    }

    [TestMethod]
    public async Task CollectionsValidateVersionAndPreserveActivationFailure()
    {
        var provider=new Provider(true);
        Assert.IsInstanceOfType((await provider.LoadAllAvatarsAsync(1)).Exception,typeof(NotSupportedException));
        Assert.IsInstanceOfType(provider.LoadAllAvatarDetails(1).Exception,typeof(NotSupportedException));
        Assert.AreEqual(0,provider.Activations);
        Assert.AreSame(provider.Cause,(await provider.LoadAllAvatarDetailsAsync()).Exception);
        Assert.AreSame(provider.Cause,provider.LoadAllAvatars().Exception);
    }

    [TestMethod]
    public async Task CollectionsFilterInactiveAndReturnSuccessfulEmptyResult()
    {
        var provider=new Provider(false);var repository=new Mock<IAvatarRepository>();
        repository.Setup(r=>r.GetList()).Returns(new List<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar> {new Avatar {IsActive=false}});
        typeof(AzureCosmosDBOASIS).GetField("avatarRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,repository.Object);
        var empty=await provider.LoadAllAvatarsAsync();
        Assert.IsFalse(empty.IsError);Assert.IsTrue(empty.IsLoaded);Assert.AreEqual(0,empty.Result.Count());
        var cause=new IOException("owned collection failure");repository.Setup(r=>r.GetList()).Throws(cause);
        Assert.AreSame(cause,provider.LoadAllAvatars().Exception);
    }

    [TestMethod]
    public async Task SaveValidatesBeforeActivationAndPreservesOriginalActivationFailure()
    {
        var provider=new Provider(true);
        Assert.IsInstanceOfType((await provider.SaveAvatarAsync(null!)).Exception,typeof(ArgumentNullException));
        Assert.IsInstanceOfType((await provider.SaveAvatarDetailAsync(null!)).Exception,typeof(ArgumentNullException));
        Assert.IsInstanceOfType((await provider.SaveHolonAsync(null!)).Exception,typeof(ArgumentNullException));
        Assert.AreEqual(0,provider.Activations);
        var avatar=await provider.SaveAvatarAsync(new Avatar());
        Assert.AreSame(provider.Cause,avatar.Exception);Assert.AreEqual("owned-code",avatar.ErrorCode);
        Assert.AreSame(provider.Cause,(await provider.SaveAvatarDetailAsync(new AvatarDetail())).Exception);
        Assert.AreSame(provider.Cause,(await provider.SaveHolonAsync(new Holon())).Exception);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task InvalidActivationConfigurationLeavesNoClientAndCanRetry(bool missingContainers)
    {
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),missingContainers ? "unused":" ","owned",
            missingContainers ? new List<string>():new List<string> {"avatarItems","avatarDetailItems","holonItems"});
        foreach(var attempt in Enumerable.Range(0,2))
        {
            var result=await provider.ActivateProviderAsync();
            Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.IsFalse(provider.IsProviderActivated);
            Assert.IsNull(typeof(AzureCosmosDBOASIS).GetField("dbClientFactory",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(provider));
        }
        Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
    }

    private sealed class Provider(bool fail) : AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new List<string>())
    {
        public int Activations;
        public readonly Exception Cause=new IOException("owned activation failure");
        public override Task<OASISResult<bool>> ActivateProviderAsync()
        {
            Activations++;
            return Task.FromResult(fail ? new OASISResult<bool> {IsError=true,Exception=Cause,ErrorCode="owned-code"}:new OASISResult<bool>(true));
        }
    }

    [TestMethod]
    public async Task CurrentReadsValidateBeforeActivationAndPreserveActivationCause()
    {
        var provider=new Provider(true);
        Assert.IsInstanceOfType((await provider.LoadAvatarAsync(Guid.Empty)).Exception,typeof(ArgumentException));
        Assert.IsInstanceOfType((await provider.LoadAvatarByProviderKeyAsync("bad-key")).Exception,typeof(ArgumentException));
        Assert.IsInstanceOfType((await provider.LoadAvatarDetailAsync(Guid.NewGuid(),version:1)).Exception,typeof(NotSupportedException));
        Assert.IsInstanceOfType((await provider.LoadHolonAsync(Guid.NewGuid(),maxChildDepth:-1)).Exception,typeof(ArgumentOutOfRangeException));
        Assert.AreEqual(0,provider.Activations);
        var result=provider.LoadAvatar(Guid.NewGuid());Assert.AreSame(provider.Cause,result.Exception);Assert.AreEqual("owned-code",result.ErrorCode);
        Assert.AreSame(provider.Cause,(await provider.LoadAvatarDetailAsync(Guid.NewGuid())).Exception);
        Assert.AreSame(provider.Cause,(await provider.LoadHolonAsync(Guid.NewGuid().ToString())).Exception);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task MissingOrInactiveRecordsAreTerminalAbsence(bool inactive)
    {
        var provider=new Provider(false);var repository=new Mock<IAvatarRepository>();
        if(inactive) repository.Setup(r=>r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new Avatar {Id=Guid.NewGuid(),IsActive=false});
        typeof(AzureCosmosDBOASIS).GetField("avatarRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,repository.Object);
        var result=await provider.LoadAvatarAsync(Guid.NewGuid());
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsNull(result.Exception);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
    }

    [TestMethod]
    public async Task SyncReadPreservesRepositoryExceptionWithoutAggregateWrapper()
    {
        var provider=new Provider(false);var repository=new Mock<IHolonRepository>();var cause=new IOException("owned read failure");
        repository.Setup(r=>r.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(cause);
        typeof(AzureCosmosDBOASIS).GetField("holonRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,repository.Object);
        Assert.AreSame(cause,provider.LoadHolon(Guid.NewGuid().ToString()).Exception);
        Assert.AreSame(cause,(await provider.LoadHolonAsync(Guid.NewGuid())).Exception);
    }
}
