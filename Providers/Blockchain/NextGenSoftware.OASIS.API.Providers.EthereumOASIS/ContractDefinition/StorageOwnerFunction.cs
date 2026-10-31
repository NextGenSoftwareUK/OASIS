using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [Function("storageOwner", "address")]
    public sealed class StorageOwnerFunction : FunctionMessage { }

    [Function("storageProtocolVersion", "uint256")]
    public sealed class StorageProtocolVersionFunction : FunctionMessage { }

    [Function("checkedStorageProtocolVersion", "uint256")]
    public sealed class CheckedStorageProtocolVersionFunction : FunctionMessage { }
}
