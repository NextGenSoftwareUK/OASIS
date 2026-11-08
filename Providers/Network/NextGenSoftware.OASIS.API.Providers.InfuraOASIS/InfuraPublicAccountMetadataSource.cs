using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.InfuraOASIS;

/// <summary>Finalized Ethereum balances for an explicitly configured public account set.</summary>
public sealed class InfuraPublicAccountMetadataSource : EvmPublicAccountMetadataSource
{
    public const string AddressMetadata = "InfuraEthereumAccountAddress";
    private static readonly Uri Origin = new("https://mainnet.infura.io/");
    private static readonly Network Mainnet = new(PType.InfuraOASIS, "InfuraEthereum", "0x1",
        "0xd4e56740f876aef8c010b86a40d5f56745a118d0906a34e69aec8c0db1cb8fa3", AddressMetadata);

    private InfuraPublicAccountMetadataSource(HttpClient client, IEnumerable<string> addresses, Guid owner)
        : base(client, Origin, Mainnet.ChainId, addresses, owner, true, Mainnet) { }

    public static InfuraPublicAccountMetadataSource Create(string apiKey, string apiSecret,
        IEnumerable<string> addresses, Guid owner)
    {
        if (apiKey is null || !Regex.IsMatch(apiKey, @"\A[a-zA-Z0-9]{1,128}\z") ||
            apiSecret is null || !Regex.IsMatch(apiSecret, @"\A[!-~]{1,256}\z"))
            throw new ArgumentException("An explicit Infura server key and secret are required.");

        var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        transport.AllowAutoRedirect = false;
        transport.UseProxy = false;
        transport.UseCookies = false;
        transport.ActivityHeadersPropagator = null;
        var client = new HttpClient(new CredentialHandler(apiKey, apiSecret, transport))
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        try
        {
            return new InfuraPublicAccountMetadataSource(client, addresses, owner);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private sealed class CredentialHandler(string apiKey, string apiSecret, HttpMessageHandler transport)
        : DelegatingHandler(transport)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Post || request.RequestUri != Origin ||
                request.Headers.Authorization is not null)
                throw new InvalidOperationException("Infura only accepts the configured Ethereum RPC request.");

            request.RequestUri = new Uri(Origin, "v3/" + apiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + apiSecret)));
            try
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException error)
            {
                throw new HttpRequestException("Infura RPC transport failed.", null, error.StatusCode);
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException("Infura RPC request was cancelled.", cancellationToken);
            }
            finally
            {
                request.RequestUri = Origin;
                request.Headers.Authorization = null;
            }
        }
    }
}
