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
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "addholon",
                Args = entity,
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Holon creating request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
        }

        public async Task Update(HolonDto entity, Guid id)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "setholon",
                Args = entity,
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Holon updating request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
        }

        public async Task<HolonDto> Read(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var avatarDetailId = HashUtility.GetNumericHash(id).ToString();
            var abiJsonToBinResponseDto = await _eosClient.AbiBinToJson(new AbiBinToJsonRequestDto()
            {
                Action = "getholon",
                BinArgs = avatarDetailId,
                Code = _eosOasisAccountCode
            });
            return JsonConvert.DeserializeObject<HolonDto>(abiJsonToBinResponseDto);         
        }

        public async Task<ImmutableArray<HolonDto>> ReadAll()
        {
            var abiJsonToBinResponseDto = await _eosClient.AbiBinToJson(new AbiBinToJsonRequestDto()
            {
                Action = "getholons",
                Code = _eosOasisAccountCode
            });
            return JsonConvert.DeserializeObject<ImmutableArray<HolonDto>>(abiJsonToBinResponseDto);
        }

        public async Task DeleteSoft(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "softholon",
                Args = id,
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Holon soft-deleting request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {id}", LogType.Info);
        }

        public async Task DeleteHard(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "hardholon",
                Args = id,
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Holon hard-deleting request was sent. " +
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

        ~HolonEosProviderRepository()
        {
            ReleaseUnmanagedResources();
        }
    }
}