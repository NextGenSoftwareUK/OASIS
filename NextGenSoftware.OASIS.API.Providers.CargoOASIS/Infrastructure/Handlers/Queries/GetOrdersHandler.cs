using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetOrdersHandler : IHandle<Response<PaginationResponseWithResults<IEnumerable<Order>>>, OrderParams>
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken = string.Empty;

        public GetOrdersHandler()
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(1),
                BaseAddress = new Uri("https://api2.cargo.build/")
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
        
        public async Task<Response<PaginationResponseWithResults<IEnumerable<Order>>>> Handle(OrderParams request)
        {
            var response = new Response<PaginationResponseWithResults<IEnumerable<Order>>>();
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
                var urlQuery = $"v3/get-orders{queryBuilder.GetQuery()}";
                var httRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_httpClient.BaseAddress + urlQuery)
                };
                var httpResponse = await _httpClient.SendAsync(httRequest);
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<PaginationResponseWithResults<IEnumerable<Order>>>(responseString);
                response.Payload = data;
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