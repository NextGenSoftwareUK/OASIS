using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [Function("GetAvatarDetailAt", typeof(GetAvatarDetailByIdOutputDTO))]
    public sealed class GetAvatarDetailAtFunction : FunctionMessage
    {
        [Parameter("uint256", "index", 1)]
        public BigInteger Index { get; set; }
    }
}
