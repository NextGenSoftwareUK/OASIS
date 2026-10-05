using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [Function("CreateAvatar", "uint256")]
    public class CreateAvatarFunctionBase : FunctionMessage
    {
        [Parameter("uint256", "entityId", 1)]
        public virtual BigInteger EntityId { get; set; }
        [Parameter("string", "avatarId", 2)]
        public virtual string AvatarId { get; set; }
        [Parameter("string", "info", 3)]
        public virtual string Info { get; set; }
    }
}