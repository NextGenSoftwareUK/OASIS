using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.ProofOfHumanityOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.ProofOfHumanityOASIS
{
    [Function("isHuman", "bool")]
    internal sealed class IsHumanFunction : FunctionMessage
    {
        [Parameter("address", "account", 1)] public string Account { get; set; }
    }

    [Function("humanityOf", "bytes20")]
    internal sealed class HumanityOfFunction : FunctionMessage
    {
        [Parameter("address", "account", 1)] public string Account { get; set; }
    }

    /// <summary>A Proof of Humanity v2 deployment to query (CrossChainProofOfHumanity proxy on a chain).</summary>
    public sealed record PohDeployment(string Chain, string RpcUrl, string CrossChainContract);

    public sealed class PohStatus
    {
        public string Address { get; init; }
        public bool IsHuman { get; init; }
        /// <summary>The humanity id (bytes20, 0x-hex) when registered.</summary>
        public string HumanityId { get; init; }
        /// <summary>Chain whose registry currently holds the humanity (a human is home on at most one chain).</summary>
        public string HomeChain { get; init; }
    }

    /// <summary>
    /// Proof of Humanity v2 provider. Reads registration on-chain from the CrossChainProofOfHumanity contracts, which the
    /// protocol designates for integrations (isHuman / humanityOf), on every configured chain because a human is
    /// registered on a single home chain. Proof of Humanity is a sybil-resistance registry, not a data store, so this is
    /// an <see cref="OASISProvider"/>.
    /// </summary>
    public class ProofOfHumanityOASIS : OASISProvider
    {
        /// <summary>Official v2 deployments (github.com/Proof-Of-Humanity/proof-of-humanity-v2-contracts).</summary>
        public static readonly IReadOnlyList<PohDeployment> MainnetDeployments = new[]
        {
            new PohDeployment("gnosis", "https://rpc.gnosischain.com", "0x16044E1063C08670f8653055A786b7CC2034d2b0"),
            new PohDeployment("ethereum", "https://ethereum-rpc.publicnode.com", "0xa478095886659168E8812154fB0DE39F103E74b2")
        };

        private readonly IReadOnlyList<(PohDeployment Deployment, IWeb3 Web3)> _chains;

        public ProofOfHumanityOASIS(IEnumerable<PohDeployment> deployments = null)
            : this((deployments ?? MainnetDeployments).Select(d => (d, (IWeb3)new Web3(d.RpcUrl))).ToList()) { }

        internal ProofOfHumanityOASIS(IReadOnlyList<(PohDeployment, IWeb3)> chains)
        {
            if (chains == null || chains.Count == 0) throw new ArgumentException("At least one Proof of Humanity deployment is required.");
            _chains = chains;
            ProviderName = "ProofOfHumanityOASIS";
            ProviderDescription = "Proof of Humanity v2 provider (on-chain CrossChainProofOfHumanity registry).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ProofOfHumanityOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            foreach (var (deployment, web3) in _chains)
            {
                try
                {
                    var code = await web3.Eth.GetCode.SendRequestAsync(deployment.CrossChainContract);
                    if (string.IsNullOrEmpty(code) || code == "0x")
                    {
                        OASISErrorHandling.HandleError(ref result, $"ProofOfHumanityOASIS: no contract at {deployment.CrossChainContract} on {deployment.Chain}.");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"ProofOfHumanityOASIS: cannot reach {deployment.Chain} RPC: {ex.Message}", ex);
                    return result;
                }
            }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "ProofOfHumanityOASIS activated.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "ProofOfHumanityOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        /// <summary>True when the address is a registered human on any configured chain.</summary>
        public async Task<OASISResult<bool>> IsHumanAsync(string address)
        {
            var status = await GetStatusAsync(address);
            return status.IsError ? new OASISResult<bool> { IsError = true, Message = status.Message, Exception = status.Exception } : new OASISResult<bool>(status.Result.IsHuman);
        }

        public async Task<OASISResult<PohStatus>> GetStatusAsync(string address)
        {
            var result = new OASISResult<PohStatus>();
            if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(address))
            {
                OASISErrorHandling.HandleError(ref result, $"ProofOfHumanityOASIS: '{address}' is not a valid EVM address.");
                return result;
            }

            foreach (var (deployment, web3) in _chains)
            {
                try
                {
                    var isHuman = await web3.Eth.GetContractQueryHandler<IsHumanFunction>()
                        .QueryAsync<bool>(deployment.CrossChainContract, new IsHumanFunction { Account = address });
                    if (!isHuman) continue;

                    var humanity = await web3.Eth.GetContractQueryHandler<HumanityOfFunction>()
                        .QueryAsync<byte[]>(deployment.CrossChainContract, new HumanityOfFunction { Account = address });
                    result.Result = new PohStatus { Address = address, IsHuman = true, HumanityId = "0x" + Convert.ToHexString(humanity).ToLowerInvariant(), HomeChain = deployment.Chain };
                    return result;
                }
                catch (RpcResponseException ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"ProofOfHumanityOASIS: {deployment.Chain} call failed: {ex.RpcError?.Message ?? ex.Message}", ex);
                    return result;
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"ProofOfHumanityOASIS: {deployment.Chain} call failed: {ex.Message}", ex);
                    return result;
                }
            }

            result.Result = new PohStatus { Address = address, IsHuman = false };
            return result;
        }
    }
}
