namespace API.Controllers.V1;

[Route($"{ApiAddresses.Base}/nft-purchase-ownership-histories")]
public sealed class RwaTokenOwnershipTransferController(
    IRwaTokenOwnershipTransferService service) : V1BaseController
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken token)
        => (await service.GetByIdAsync(id, token)).ToActionResult();
}