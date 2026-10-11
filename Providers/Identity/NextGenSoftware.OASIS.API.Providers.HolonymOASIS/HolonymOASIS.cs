using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.Util;
using Nethereum.Web3;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.HolonymOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.HolonymOASIS
{
    [Function("sbtOwners", typeof(SbtOwnersOutput))]
    internal sealed class SbtOwnersFunction : FunctionMessage
    {
        [Parameter("bytes32", "identifier", 1)] public byte[] Identifier { get; set; }
    }

    [FunctionOutput]
    internal sealed class SbtOwnersOutput : IFunctionOutputDTO
    {
        [Parameter("uint256", "expiry", 1)] public BigInteger Expiry { get; set; }
        [Parameter("bool", "revoked", 2)] public bool Revoked { get; set; }
    }

    public enum HolonymSbtState { None, Valid, Expired, Revoked }

    public sealed class HolonymSbt
    {
        public string Address { get; init; }
        public string CircuitId { get; init; }
        public HolonymSbtState State { get; init; }
        public DateTimeOffset? Expiry { get; init; }
        public bool IsValid => State == HolonymSbtState.Valid;
    }

    /// <summary>
    /// Human ID (Holonym) provider. Reads Human ID V3 soul-bound tokens on-chain from the Hub contract on Optimism
    /// (github.com/holonym-foundation/id-hub-contracts). An SBT is keyed by keccak256(abi.encodePacked(owner, circuitId));
    /// the public <c>sbtOwners</c> mapping is read rather than <c>getSBT</c> so that missing, expired and revoked SBTs are
    /// distinguished instead of surfacing as reverts. Human ID is a credential registry, not a data store, so this is an
    /// <see cref="OASISProvider"/>.
    /// </summary>
    public class HolonymOASIS : OASISProvider
    {
        public const string OptimismRpcUrl = "https://optimism-rpc.publicnode.com";
        public const string HubV3Optimism = "0x2AA822e264F8cc31A2b9C22f39e5551241e94DfB";

        /// <summary>Government ID (KYC) uniqueness circuit.</summary>
        public const string GovIdCircuitId = "0x729d660e1c02e4e419745e617d643f897a538673ccf1051e093bbfa58b0a120b";
        /// <summary>Phone number uniqueness circuit.</summary>
        public const string PhoneCircuitId = "0xbce052cf723dca06a21bd3cf838bc518931730fb3db7859fc9cc86f0d5483495";

        private readonly IWeb3 _web3;
        private readonly string _hub;
        private readonly Func<DateTimeOffset> _now;

        public HolonymOASIS(string rpcUrl = OptimismRpcUrl, string hubAddress = HubV3Optimism)
            : this(new Web3(rpcUrl), hubAddress, () => DateTimeOffset.UtcNow) { }

        internal HolonymOASIS(IWeb3 web3, string hubAddress, Func<DateTimeOffset> now)
        {
            _web3 = web3;
            _hub = hubAddress;
            _now = now;
            ProviderName = "HolonymOASIS";
            ProviderDescription = "Human ID (Holonym) provider (on-chain Hub V3 soul-bound tokens).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.HolonymOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var code = await _web3.Eth.GetCode.SendRequestAsync(_hub);
                if (string.IsNullOrEmpty(code) || code == "0x")
                {
                    OASISErrorHandling.HandleError(ref result, $"HolonymOASIS: no Hub contract at {_hub}.");
                    return result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"HolonymOASIS: cannot reach RPC: {ex.Message}", ex);
                return result;
            }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "HolonymOASIS activated.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "HolonymOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        /// <summary>True when the address holds a valid (unexpired, unrevoked) government-ID uniqueness SBT.</summary>
        public async Task<OASISResult<bool>> IsUniqueAsync(string address)
        {
            var sbt = await GetSbtAsync(address, GovIdCircuitId);
            return sbt.IsError ? new OASISResult<bool> { IsError = true, Message = sbt.Message, Exception = sbt.Exception } : new OASISResult<bool>(sbt.Result.IsValid);
        }

        /// <summary>The SBT the address holds for a circuit (e.g. <see cref="GovIdCircuitId"/>, <see cref="PhoneCircuitId"/>).</summary>
        public async Task<OASISResult<HolonymSbt>> GetSbtAsync(string address, string circuitId)
        {
            var result = new OASISResult<HolonymSbt>();
            if (!AddressUtil.Current.IsValidEthereumAddressHexFormat(address))
            {
                OASISErrorHandling.HandleError(ref result, $"HolonymOASIS: '{address}' is not a valid EVM address.");
                return result;
            }
            byte[] circuit;
            try { circuit = circuitId.HexToByteArray(); } catch (FormatException) { circuit = null; }
            if (circuit == null || circuit.Length != 32)
            {
                OASISErrorHandling.HandleError(ref result, $"HolonymOASIS: '{circuitId}' is not a 32-byte circuit id.");
                return result;
            }

            try
            {
                var identifier = Sha3Keccack.Current.CalculateHash(address.HexToByteArray().Concat32(circuit));
                var sbt = await _web3.Eth.GetContractQueryHandler<SbtOwnersFunction>()
                    .QueryDeserializingToObjectAsync<SbtOwnersOutput>(new SbtOwnersFunction { Identifier = identifier }, _hub);
                DateTimeOffset? expiry = sbt.Expiry.IsZero ? null : DateTimeOffset.FromUnixTimeSeconds((long)sbt.Expiry);
                var state = expiry == null ? HolonymSbtState.None
                    : sbt.Revoked ? HolonymSbtState.Revoked
                    : expiry < _now() ? HolonymSbtState.Expired
                    : HolonymSbtState.Valid;
                result.Result = new HolonymSbt { Address = address, CircuitId = circuitId, State = state, Expiry = expiry };
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"HolonymOASIS: Hub call failed: {(ex is RpcResponseException rpc ? rpc.RpcError?.Message ?? rpc.Message : ex.Message)}", ex);
            }
            return result;
        }
    }

    internal static class ByteExtensions
    {
        /// <summary>abi.encodePacked(address, bytes32): 20 address bytes followed by the 32 circuit bytes.</summary>
        public static byte[] Concat32(this byte[] address, byte[] circuit)
        {
            var packed = new byte[address.Length + circuit.Length];
            Buffer.BlockCopy(address, 0, packed, 0, address.Length);
            Buffer.BlockCopy(circuit, 0, packed, address.Length, circuit.Length);
            return packed;
        }
    }
}
