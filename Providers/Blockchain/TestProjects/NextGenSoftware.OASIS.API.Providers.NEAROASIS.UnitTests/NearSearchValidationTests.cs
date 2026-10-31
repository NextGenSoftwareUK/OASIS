using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearSearchValidationTests
{
    [TestMethod]
    public async Task InvalidSearchScopeAndVersionFailBeforeActivation()
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","unused","unused","unused","","");
        var scoped=await provider.SearchAsync(new SearchParams());
        Assert.IsTrue(scoped.IsError);
        StringAssert.Contains(scoped.Message,"Current-avatar search requires AvatarId");
        var version=provider.Search(new SearchParams{SearchOnlyForCurrentAvatar=false},version:-1);
        Assert.IsTrue(version.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
