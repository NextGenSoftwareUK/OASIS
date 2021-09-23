using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Commands;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public class CargoService : ICargoService
    {
        public async Task<OASISResult<CreateAccountResponseModel>> AuthorizeCargoAccount(CreateAccountRequestModel requestModel)
        {
            var handler = new CreateAccountHandler(
                new HttpHandler(), 
                new MemoryCacheTokenStorage(),
                new MemoryCacheSignatureProvider());
            var handlerRespone = await handler.Handle(requestModel);
            return GetResponse(handlerRespone);
        }

        public async Task<OASISResult<CreateAccountResponseModel>> AuthenticateCargoAccount()
        {
            var handler = new AuthenticateAccountHandler(new MemoryCacheTokenStorage(),
                new MemoryCacheSignatureProvider(),
                new HttpHandler());
            var handlerRespone = await handler.Handle();
            return GetResponse(handlerRespone);
        }

        public async Task<OASISResult<PurchaseResponseModel>> PurchaseCargoSale(PurchaseRequestModel requestModel)
        {
            var handler = new PurchaseHandler(new HttpHandler());
            var handlerRespone = await handler.Handle(requestModel);
            return GetResponse(handlerRespone);
        }
        
        public async Task<OASISResult<CancelSaleResponseModel>> CancelCargoSale(CancelSaleRequestModel requestModel)
        {
            var handler = new CancelSaleHandler(new HttpHandler(), new MemoryCacheTokenStorage());
            var handlerRespone = await handler.Handle(requestModel);
            return GetResponse(handlerRespone);
        }

        public async Task<OASISResult<PaginationResponseWithResults<IEnumerable<Order>>>> GetCargoOrders(OrderParams orderParams)
        {
            var handler = new GetOrdersHandler(new HttpHandler(), new MemoryCacheTokenStorage());
            var ordersResult = await handler.Handle(orderParams);
            return GetResponse(ordersResult);
        }

        public async Task<OASISResult<GetUserTokensByContractResponseModel>> GetUserTokensByContract(GetUserTokensByContractRequestModel requestModel)
        {
            var handler = new GetUserTokensByContractHandler(new HttpHandler(), new MemoryCacheTokenStorage());
            var handlerRespone = await handler.Handle(requestModel);
            return GetResponse(handlerRespone);
        }

        public async Task<OASISResult<GetCollectiblesListByProjectIdResponseModel>> CollectiblesListByProjectId(GetCollectiblesListByProjectIdRequestModel requestModel)
        {
            var handler = new GetListCollectiblesByProjectIdHandler(new HttpHandler());
            var handlerRespone = await handler.Handle(requestModel);
            return GetResponse(handlerRespone);
        }

        private OASISResult<T> GetResponse<T>(Response<T> responseTemplate) where T : new()
        {
            var response = new OASISResult<T>
            {
                Result = responseTemplate.Payload, 
                Message = responseTemplate.Message
            };
            switch (responseTemplate.ResponseStatus)
            {
                case ResponseStatus.Success:
                    response.IsError = false;
                    break;
                case ResponseStatus.Fail:
                case ResponseStatus.Unauthorized:
                case ResponseStatus.NotRegistered:
                    response.IsError = true;
                    break;
                default:
                    response.IsError = true;
                    break;
            }
            return response;
        }
    }
}