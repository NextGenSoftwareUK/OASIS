using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [FunctionOutput]
    public class GetAvatarDetailsCountOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("uint256", "count", 1)]
        public virtual BigInteger Count { get; set; }
    }
}