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
    public class AvatarEosProviderRepository : IEosProviderRepository<AvatarDto>
    {
        private readonly IEosClient _eosClient;
        private readonly string _eosOasisAccountCode;
        private static string _avatarTable = "avatar";

        public AvatarEosProviderRepository(IEosClient eosClient, string eosOasisAccountCode)
        {
            _eosClient = eosClient ?? throw new ArgumentNullException(nameof(eosClient));
            _eosOasisAccountCode = eosOasisAccountCode;
        }

        // TODO: Implement Send/Push transaction within AbiJsonToBin
        public async Task Create(AvatarDto entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "addavatar",
                Args = new
                {
                    entityId = entity.EntityId,
                    avatarId = entity.AvatarId,
                    info = entity.Info
                },
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Avatar creating request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
        }

        // TODO: Implement Send/Push transaction within AbiJsonToBin
        public async Task Update(AvatarDto entity, Guid id)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            var avatarId = HashUtility.GetNumericHash(id);
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "setavatar",
                Args = new
                {
                    entityId = avatarId,
                    info = entity.Info
                },
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Avatar updating request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
        }

        public async Task<AvatarDto> Read(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var avatarId = HashUtility.GetNumericHash(id);
            var avatarTableRows = await _eosClient.GetTableRows<AvatarDto>(new GetTableRowsRequestDto()
            {
                Code = _eosOasisAccountCode,
                Scope = _eosOasisAccountCode,
                Table = _avatarTable
            });
            return avatarTableRows.Rows.FirstOrDefault(avatarDetailDto => avatarDetailDto.EntityId == avatarId);     
        }

        public async Task<ImmutableArray<AvatarDto>> ReadAll()
        {
            var avatarTableRows = await _eosClient.GetTableRows<AvatarDto>(new GetTableRowsRequestDto()
            {
                Code = _eosOasisAccountCode,
                Scope = _eosOasisAccountCode,
                Table = _avatarTable
            });
            return avatarTableRows.Rows.ToImmutableArray();
        }

        // TODO: Implement Send/Push transaction within AbiJsonToBin
        public async Task DeleteSoft(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var avatarId = HashUtility.GetNumericHash(id);
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "softavatar",
                Args = new
                {
                    entityId = avatarId
                },
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Avatar soft-deleting request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {id}", LogType.Info);
        }

        // TODO: Implement Send/Push transaction within AbiJsonToBin
        public async Task DeleteHard(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var avatarId = HashUtility.GetNumericHash(id);
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "hardavatar",
                Args = new
                {
                    entityId = avatarId
                },
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Avatar hard-deleting request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {id}", LogType.Info);
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

        ~AvatarEosProviderRepository()
        {
            ReleaseUnmanagedResources();
        }
    }
}