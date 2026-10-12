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
