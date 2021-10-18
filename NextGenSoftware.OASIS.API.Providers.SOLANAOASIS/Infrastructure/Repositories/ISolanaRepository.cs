using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories
{
    public interface ISolanaRepository<T>
    {
        Task<string> CreateAsync(T entity);
        Task<string> UpdateAsync(T avatar, string hash);
        Task<string> DeleteAsync(string hash);
        Task<IAvatar> GetAsync(string hash);
        
        Task<string> Create(T entity);
        Task<string> Update(T avatar, string hash);
        Task<string> Delete(string hash);
        Task<IAvatar> Get(string hash);
    }
}