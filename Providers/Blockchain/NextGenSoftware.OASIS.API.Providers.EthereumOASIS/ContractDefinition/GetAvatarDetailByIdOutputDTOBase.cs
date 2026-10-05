using Nethereum.ABI.FunctionEncoding.Attributes;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [FunctionOutput]
    public class GetAvatarDetailByIdOutputDTOBase : IFunctionOutputDTO 
    {
        [Parameter("tuple", "", 1)]
        public virtual AvatarDetail ReturnValue1 { get; set; }
    }
}