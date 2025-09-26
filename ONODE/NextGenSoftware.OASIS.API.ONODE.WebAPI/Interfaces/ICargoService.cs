//using System.Collections.Generic;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Cargo;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Request;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Response;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
//{
//    public interface ICargoService
//    {
//        Task<OASISResult<CreateAccountResponseModel>> AuthorizeCargoAccount(CreateAccountRequestModel requestModel);
//        Task<OASISResult<CreateAccountResponseModel>> AuthenticateCargoAccount(AuthenticateAccountRequestModel requestModel);
//        Task<OASISResult<PurchaseResponseModel>> PurchaseCargoSale(PurchaseRequestModel requestModel);
//        Task<OASISResult<CancelSaleResponseModel>> CancelCargoSale(CancelSaleRequestModel requestModel); 
//        Task<OASISResult<PaginationResponseWithResults<IEnumerable<Order>>>> GetCargoOrders(OrderParams orderParams);
//        Task<OASISResult<GetUserTokensByContractResponseModel>> GetUserTokensByContract(GetUserTokensByContractRequestModel requestModel);
//        Task<OASISResult<GetCollectiblesListByProjectIdResponseModel>> CollectiblesListByProjectId(GetCollectiblesListByProjectIdRequestModel requestModel);
//    }
//}