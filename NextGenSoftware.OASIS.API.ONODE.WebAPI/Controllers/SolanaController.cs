using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

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
            var mintNftResult = await _solanaService.MintNft(request);
            return new OASISResult<MintNftResult>(mintNftResult.Payload);
        }

        
        [HttpPost]
        [Route("Exchange")]
        public async Task<OASISResult<ExchangeTokenResult>> ExchangeToken([FromBody] ExchangeTokenRequest request)
        {
            var exchangeTokens = await _solanaService.ExchangeTokens(request);
            return new OASISResult<ExchangeTokenResult>(exchangeTokens.Payload);
        }

        [HttpPost]
        [Route("Send")]
        public async Task<OASISResult<SendTransactionResult>> SendTransaction([FromBody] SendTransactionRequest request)
        {
            var transactResult = await _solanaService.SendTransaction(request);
            return new OASISResult<SendTransactionResult>(transactResult.Payload);
        }

        [HttpGet]
        [Route("GetNftMetadata")]
        public async Task<OASISResult<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest request)
        {
            var metadata = await _solanaService.GetNftMetadata(request);
            return new OASISResult<GetNftMetadataResult>(metadata.Payload);
        }

        [HttpGet]
        [Route("GetNftWallet")]
        public async Task<OASISResult<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest request)
        {
            var wallet = await _solanaService.GetNftWallet(request);
            return new OASISResult<GetNftWalletResult>(wallet.Payload);
        }
    }
}