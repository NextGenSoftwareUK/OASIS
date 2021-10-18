using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories
{
    public interface ISolanaRepository
    {
        Task<string> CreateAsync<T>(T entity) where T : IHolonBase, new();
        Task<string> UpdateAsync<T>(T entity) where T : IHolonBase, new();
        Task<string> DeleteAsync<T>(string hash) where T : IHolonBase, new();
        Task<T> GetAsync<T>(string hash) where T : IHolonBase, new();
        
        string Create<T>(T entity) where T : IHolonBase, new();
        string Update<T>(T entity) where T : IHolonBase, new();
        string Delete<T>(string hash) where T : IHolonBase, new();
        T Get<T>(string hash) where T : IHolonBase, new();
    }
}