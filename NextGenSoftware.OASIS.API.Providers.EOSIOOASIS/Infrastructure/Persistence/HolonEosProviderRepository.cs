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
    public class HolonEosProviderRepository : IEosProviderRepository<HolonDto>
    {
        private readonly IEosClient _eosClient;
        private readonly string _eosOasisAccountCode;
        private static string _holonTable = "holon";

        public HolonEosProviderRepository(IEosClient eosClient, string eosOasisAccountCode)
        {
            _eosClient = eosClient ?? throw new ArgumentNullException(nameof(eosClient));
            _eosOasisAccountCode = eosOasisAccountCode;
        }

        // TODO: Implement Send/Push transaction within AbiJsonToBin
        public async Task Create(HolonDto entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "addholon",
                Args = new
                {
                    entityId = entity.EntityId,
                    holonId = entity.HolonId,
                    info = entity.Info
                },
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Holon creating request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
        }

        // TODO: Implement Send/Push transaction within AbiJsonToBin
        public async Task Update(HolonDto entity, Guid id)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var holonEntityId = HashUtility.GetNumericHash(id);
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "setholon",
                Args = new
                {
                    entityId = holonEntityId,
                    info = entity.Info
                },
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
            
            var holonId = HashUtility.GetNumericHash(id);
            var holonTableRows = await _eosClient.GetTableRows<HolonDto>(new GetTableRowsRequestDto()
            {
                Code = _eosOasisAccountCode,
                Scope = _eosOasisAccountCode,
                Table = _holonTable
            });
            return holonTableRows.Rows.FirstOrDefault(avatarDetailDto => avatarDetailDto.EntityId == holonId);         
        }

        public async Task<ImmutableArray<HolonDto>> ReadAll()
        {
            var holonTableRows = await _eosClient.GetTableRows<HolonDto>(new GetTableRowsRequestDto()
            {
                Code = _eosOasisAccountCode,
                Scope = _eosOasisAccountCode,
                Table = _holonTable
            });
            return holonTableRows.Rows.ToImmutableArray();
        }

        // TODO: Implement Send/Push transaction within AbiJsonToBin
        public async Task DeleteSoft(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var holonEntityId = HashUtility.GetNumericHash(id);
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "softholon",
                Args = new
                {
                    entityId = holonEntityId
                },
                Code = _eosOasisAccountCode
            });
            
            LoggingManager.Log(
                "Holon soft-deleting request was sent. " +
                $"Received BinArgs response: {abiJsonToBinResponseDto.BinArgs}. " +
                $"Request sent: {id}", LogType.Info);
        }

        // TODO: Implement Send/Push transaction within AbiJsonToBin
        public async Task DeleteHard(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            
            var holonEntityId = HashUtility.GetNumericHash(id);
            var abiJsonToBinResponseDto = await _eosClient.AbiJsonToBin(new AbiJsonToBinRequestDto()
            {
                Action = "hardholon",
                Args = new
                {
                    entityId = holonEntityId
                },
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