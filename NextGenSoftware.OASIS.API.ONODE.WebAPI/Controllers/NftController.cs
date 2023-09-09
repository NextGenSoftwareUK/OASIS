using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.ONode.Core.Managers;
using NextGenSoftware.OASIS.API.ONode.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NftController : OASISControllerBase
    {
        public NftController()
        {
           
        }

        [HttpPost]
        [Route("CreateNftTransaction")]
        public async Task<OASISResult<TransactionRespone>> CreateNftTransaction(CreateNftTransactionRequest request)
        {
            return await NFTManager.Instance.CreateNftTransactionAsync(request);
        }

        [HttpPost]
        [Route("CreateNftTransaction")]
        public async Task<OASISResult<TransactionRespone>> CreateNftTransaction(NFTWalletTransaction request)
        {
            return await NFTManager.Instance.CreateNftTransactionAsync(request);
        }

        [HttpPost]
        [Route("MintNft")]
        public async Task<OASISResult<TransactionRespone>> MintNft(NFTWalletTransaction request)
        {
            return await NFTManager.Instance.CreateNftTransactionAsync(request);
        }

        //[HttpGet]
        //[Route("GetOLANDPrice")]
        //public async Task<OASISResult<int>> GetOlandPrice(int count, string couponCode)
        //{
        //    return await _nftService.GetOlandPrice(count, couponCode);
        //}

        //[HttpPost]
        //[Route("PurchaseOLAND")]
        //public async Task<OASISResult<PurchaseOlandResponse>> PurchaseOland(PurchaseOlandRequest request)
        //{
        //    return await _nftService.PurchaseOland(request);
        //}
    }
}