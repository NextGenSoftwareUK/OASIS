using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
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
            throw new NotImplementedException();
        }

        public async Task<AvatarDto> Read(Guid id)
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

        public async Task<ImmutableArray<AvatarDto>> ReadAll()
        {
            var abiJsonToBinResponseDto = await _eosClient.AbiBinToJson(new AbiBinToJsonRequestDto()
            {
                Action = "getdetails",
                Code = _eosOasisAccountCode
            });
            return JsonConvert.DeserializeObject<ImmutableArray<AvatarDetailDto>>(abiJsonToBinResponseDto);
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

        ~AvatarEosProviderRepository()
        {
            ReleaseUnmanagedResources();
        }
    }
}