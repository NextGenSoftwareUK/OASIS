using System;
using System.Net.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Extensions;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Map;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Services.MapboxMaps
{
    public class MapboxMapService : IMapboxMapService
    {
        private readonly string _mapBoxToken;
        private readonly string _baseUrl;

        private readonly HttpClient _httpClient;
        
        public MapboxMapService()
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
        /// Sends get request to Mapbox API for getting tile maps
        /// </summary>
        /// <param name="url">Represents url with required parameters</param>
        /// <returns>Tile Map Bytes</returns>
        private async Task<byte[]> RetrieveBytes(string url)
        {
            var httpRequest = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_httpClient.BaseAddress + url)
            };
            var responseMessage = await _httpClient.SendAsync(httpRequest);
            var responseBytes = await responseMessage.Content.ReadAsByteArrayAsync();
            return responseBytes;
        }
        
        /// <summary>
        /// Represents an Mapbox API for getting Vector Tile Map
        /// More Information On: https://docs.mapbox.com/api/maps/vector-tiles/
        /// </summary>
        /// <param name="request">URL Parameters</param>
        /// <returns>Vector Tile Bytes</returns>
        /// <exception cref="ArgumentNullException">If request is null</exception>
        public async Task<Response<VectorTile>> GetVectorTileMap(GetVectorTileRequest request)
        {
            if(request == null)
                throw new ArgumentNullException();
            var response = new Response<VectorTile>();
            try
            {
                response.Payload.Bytes = await RetrieveBytes($"v4/{request.TilesetId}/{request.Zoom}/{request.X}/{request.Y}.{request.Format.GetDescription()}?access_token={_mapBoxToken}");
                return response;
            }
            catch (Exception e)
            {
                response.StatusCode = ResponseStatusCode.Fail;
                response.Message = e.Message;
                return response;
            }
        }

        /// <summary>
        /// Represents an Mapbox API for getting Raster Tile Map
        /// More Information On: https://docs.mapbox.com/api/maps/raster-tiles/
        /// </summary>
        /// <param name="request">URL Parameters</param>
        /// <returns>Raster Tile Bytes</returns>
        /// <exception cref="ArgumentNullException">If request is null</exception>
        public async Task<Response<RasterTile>> GetRasterTileMap(GetRasterTileRequest request)
        {
            if(request == null)
                throw new ArgumentNullException();
            var response = new Response<RasterTile>();
            try
            {
                response.Payload.Bytes = await RetrieveBytes($"v4/{request.TilesetId}/{request.Zoom}/{request.X}/{request.Y}@2x.{request.Format.GetDescription()}?access_token={_mapBoxToken}");
                return response;
            }
            catch (Exception e)
            {
                response.StatusCode = ResponseStatusCode.Fail;
                response.Message = e.Message;
                return response;
            }
        }

        /// <summary>
        /// Represents an Mapbox API for getting Static Image Map
        /// More Information On: https://docs.mapbox.com/api/maps/static-images/
        /// </summary>
        /// <param name="request">URL Parameters</param>
        /// <returns>Static Image Bytes</returns>
        /// <exception cref="ArgumentNullException">If request is null</exception>
        public async Task<Response<StaticImage>> GetStaticImageMap(GetStaticImageRequest request)
        {
            if(request == null)
                throw new ArgumentNullException();
            var response = new Response<StaticImage>();
            try
            {
                response.Payload.Bytes = await RetrieveBytes($"styles/v1/{request.Username}/{request.StyleId}/static/{request.Overlay}/{request.Longitude},{request.Latitude},{request.Zoom},{request.Bearing},{request.Pitch}/{request.Width}x{request.Height}@2x?access_token={_mapBoxToken}");
                return response;
            }
            catch (Exception e)
            {
                response.StatusCode = ResponseStatusCode.Fail;
                response.Message = e.Message;
                return response;
            }
        }

        /// <summary>
        /// Represents an Mapbox API for getting Static Tile Map
        /// More Information On: https://docs.mapbox.com/api/maps/static-tiles/
        /// </summary>
        /// <param name="request">URL Parameters</param>
        /// <returns>Static Tile Bytes</returns>
        /// <exception cref="ArgumentNullException">If request is null</exception>
        public async Task<Response<StaticTile>> GetStaticTileMap(GetStaticTileRequest request)
        {
            if(request == null)
                throw new ArgumentNullException();
            var response = new Response<StaticTile>();
            try
            {
                response.Payload.Bytes = await RetrieveBytes($"styles/v1/{request.Username}/{request.StyleId}/tiles/{request.TileSize}/{request.Z}/{request.X}/{request.Y}@2x?access_token={_mapBoxToken}");
                return response;
            }
            catch (Exception e)
            {
                response.StatusCode = ResponseStatusCode.Fail;
                response.Message = e.Message;
                return response;
            }        
        }
    }
}