using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT;
using NextGenSoftware.Utilities;

public class NftMintOptionValidationTests
{
    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task ExplicitInvalidMintActorDoesNotSilentlyBecomeCaller(string actor)
    {
        var controller = new NftController { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.Avatar = new Avatar { Id=Guid.NewGuid(),AvatarType=new EnumValue<AvatarType>(AvatarType.Wizard) };
        var result = await controller.MintNftAsync(new MintNFTTransactionRequest { OnChainProvider="SuiOASIS",OffChainProvider="LocalFileOASIS",
            NFTOffChainMetaType="OASIS",NFTStandardType="SuiObject",MintedByAvatarId=actor });
        Assert.True(result.IsError); Assert.Null(result.Result); Assert.Contains("MintedByAvatarId",result.Message);
    }

    [Fact]
    public async Task MissingMintBodyReturnsExplicitError()
    {
        var controller = new NftController { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.Avatar = new Avatar { Id=Guid.NewGuid(),AvatarType=new EnumValue<AvatarType>(AvatarType.Wizard) };
        var result = await controller.MintNftAsync(null!);
        Assert.True(result.IsError); Assert.Null(result.Result); Assert.Contains("body",result.Message);
    }
    [Theory]
    [InlineData("OnChainProvider")]
    [InlineData("OffChainProvider")]
    [InlineData("NFTOffChainMetaType")]
    [InlineData("NFTStandardType")]
    public async Task UndefinedNumericOptionIsRejectedBeforeMintManagerAccess(string field)
    {
        var controller = new NftController { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.Avatar = new Avatar { Id=Guid.NewGuid(),AvatarType=new EnumValue<AvatarType>(AvatarType.Wizard) };
        var request = new MintNFTTransactionRequest { OnChainProvider="SuiOASIS",OffChainProvider="LocalFileOASIS",
            NFTOffChainMetaType="OASIS",NFTStandardType="SuiObject" };
        typeof(MintNFTTransactionRequest).GetProperty(field)!.SetValue(request,"2147483647");
        var result = await controller.MintNftAsync(request);
        Assert.True(result.IsError); Assert.Null(result.Result); Assert.Contains(field,result.Message);
    }
}
