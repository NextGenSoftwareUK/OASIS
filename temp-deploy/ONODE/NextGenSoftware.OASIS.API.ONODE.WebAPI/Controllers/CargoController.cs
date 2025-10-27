//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Cargo;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Request;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Response;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    [Authorize]
//    public class CargoController : OASISControllerBase
//    {
//        private readonly ICargoService _cargoService;

//        public CargoController(ICargoService cargoService)
//        {
//            _cargoService = cargoService;
//        }

//        /// <summary>
//        /// Authorize Account with optional (email and username) or required account address
//        /// </summary>
//        /// <param name="requestModel">Account Address that need to be authorized</param>
//        /// <returns>JWT Token</returns>
//        [HttpPost]
//        [Route("AuthorizeAccount")]
//        public async Task<OASISResult<CreateAccountResponseModel>> AuthorizeAccount(
//            [FromBody] CreateAccountRequestModel requestModel)
//        {
//            return await _cargoService.AuthorizeCargoAccount(requestModel);
//        }

//        /// <summary>
//        /// Authenticate specified account address 
//        /// </summary>
//        /// <param name="requestModel">Account Address</param>
//        /// <returns>JWT Token</returns>
//        [HttpPost]
//        [Route("AuthenticateAccount")]
//        public async Task<OASISResult<CreateAccountResponseModel>> AuthenticateAccount([FromBody] AuthenticateAccountRequestModel requestModel)
//        {
//            return await _cargoService.AuthenticateCargoAccount(requestModel);
//        }

//        /// <summary>
//        /// Handling cargo purchasing with specified saleId
//        /// </summary>
//        /// <param name="requestModel">SaleId that need to be purchased</param>
//        /// <returns>Purchase Transaction Hash</returns>
//        [HttpPost]
//        [Route("PurchaseSale")]
//        public async Task<OASISResult<PurchaseResponseModel>> PurchaseCargoSale([FromBody] PurchaseRequestModel requestModel)
//        {
//            return await _cargoService.PurchaseCargoSale(requestModel);
//        }

//        /// <summary>
//        /// Handling cargo sale cancellation
//        /// </summary>
//        /// <param name="requestModel">The ID of the resale item</param>
//        /// <returns>Cancellation signature</returns>
//        [HttpPost]
//        [Route("CancelSale")]
//        public async Task<OASISResult<CancelSaleResponseModel>> CancelCargoSale([FromBody] CancelSaleRequestModel requestModel)
//        {
//            return await _cargoService.CancelCargoSale(requestModel);
//        }

//        /// <summary>
//        /// Returning orders list
//        /// </summary>
//        /// <param name="orderParams">Fetching order list request</param>
//        /// <returns>Orders List</returns>
//        [HttpGet]
//        [Route("GetOrders")]
//        public async Task<OASISResult<PaginationResponseWithResults<IEnumerable<Order>>>> GetCargoOrders(
//            [FromBody] OrderParams orderParams)
//        {
//            return await _cargoService.GetCargoOrders(orderParams);
//        }

//        /// <summary>
//        /// Returns user token list by specified contract
//        /// </summary>
//        /// <param name="requestModel">Fetching user token list request</param>
//        /// <returns>User token with specified contract</returns>
//        [HttpGet]
//        [Route("GetUserTokensByContract")]
//        public async Task<OASISResult<GetUserTokensByContractResponseModel>> GetUserTokensByContract(
//            [FromBody] GetUserTokensByContractRequestModel requestModel)
//        {
//            return await _cargoService.GetUserTokensByContract(requestModel);
//        }

//        /// <summary>
//        /// Returns collectibles list by specified project id
//        /// </summary>
//        /// <param name="requestModel">Project Id</param>
//        /// <returns>Collectibles list by specified project id</returns>
//        [HttpGet]
//        [Route("GetCollectiblesListByProjectId")]
//        public async Task<OASISResult<GetCollectiblesListByProjectIdResponseModel>> CollectiblesListByProjectId(
//            [FromBody] GetCollectiblesListByProjectIdRequestModel requestModel)
//        {
//            return await _cargoService.CollectiblesListByProjectId(requestModel);
//        }
//    }
//}