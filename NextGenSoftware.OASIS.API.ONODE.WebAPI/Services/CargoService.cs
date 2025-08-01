//using System.Collections.Generic;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.DNA;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Cargo;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Request;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Response;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
//{
//    public class CargoService : ICargoService
//    {
//        private readonly OASISDNA _OASISDNA;
//        public CargoService()
//        {
//            _OASISDNA = OASISBootLoader.OASISBootLoader.OASISDNA;
//        }
        
//        public async Task<OASISResult<CreateAccountResponseModel>> AuthorizeCargoAccount(CreateAccountRequestModel requestModel)
//        {
//            var handler = new CreateAccountHandler(new HttpHandler(), new Web3SignatureProvider());
//            requestModel.HostUrl = _OASISDNA.OASIS.StorageProviders.CargoOASIS.HostUrl;
//            requestModel.PrivateKey = _OASISDNA.OASIS.StorageProviders.CargoOASIS.PrivateKey;
//            requestModel.SingingMessage = _OASISDNA.OASIS.StorageProviders.CargoOASIS.SingingMessage;
//            return await handler.Handle(requestModel);
//        }

//        public async Task<OASISResult<CreateAccountResponseModel>> AuthenticateCargoAccount(AuthenticateAccountRequestModel requestModel)
//        {
//            var handler = new AuthenticateAccountHandler(new HttpHandler(), new Web3SignatureProvider());
//            requestModel.HostUrl = _OASISDNA.OASIS.StorageProviders.CargoOASIS.HostUrl;
//            requestModel.PrivateKey = _OASISDNA.OASIS.StorageProviders.CargoOASIS.PrivateKey;
//            requestModel.SingingMessage = _OASISDNA.OASIS.StorageProviders.CargoOASIS.SingingMessage;
//            return await handler.Handle(requestModel);
//        }

//        public async Task<OASISResult<PurchaseResponseModel>> PurchaseCargoSale(PurchaseRequestModel requestModel)
//        {
//            var handler = new PurchaseHandler(new HttpHandler());
//            return await handler.Handle(requestModel);
//        }
        
//        public async Task<OASISResult<CancelSaleResponseModel>> CancelCargoSale(CancelSaleRequestModel requestModel)
//        {
//            var handler = new CancelSaleHandler(new HttpHandler());
//            return await handler.Handle(requestModel);
//        }

//        public async Task<OASISResult<PaginationResponseWithResults<IEnumerable<Order>>>> GetCargoOrders(OrderParams orderParams)
//        {
//            var handler = new GetOrdersHandler(new HttpHandler());
//            return await handler.Handle(orderParams);
//        }

//        public async Task<OASISResult<GetUserTokensByContractResponseModel>> GetUserTokensByContract(GetUserTokensByContractRequestModel requestModel)
//        {
//            var handler = new GetUserTokensByContractHandler(new HttpHandler());
//            return await handler.Handle(requestModel);
//        }

//        public async Task<OASISResult<GetCollectiblesListByProjectIdResponseModel>> CollectiblesListByProjectId(GetCollectiblesListByProjectIdRequestModel requestModel)
//        {
//            var handler = new GetListCollectiblesByProjectIdHandler(new HttpHandler());
//            return await handler.Handle(requestModel);
//        }
//    }
//}