using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Isochrone;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Services.MapboxIsochrone
{
    public interface IMapboxIsochrone
    {
        Task<Response<Isochrone>> GetIsochrone(GetIsochroneRequest request);
    }
}