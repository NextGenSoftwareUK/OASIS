using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition;
using Avatar = NextGenSoftware.OASIS.API.Core.Holons.Avatar;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.TestHarness
{
    internal static class Program
    {
        private static readonly string _chainUrl = "http://testchain.nethereum.com:8545";
        private static readonly string _chainPrivateKey = "0x7580e7fb49df1c861f0050fae31c2224c6aba908e116b8da44ee8cd927b990b0";
        private static readonly BigInteger _chainId = 444444444500;
        
        private static async Task ExecuteRawExample()
        {
            // Setting up ethereum account and web3 protocol
            var account = new Account(_chainPrivateKey, _chainId);
            var web3 = new Web3(account, _chainUrl);

            // Deploying contract
            Console.WriteLine("Deploying...");
            var contractDeployment = new NextGenSoftwareOASISDeployment();
            var contractDeploymentReceipt = await NextGenSoftwareOASISService.DeployContractAndWaitForReceiptAsync(web3, contractDeployment);
            var nextGenSoftwareOasisService = new NextGenSoftwareOASISService(web3, contractDeploymentReceipt.ContractAddress);
            Console.WriteLine($"Contract deployed, address: {nextGenSoftwareOasisService.ContractHandler.ContractAddress}");
            
            // Creating avatar instances for creating
            IAvatar bobAvatar = new Avatar
            {
                Id = Guid.NewGuid(),
                AvatarId = Guid.NewGuid(),
                Username = "@Bob",
                CreatedDate = DateTime.Now
            };

            var avatarInfo = JsonConvert.SerializeObject(bobAvatar);
            var avatarEntityId = HashUtility.GetNumericHash(bobAvatar.Id.ToString());
            var avatarId = bobAvatar.AvatarId.ToString();

            Console.WriteLine("Creating avatar...");
            var creatingTransactionHash = await nextGenSoftwareOasisService
                .CreateAvatarRequestAsync(avatarEntityId, avatarId, avatarInfo);

            if (string.IsNullOrEmpty(creatingTransactionHash))
            {
                Console.WriteLine("Avatar creating failed, please try again later...");
                return;
            }
            
            Console.WriteLine("Creating avatar completed successfully!");

            Console.WriteLine("Querying avatar...");
            
            var avatarByIdOutputDto = await nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId);

            if (avatarByIdOutputDto == null)
                Console.WriteLine("Avatar not found, avatar creating failed...");
            else
                Console.WriteLine($"{avatarByIdOutputDto.ReturnValue1.AvatarId} - avatar queried successfully!");
        }

        private static async Task ExecuteProviderExample()
        {
            
        }
        
        private static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.OASIS.API.Providers.EthereumOASIS - TEST HARNESS");
            await ExecuteRawExample();
            await ExecuteProviderExample();
        }
    }
}