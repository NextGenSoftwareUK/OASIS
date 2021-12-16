using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories
{
    public interface ISolanaRepository
    {
        Task<OASISResult<T>> CreateAsync<T>(T entity) where T : IHolonBase, new();
        Task<OASISResult<T>> UpdateAsync<T>(T entity) where T : IHolonBase, new();
        Task<OASISResult<bool>> DeleteAsync<T>(string hash) where T : IHolonBase, new();
        Task<OASISResult<T>> GetAsync<T>(string hash) where T : IHolonBase, new();

        OASISResult<T> Create<T>(T entity) where T : IHolonBase, new();
        OASISResult<T> Update<T>(T entity) where T : IHolonBase, new();
        OASISResult<bool> Delete<T>(string hash) where T : IHolonBase, new();
        OASISResult<T> Get<T>(string hash) where T : IHolonBase, new();
    }
}