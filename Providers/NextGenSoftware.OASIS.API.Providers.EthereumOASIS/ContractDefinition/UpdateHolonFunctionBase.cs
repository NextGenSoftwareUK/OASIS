using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [Function("UpdateHolon", "bool")]
    public class UpdateHolonFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "entityId", 1)]
        public virtual BigInteger EntityId { get; set; }
        [Parameter("string", "info", 2)]
        public virtual string Info { get; set; }
    }
}