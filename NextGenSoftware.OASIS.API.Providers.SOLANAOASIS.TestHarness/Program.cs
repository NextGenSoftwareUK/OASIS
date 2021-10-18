using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness
{
    public class TestEntity
    {
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Mail { get; set; }
        public int Age { get; set; }
        public string Key { get; set; }
        public string SomeProperty { get; set; }
        public string SomeProperty2 { get; set; }
        public string SomeProperty3 { get; set; }
    }

    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var wallet = new Wallet("pattern vessel trade prosper cube okay dust pet primary during captain endless");
            var rpcClient = ClientFactory.GetClient(Cluster.MainNet);
            
            var publicKey = "GsguXojeGATpZGW8VNfW8qQCBVodbW2qGS8bUEbdGZfv";
            
            var fromAccount = new PublicKey(publicKey);
            var toAccount = new PublicKey(publicKey);
            var blockHash = await rpcClient.GetRecentBlockHashAsync();

            var tx = new TransactionBuilder().
                SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                SetFeePayer(fromAccount).
                AddInstruction(MemoProgram.NewMemo(fromAccount, JsonConvert.SerializeObject(new TestEntity()
                {
                    Age = 12,
                    UserName = "TUTI",
                    SomeProperty2 = "HELLOOOO",
                    SomeProperty3 = "HELLOOOO",
                    Key = "HELLOOOO",
                    SomeProperty = "HELLOOOO",
                    Name = "HELLOOOO",
                    Mail = "@mail.ru"
                }))).
                Build(wallet.Account);
            var sendTransactionResult = await rpcClient.SendTransactionAsync(tx);
            
            var entity = await rpcClient.GetTransactionAsync(sendTransactionResult.Result, Commitment.Confirmed);

            var bytesData = Encoders.Base58.DecodeData(entity.Result.Transaction.Message.Instructions[0].Data);
            Console.WriteLine(Encoding.UTF8.GetString(bytesData));


            var entityFromData = JsonConvert.DeserializeObject<TestEntity>(Encoding.UTF8.GetString(bytesData));
            Console.WriteLine(entityFromData);

            Console.WriteLine(entity.RawRpcResponse);
                
            OutputProperty(nameof(sendTransactionResult.WasSuccessful), sendTransactionResult.WasSuccessful.ToString());
            OutputProperty(nameof(sendTransactionResult.HttpStatusCode), sendTransactionResult.HttpStatusCode.ToString());
            OutputProperty(nameof(sendTransactionResult.Reason), sendTransactionResult.Reason);
            OutputProperty(nameof(sendTransactionResult.Result), sendTransactionResult.Result);

        }

        private static void OutputProperty(string name, string value)
        {
            Console.WriteLine($"{name}: {value}");
        }
        
    }
}