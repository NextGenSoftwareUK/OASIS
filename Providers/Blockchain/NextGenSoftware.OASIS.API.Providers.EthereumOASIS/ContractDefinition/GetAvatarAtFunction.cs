using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [Function("GetAvatarAt", typeof(GetAvatarByIdOutputDTO))]
    public sealed class GetAvatarAtFunction : FunctionMessage
    {
        [Parameter("uint256", "index", 1)]
        public BigInteger Index { get; set; }
    }
}
