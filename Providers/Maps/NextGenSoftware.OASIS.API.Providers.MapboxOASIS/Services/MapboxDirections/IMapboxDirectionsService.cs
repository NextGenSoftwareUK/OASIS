using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Direction;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Services.MapboxDirections
{
    public interface IMapboxDirectionsService
    {
        Task<Response<Direction>> RetrieveDirections(GetDirectionRequest request);
    }
}