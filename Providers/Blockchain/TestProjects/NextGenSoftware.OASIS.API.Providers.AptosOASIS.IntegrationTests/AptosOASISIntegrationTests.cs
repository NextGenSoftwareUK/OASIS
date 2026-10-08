using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.AptosOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.IntegrationTests;

[TestClass]
public class AptosOASISIntegrationTests
{
    private static string RequiredEnvironment(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new AssertFailedException($"Required real-runtime setting {name} is missing.");

    private static AptosOASIS CreateProvider() => new(
        RequiredEnvironment("OASIS_APTOS_RPC_ENDPOINT"),
        "local",
        RequiredEnvironment("OASIS_APTOS_PRIVATE_KEY"),
        RequiredEnvironment("OASIS_APTOS_CONTRACT_ADDRESS"));

    [TestMethod]
    public async Task OfficialSdk_ActivatesAgainstRealAptosLedger()
    {
        using var provider = CreateProvider();
        var result = await provider.ActivateProviderAsync();

        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.Result);
        Assert.IsTrue(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task OfficialSdk_PerformsRealHolonCreateReadUpdateDelete()
    {
        using var provider = CreateProvider();
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);

        var holon = new Holon
        {
            Id = Guid.NewGuid(),
            Name = "aptos-runtime-create",
            Description = "written through the Aptos Labs SDK"
        };

        var created = await provider.SaveHolonAsync(holon);
        Assert.IsFalse(created.IsError, created.Message);

        var loaded = await provider.LoadHolonAsync(holon.Id);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(holon.Name, loaded.Result.Name);

        holon.Name = "aptos-runtime-update";
        var updated = await provider.SaveHolonAsync(holon);
        Assert.IsFalse(updated.IsError, updated.Message);
        var reloaded = await provider.LoadHolonAsync(holon.Id);
        Assert.IsFalse(reloaded.IsError, reloaded.Message);
        Assert.AreEqual("aptos-runtime-update", reloaded.Result.Name);

        var deleted = await provider.DeleteHolonAsync(holon.Id);
        Assert.IsFalse(deleted.IsError, deleted.Message);
        Assert.AreEqual(holon.Id, deleted.Result.Id);
    }

    [TestMethod]
    public async Task OfficialSdk_PerformsRealAvatarAndDetailCrudAndLookup()
    {
        using var provider = CreateProvider();
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);

        var marker = Guid.NewGuid().ToString("N");
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = $"aptos-{marker}",
            Email = $"{marker}@aptos.test"
        };
        var detail = new AvatarDetail
        {
            Id = avatar.Id,
            Username = avatar.Username,
            Email = avatar.Email,
            Karma = 42
        };

        var savedAvatar = await provider.SaveAvatarAsync(avatar);
        var savedDetail = await provider.SaveAvatarDetailAsync(detail);
        Assert.IsFalse(savedAvatar.IsError, savedAvatar.Message);
        Assert.IsFalse(savedDetail.IsError, savedDetail.Message);

        var byId = await provider.LoadAvatarAsync(avatar.Id);
        var byUsername = await provider.LoadAvatarByUsernameAsync(avatar.Username);
        var byEmail = await provider.LoadAvatarByEmailAsync(avatar.Email);
        var loadedDetail = await provider.LoadAvatarDetailAsync(detail.Id);
        Assert.IsFalse(byId.IsError, byId.Message);
        Assert.IsFalse(byUsername.IsError, byUsername.Message);
        Assert.IsFalse(byEmail.IsError, byEmail.Message);
        Assert.IsFalse(loadedDetail.IsError, loadedDetail.Message);
        Assert.AreEqual(avatar.Id, byId.Result.Id);
        Assert.AreEqual(avatar.Id, byUsername.Result.Id);
        Assert.AreEqual(avatar.Id, byEmail.Result.Id);
        Assert.AreEqual(42, loadedDetail.Result.Karma);

        var deleted = await provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
        Assert.IsFalse(deleted.IsError, deleted.Message);
        Assert.IsTrue(deleted.Result);
        var missing = await provider.LoadAvatarAsync(avatar.Id);
        Assert.IsFalse(missing.IsError, missing.Message);
        Assert.IsNull(missing.Result);
    }

    [TestMethod]
    public async Task OfficialSdk_EnumeratesFiltersSearchesAndDeletesStoredRecords()
    {
        using var provider = CreateProvider();
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var marker = Guid.NewGuid().ToString("N");
        var parentId = Guid.NewGuid();
        var holon = new Holon
        {
            Id = Guid.NewGuid(),
            ParentHolonId = parentId,
            Name = $"searchable-{marker}",
            Description = "official Aptos Move table search evidence",
            MetaData = new Dictionary<string, object> { ["runtime"] = marker }
        };
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = $"delete-{marker}", Email = $"delete-{marker}@aptos.test" };
        Assert.IsFalse((await provider.SaveHolonAsync(holon)).IsError);
        Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);

        var all = await provider.LoadAllHolonsAsync();
        var children = await provider.LoadHolonsForParentAsync(parentId);
        var metadata = await provider.LoadHolonsByMetaDataAsync("runtime", marker);
        var search = await provider.SearchAsync(new SearchParams
        {
            SearchOnlyForCurrentAvatar = false,
            SearchGroups = new List<ISearchGroupBase> { new SearchTextGroup { SearchQuery = marker, SearchHolons = true } }
        });
        Assert.IsFalse(all.IsError, all.Message);
        Assert.IsTrue(all.Result.Any(x => x.Id == holon.Id));
        Assert.IsTrue(children.Result.Any(x => x.Id == holon.Id));
        Assert.IsTrue(metadata.Result.Any(x => x.Id == holon.Id));
        Assert.IsFalse(search.IsError, search.Message);
        Assert.IsTrue(search.Result.SearchResultHolons.Any(x => x.Id == holon.Id));

        var deleteByUsername = await provider.DeleteAvatarByUsernameAsync(avatar.Username, softDelete: false);
        Assert.IsFalse(deleteByUsername.IsError, deleteByUsername.Message);
        Assert.IsTrue(deleteByUsername.Result);
        Assert.IsFalse((await provider.DeleteHolonAsync(holon.Id)).IsError);
    }

    [TestMethod]
    public async Task OfficialSdk_RejectsUnreachableFullnode()
    {
        using var provider = new AptosOASIS(
            "http://127.0.0.1:1/v1",
            "unreachable",
            RequiredEnvironment("OASIS_APTOS_PRIVATE_KEY"),
            RequiredEnvironment("OASIS_APTOS_CONTRACT_ADDRESS"));

        var result = await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
