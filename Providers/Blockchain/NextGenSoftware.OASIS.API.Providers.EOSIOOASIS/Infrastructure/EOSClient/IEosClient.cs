using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.AbiBinToJson;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.AbiJsonToBin;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetBlock;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetInfo;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRawAbi;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRequiredKeys;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetTableRows;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.Transaction;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient
{
    public interface IEosClient : IDisposable
    {
        public Task<GetNodeInfoResponseDto> GetNodeInfo();
        public Task<GetTableRowsResponseDto<T>> GetTableRows<T>(GetTableRowsRequestDto getTableRowsRequest);
        public Task<string[]> GetCurrencyBalance(GetCurrencyBalanceRequestDto getCurrencyBalanceRequestDto);
        public Task<GetAccountResponseDto> GetAccount(GetAccountDtoRequest getAccountDtoRequest);
        public Task<AbiJsonToBinResponseDto> AbiJsonToBin(AbiJsonToBinRequestDto abiJsonToBinRequestDto);
        public Task<string> AbiBinToJson(AbiBinToJsonRequestDto abiJsonToBinRequestDto);
        public Task<string> SendTransaction(PerformTransactionRequestDto performTransactionRequestDto);
        public Task<string> PushTransaction(PerformTransactionRequestDto performTransactionRequestDto);
        public Task<GetRawAbiResponseDto> GetRawAbi(GetRawAbiRequestDto getRawAbiRequestDto);
        public Task<GetBlockResponseDto> GetBlock(GetBlockRequestDto getBlockRequestDto);
        public Task<GetBlockHeaderStateResponseDto> GetBlockHeaderState(GetBlockRequestDto getBlockRequestDto);
        public Task<string> GetRequiredKeys(GetRequiredKeysRequestDto getRequiredKeysRequestDto);
        public Task<GetAccountResponseDto> GetAccountAsync(GetAccountDtoRequest getAccountDtoRequest);
        public Task<string[]> GetCurrencyBalanceAsync(GetCurrencyBalanceRequestDto getCurrencyBalanceRequestDto);
        public Task<object> GetHolonByProviderKeyAsync(string providerKey);
        public Task<List<object>> GetHolonsForParentAsync(Guid parentId);
        public Task<List<object>> GetHolonsForParentByProviderKeyAsync(string providerKey);
        public Task<bool> DeleteHolonByProviderKeyAsync(string providerKey);
        public Task<object> ExportAllDataForAvatarByIdAsync(Guid avatarId);
    }
}