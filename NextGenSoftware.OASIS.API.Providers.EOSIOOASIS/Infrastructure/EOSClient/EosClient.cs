using System;
using System.Net.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient
{
    public class EosClient : IEosClient
    {
        private readonly Uri _eosHostNodeUri;

        public EosClient(Uri eosHostNodeUri)
        {
            _eosHostNodeUri = eosHostNodeUri;
        }
        
        public async Task<GetNodeInfoResponseDto> GetNodeInfo()
        {
            return await SendRequest<GetNodeInfoResponseDto, object>(null, HttpMethod.Get, new Uri(_eosHostNodeUri.Host + ""));
        }

        public async Task<GetTableRowsResponseDto> GetTableRows(GetTableRowsRequestDto getTableRowsRequest)
        {
            return await SendRequest<GetTableRowsResponseDto, GetTableRowsRequestDto>(getTableRowsRequest, HttpMethod.Post, new Uri(_eosHostNodeUri.Host + ""));
        }

        public async Task<AbiJsonToBinResponseDto> AbiJsonToBin(AbiJsonToBinRequestDto abiJsonToBinRequestDto)
        {
            return await SendRequest<AbiJsonToBinResponseDto, AbiJsonToBinRequestDto>(abiJsonToBinRequestDto, HttpMethod.Post, new Uri(_eosHostNodeUri.Host + ""));
        }

        public async Task<string> AbiBinToJson(AbiBinToJsonRequestDto abiJsonToBinRequestDto)
        {
            return await SendRequest<string, AbiBinToJsonRequestDto>(abiJsonToBinRequestDto, HttpMethod.Post, new Uri(_eosHostNodeUri.Host + ""));
        }

        public async Task<TResponse> SendRequest<TResponse, TRequest>(TRequest request, HttpMethod httpMethod, Uri uri)
        {
            throw new NotImplementedException();
        }
    }
}