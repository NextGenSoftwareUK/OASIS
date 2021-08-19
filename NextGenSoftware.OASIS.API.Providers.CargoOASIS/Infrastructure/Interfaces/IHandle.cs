using System.Threading.Tasks;

namespace Infrastructure.Interfaces
{
    public interface IHandle<T, in TK>
    {
        Task<T> Handle(TK request);
    }
}