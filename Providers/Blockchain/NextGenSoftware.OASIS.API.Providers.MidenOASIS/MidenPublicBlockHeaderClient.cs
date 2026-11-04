using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.PublicRpc;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS;

internal sealed class MidenPublicBlockHeaderClient : IDisposable
{
    private const string TestnetGenesisCommitment = "280189B64C3A22C9D8E6E6C2CD567F58C528FEE3B73724A6D855066642D7E567";
    private readonly GrpcChannel _channel;
    private readonly NodeService.NodeServiceClient _client;

    public MidenPublicBlockHeaderClient()
    {
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        handler.UseProxy = false;
        _channel = GrpcChannel.ForAddress("https://rpc.testnet.miden.io", new GrpcChannelOptions
        {
            HttpHandler = handler,
            DisposeHttpClient = true,
            MaxReceiveMessageSize = 64 * 1024
        });
        _client = new NodeService.NodeServiceClient(_channel);
    }

    public async Task<BlockHeader> ReadAsync(uint height, CancellationToken cancellationToken)
    {
        using var call = _client.GetBlockHeaderByNumberAsync(
            new GetBlockHeaderByNumberRequest { BlockNum = height },
            deadline: DateTime.UtcNow.AddSeconds(12),
            cancellationToken: cancellationToken);
        var response = await call.ResponseAsync.ConfigureAwait(false);
        return ValidateHeader(height, response.BlockHeader);
    }

    internal static BlockHeader ValidateHeader(uint height, BlockHeader header)
    {
        if (header?.BlockNum?.BlockNum != height || header.Version != BlockVersion.V1 ||
            header.Timestamp == 0 || header.PrevBlockCommitment?.Encoded.Length != 32)
        {
            throw new InvalidDataException($"Miden returned an invalid block header for height {height}.");
        }

        if (height == 1 && Convert.ToHexString(header.PrevBlockCommitment.Encoded.Span) != TestnetGenesisCommitment)
            throw new InvalidDataException("Miden block 1 does not reference the expected testnet genesis.");

        return header;
    }

    public void Dispose() => _channel.Dispose();
}
