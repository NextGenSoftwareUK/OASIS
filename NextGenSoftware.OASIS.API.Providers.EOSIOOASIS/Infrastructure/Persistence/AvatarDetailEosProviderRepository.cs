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
    /// <summary>
    /// Repository contains CRUD methods for AvatarDetail entity.
    /// </summary>
    public class AvatarDetailEosProviderRepository : IEosProviderRepository<AvatarDetailDto>
    {
        private readonly IEosClient _eosClient;
        private readonly string _eosOasisAccountCode;

        public AvatarDetailEosProviderRepository(IEosClient eosClient, string eosOasisAccountCode)
        {
            _eosClient = eosClient ?? throw new ArgumentNullException(nameof(eosClient));
            _eosOasisAccountCode = eosOasisAccountCode;
        }
        
        public async Task Create(AvatarDetailDto entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "adddetail",
                Args = entity,
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
            
            var avatarDetailId = HashUtility.GetNumericHash(id).ToString();
            var abiJsonToBinResponseDto = await _eosClient.AbiBinToJson(new AbiBinToJsonRequestDto()
            {
                Action = "getdetail",
                BinArgs = avatarDetailId,
                Code = _eosOasisAccountCode
            });
            return JsonConvert.DeserializeObject<AvatarDetailDto>(abiJsonToBinResponseDto);
        }

        public async Task<ImmutableArray<AvatarDetailDto>> ReadAll()
        {
            var abiJsonToBinResponseDto = await _eosClient.AbiBinToJson(new AbiBinToJsonRequestDto()
            {
                Action = "getdetails",
                Code = _eosOasisAccountCode
            });
            return JsonConvert.DeserializeObject<ImmutableArray<AvatarDetailDto>>(abiJsonToBinResponseDto);
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