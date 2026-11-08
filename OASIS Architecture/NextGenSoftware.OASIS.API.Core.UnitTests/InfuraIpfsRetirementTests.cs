using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;
using InfuraProvider = NextGenSoftware.OASIS.API.Providers.InfuraOASIS.InfuraOASIS;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class InfuraIpfsRetirementTests
{
    [Fact]
    public void RetiredStorageIsNotAdvertised()
    {
        var provider = new InfuraProvider();
        Assert.Equal(ProviderType.InfuraOASIS, provider.ProviderType.Value);
        Assert.Equal(ProviderCategory.Network, provider.ProviderCategory.Value);
        Assert.Contains(provider.ProviderCapabilities, category => category.Value == ProviderCategory.Blockchain);
        Assert.DoesNotContain(provider.ProviderCapabilities, category => category.Value == ProviderCategory.Storage);
    }

    [Fact]
    public async Task RetiredStorageWritesPreserveRetainedCidAndInput()
    {
        var provider = new InfuraProvider();
        var holon = new Holon { Id = Guid.NewGuid(), Name = "retained Infura record" };
        const string cid = "retained-cid";
        holon.MetaData["infura_ipfs_cid"] = cid;
        var originalId = holon.Id;
        Rejected(await provider.SaveHolonAsync(holon));
        Rejected(provider.SaveHolon(holon));
        Rejected(await provider.SaveHolonsAsync(new[] { holon }));
        Rejected(await provider.DeleteHolonAsync(cid));
        Rejected(provider.DeleteHolon(originalId));
        Rejected(await provider.ImportAsync(new[] { holon }));
        Assert.Equal(originalId, holon.Id);
        Assert.Equal("retained Infura record", holon.Name);
        Assert.Equal(cid, holon.MetaData["infura_ipfs_cid"]);
    }

    [Fact]
    public async Task RetiredStorageReadsRejectInsteadOfReturningEmptySuccess()
    {
        var provider = new InfuraProvider();
        const string cid = "retained-cid";
        Rejected(await provider.LoadHolonAsync(cid));
        Rejected(provider.LoadHolon(Guid.NewGuid()));
        Rejected(await provider.LoadHolonsForParentAsync(cid));
        Rejected(await provider.LoadHolonsForParentAsync(Guid.NewGuid()));
        Rejected(await provider.LoadAllHolonsAsync());
        Rejected(await provider.LoadHolonsByMetaDataAsync("infura_ipfs_cid", cid));
        Rejected(await provider.LoadHolonsByMetaDataAsync(new Dictionary<string, string>(), default));
        Rejected(await provider.SearchAsync(null!));
    }

    private static void Rejected<T>(OASISResult<T> result)
    {
        Assert.True(result.IsError);
        Assert.Equal(1, result.ErrorCount);
        Assert.Equal("INFURA_IPFS_RETIRED", result.ErrorCode);
        Assert.Equal(default(T), result.Result);
    }
}
