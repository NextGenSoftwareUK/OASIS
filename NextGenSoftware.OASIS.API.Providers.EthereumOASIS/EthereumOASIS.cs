using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nethereum.Contracts;
//using Nethereum.Model;
using Nethereum.Web3.Accounts;
using Nethereum.Web3;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public class EthereumOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        private class account
        {
            public string userid;
            public string name;
            public string providerkey;
            public string password;
            public string email;
            public string title;
            public string firstname;
            public string lastname;
            public string dob;
            public string playeraddr;
            public int karma;
        };


        public EthereumOASIS()
        {
            this.ProviderName = "EthereumOASIS";
            this.ProviderDescription = "Ethereum Provider";
            this.ProviderType = ProviderType.EthereumOASIS;
            this.ProviderCategory = ProviderCategory.StorageAndNetwork;

            oasisTest();
        }

        private static void oasisTest()
        {
            string url = "HTTP://localhost:7545";
            string address = "0x5AD3aeBBB1E99c851D68aeFaF07FC98c2e706Fd5";

            //The ABI for the contract.
            string ABI = @"[{'inputs':[],'stateMutability':'nonpayable','type':'constructor'},{'inputs':[{'internalType':'string','name':'first','type':'string'},{'internalType':'string','name':'second','type':'string'}],'name':'compareString','outputs':[{'internalType':'bool','name':'','type':'bool'}],'stateMutability':'pure','type':'function'},{'inputs':[{'internalType':'string','name':'userid','type':'string'},{'internalType':'string','name':'name','type':'string'},{'internalType':'string','name':'providerkey','type':'string'},{'internalType':'string','name':'password','type':'string'},{'internalType':'string','name':'email','type':'string'},{'internalType':'string','name':'title','type':'string'},{'internalType':'string','name':'firstname','type':'string'},{'internalType':'string','name':'lastname','type':'string'},{'internalType':'string','name':'dob','type':'string'},{'internalType':'string','name':'playeraddr','type':'string'},{'internalType':'uint32','name':'karma','type':'uint32'}],'name':'createAccount','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'string','name':'userid','type':'string'}],'name':'deleteAccount','outputs':[{'internalType':'bool','name':'','type':'bool'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[],'name':'getAccountCount','outputs':[{'internalType':'uint256','name':'count','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'string','name':'userid','type':'string'}],'name':'getAccountParameter','outputs':[{'components':[{'internalType':'string','name':'userid','type':'string'},{'internalType':'string','name':'name','type':'string'},{'internalType':'string','name':'providerkey','type':'string'},{'internalType':'string','name':'password','type':'string'},{'internalType':'string','name':'email','type':'string'},{'internalType':'string','name':'title','type':'string'},{'internalType':'string','name':'firstname','type':'string'},{'internalType':'string','name':'lastname','type':'string'},{'internalType':'string','name':'dob','type':'string'},{'internalType':'string','name':'playeraddr','type':'string'},{'internalType':'uint32','name':'karma','type':'uint32'}],'internalType':'structOASIS.account','name':'','type':'tuple'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'totalAccounts','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'string','name':'userid','type':'string'},{'internalType':'string','name':'name','type':'string'},{'internalType':'string','name':'providerkey','type':'string'},{'internalType':'string','name':'password','type':'string'},{'internalType':'string','name':'email','type':'string'},{'internalType':'string','name':'title','type':'string'},{'internalType':'string','name':'firstname','type':'string'},{'internalType':'string','name':'lastname','type':'string'},{'internalType':'string','name':'dob','type':'string'},{'internalType':'string','name':'playeraddr','type':'string'},{'internalType':'uint32','name':'karma','type':'uint32'}],'name':'updateAccount','outputs':[{'internalType':'bool','name':'','type':'bool'}],'stateMutability':'nonpayable','type':'function'}]";

            var account = new Account("0x96a2cdfa6e4198b683188e0265d548da23a829f00fdf4858f9b7899e5d55fd4c");
            Web3 web3 = new Web3(account, url);
            Contract oasisContract = web3.Eth.GetContract(ABI, address);

            account newAccount = new account();

            Console.Write("User Id: ");
            newAccount.userid = Console.ReadLine();
            Console.Write("Name: ");
            newAccount.name = Console.ReadLine();
            Console.Write("Provider Key: ");
            newAccount.providerkey = Console.ReadLine();
            Console.Write("Password: ");
            newAccount.password = Console.ReadLine();
            Console.Write("Email: ");
            newAccount.email = Console.ReadLine();
            Console.Write("Title: ");
            newAccount.title = Console.ReadLine();
            Console.Write("First Name: ");
            newAccount.firstname = Console.ReadLine();
            Console.Write("Last Name: ");
            newAccount.lastname = Console.ReadLine();
            Console.Write("Date of Birth: ");
            newAccount.dob = Console.ReadLine();
            Console.Write("Player Address: ");
            newAccount.playeraddr = Console.ReadLine();
            Console.Write("Karma: ");
            newAccount.karma = Convert.ToInt32(Console.ReadLine());

            var newAccountFunction = oasisContract.GetFunction("createAccount");

            object[] accParams = new object[]{
                newAccount.userid,
                newAccount.name,
                newAccount.providerkey,
                newAccount.password,
                newAccount.email,
                newAccount.title,
                newAccount.firstname,
                newAccount.lastname,
                newAccount.dob,
                newAccount.playeraddr,
                newAccount.karma
            };

            // await newAccountFunction.CallAsync<int>(accParams);

            var getAccountCountFunction = oasisContract.GetFunction("getAccountCount");
            var totalAccount = getAccountCountFunction.CallAsync<int>();

            Console.Write("Account Creation Succeed, Count: ", totalAccount);
        }
        public List<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public List<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<ISearchResults> SearchAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }
    }
}
