using System.Threading.Tasks;
using Models;
using Models.Matrix;
using Models.Requests;

namespace Services.MapboxMatrix
{
    public interface IMapboxMatrixService
    {
        Task<Response<Matrix>> GetMatrix(GetMatrixRequest request);
    }
}