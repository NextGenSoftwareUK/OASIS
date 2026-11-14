using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    private async Task<OASISResult<IWeb3NFT>> ReadOrdinalInscriptionAsync(string identifier)
    {
        var result = new OASISResult<IWeb3NFT>();
        try
        {
            if (identifier == null || identifier.Length < 66 || identifier[64] != 'i' ||
                !uint256.TryParse(identifier[..64], out var reveal) ||
                !uint.TryParse(identifier.AsSpan(65), NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                throw new ArgumentException("A Bitcoin Ordinals inscription ID is required.", nameof(identifier));

            var inscriptionId = reveal + "i" + index.ToString(CultureInfo.InvariantCulture);
            var endpoint = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_API");
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var ord) ||
                (ord.Scheme != Uri.UriSchemeHttps && !(ord.Scheme == Uri.UriSchemeHttp && ord.IsLoopback)) ||
                ord.UserInfo.Length != 0 || ord.Query.Length != 0 || ord.Fragment.Length != 0)
                throw new InvalidOperationException("Configure OASIS_BITCOIN_ORD_API with an HTTPS or loopback ord server root.");

            using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                BaseAddress = new Uri(ord.AbsoluteUri.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            var genesis = await client.GetFromJsonAsync<string>("r/blockhash/0");
            if (!uint256.TryParse(genesis, out var genesisHash) || genesisHash != SigningNetwork.GetGenesis().GetHash())
                throw new InvalidOperationException("The ord server is indexing a different Bitcoin network.");

            using var response = await client.GetAsync("inscription/" + inscriptionId);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                OASISErrorHandling.HandleError(ref result, "The configured ord server has no matching Bitcoin inscription.");
                return result;
            }
            response.EnsureSuccessStatusCode();

            using var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var inscription = data.RootElement;
            if (inscription.GetProperty("id").GetString() != inscriptionId)
                throw new InvalidOperationException("The ord server returned a different inscription ID.");

            var metadata = new Dictionary<string, string>
            {
                ["InscriptionNumber"] = inscription.GetProperty("number").GetInt64().ToString(CultureInfo.InvariantCulture),
                ["InscriptionHeight"] = inscription.GetProperty("height").GetInt64().ToString(CultureInfo.InvariantCulture),
                ["Satpoint"] = inscription.GetProperty("satpoint").GetString(),
                ["ContentUrl"] = new Uri(client.BaseAddress, "content/" + inscriptionId).AbsoluteUri
            };
            if (inscription.GetProperty("address").GetString() is string owner)
                metadata["CurrentOwnerAddress"] = BitcoinAddress.Create(owner, SigningNetwork).ToString();
            if (inscription.GetProperty("content_type").GetString() is string contentType)
                metadata["ContentType"] = contentType;

            result.Result = new Web3NFT
            {
                TokenId = inscriptionId,
                NFTTokenAddress = inscriptionId,
                MintTransactionHash = reveal.ToString(),
                MintedOn = DateTimeOffset.FromUnixTimeSeconds(inscription.GetProperty("timestamp").GetInt64()).UtcDateTime,
                OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.BitcoinOASIS),
                StoreNFTMetaDataOnChain = true,
                MetaData = metadata
            };
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex is ArgumentException or InvalidOperationException
                ? ex.Message : $"Bitcoin inscription lookup failed ({ex.GetType().Name}).");
        }

        return result;
    }
}
