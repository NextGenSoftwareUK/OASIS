using ScGen.Lib.Shared.DTOs.Requests;
using ScGen.Lib.Shared.Services.PropertyExtractor;

namespace ScGen.API.Infrastructure.Controllers.V1;

[ApiController]
[Route($"{ApiAddresses.Base}/property")]
public sealed class PropertyExtractorController(IPropertyExtractorService propertyExtractorService) : BaseController
{
    [HttpPost("extract")]
    public async Task<IActionResult> ExtractPropertyDataAsync([FromBody] ExtractPropertyDataRequest request)
        => (await propertyExtractorService.ExtractAsync(request)).ToActionResult();
}

