using System.Threading.Tasks;
using Models;
using Models.Map;
using Models.Requests;

namespace Services.MapboxMaps
{
    public interface IMapboxMapService
    {
        Task<Response<VectorTile>> GetVectorTileMap(GetVectorTileRequest request);
        Task<Response<RasterTile>> GetRasterTileMap(GetRasterTileRequest request);
        Task<Response<StaticImage>> GetStaticImageMap(GetStaticImageRequest request);
        Task<Response<StaticTile>> GetStaticTileMap(GetStaticTileRequest request);
    }
}
