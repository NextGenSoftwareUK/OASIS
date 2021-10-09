using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using Solnet.Metaplex;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class SolanaController : OASISControllerBase
    {
        private readonly ISolanaService _solanaService;

        public SolanaController(ISolanaService solanaService)
        {
            _solanaService = solanaService;
        }

        [HttpPost]
        [Route("Mint")]
        public async Task<OASISResult<MintNftResult>> MintNft([FromBody] MintNftRequest request)
        {
            return await _solanaService.MintNft(request);
        }

        
        [HttpPost]
        [Route("Exchange")]
        public async Task<OASISResult<ExchangeTokenResult>> ExchangeToken([FromBody] ExchangeTokenRequest request)
        {
            return await _solanaService.ExchangeTokens(request);
        }

        [HttpPost]
        [Route("Send")]
        public async Task<OASISResult<SendTransactionResult>> SendTransaction([FromBody] SendTransactionRequest request)
        {
            return await _solanaService.SendTransaction(request);
        }

        [HttpGet]
        [Route("GetNftMetadata")]
        public async Task<OASISResult<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest request)
        {
            return await _solanaService.GetNftMetadata(request);
        }

        [HttpGet]
        [Route("GetNftWallet")]
        public async Task<OASISResult<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest request)
        {
            return await _solanaService.GetNftWallet(request);
        }
    }
}