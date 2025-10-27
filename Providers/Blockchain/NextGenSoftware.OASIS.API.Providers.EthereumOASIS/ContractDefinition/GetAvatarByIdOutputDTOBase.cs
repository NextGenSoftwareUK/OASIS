using Nethereum.ABI.FunctionEncoding.Attributes;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [FunctionOutput]
    public class GetAvatarByIdOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("tuple", "", 1)]
        public virtual Avatar ReturnValue1 { get; set; }
    }
}