using System;
using System.Net.Http;
using System.Threading.Tasks;
using Enums;
using Extensions;
using Models;
using Models.Direction;
using Models.Requests;
using Newtonsoft.Json;

namespace Services.MapboxDirections
{
    public class MapboxDirectionsService : IMapboxDirectionsService
    {
        private readonly string _mapBoxToken;
        private readonly string _baseUrl;

        private readonly HttpClient _httpClient;
        
        public MapboxDirectionsService()
        {
            _baseUrl = "https://api.mapbox.com/";
            _mapBoxToken = "pk.eyJ1IjoiZmFoYS1iZXJkaWV2IiwiYSI6ImNrczl3MTdvMzFhMDkyb3MwNzFlNTZpcmwifQ.aeRE3fuGsx2eyvArIoXPUg";
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromMinutes(1)
            };
        }
        /// <summary>
        /// Represents Mapbox Directions API
        /// More Information On: https://docs.mapbox.com/api/navigation/directions/
        /// </summary>
        /// <param name="request">URL Parameters</param>
        /// <returns>Built Direction From Mapbox Direction API</returns>
        /// <exception cref="ArgumentNullException">If request is null</exception>
        public async Task<Response<Direction>> RetrieveDirections(GetDirectionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException();
            
            var response = new Response<Direction>();
            
            try
            {
                var url = $"directions/v5/{request.RoutingProfile.GetDescription()}/{string.Join(";",request.Coordinates)}?access_token={_mapBoxToken}";
                var requestMessage = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_httpClient.BaseAddress + url)
                };
                var responseMessage = await _httpClient.SendAsync(requestMessage);
                if (!responseMessage.IsSuccessStatusCode)
                {
                    response.StatusCode = ResponseStatusCode.Fail;
                    response.Message = responseMessage.ReasonPhrase;
                    return response;
                }
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                var entity = JsonConvert.DeserializeObject<Direction>(responseContent);
                response.Payload = entity;
                return response;
            }
            catch (Exception e)
            {
                response.Message = e.Message;
                response.StatusCode = ResponseStatusCode.Fail;
                return response;
            }
        }
    }
}