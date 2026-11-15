using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class NftTransferRequestTests
{
    [Fact]
    public async Task RejectsUnboundOrAmbiguousNftTransfers()
    {
        var http = new DefaultHttpContext();
        http.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar
            { Id = Guid.NewGuid(), AvatarType = new EnumValue<AvatarType>(AvatarType.Wizard) };
        var controller = new NftController { ControllerContext = new ControllerContext { HttpContext = http } };
        var request = new NFTWalletTransactionRequest
        {
            FromProvider = nameof(ProviderType.SolanaOASIS),
            ToProvider = nameof(ProviderType.SolanaOASIS),
            FromWalletAddress = "sender",
            ToWalletAddress = "recipient",
            Amount = 1
        };

        var missingIdentity = await controller.SendNFTAsync(request);
        Assert.True(missingIdentity.IsError);
        Assert.Contains("NFT token address", missingIdentity.Message);

        request.TokenAddress = "mint";
        request.ToAvatarUsername = "another-recipient";
        var ambiguousRecipient = await controller.SendNFTAsync(request);
        Assert.True(ambiguousRecipient.IsError);
        Assert.Contains("one recipient", ambiguousRecipient.Message);

        request.ToAvatarUsername = null;
        request.ToProvider = nameof(ProviderType.MongoDBOASIS);
        var mixedProviders = await controller.SendNFTAsync(request);
        Assert.True(mixedProviders.IsError);
        Assert.Contains("same concrete blockchain provider", mixedProviders.Message);
    }
}
