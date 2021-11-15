using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.TestHarness
{
    public class AvatarAccount
    {
        public string Name;
        public string Password;
        public string FirstName;
        public string LastName;

        public string GetAvatarAccountJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    internal static class Program
    {
        private static string HostUri { get; set; } = "https://mainnet.infura.io/v3/deeae1e5eba14585a662655f06497def";
        private static string ProjectId { get; set; } = "deeae1e5eba14585a662655f06497def";

        private static string AbiPath { get; set; } =
            @"abi_file";

        private static string NewAbiPath { get; set; } = @"E:\Faridun's Projects\NextGen World\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.API.Providers.EthereumOASIS.TestHarness\contracts\bin\testHarnessContract.abi";
        private static string NewAbiByteCodePath { get; set; } = @"E:\Faridun's Projects\NextGen World\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.API.Providers.EthereumOASIS.TestHarness\contracts\bin\testHarnessContract.bin";

        private static async Task<string> ReadContentFromFile(string path)
        {
            using var reader = new StreamReader(path, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
        
        private static async Task Multiply()
        {
            var newAbiContent = await ReadContentFromFile(NewAbiPath);
            var newAbiByteCodeContent = await ReadContentFromFile(NewAbiByteCodePath);

            var senderAddress = "0x12890d2cce102216644c59daE5baed380d84830c";
            var password = "password";
            
            var multiplier = 7;
            
            var web3 = new Web3();
            var unlockAccountDetail =
                await web3.Personal.UnlockAccount.SendRequestAsync(senderAddress, password, 120);
            Console.WriteLine("Unlock Account Status: {0}", unlockAccountDetail);

            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(newAbiContent, newAbiByteCodeContent, senderAddress, multiplier);

            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            while (receipt == null)
            {
                await Task.Delay(5000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }

            var contractAddress = receipt.ContractAddress;
            var contract = web3.Eth.GetContract(newAbiContent, contractAddress);
            var mFunc = contract.GetFunction("multiply");
            var result = await mFunc.CallAsync<int>(7);
            Console.WriteLine("Multiply Function Result: {0}", result);
        }
        
        private static async Task SendEthereum()
        {
            var privateKey = "0xb5b1870957d373ef0eeffecc6e4812c0fd08f554b37b233526acc331bf1544f7";
            var account = new Account(privateKey);
            var web3 = new Web3(account);
            var toAddress = "0x13f022d72158410433cbd66f5dd8bf6d2d129924";
            var transaction = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toAddress, 1.11m);
            Console.WriteLine("Transaction Block Hash: {0}", transaction.BlockHash);
        }
        
        private static async Task GetAccountBalanceAsync()
        {
            var web3 = new Web3(HostUri);
            var balance = await web3.Eth.GetBalance.SendRequestAsync("0xde0b295669a9fd93d5f28d9ec85e40f4cb697bae");
            Console.WriteLine($"Balance in Wei: {balance.Value}");

            var etherAmount = Web3.Convert.FromWei(balance.Value);
            Console.WriteLine($"Balance in Ether: {etherAmount}");
        }

        private static async Task GetCreateAccountWithContractAsync()
        {
            var abiContent = await ReadContentFromFile(AbiPath);
            var account = new Account("0x96a2cdfa6e4198b683188e0265d548da23a829f00fdf4858f9b7899e5d55fd4c");
            var web3 = new Web3(account, HostUri);
            var oasisContract = web3.Eth.GetContract(abiContent, "0x5AD3aeBBB1E99c851D68aeFaF07FC98c2e706Fd5");

            var newAccount = new AvatarAccount
            {
                Name = "_faha_", Password = "0000", FirstName = "Faridun", LastName = "Berdiev"
            };
            Console.WriteLine(newAccount.GetAvatarAccountJson());
            
            var newAccountFunction = oasisContract.GetFunction("createAccount");
            var accParams = new object[]{
                newAccount.Name,
                newAccount.Password,
                newAccount.FirstName,
                newAccount.LastName,
            };

            var newAccountFuncCount = await newAccountFunction.CallAsync<int>(accParams);
            var getAccountCountFunction = oasisContract.GetFunction("getAccountCount");
            var totalAccount = await getAccountCountFunction.CallAsync<int>();
            Console.Write("Account Creation Succeed, Count: {0}, {1}", totalAccount, newAccountFuncCount);
        }
        
        private static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.OASIS.API.Providers.EthereumOASIS - TEST HARNESS");
            await Multiply();
        }
    }
}