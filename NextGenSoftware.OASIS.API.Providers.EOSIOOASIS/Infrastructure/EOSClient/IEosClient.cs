using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient
{
    public interface IEosClient
    {
        public Task<GetNodeInfoResponseDto> GetNodeInfo();
        public Task<GetTableRowsResponseDto> GetTableRows(GetTableRowsRequestDto getTableRowsRequest);
        public Task<AbiJsonToBinResponseDto> AbiJsonToBin(AbiJsonToBinRequestDto abiJsonToBinRequestDto);
        public Task<string> AbiBinToJson(AbiBinToJsonRequestDto abiJsonToBinRequestDto);
    }
}