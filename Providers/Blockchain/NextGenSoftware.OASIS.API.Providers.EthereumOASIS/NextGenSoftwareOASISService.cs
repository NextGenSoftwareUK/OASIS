using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class NextGenSoftwareOASISService
    {
        public static Task<TransactionReceipt> DeployContractAndWaitForReceiptAsync(Nethereum.Web3.Web3 web3, NextGenSoftwareOASISDeployment nextGenSoftwareOASISDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            return web3.Eth.GetContractDeploymentHandler<NextGenSoftwareOASISDeployment>().SendRequestAndWaitForReceiptAsync(nextGenSoftwareOASISDeployment, cancellationTokenSource);
        }

        public static Task<string> DeployContractAsync(Nethereum.Web3.Web3 web3, NextGenSoftwareOASISDeployment nextGenSoftwareOASISDeployment)
        {
            return web3.Eth.GetContractDeploymentHandler<NextGenSoftwareOASISDeployment>().SendRequestAsync(nextGenSoftwareOASISDeployment);
        }

        public static async Task<NextGenSoftwareOASISService> DeployContractAndGetServiceAsync(Nethereum.Web3.Web3 web3, NextGenSoftwareOASISDeployment nextGenSoftwareOASISDeployment, CancellationTokenSource cancellationTokenSource = null)
        {
            var receipt = await DeployContractAndWaitForReceiptAsync(web3, nextGenSoftwareOASISDeployment, cancellationTokenSource);
            return new NextGenSoftwareOASISService(web3, receipt.ContractAddress);
        }

        protected Nethereum.Web3.Web3 Web3{ get; }

        public ContractHandler ContractHandler { get; }

        public NextGenSoftwareOASISService(Nethereum.Web3.Web3 web3, string contractAddress)
        {
            Web3 = web3;
            ContractHandler = web3.Eth.GetContractHandler(contractAddress);
        }

        public Task<string> CreateAvatarRequestAsync(CreateAvatarFunction createAvatarFunction)
        {
             return ContractHandler.SendRequestAsync(createAvatarFunction);
        }

        public Task<TransactionReceipt> CreateAvatarRequestAndWaitForReceiptAsync(CreateAvatarFunction createAvatarFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(createAvatarFunction, cancellationToken);
        }

        public Task<string> CreateAvatarRequestAsync(BigInteger entityId, string avatarId, string info)
        {
            var createAvatarFunction = new CreateAvatarFunction();
                createAvatarFunction.EntityId = entityId;
                createAvatarFunction.AvatarId = avatarId;
                createAvatarFunction.Info = info;
            
             return ContractHandler.SendRequestAsync(createAvatarFunction);
        }

        public Task<TransactionReceipt> CreateAvatarRequestAndWaitForReceiptAsync(BigInteger entityId, string avatarId, string info, CancellationTokenSource cancellationToken = null)
        {
            var createAvatarFunction = new CreateAvatarFunction();
                createAvatarFunction.EntityId = entityId;
                createAvatarFunction.AvatarId = avatarId;
                createAvatarFunction.Info = info;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(createAvatarFunction, cancellationToken);
        }

        public Task<string> CreateAvatarDetailRequestAsync(CreateAvatarDetailFunction createAvatarDetailFunction)
        {
             return ContractHandler.SendRequestAsync(createAvatarDetailFunction);
        }

        public Task<TransactionReceipt> CreateAvatarDetailRequestAndWaitForReceiptAsync(CreateAvatarDetailFunction createAvatarDetailFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(createAvatarDetailFunction, cancellationToken);
        }

        public Task<string> CreateAvatarDetailRequestAsync(BigInteger entityId, string avatarId, string info)
        {
            var createAvatarDetailFunction = new CreateAvatarDetailFunction();
                createAvatarDetailFunction.EntityId = entityId;
                createAvatarDetailFunction.AvatarId = avatarId;
                createAvatarDetailFunction.Info = info;
            
             return ContractHandler.SendRequestAsync(createAvatarDetailFunction);
        }

        public Task<TransactionReceipt> CreateAvatarDetailRequestAndWaitForReceiptAsync(BigInteger entityId, string avatarId, string info, CancellationTokenSource cancellationToken = null)
        {
            var createAvatarDetailFunction = new CreateAvatarDetailFunction();
                createAvatarDetailFunction.EntityId = entityId;
                createAvatarDetailFunction.AvatarId = avatarId;
                createAvatarDetailFunction.Info = info;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(createAvatarDetailFunction, cancellationToken);
        }

        public Task<string> CreateHolonRequestAsync(CreateHolonFunction createHolonFunction)
        {
             return ContractHandler.SendRequestAsync(createHolonFunction);
        }

        public Task<TransactionReceipt> CreateHolonRequestAndWaitForReceiptAsync(CreateHolonFunction createHolonFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(createHolonFunction, cancellationToken);
        }

        public Task<string> CreateHolonRequestAsync(BigInteger entityId, string holonId, string info)
        {
            var createHolonFunction = new CreateHolonFunction();
                createHolonFunction.EntityId = entityId;
                createHolonFunction.HolonId = holonId;
                createHolonFunction.Info = info;
            
             return ContractHandler.SendRequestAsync(createHolonFunction);
        }

        public Task<TransactionReceipt> CreateHolonRequestAndWaitForReceiptAsync(BigInteger entityId, string holonId, string info, CancellationTokenSource cancellationToken = null)
        {
            var createHolonFunction = new CreateHolonFunction();
                createHolonFunction.EntityId = entityId;
                createHolonFunction.HolonId = holonId;
                createHolonFunction.Info = info;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(createHolonFunction, cancellationToken);
        }

        public Task<string> DeleteAvatarRequestAsync(DeleteAvatarFunction deleteAvatarFunction)
        {
             return ContractHandler.SendRequestAsync(deleteAvatarFunction);
        }

        public Task<TransactionReceipt> DeleteAvatarRequestAndWaitForReceiptAsync(DeleteAvatarFunction deleteAvatarFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(deleteAvatarFunction, cancellationToken);
        }

        public Task<string> DeleteAvatarRequestAsync(BigInteger entityId)
        {
            var deleteAvatarFunction = new DeleteAvatarFunction();
                deleteAvatarFunction.EntityId = entityId;
            
             return ContractHandler.SendRequestAsync(deleteAvatarFunction);
        }

        public Task<TransactionReceipt> DeleteAvatarRequestAndWaitForReceiptAsync(BigInteger entityId, CancellationTokenSource cancellationToken = null)
        {
            var deleteAvatarFunction = new DeleteAvatarFunction();
                deleteAvatarFunction.EntityId = entityId;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(deleteAvatarFunction, cancellationToken);
        }

        public Task<string> DeleteAvatarDetailRequestAsync(DeleteAvatarDetailFunction deleteAvatarDetailFunction)
        {
             return ContractHandler.SendRequestAsync(deleteAvatarDetailFunction);
        }

        public Task<TransactionReceipt> DeleteAvatarDetailRequestAndWaitForReceiptAsync(DeleteAvatarDetailFunction deleteAvatarDetailFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(deleteAvatarDetailFunction, cancellationToken);
        }

        public Task<string> DeleteAvatarDetailRequestAsync(BigInteger entityId)
        {
            var deleteAvatarDetailFunction = new DeleteAvatarDetailFunction();
                deleteAvatarDetailFunction.EntityId = entityId;
            
             return ContractHandler.SendRequestAsync(deleteAvatarDetailFunction);
        }

        public Task<TransactionReceipt> DeleteAvatarDetailRequestAndWaitForReceiptAsync(BigInteger entityId, CancellationTokenSource cancellationToken = null)
        {
            var deleteAvatarDetailFunction = new DeleteAvatarDetailFunction();
                deleteAvatarDetailFunction.EntityId = entityId;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(deleteAvatarDetailFunction, cancellationToken);
        }

        public Task<string> DeleteHolonRequestAsync(DeleteHolonFunction deleteHolonFunction)
        {
             return ContractHandler.SendRequestAsync(deleteHolonFunction);
        }

        public Task<TransactionReceipt> DeleteHolonRequestAndWaitForReceiptAsync(DeleteHolonFunction deleteHolonFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(deleteHolonFunction, cancellationToken);
        }

        public Task<string> DeleteHolonRequestAsync(BigInteger entityId)
        {
            var deleteHolonFunction = new DeleteHolonFunction();
                deleteHolonFunction.EntityId = entityId;
            
             return ContractHandler.SendRequestAsync(deleteHolonFunction);
        }

        public Task<TransactionReceipt> DeleteHolonRequestAndWaitForReceiptAsync(BigInteger entityId, CancellationTokenSource cancellationToken = null)
        {
            var deleteHolonFunction = new DeleteHolonFunction();
                deleteHolonFunction.EntityId = entityId;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(deleteHolonFunction, cancellationToken);
        }

        public Task<GetAvatarByIdOutputDTO> GetAvatarByIdQueryAsync(GetAvatarByIdFunction getAvatarByIdFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<GetAvatarByIdFunction, GetAvatarByIdOutputDTO>(getAvatarByIdFunction, blockParameter);
        }

        public Task<GetAvatarByIdOutputDTO> GetAvatarByIdQueryAsync(BigInteger entityId, BlockParameter blockParameter = null)
        {
            var getAvatarByIdFunction = new GetAvatarByIdFunction();
                getAvatarByIdFunction.EntityId = entityId;
            
            return ContractHandler.QueryDeserializingToObjectAsync<GetAvatarByIdFunction, GetAvatarByIdOutputDTO>(getAvatarByIdFunction, blockParameter);
        }

        public Task<GetAvatarDetailByIdOutputDTO> GetAvatarDetailByIdQueryAsync(GetAvatarDetailByIdFunction getAvatarDetailByIdFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<GetAvatarDetailByIdFunction, GetAvatarDetailByIdOutputDTO>(getAvatarDetailByIdFunction, blockParameter);
        }

        public Task<GetAvatarDetailByIdOutputDTO> GetAvatarDetailByIdQueryAsync(BigInteger entityId, BlockParameter blockParameter = null)
        {
            var getAvatarDetailByIdFunction = new GetAvatarDetailByIdFunction();
                getAvatarDetailByIdFunction.EntityId = entityId;
            
            return ContractHandler.QueryDeserializingToObjectAsync<GetAvatarDetailByIdFunction, GetAvatarDetailByIdOutputDTO>(getAvatarDetailByIdFunction, blockParameter);
        }

        public Task<BigInteger> GetAvatarDetailsCountQueryAsync(GetAvatarDetailsCountFunction getAvatarDetailsCountFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<GetAvatarDetailsCountFunction, BigInteger>(getAvatarDetailsCountFunction, blockParameter);
        }

        
        public Task<BigInteger> GetAvatarDetailsCountQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<GetAvatarDetailsCountFunction, BigInteger>(null, blockParameter);
        }

        public Task<BigInteger> GetAvatarsCountQueryAsync(GetAvatarsCountFunction getAvatarsCountFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<GetAvatarsCountFunction, BigInteger>(getAvatarsCountFunction, blockParameter);
        }

        
        public Task<BigInteger> GetAvatarsCountQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<GetAvatarsCountFunction, BigInteger>(null, blockParameter);
        }

        public Task<GetHolonByIdOutputDTO> GetHolonByIdQueryAsync(GetHolonByIdFunction getHolonByIdFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryDeserializingToObjectAsync<GetHolonByIdFunction, GetHolonByIdOutputDTO>(getHolonByIdFunction, blockParameter);
        }

        public Task<GetHolonByIdOutputDTO> GetHolonByIdQueryAsync(BigInteger entityId, BlockParameter blockParameter = null)
        {
            var getHolonByIdFunction = new GetHolonByIdFunction();
                getHolonByIdFunction.EntityId = entityId;
            
            return ContractHandler.QueryDeserializingToObjectAsync<GetHolonByIdFunction, GetHolonByIdOutputDTO>(getHolonByIdFunction, blockParameter);
        }

        public Task<BigInteger> GetHolonsCountQueryAsync(GetHolonsCountFunction getHolonsCountFunction, BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<GetHolonsCountFunction, BigInteger>(getHolonsCountFunction, blockParameter);
        }

        
        public Task<BigInteger> GetHolonsCountQueryAsync(BlockParameter blockParameter = null)
        {
            return ContractHandler.QueryAsync<GetHolonsCountFunction, BigInteger>(null, blockParameter);
        }

        public Task<string> UpdateAvatarRequestAsync(UpdateAvatarFunction updateAvatarFunction)
        {
             return ContractHandler.SendRequestAsync(updateAvatarFunction);
        }

        public Task<TransactionReceipt> UpdateAvatarRequestAndWaitForReceiptAsync(UpdateAvatarFunction updateAvatarFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(updateAvatarFunction, cancellationToken);
        }

        public Task<string> UpdateAvatarRequestAsync(BigInteger entityId, string info)
        {
            var updateAvatarFunction = new UpdateAvatarFunction();
                updateAvatarFunction.EntityId = entityId;
                updateAvatarFunction.Info = info;
            
             return ContractHandler.SendRequestAsync(updateAvatarFunction);
        }

        public Task<TransactionReceipt> UpdateAvatarRequestAndWaitForReceiptAsync(BigInteger entityId, string info, CancellationTokenSource cancellationToken = null)
        {
            var updateAvatarFunction = new UpdateAvatarFunction();
                updateAvatarFunction.EntityId = entityId;
                updateAvatarFunction.Info = info;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(updateAvatarFunction, cancellationToken);
        }

        public Task<string> UpdateAvatarDetailRequestAsync(UpdateAvatarDetailFunction updateAvatarDetailFunction)
        {
             return ContractHandler.SendRequestAsync(updateAvatarDetailFunction);
        }

        public Task<TransactionReceipt> UpdateAvatarDetailRequestAndWaitForReceiptAsync(UpdateAvatarDetailFunction updateAvatarDetailFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(updateAvatarDetailFunction, cancellationToken);
        }

        public Task<string> UpdateAvatarDetailRequestAsync(BigInteger entityId, string info)
        {
            var updateAvatarDetailFunction = new UpdateAvatarDetailFunction();
                updateAvatarDetailFunction.EntityId = entityId;
                updateAvatarDetailFunction.Info = info;
            
             return ContractHandler.SendRequestAsync(updateAvatarDetailFunction);
        }

        public Task<TransactionReceipt> UpdateAvatarDetailRequestAndWaitForReceiptAsync(BigInteger entityId, string info, CancellationTokenSource cancellationToken = null)
        {
            var updateAvatarDetailFunction = new UpdateAvatarDetailFunction();
                updateAvatarDetailFunction.EntityId = entityId;
                updateAvatarDetailFunction.Info = info;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(updateAvatarDetailFunction, cancellationToken);
        }

        public Task<string> UpdateHolonRequestAsync(UpdateHolonFunction updateHolonFunction)
        {
             return ContractHandler.SendRequestAsync(updateHolonFunction);
        }

        public Task<TransactionReceipt> UpdateHolonRequestAndWaitForReceiptAsync(UpdateHolonFunction updateHolonFunction, CancellationTokenSource cancellationToken = null)
        {
             return ContractHandler.SendRequestAndWaitForReceiptAsync(updateHolonFunction, cancellationToken);
        }

        public Task<string> UpdateHolonRequestAsync(BigInteger entityId, string info)
        {
            var updateHolonFunction = new UpdateHolonFunction();
                updateHolonFunction.EntityId = entityId;
                updateHolonFunction.Info = info;
            
             return ContractHandler.SendRequestAsync(updateHolonFunction);
        }

        public Task<TransactionReceipt> UpdateHolonRequestAndWaitForReceiptAsync(BigInteger entityId, string info, CancellationTokenSource cancellationToken = null)
        {
            var updateHolonFunction = new UpdateHolonFunction();
                updateHolonFunction.EntityId = entityId;
                updateHolonFunction.Info = info;
            
             return ContractHandler.SendRequestAndWaitForReceiptAsync(updateHolonFunction, cancellationToken);
        }
    }
}
