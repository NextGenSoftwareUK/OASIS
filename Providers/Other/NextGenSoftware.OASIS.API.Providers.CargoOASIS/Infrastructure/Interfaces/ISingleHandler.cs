using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces
{
    public interface ISingleHandler<T>
    {
        Task<T> Handle();
    }
}