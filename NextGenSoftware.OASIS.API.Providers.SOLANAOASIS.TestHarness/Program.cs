using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness
{
    internal static class Program
    {
        private static string _mnemonicWords =
            "pattern vessel trade prosper cube okay dust pet primary during captain endless";

        #region Raw Entity Examples

        private static async Task Run_RawEntityCreation()
        {
            Console.WriteLine("Run_RawEntityCreation()->Connecting with wallet and querying block...");
            var wallet = new Wallet(_mnemonicWords);
            var rpcClient = ClientFactory.GetClient(Cluster.MainNet);
            var blockHash = await rpcClient.GetRecentBlockHashAsync();

            var avatar = new SolanaAvatarDto()
            {
                Id = Guid.NewGuid(),
                UserName = "@bob",
                AvatarId = Guid.NewGuid()
            };
            string avatarInfo = JsonConvert.SerializeObject(avatar);
            
            Console.WriteLine("Run_RawEntityCreation()->Creating transaction started...");
            var transactionBytes = new TransactionBuilder().
                SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                SetFeePayer(wallet.Account).
                AddInstruction(MemoProgram.NewMemo(wallet.Account, avatarInfo)).
                Build(wallet.Account);
            var sendTransactionResult = await rpcClient.SendTransactionAsync(transactionBytes);
            
            // Wait for transaction creating is done...
            await Task.Delay(3000);
            
            Console.WriteLine("Run_RawEntityCreation()->Transaction creating completed...");
            Console.WriteLine("Run_RawEntityCreation()->Was transaction request successfully: " + sendTransactionResult.WasSuccessful);
            Console.WriteLine("Run_RawEntityCreation()->Request status code: " + sendTransactionResult.HttpStatusCode);
        }

        private static async Task Run_RawCreateAndQueryEntity()
        {
            Console.WriteLine("Run_RawCreateAndQueryEntity()->Connecting with wallet and querying block...");
            var wallet = new Wallet(_mnemonicWords);
            var rpcClient = ClientFactory.GetClient(Cluster.MainNet);
            var blockHash = await rpcClient.GetRecentBlockHashAsync();

            var avatar = new SolanaAvatarDto()
            {
                Id = Guid.NewGuid(),
                UserName = "@bob",
                AvatarId = Guid.NewGuid()
            };
            string avatarInfo = JsonConvert.SerializeObject(avatar);
            
            Console.WriteLine("Run_RawCreateAndQueryEntity()->Creating transaction started...");
            var transactionBytes = new TransactionBuilder().
                SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                SetFeePayer(wallet.Account).
                AddInstruction(MemoProgram.NewMemo(wallet.Account, avatarInfo)).
                Build(wallet.Account);
            var sendTransactionResult = await rpcClient.SendTransactionAsync(transactionBytes);
            
            // Wait for transaction creating is done...
            await Task.Delay(3000);
            
            Console.WriteLine("Run_RawCreateAndQueryEntity()->Transaction creating completed...");
            Console.WriteLine("Run_RawCreateAndQueryEntity()->Was transaction request successfully: " + sendTransactionResult.WasSuccessful);
            Console.WriteLine("Run_RawCreateAndQueryEntity()->Request status code: " + sendTransactionResult.HttpStatusCode);
            
            Console.WriteLine("Run_RawCreateAndQueryEntity()->Querying transaction...");
            var transactionResult = await rpcClient.GetTransactionAsync(sendTransactionResult.Result, Commitment.Confirmed);
            
            Console.WriteLine("Run_RawCreateAndQueryEntity()->Transaction queried!");
            
            var transactionDataBuffer = Encoders.Base58.DecodeData(transactionResult.Result.Transaction.Message.Instructions[0].Data);
            var transactionContent = Encoding.UTF8.GetString(transactionDataBuffer);
            var deserializedAvatarEntity = JsonConvert.DeserializeObject<SolanaAvatarDto>(transactionContent);
            
            Console.WriteLine("Avatar UserName: " + deserializedAvatarEntity?.UserName);
            Console.WriteLine("Avatar Id: " + deserializedAvatarEntity?.Id);
            Console.WriteLine("Avatar AvatarId: " + deserializedAvatarEntity?.AvatarId);
        }

        #endregion

        #region Solana Provider Example

        private static async Task Run_SaveAndLoadAvatar()
        {
            SolanaOASIS solanaOasis = new SolanaOASIS(_mnemonicWords);
            Console.WriteLine("Run_SaveAndLoadAvatar()->ActivateProvider()");
            solanaOasis.ActivateProvider();

            Console.WriteLine("Run_SaveAndLoadAvatar()->SaveAvatarAsync()");
            var saveAvatarResult = await solanaOasis.SaveAvatarAsync(new Avatar()
            {
                Username = "@bob",
                Password = "P@ssw0rd!",
                Email = "bob@mail.ru",
                Id = Guid.NewGuid(),
                AvatarId = Guid.NewGuid()
            });

            if (saveAvatarResult.IsError)
            {
                Console.WriteLine(saveAvatarResult.Message);
                return;
            }

            Console.WriteLine("Run_SaveAndLoadAvatar()->LoadAvatarAsync()");
            var transactionHashProviderKey = saveAvatarResult.Result.ProviderUniqueStorageKey[ProviderType.SolanaOASIS];
            var loadAvatarResult = await solanaOasis.LoadAvatarForProviderKeyAsync(transactionHashProviderKey);
            
            if (loadAvatarResult.IsError)
            {
                Console.WriteLine(loadAvatarResult.Message);
                return;
            }
            
            Console.WriteLine("Avatar UserName: " + loadAvatarResult.Result.Username);
            Console.WriteLine("Avatar Password: " + loadAvatarResult.Result.Password);
            Console.WriteLine("Avatar Email: " + loadAvatarResult.Result.Email);
            Console.WriteLine("Avatar Id: " + loadAvatarResult.Result.Id);
            Console.WriteLine("Avatar AvatarId: " + loadAvatarResult.Result.AvatarId);

            Console.WriteLine("Run_SaveAndLoadAvatar()->DeActivateProvider()");
            solanaOasis.DeActivateProvider();
        }

        private static async Task Run_SendNftAsync()
        {
            SolanaOASIS solanaOasis = new SolanaOASIS(_mnemonicWords);
            Console.WriteLine("Run_SendNftAsync()->ActivateProvider()");
            solanaOasis.ActivateProvider();
            
            var sendNftRequest = new WalletTransaction()
            {
                Amount = 0.001m,
                Date = DateTime.Now,
                ProviderType = ProviderType.EOSIOOASIS,
                FromWalletAddress = "",
                ToWalletAddress = ""
            };
            Console.WriteLine("Run_SendNftAsync-->SendNFTAsync()-->Sending...");
            var sendNftResult = await solanaOasis.SendNFTAsync(sendNftRequest);
            if (sendNftResult.IsError)
            {
                Console.WriteLine("Run_SendNftAsync-->SendNFTAsync()-->Failed...");
                Console.WriteLine(sendNftResult.Message);
                return;
            }
            Console.WriteLine("Run_SendNftAsync-->SendNFTAsync()-->Completed...");

            Console.WriteLine("Run_SendNftAsync()->DeActivateProvider()");
            solanaOasis.DeActivateProvider();
        }

        private static async Task Run_SendTransactionAsync()
        {
            SolanaOASIS solanaOasis = new SolanaOASIS(_mnemonicWords);
            Console.WriteLine("Run_SendTransactionAsync()->ActivateProvider()");
            solanaOasis.ActivateProvider();

            var walletTransaction = new WalletTransaction()
            {
                Amount = 0.001m,
                Date = DateTime.Now,
                ProviderType = ProviderType.EOSIOOASIS,
                FromWalletAddress = "",
                ToWalletAddress = ""
            };
            Console.WriteLine("Run_SendTransactionAsync-->SendTransactionAsync()-->Sending...");
            var sendTransactionResult = await solanaOasis.SendTransactionAsync(walletTransaction);
            if (sendTransactionResult.IsError)
            {
                Console.WriteLine("Run_SendTransactionAsync-->SendTransactionAsync()-->Failed...");
                Console.WriteLine(sendTransactionResult.Message);
                return;
            }
            Console.WriteLine("Run_SendTransactionAsync-->SendTransactionAsync()-->Completed...");

            Console.WriteLine("Run_SendTransactionAsync()->DeActivateProvider()");
            solanaOasis.DeActivateProvider();
        }
        
        #endregion

        private static async Task Main(string[] args)
        {
            // Transferring Examples
            await Run_SendNftAsync();
            await Run_SendTransactionAsync();
            
            // Solana Provider Examples
            await Run_SaveAndLoadAvatar();
            
            // Raw entity example
            await Run_RawEntityCreation();
            await Run_RawCreateAndQueryEntity();
        }
    }
}