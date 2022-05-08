using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    public class HolonBase 
    {
        [Parameter("uint256", "EntityId", 1)]
        public virtual BigInteger EntityId { get; set; }
        [Parameter("string", "HolonId", 2)]
        public virtual string HolonId { get; set; }
        [Parameter("string", "Info", 3)]
        public virtual string Info { get; set; }
    }
}