using System;
using System.Collections.Immutable;
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

        public AvatarEosProviderRepository(IEosClient eosClient, string eosOasisAccountCode)
        {
            _eosClient = eosClient ?? throw new ArgumentNullException(nameof(eosClient));
            _eosOasisAccountCode = eosOasisAccountCode;
        }

        public async Task Create(AvatarDto entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "addavatar",
                Args = entity,
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Avatar creating request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
        }

        public async Task Update(AvatarDto entity, Guid id)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "setavatar",
                Args = entity,
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
            
            var avatarDetailId = HashUtility.GetNumericHash(id).ToString();
            var abiJsonToBinResponseDto = await _eosClient.AbiBinToJson(new AbiBinToJsonRequestDto()
            {
                Action = "getavatar",
                BinArgs = avatarDetailId,
                Code = _eosOasisAccountCode
            });
            return JsonConvert.DeserializeObject<AvatarDto>(abiJsonToBinResponseDto);        
        }

        public async Task<ImmutableArray<AvatarDto>> ReadAll()
        {
            var abiJsonToBinResponseDto = await _eosClient.AbiBinToJson(new AbiBinToJsonRequestDto()
            {
                Action = "getavatars",
                Code = _eosOasisAccountCode
            });
            return JsonConvert.DeserializeObject<ImmutableArray<AvatarDto>>(abiJsonToBinResponseDto);
        }

        public async Task DeleteSoft(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "softavatar",
                Args = id,
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Avatar soft-deleting request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {id}", LogType.Info);
        }

        public async Task DeleteHard(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "hardavatar",
                Args = id,
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