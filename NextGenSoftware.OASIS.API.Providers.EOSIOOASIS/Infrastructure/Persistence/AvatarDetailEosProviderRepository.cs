using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence
{
    /// <summary>
    /// Repository contains CRUD methods for AvatarDetail entity.
    /// </summary>
    public class AvatarDetailEosProviderRepository : IEosProviderRepository<AvatarDetailDto>
    {
        private readonly IEosClient _eosClient;
        private readonly string _eosOasisAccountCode;
        private static string _avatarDetailTable = "avatardetail";

        public AvatarDetailEosProviderRepository(IEosClient eosClient, string eosOasisAccountCode)
        {
            _eosClient = eosClient ?? throw new ArgumentNullException(nameof(eosClient));
            _eosOasisAccountCode = eosOasisAccountCode;
        }
        
        // TODO: Implement Send/Push transaction within AbiJsonToBin
        public async Task Create(AvatarDetailDto entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "adddetail",
                Args = new
                {
                    entityId = entity.EntityId,
                    avatarId = entity.AvatarId,
                    info = entity.Info
                },
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Avatar detail request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
        }

        public async Task<AvatarDetailDto> Read(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var avatarDetailId = HashUtility.GetNumericHash(id);
            var avatarDetailTableRows = await _eosClient.GetTableRows<AvatarDetailDto>(new GetTableRowsRequestDto()
            {
                Code = _eosOasisAccountCode,
                Scope = _eosOasisAccountCode,
                Table = _avatarDetailTable
            });
            return avatarDetailTableRows.Rows.FirstOrDefault(avatarDetailDto => avatarDetailDto.EntityId == avatarDetailId);
        }

        public async Task<ImmutableArray<AvatarDetailDto>> ReadAll()
        {
            var avatarDetailTableRows = await _eosClient.GetTableRows<AvatarDetailDto>(new GetTableRowsRequestDto()
            {
                Code = _eosOasisAccountCode,
                Scope = _eosOasisAccountCode,
                Table = _avatarDetailTable
            });
            return avatarDetailTableRows.Rows.ToImmutableArray();
        }
        
        // Update method not supported for avatar detail entity
        public async Task Update(AvatarDetailDto entity, Guid id)
        {
            throw new NotImplementedException();
        }

        // Soft delete method not supported by avatar detail entity
        public async Task DeleteSoft(Guid id)
        {
            throw new NotImplementedException();
        }

        // Hard delete method not supported by avatar detail entity
        public async Task DeleteHard(Guid id)
        {
            throw new NotImplementedException();
        }

        // Release http connections, to avoid socket descriptor leak.
        private void ReleaseUnmanagedResources()
        {
            _eosClient.Dispose();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~AvatarDetailEosProviderRepository()
        {
            ReleaseUnmanagedResources();
        }
    }
}