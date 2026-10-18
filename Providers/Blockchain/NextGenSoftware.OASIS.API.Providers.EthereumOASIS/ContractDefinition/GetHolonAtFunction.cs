using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [Function("GetHolonAt", typeof(GetHolonByIdOutputDTO))]
    public sealed class GetHolonAtFunction : FunctionMessage
    {
        [Parameter("uint256", "index", 1)]
        public BigInteger Index { get; set; }
    }
}
