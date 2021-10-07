using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NftController : OASISControllerBase
    {
        private readonly INftService _nftService;
        public NftController(INftService nftService)
        {
            _nftService = nftService;
        }

        [HttpPost]
        [Route("CreatePurchase")]
        public async Task<OASISResult<NftTransactionRespone>> CreateNftTransaction(CreateNftTransactionRequest request)
        {
            return await _nftService.CreateNftTransaction(request);
        }

        [HttpGet]
        [Route("GetOLANDPrice")]
        public async Task<OASISResult<int>> GetOLANDPrice(int count, string couponCode)
        {
            return await _nftService.GetOlandPrice(count, couponCode);
        }
    }
}