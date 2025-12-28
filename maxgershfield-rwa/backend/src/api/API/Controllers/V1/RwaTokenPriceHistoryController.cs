namespace API.Controllers.V1;

[Route($"{ApiAddresses.Base}/rwa-price-histories")]
public class RwaTokenPriceHistoryController(IRwaTokenPriceHistoryService service) : V1BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] RwaTokenPriceHistoryFilter filter, CancellationToken token)
        => (await service.GetAsync(filter, token)).ToActionResult();


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken token)
        => (await service.GetAsync(id, token)).ToActionResult();
}