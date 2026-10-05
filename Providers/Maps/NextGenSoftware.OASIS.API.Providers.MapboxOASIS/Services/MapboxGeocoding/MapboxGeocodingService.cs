using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Extensions;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Geocoding;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Services.MapboxGeocoding
{
    public class MapboxGeocodingService : IMapboxGeocodingService
    {
        private readonly string _mapBoxToken;
        private readonly string _baseUrl;

        private readonly HttpClient _httpClient;
        
        public MapboxGeocodingService()
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
        /// Represents an Mapbox Geocoding API for Forward Geocoding
        /// More Information On: https://docs.mapbox.com/api/search/geocoding/
        /// </summary>
        /// <param name="request">URL Parameters</param>
        /// <returns>Built Forward Geocoding Response from Mapbox API</returns>
        /// <exception cref="ArgumentNullException">If request is null</exception>
        public async Task<Response<Geocoding>> GetForwardGeocoding(GetForwardGeocodingRequest request)
        {
            if(request == null)
                throw new ArgumentNullException();
            
            var response = new Response<Geocoding>();
            
            try
            {
                var endpoint = $"geocoding/v5/{request.Endpoint.GetDescription()}/{request.SearchText}.json?access_token={_mapBoxToken}";
                var requestMessage = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_httpClient.BaseAddress + endpoint)
                };

                var responseMessage = await _httpClient.SendAsync(requestMessage);
                var responseString = await responseMessage.Content.ReadAsStringAsync();
                if (!responseMessage.IsSuccessStatusCode)
                {
                    response.StatusCode = ResponseStatusCode.Fail;
                    response.Message = responseMessage.ReasonPhrase;
                    return response;
                }
                var entity = JsonConvert.DeserializeObject<Geocoding>(responseString);
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

        /// <summary>
        /// Represents an Mapbox Geocoding API for Reverse Geocoding
        /// More Information On: https://docs.mapbox.com/api/search/geocoding/
        /// </summary>
        /// <param name="request">URL Parameters</param>
        /// <returns>Built Reverse Geocoding Response from Mapbox API</returns>
        /// <exception cref="ArgumentNullException">If request is null</exception>
        public async Task<Response<Geocoding>> GetReverseGeocoding(GetReverseGeocodingRequest request)
        {
            if(request == null)
                throw new ArgumentNullException();
            
            var response = new Response<Geocoding>();
            
            try
            {
                var endpoint = $"geocoding/v5/{request.Endpoint}/{request.Longitude},{request.Latitude}.json?access_token={_mapBoxToken}";
                var requestMessage = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_httpClient.BaseAddress + endpoint)
                };

                var responseMessage = await _httpClient.SendAsync(requestMessage);
                var responseString = await responseMessage.Content.ReadAsStringAsync();
                if (!responseMessage.IsSuccessStatusCode)
                {
                    response.StatusCode = ResponseStatusCode.Fail;
                    response.Message = responseMessage.ReasonPhrase;
                    return response;
                }
                var entity = JsonConvert.DeserializeObject<Geocoding>(responseString);
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
        
        /// <summary>
        /// Represents an Mapbox Geocoding API for Batch Reverse Geocoding
        /// More Information On: https://docs.mapbox.com/api/search/geocoding/
        /// </summary>
        /// <param name="request">URL Parameters</param>
        /// <returns>Built Batch Reverse Geocoding Response from Mapbox API</returns>
        /// <exception cref="ArgumentNullException">If request is null</exception>
        public async Task<Response<List<Geocoding>>> GetReverseBatchGeocoding(GetReverseBatchGeocodingRequest request)
        {
            if(request == null)
                throw new ArgumentNullException();
            
            var response = new Response<List<Geocoding>>();
            
            try
            {
                var endpoint = $"/geocoding/v5/{request.Endpoint}/{string.Join(";", request.Coordinates)}.json?access_token={_mapBoxToken}";
                var requestMessage = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_httpClient.BaseAddress + endpoint)
                };

                var responseMessage = await _httpClient.SendAsync(requestMessage);
                var responseString = await responseMessage.Content.ReadAsStringAsync();
                if (!responseMessage.IsSuccessStatusCode)
                {
                    response.StatusCode = ResponseStatusCode.Fail;
                    response.Message = responseMessage.ReasonPhrase;
                    return response;
                }
                var entity = JsonConvert.DeserializeObject<List<Geocoding>>(responseString);
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
        
        /// <summary>
        /// Represents an Mapbox Geocoding API for Batch Forward Geocoding
        /// More Information On: https://docs.mapbox.com/api/search/geocoding/
        /// </summary>
        /// <param name="request">URL Parameters</param>
        /// <returns>Built Batch Forward Geocoding Response from Mapbox API</returns>
        /// <exception cref="ArgumentNullException">If request is null</exception>
        public async Task<Response<List<Geocoding>>> GetForwardBatchGeocoding(GetForwardBatchGeocodingRequest request)
        {
            if(request == null)
                throw new ArgumentNullException();
            
            var response = new Response<List<Geocoding>>();
            
            try
            {
                var endpoint = $"/geocoding/v5/{request.Endpoint}/{string.Join(";", request.SearchText)}.json?access_token={_mapBoxToken}";
                var requestMessage = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_httpClient.BaseAddress + endpoint)
                };

                var responseMessage = await _httpClient.SendAsync(requestMessage);
                var responseString = await responseMessage.Content.ReadAsStringAsync();
                if (!responseMessage.IsSuccessStatusCode)
                {
                    response.StatusCode = ResponseStatusCode.Fail;
                    response.Message = responseMessage.ReasonPhrase;
                    return response;
                }
                var entity = JsonConvert.DeserializeObject<List<Geocoding>>(responseString);
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