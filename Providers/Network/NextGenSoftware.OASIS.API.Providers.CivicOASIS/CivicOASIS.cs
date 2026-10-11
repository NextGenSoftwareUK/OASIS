using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.Util;
using Nethereum.Web3;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.CivicOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.CivicOASIS
{
    [Function("verifyToken", "bool")]
    internal sealed class VerifyTokenFunction : FunctionMessage
    {
        [Parameter("address", "owner", 1)] public string Owner { get; set; }
        [Parameter("uint256", "network", 2)] public BigInteger Network { get; set; }
    }

    [Function("getTokenIdsByOwnerAndNetwork", "uint256[]")]
    internal sealed class GetTokenIdsByOwnerAndNetworkFunction : FunctionMessage
    {
        [Parameter("address", "owner", 1)] public string Owner { get; set; }
        [Parameter("uint256", "network", 2)] public BigInteger Network { get; set; }
        [Parameter("bool", "onlyActive", 3)] public bool OnlyActive { get; set; }
    }

    [Function("getToken", typeof(GetTokenOutput))]
    internal sealed class GetTokenFunction : FunctionMessage
    {
        [Parameter("uint256", "tokenId", 1)] public BigInteger TokenId { get; set; }
    }

    [FunctionOutput]
    internal sealed class GetTokenOutput : IFunctionOutputDTO
    {
        [Parameter("address", "owner", 1)] public string Owner { get; set; }
        [Parameter("uint8", "state", 2)] public byte State { get; set; }
        [Parameter("string", "identity", 3)] public string Identity { get; set; }
        [Parameter("uint256", "expiration", 4)] public BigInteger Expiration { get; set; }
        [Parameter("uint256", "bitmask", 5)] public BigInteger Bitmask { get; set; }
    }

    /// <summary>A Gateway Protocol deployment (GatewayToken proxy) on an EVM chain.</summary>
    public sealed record CivicDeployment(string Chain, string RpcUrl, string GatewayTokenContract);

    /// <summary>Gateway token state as defined by the GatewayToken contract (TokenState enum).</summary>
    public enum CivicPassState { Active = 0, Frozen = 1, Revoked = 2 }

    public sealed class CivicPass
    {
        public string Chain { get; init; }
        public BigInteger TokenId { get; init; }
        public string Owner { get; init; }
        public CivicPassState State { get; init; }
        /// <summary>Expiry, or null when the pass never expires (expiration 0).</summary>
        public DateTimeOffset? Expiration { get; init; }
        public BigInteger Bitmask { get; init; }
    }

    /// <summary>
    /// Civic Pass provider. Reads Civic Pass gateway tokens on-chain from the Identity.com Gateway Protocol GatewayToken
    /// contract (verifyToken / getTokenIdsByOwnerAndNetwork / getToken). Civic issues passes per gatekeeper network
    /// (e.g. 4 = CAPTCHA, 10 = uniqueness), so callers pass the network id. Civic Pass is a verification registry, not a
    /// data store, so this is an <see cref="OASISProvider"/>.
    /// </summary>
    public class CivicOASIS : OASISProvider
    {
        /// <summary>Well-known Civic gatekeeper network ids on the EVM GatewayToken contract.</summary>
        public const int CaptchaNetwork = 4;
        public const int UniquenessNetwork = 10;

        /// <summary>Official GatewayToken proxy (same address on Ethereum and Polygon).</summary>
        public static readonly IReadOnlyList<CivicDeployment> MainnetDeployments = new[]
        {
            new CivicDeployment("polygon", "https://polygon-bor-rpc.publicnode.com", "0xF65b6396dF6B7e2D8a6270E3AB6c7BB08BAEF22E"),
            new CivicDeployment("ethereum", "https://ethereum-rpc.publicnode.com", "0xF65b6396dF6B7e2D8a6270E3AB6c7BB08BAEF22E")
        };

        private readonly IReadOnlyList<(CivicDeployment Deployment, IWeb3 Web3)> _chains;

        public CivicOASIS(IEnumerable<CivicDeployment> deployments = null)
            : this((deployments ?? MainnetDeployments).Select(d => (d, (IWeb3)new Web3(d.RpcUrl))).ToList()) { }

        internal CivicOASIS(IReadOnlyList<(CivicDeployment, IWeb3)> chains)
        {
            if (chains == null || chains.Count == 0) throw new ArgumentException("At least one Civic GatewayToken deployment is required.");
            _chains = chains;
            ProviderName = "CivicOASIS";
            ProviderDescription = "Civic Pass provider (on-chain Gateway Protocol GatewayToken).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CivicOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            foreach (var (deployment, web3) in _chains)
            {
                try
                {
                    var code = await web3.Eth.GetCode.SendRequestAsync(deployment.GatewayTokenContract);
                    if (string.IsNullOrEmpty(code) || code == "0x")
                    {
                        OASISErrorHandling.HandleError(ref result, $"CivicOASIS: no contract at {deployment.GatewayTokenContract} on {deployment.Chain}.");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"CivicOASIS: cannot reach {deployment.Chain} RPC: {ex.Message}", ex);
                    return result;
                }
            }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "CivicOASIS activated.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "CivicOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        /// <summary>True when the address holds a valid (active, unexpired) pass for the network on any configured chain.</summary>
        public async Task<OASISResult<bool>> VerifyPassAsync(string address, int gatekeeperNetwork)
        {
            var result = new OASISResult<bool>();
            if (!ValidateAddress(address, ref result)) return result;

            foreach (var (deployment, web3) in _chains)
            {
                try
                {
                    var valid = await web3.Eth.GetContractQueryHandler<VerifyTokenFunction>()
                        .QueryAsync<bool>(deployment.GatewayTokenContract, new VerifyTokenFunction { Owner = address, Network = gatekeeperNetwork });
                    if (valid) { result.Result = true; return result; }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, CallError(deployment, ex), ex);
                    return result;
                }
            }
            result.Result = false;
            return result;
        }

        /// <summary>All passes (including frozen/revoked/expired) the address holds for the network across configured chains.</summary>
        public async Task<OASISResult<IReadOnlyList<CivicPass>>> GetPassesAsync(string address, int gatekeeperNetwork)
        {
            var result = new OASISResult<IReadOnlyList<CivicPass>>();
            if (!ValidateAddress(address, ref result)) return result;

            var passes = new List<CivicPass>();
            foreach (var (deployment, web3) in _chains)
            {
                try
                {
                    var ids = await web3.Eth.GetContractQueryHandler<GetTokenIdsByOwnerAndNetworkFunction>()
                        .QueryAsync<List<BigInteger>>(deployment.GatewayTokenContract,
                            new GetTokenIdsByOwnerAndNetworkFunction { Owner = address, Network = gatekeeperNetwork, OnlyActive = false });
                    foreach (var id in ids)
                    {
                        var token = await web3.Eth.GetContractQueryHandler<GetTokenFunction>()
                            .QueryDeserializingToObjectAsync<GetTokenOutput>(new GetTokenFunction { TokenId = id }, deployment.GatewayTokenContract);
                        passes.Add(new CivicPass
                        {
                            Chain = deployment.Chain,
                            TokenId = id,
                            Owner = token.Owner,
                            State = (CivicPassState)token.State,
                            Expiration = token.Expiration.IsZero ? null : DateTimeOffset.FromUnixTimeSeconds((long)token.Expiration),
                            Bitmask = token.Bitmask
                        });
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, CallError(deployment, ex), ex);
                    return result;
                }
            }
            result.Result = passes;
            return result;
        }

        private static bool ValidateAddress<T>(string address, ref OASISResult<T> result)
        {
            if (AddressUtil.Current.IsValidEthereumAddressHexFormat(address)) return true;
            OASISErrorHandling.HandleError(ref result, $"CivicOASIS: '{address}' is not a valid EVM address.");
            return false;
        }

        private static string CallError(CivicDeployment deployment, Exception ex) =>
            $"CivicOASIS: {deployment.Chain} call failed: {(ex is RpcResponseException rpc ? rpc.RpcError?.Message ?? rpc.Message : ex.Message)}";
    }
}
