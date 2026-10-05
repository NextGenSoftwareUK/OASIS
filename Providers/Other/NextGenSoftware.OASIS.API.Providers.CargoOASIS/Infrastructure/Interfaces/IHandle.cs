using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces
{
    public interface IHandle<T, in TK>
    {
        Task<T> Handle(TK request);
    }
}