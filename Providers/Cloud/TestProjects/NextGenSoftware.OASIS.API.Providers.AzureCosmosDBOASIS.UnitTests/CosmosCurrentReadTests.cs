using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosCurrentReadTests
{
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
