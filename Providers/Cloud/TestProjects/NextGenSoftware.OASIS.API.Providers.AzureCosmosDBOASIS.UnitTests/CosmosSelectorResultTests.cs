using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosSelectorResultTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task SelectorWrappersPreserveAbsenceAndAmbiguity(bool ambiguous)
    {
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new List<string>());
        var avatar=new Mock<IAvatarRepository>();var detail=new Mock<IAvatarDetailRepository>();
        var cause=new StorageIdentityAmbiguousException();
        if(ambiguous)
        {
            avatar.Setup(r=>r.GetByField(It.IsAny<string>(),It.IsAny<string>(),0)).Throws(cause);
            detail.Setup(r=>r.GetByField(It.IsAny<string>(),It.IsAny<string>(),0)).Throws(cause);
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
