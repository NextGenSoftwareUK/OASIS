using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence
{
    public class HolonEosProviderRepository : IEosProviderRepository<HolonDto>
    {
        private readonly IEosClient _eosClient;
        private readonly string _eosOasisAccountCode;

        public HolonEosProviderRepository(IEosClient eosClient, string eosOasisAccountCode)
        {
            _eosClient = eosClient ?? throw new ArgumentNullException(nameof(eosClient));
            _eosOasisAccountCode = eosOasisAccountCode;
        }

        public async Task Create(HolonDto entity)
        {
            throw new NotImplementedException();
        }

        public async Task Update(HolonDto entity, Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<HolonDto> Read(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<ImmutableArray<HolonDto>> ReadAll()
        {
            throw new NotImplementedException();
        }

        public async Task DeleteSoft(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteHard(Guid id)
        {
            throw new NotImplementedException();
        }

        private void ReleaseUnmanagedResources()
        {
            _eosClient.Dispose();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~HolonEosProviderRepository()
        {
            ReleaseUnmanagedResources();
        }
    }
}