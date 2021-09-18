using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Exceptions;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetOrdersHandler : IHandle<Response<PaginationResponseWithResults<IEnumerable<Order>>>, OrderParams>
    {
        private readonly IHttpHandler _httpClient;
        private readonly ITokenStorage _tokenStorage;
        public GetOrdersHandler(IHttpHandler httpClient, ITokenStorage tokenStorage)
        {
            _httpClient = httpClient;
            _tokenStorage = tokenStorage;
        }
        
        /// <summary>
        /// Get orders list
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-orders
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Order list with pagination</returns>
        public async Task<Response<PaginationResponseWithResults<IEnumerable<Order>>>> Handle(OrderParams request)
        {
            var response = new Response<PaginationResponseWithResults<IEnumerable<Order>>>();
            try
            {
                var accessToken = await _tokenStorage.GetToken();
                
                var queryBuilder = new UrlQueryBuilder();
                queryBuilder.AppendParameter("from", request.From);
                queryBuilder.AppendParameter("limit", request.Limit);
                queryBuilder.AppendParameter("page", request.Page);
                queryBuilder.AppendParameter("to", request.To);
                queryBuilder.AppendParameter("buyerAddress", request.BuyerAddress);
                queryBuilder.AppendParameter("contractAddress", request.ContractAddress);
                queryBuilder.AppendParameter("crateId", request.CrateId);
                queryBuilder.AppendParameter("sellerAddress", request.SellerAddress);
                queryBuilder.AppendParameter("tokenId", request.TokenId);
                queryBuilder.AppendParameter("vendorId", request.VendorId);
                var urlQuery = $"https://api2.cargo.build/v3/get-orders{queryBuilder.GetQuery()}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(urlQuery),
                    Headers =
                    {
                        { "Content-Type", "application/json" },
                        { "Authorization", $"Bearer {accessToken}" }
                    }
                };
                var httpResponse = await _httpClient.SendAsync(httRequest);
                if(httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                    throw new UserNotAuthorizedException();
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.Message = httpResponse.ReasonPhrase;
                    response.ResponseStatus = ResponseStatus.Fail;
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<PaginationResponseWithResults<IEnumerable<Order>>>(responseString);
                response.Payload = data;
                return response;
            }
            catch (UserNotAuthorizedException e)
            {
                response.ResponseStatus = ResponseStatus.Unauthorized;
                response.Message = e.Message;
                return response;
            }
            catch (UserNotRegisteredException e)
            {
                response.ResponseStatus = ResponseStatus.NotRegistered;
                response.Message = e.Message;
                return response;
            }
            catch (Exception e)
            {
                response.ResponseStatus = ResponseStatus.Fail;
                response.Message = e.Message;
                return response;
            }
        }
    }
}