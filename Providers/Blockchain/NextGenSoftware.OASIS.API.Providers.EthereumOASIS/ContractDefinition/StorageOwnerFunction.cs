using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [Function("storageOwner", "address")]
    public sealed class StorageOwnerFunction : FunctionMessage { }
}
