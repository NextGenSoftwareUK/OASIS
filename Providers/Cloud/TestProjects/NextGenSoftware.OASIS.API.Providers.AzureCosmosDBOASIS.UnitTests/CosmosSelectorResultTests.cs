using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosSelectorResultTests
{
    [TestMethod]
    public async Task CanonicalSelectorsIgnoreInactiveMatchCaseAndRejectDuplicates()
    {
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new List<string>()) {IsProviderActivated=true};
        var owner=new NextGenSoftware.OASIS.API.Core.Holons.Avatar {Id=Guid.NewGuid(),IsActive=true,Username="owner",Email="owner@example.invalid"};
        var repository=new Mock<IAvatarRepository>();
        var rows=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar> {owner,new NextGenSoftware.OASIS.API.Core.Holons.Avatar {Id=Guid.NewGuid(),IsActive=false,Username=owner.Username,Email=owner.Email}};
        repository.Setup(r=>r.GetList()).Returns(rows);
        typeof(AzureCosmosDBOASIS).GetField("avatarRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,repository.Object);
        Assert.AreEqual(owner.Id,(await provider.LoadAvatarByUsernameAsync("OWNER")).Result.Id);
        Assert.IsTrue(provider.LoadAvatarByEmail("OWNER@EXAMPLE.INVALID").IsLoaded);
        rows.Add(new NextGenSoftware.OASIS.API.Core.Holons.Avatar {Id=Guid.NewGuid(),IsActive=true,Username="OWNER"});
        Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,provider.LoadAvatarByUsername("owner").ErrorCode);
        Assert.IsInstanceOfType((await provider.LoadAvatarByEmailAsync(" ")).Exception,typeof(ArgumentException));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task SelectorWrappersPreserveAbsenceAndAmbiguity(bool ambiguous)
    {
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new List<string>());
        var avatar=new Mock<IAvatarRepository>();var detail=new Mock<IAvatarDetailRepository>();
        var cause=new StorageIdentityAmbiguousException();
        provider.IsProviderActivated=true;
        avatar.Setup(r=>r.GetList()).Returns(new List<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar>());
        detail.Setup(r=>r.GetList()).Returns(new List<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>());
        if(ambiguous)
        {
            avatar.Setup(r=>r.GetList()).Throws(cause);
            detail.Setup(r=>r.GetList()).Throws(cause);
        }
        typeof(AzureCosmosDBOASIS).GetField("avatarRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,avatar.Object);
        typeof(AzureCosmosDBOASIS).GetField("avatarDetailRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,detail.Object);
        void Check<T>(OASISResult<T> result)
        {
            Assert.IsTrue(result.IsError);
            Assert.AreEqual(ambiguous ? OASISResultErrorCodes.AmbiguousIdentity:OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
            if(ambiguous) Assert.AreSame(cause,result.Exception);else Assert.IsNull(result.Exception);
        }
        Check(provider.LoadAvatarByEmail("owned@example.invalid"));
        Check(await provider.LoadAvatarByEmailAsync("owned@example.invalid"));
        Check(provider.LoadAvatarByUsername("owned"));Check(await provider.LoadAvatarByUsernameAsync("owned"));
        Check(await provider.LoadAvatarDetailByEmailAsync("owned@example.invalid"));
        Check(await provider.LoadAvatarDetailByUsernameAsync("owned"));
    }
}
