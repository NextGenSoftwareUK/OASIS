using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [Function("CreateHolon", "uint256")]
    public class CreateHolonFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "entityId", 1)]
        public virtual BigInteger EntityId { get; set; }
        [Parameter("string", "holonId", 2)]
        public virtual string HolonId { get; set; }
        [Parameter("string", "info", 3)]
        public virtual string Info { get; set; }
    }
}