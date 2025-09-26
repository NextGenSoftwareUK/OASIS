using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Matrix;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Services.MapboxMatrix
{
    public interface IMapboxMatrixService
    {
        Task<Response<Matrix>> GetMatrix(GetMatrixRequest request);
    }
}