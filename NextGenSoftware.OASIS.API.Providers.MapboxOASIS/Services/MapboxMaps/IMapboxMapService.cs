using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Map;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Services.MapboxMaps
{
    public interface IMapboxMapService
    {
        Task<Response<VectorTile>> GetVectorTileMap(GetVectorTileRequest request);
        Task<Response<RasterTile>> GetRasterTileMap(GetRasterTileRequest request);
        Task<Response<StaticImage>> GetStaticImageMap(GetStaticImageRequest request);
        Task<Response<StaticTile>> GetStaticTileMap(GetStaticTileRequest request);
    }
}
