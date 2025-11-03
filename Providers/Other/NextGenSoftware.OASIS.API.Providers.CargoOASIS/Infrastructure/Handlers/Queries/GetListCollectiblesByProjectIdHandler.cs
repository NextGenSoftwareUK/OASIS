//using System;
//using System.Net.Http;
//using System.Threading.Tasks;
//using Newtonsoft.Json;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;
//using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Response;
//using NextGenSoftware.OASIS.Common;

//namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
//{
//    public class GetListCollectiblesByProjectIdHandler : IHandle<OASISResult<GetCollectiblesListByProjectIdResponseModel>, GetCollectiblesListByProjectIdRequestModel>
//    {
//        private readonly IHttpHandler _httpClient;

//        public GetListCollectiblesByProjectIdHandler(IHttpHandler httpClient)
//        {
//            _httpClient = httpClient;
//        }
        
//        /// <summary>
//        /// Gets collectibles list by project id
//        /// More information: https://docs.cargo.build/cargo-js/cargo.api#get-a-list-of-collectibles-by-project-id 
//        /// </summary>
//        /// <param name="request">Request Parameters</param>
//        /// <returns>Collectibles list</returns>
//        public async Task<OASISResult<GetCollectiblesListByProjectIdResponseModel>> Handle(GetCollectiblesListByProjectIdRequestModel request)
//        {
//            var response = new OASISResult<GetCollectiblesListByProjectIdResponseModel>();
//            try
//            {
//                var queryBuilder = new UrlQueryBuilder();
//                queryBuilder.AppendParameter("ownerAddress", request.OwnerAddress);
//                queryBuilder.AppendParameter("limit", request.Limit.ToString());
//                queryBuilder.AppendParameter("page", request.Page.ToString());
//                var urlQuery = $"https://api2.cargo.build/v5/get-tokens-by-project/{request.ProjectId}{queryBuilder.GetQuery()}";
//                var httRequest = new HttpRequestMessage()
//                {
//                    Method = HttpMethod.Get,
//                    RequestUri = new Uri(urlQuery)
//                };
//                var httpResponse = await _httpClient.SendAsync(httRequest);
//                if (!httpResponse.IsSuccessStatusCode)
//                {
//                    response.IsError = true;
//                    response.Message = httpResponse.ReasonPhrase;
//                    ErrorHandling.HandleError(ref response, response.Message);
//                    return response;
//                }
//                var responseString = await httpResponse.Content.ReadAsStringAsync();
//                var data = JsonConvert.DeserializeObject<GetCollectiblesListByProjectIdResponseModel>(responseString);
//                response.Result = data;
//                return response;
//            }
//            catch (Exception e)
//            {
//                response.IsError = true;
//                response.Message = e.Message;
//                ErrorHandling.HandleError(ref response, e.Message);
//                return response;
//            }
//        }
//    }
//}