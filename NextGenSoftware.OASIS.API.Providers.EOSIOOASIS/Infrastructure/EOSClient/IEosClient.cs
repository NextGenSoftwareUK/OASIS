using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient
{
    public interface IEosClient : IDisposable
    {
        public Task<GetNodeInfoResponseDto> GetNodeInfo();
        public Task<GetTableRowsResponseDto<T>> GetTableRows<T>(GetTableRowsRequestDto getTableRowsRequest);
        public Task<AbiJsonToBinResponseDto> AbiJsonToBin(AbiJsonToBinRequestDto abiJsonToBinRequestDto);
        public Task<string> AbiBinToJson(AbiBinToJsonRequestDto abiJsonToBinRequestDto);
        public Task<string> SendTransaction(PerformTransactionRequestDto performTransactionRequestDto);
        public Task<string> PushTransaction(PerformTransactionRequestDto performTransactionRequestDto);
    }
}