using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        private static async Task Main(string[] args)
        {
            // Raw entity example
            await Run_RawEntityCreation();
            await Run_RawCreateAndQueryEntity();
        }
    }
}