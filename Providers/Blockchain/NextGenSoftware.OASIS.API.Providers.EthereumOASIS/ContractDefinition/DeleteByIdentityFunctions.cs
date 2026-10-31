using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    public abstract class DeleteByIdentityFunction : FunctionMessage
    {
        [Parameter("uint256", "entityId", 1)]
        public BigInteger EntityId { get; set; }
        [Parameter("string", "identity", 2)]
        public string Identity { get; set; }
    }

    [Function("DeleteHolonByIdentity", "bool")]
    public sealed class DeleteHolonByIdentityFunction : DeleteByIdentityFunction { }

    [Function("DeleteAvatarByIdentity", "bool")]
    public sealed class DeleteAvatarByIdentityFunction : DeleteByIdentityFunction { }

    [Function("DeleteAvatarDetailByIdentity", "bool")]
    public sealed class DeleteAvatarDetailByIdentityFunction : DeleteByIdentityFunction { }
}
