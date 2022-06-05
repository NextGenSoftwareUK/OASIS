using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories
{
    public interface ISolanaRepository
    {
        Task<T> CreateAsync<T>(T entity) where T : SolanaBaseDto, new();
        Task<T> UpdateAsync<T>(T entity) where T : SolanaBaseDto, new();
        Task<bool> DeleteAsync(string transactionHashReference);
        Task<T> GetAsync<T>(string transactionHashReference) where T : SolanaBaseDto, new();
    }
}