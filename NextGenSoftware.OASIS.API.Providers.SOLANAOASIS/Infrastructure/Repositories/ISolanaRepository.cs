using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories
{
    public interface ISolanaRepository<T> where T : IHolonBase
    {
        Task<string> CreateAsync(T entity);
        Task<string> UpdateAsync(T entity);
        Task<string> DeleteAsync(string hash);
        Task<T> GetAsync(string hash);
        
        string Create(T entity);
        string Update(T entity);
        string Delete(string hash);
        T Get(string hash);
    }
}