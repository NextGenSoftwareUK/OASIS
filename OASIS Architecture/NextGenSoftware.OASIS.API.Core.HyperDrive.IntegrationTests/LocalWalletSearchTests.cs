using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class RealProviderRoutingTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LocalWalletSearchUsesSharedSelectedFields(bool synchronous)
    {
        Directory.CreateDirectory(_root);var local=new LocalFileOASIS(storageDirectory:FilesPath);
        var avatar=new Avatar {Id=Guid.NewGuid(),Username="owned-wallet-search",Email="owned-wallet@example.invalid",
            ProviderWallets=new() {{ProviderType.SuiOASIS,new() {new ProviderWallet {WalletAddress="selected-wallet",SecretRecoveryPhrase="excluded-recovery"}}}}};
        try{
            (await local.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
            var group=new SearchTextGroup {SearchAvatars=true,SearchQuery="selected-wallet",HolonType=HolonType.All,
                AvatarSearchParams=new SearchAvatarParams {SearchAvatarWalletParams=new SearchAvatarWalletParams {WalletAddress=true}}};
            var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {group}};
            var found=synchronous?local.Search(query,loadChildren:false):await local.SearchAsync(query,loadChildren:false);
            found.IsError.Should().BeFalse(found.Message);found.Result.SearchResultAvatars.Single().Id.Should().Be(avatar.Id);
            group.SearchQuery="excluded-recovery";group.AvatarSearchParams=new SearchAvatarParams {SearchAllFields=true};
            var hidden=synchronous?local.Search(query,loadChildren:false):await local.SearchAsync(query,loadChildren:false);
            hidden.IsError.Should().BeFalse(hidden.Message);hidden.Result.NumberOfResults.Should().Be(0);
        }finally{await local.DeActivateProviderAsync();}
    }
}
