namespace API.Controllers.V1;

[Route($"{ApiAddresses.Base}/nft-purchase")]
public sealed class NftPurchaseController(INftPurchaseService nftPurchaseService) : V1BaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateNftPurchaseRequest request)
        => (await nftPurchaseService.CreateAsync(request)).ToActionResult();

    [HttpPost("send")]
    public async Task<IActionResult> SendAsync([FromBody] SendNftPurchaseTrRequest request)
        => (await nftPurchaseService.SendAsync(request)).ToActionResult();
}