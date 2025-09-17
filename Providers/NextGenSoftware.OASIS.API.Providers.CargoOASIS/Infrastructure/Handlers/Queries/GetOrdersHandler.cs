using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetOrdersHandler : IHandle<OASISResult<PaginationResponseWithResults<IEnumerable<Order>>>, OrderParams>
    {
        private readonly IHttpHandler _httpClient;
        public GetOrdersHandler(IHttpHandler httpClient)
        {
            _httpClient = httpClient;
        }
        
        /// <summary>
        /// Get orders list
        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-orders
        /// </summary>
        /// <param name="request">Request parameters</param>
        /// <returns>Order list with pagination</returns>
        public async Task<OASISResult<PaginationResponseWithResults<IEnumerable<Order>>>> Handle(OrderParams request)
        {
            var response = new OASISResult<PaginationResponseWithResults<IEnumerable<Order>>>();
            try
            {
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
                        { "Authorization", $"Bearer {request.AccessJwtToken}" }
                    }
                };
                var httpResponse = await _httpClient.SendAsync(httRequest);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    response.Message = httpResponse.ReasonPhrase;
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<PaginationResponseWithResults<IEnumerable<Order>>>(responseString);
                response.Result = data;
                return response;
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.Message = e.Message;
                ErrorHandling.HandleError(ref response, e.Message);
                return response;
            }
        }
    }
}