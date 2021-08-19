using System.Threading.Tasks;
using Models;
using Models.Isochrone;
using Models.Requests;

namespace Services.MapboxIsochrone
{
    public interface IMapboxIsochrone
    {
        Task<Response<Isochrone>> GetIsochrone(GetIsochroneRequest request);
    }
}