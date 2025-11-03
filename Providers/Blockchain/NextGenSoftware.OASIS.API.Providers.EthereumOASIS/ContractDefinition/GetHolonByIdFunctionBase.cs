using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [Function("GetHolonById", typeof(GetHolonByIdOutputDTO))]
    public class GetHolonByIdFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "entityId", 1)]
        public virtual BigInteger EntityId { get; set; }
    }
}