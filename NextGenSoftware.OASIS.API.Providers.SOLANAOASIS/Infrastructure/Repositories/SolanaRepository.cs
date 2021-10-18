using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Solnet.Rpc;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories
{
    public class SolanaRepository<T> : ISolanaRepository<T>
    {
        private IRpcClient _rpcClient;

        public SolanaRepository()
        {
            _rpcClient = ClientFactory.GetClient(Cluster.MainNet);
        }

        public async Task<string> CreateAsync(T entity)
        {
        }

        public async Task<string> UpdateAsync(T avatar, string hash)
        {
        }

        public async Task<string> DeleteAsync(string hash)
        {
        }

        public async Task<IAvatar> GetAsync(string hash)
        {
        }

        public async Task<string> Create(T entity)
        {
        }

        public async Task<string> Update(T avatar, string hash)
        {
        }

        public async Task<string> Delete(string hash)
        {
        }

        public async Task<IAvatar> Get(string hash)
        {
        }
    }
}