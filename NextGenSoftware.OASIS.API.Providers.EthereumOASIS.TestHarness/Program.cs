using System;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Web3.Accounts.Managed;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts.CQS;
using Nethereum.Util;
using Nethereum.Contracts;
using Nethereum.Contracts.Extensions;
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
    };

    internal static class Program
    {
        private static string HostUri { get; set; } = "https://mainnet.infura.io/v3/deeae1e5eba14585a662655f06497def";
        private static string ProjectId { get; set; } = "deeae1e5eba14585a662655f06497def";

        private static async Task TransferToken()
        {
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

        private static async Task GetCreateAccountAsync()
        {
            string ABI = @"[{'inputs':[],'stateMutability':'nonpayable','type':'constructor'},{'inputs':[{'internalType':'string','name':'first','type':'string'},{'internalType':'string','name':'second','type':'string'}],'name':'compareString','outputs':[{'internalType':'bool','name':'','type':'bool'}],'stateMutability':'pure','type':'function'},{'inputs':[{'internalType':'string','name':'userid','type':'string'},{'internalType':'string','name':'name','type':'string'},{'internalType':'string','name':'providerkey','type':'string'},{'internalType':'string','name':'password','type':'string'},{'internalType':'string','name':'email','type':'string'},{'internalType':'string','name':'title','type':'string'},{'internalType':'string','name':'firstname','type':'string'},{'internalType':'string','name':'lastname','type':'string'},{'internalType':'string','name':'dob','type':'string'},{'internalType':'string','name':'playeraddr','type':'string'},{'internalType':'uint32','name':'karma','type':'uint32'}],'name':'createAccount','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'string','name':'userid','type':'string'}],'name':'deleteAccount','outputs':[{'internalType':'bool','name':'','type':'bool'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[],'name':'getAccountCount','outputs':[{'internalType':'uint256','name':'count','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'string','name':'userid','type':'string'}],'name':'getAccountParameter','outputs':[{'components':[{'internalType':'string','name':'userid','type':'string'},{'internalType':'string','name':'name','type':'string'},{'internalType':'string','name':'providerkey','type':'string'},{'internalType':'string','name':'password','type':'string'},{'internalType':'string','name':'email','type':'string'},{'internalType':'string','name':'title','type':'string'},{'internalType':'string','name':'firstname','type':'string'},{'internalType':'string','name':'lastname','type':'string'},{'internalType':'string','name':'dob','type':'string'},{'internalType':'string','name':'playeraddr','type':'string'},{'internalType':'uint32','name':'karma','type':'uint32'}],'internalType':'structOASIS.account','name':'','type':'tuple'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'totalAccounts','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'string','name':'userid','type':'string'},{'internalType':'string','name':'name','type':'string'},{'internalType':'string','name':'providerkey','type':'string'},{'internalType':'string','name':'password','type':'string'},{'internalType':'string','name':'email','type':'string'},{'internalType':'string','name':'title','type':'string'},{'internalType':'string','name':'firstname','type':'string'},{'internalType':'string','name':'lastname','type':'string'},{'internalType':'string','name':'dob','type':'string'},{'internalType':'string','name':'playeraddr','type':'string'},{'internalType':'uint32','name':'karma','type':'uint32'}],'name':'updateAccount','outputs':[{'internalType':'bool','name':'','type':'bool'}],'stateMutability':'nonpayable','type':'function'}]";

            var account = new Account("0x96a2cdfa6e4198b683188e0265d548da23a829f00fdf4858f9b7899e5d55fd4c");
            var web3 = new Web3(account, HostUri);
            var oasisContract = web3.Eth.GetContract(ABI, "0x5AD3aeBBB1E99c851D68aeFaF07FC98c2e706Fd5");

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

            await newAccountFunction.CallAsync<int>(accParams);
            var getAccountCountFunction = oasisContract.GetFunction("getAccountCount");
            var totalAccount = await getAccountCountFunction.CallAsync<int>();
            Console.Write("Account Creation Succeed, Count: {0}", totalAccount);
        }
        
        private static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.OASIS.API.Providers.EthereumOASIS - TEST HARNESS");
            await GetAccountBalanceAsync();
            await GetCreateAccountAsync();
            await SendEthereum();
        }
    }
}