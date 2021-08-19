using System.Threading.Tasks;
using Models;
using Models.Direction;
using Models.Requests;

namespace Services.MapboxDirections
{
    public interface IMapboxDirectionsService
    {
        Task<Response<Direction>> RetrieveDirections(GetDirectionRequest request);
    }
}