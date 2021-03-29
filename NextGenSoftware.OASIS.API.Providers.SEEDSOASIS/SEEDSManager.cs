using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using EOSNewYork.EOSCore;
using EOSNewYork.EOSCore.ActionArgs;
using EOSNewYork.EOSCore.Response.API;
using EOSNewYork.EOSCore.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.EOSIOClasses;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.ParamObjects;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS
{
    public class SEEDSManager : IOASISCustomProviderManager
    {
        public const string ENDPOINT_TEST = "https://test.hypha.earth";
        public const string ENDPOINT_LIVE = "https://node.hypha.earth";
        public const string SEEDS_EOSIO_ACCOUNT_TEST = "seeds";
        public const string SEEDS_EOSIO_ACCOUNT_LIVE = "seed.seeds";
        public const string CHAINID_TEST = "1eaa0824707c8c16bd25145493bf062aecddfeb56c736f6ba6397f3195f33c9f";
        public const string CHAINID_LIVE = "4667b205c6838ef70ff7988f6e8257e8be0e1284a2f59699054a018f743b1d11";
        public const string PUBLICKEY_TEST = "EOS8MHrY9xo9HZP4LvZcWEpzMVv1cqSLxiN2QMVNy8naSi1xWZH29";
        public const string PUBLICKEY_TEST2 = "EOS8C9tXuPMkmB6EA7vDgGtzA99k1BN6UxjkGisC1QKpQ6YV7MFqm";
        public const string PUBLICKEY_LIVE = "EOS6kp3dm9Ug5D3LddB8kCMqmHg2gxKpmRvTNJ6bDFPiop93sGyLR";
        public const string PUBLICKEY_LIVE2 = "EOS6kp3dm9Ug5D3LddB8kCMqmHg2gxKpmRvTNJ6bDFPiop93sGyLR";
        public const string APIKEY_TEST = "EOS7YXUpe1EyMAqmuFWUheuMaJoVuY3qTD33WN4TrXbEt8xSKrdH9";
        public const string APIKEY_LIVE = "EOS7YXUpe1EyMAqmuFWUheuMaJoVuY3qTD33WN4TrXbEt8xSKrdH9";

        private static Random _random = new Random();
        //private ChainAPI _eosioOaisis.ChainAPI = new ChainAPI(ENDPOINT_TEST);
        //AvatarManager _avatarManager = new AvatarManager(_eosioOasis);
        private AvatarManager _avatarManager = null;
        private EOSIOOASIS.EOSIOOASIS _eosioOaisis = null;

        private AvatarManager AvatarManager
        {
            get
            {
                if (_avatarManager == null && _eosioOaisis != null)
                    _avatarManager = new AvatarManager(_eosioOaisis);

                return _avatarManager;
            }
        }

        public SEEDSManager(EOSIOOASIS.EOSIOOASIS eosioOaisis)
        {
            _eosioOaisis = eosioOaisis;
        }

        public async Task<string> GetUserAsync(string user)
        {
            var rows = await _eosioOaisis.ChainAPI.GetTableRowsAsync("accounts", "accounts", "users", "true", user, user, 1, 3);

            if (rows.rows.Count == 0)
                return "";

            var row = (EOSIOAccountTableRow)rows.rows[0];
            //var Avatar = AvatarRow.ToAvatar();
           // Avatar.Password = StringCipher.Decrypt(Avatar.Password, OASIS_PASS_PHRASE);

            return "";
        }

        public Account GetUser(string user)
        {
            //TODO: Finish this...
          //  var rows = _eosioOaisis.ChainAPI.GetTableRows("accounts", "accounts", "users", "true", user, user, 1, 3);

           // if (rows.rows.Count == 0)
            //    return "";

           // var row = (EOSIOAccountTableRow)rows.rows[0];
            //var Avatar = AvatarRow.ToAvatar();
            // Avatar.Password = StringCipher.Decrypt(Avatar.Password, OASIS_PASS_PHRASE);

            var account = _eosioOaisis.ChainAPI.GetAccount(user);
          //  logger.Info("{0} is currently the returned account name", account.account_name);
            string json = JsonConvert.SerializeObject(account);
            // logger.Info("{0}", json);

            return account;
        }

        public async Task<TableRows> GetAllOrganisationsAsync()
        {
            TableRows rows = await _eosioOaisis.ChainAPI.GetTableRowsAsync("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return rows;
        }

        public TableRows GetAllOrganisations()
        {
            TableRows rows = _eosioOaisis.ChainAPI.GetTableRows("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return rows;
        }

        public async Task<string> GetAllOrganisationsAsJSONAsync()
        {
            TableRows rows = await _eosioOaisis.ChainAPI.GetTableRowsAsync("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return JsonConvert.SerializeObject(rows);
        }

        public string GetAllOrganisationsAsJSON()
        {
            TableRows rows = _eosioOaisis.ChainAPI.GetTableRows("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return JsonConvert.SerializeObject(rows);
        }

        public async Task<string> GetBalanceAsync(string account)
        {
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/balancecheck.js
            //eos.getCurrencyBalance("token.seeds", account, 'SEEDS')

            //var accountBalance = tableAPI.GetTokenAccountBalance(new GetTokenAccountBalanceConstructorSettings() { accountName = "wozzawozza11", tokenContract = "epraofficial" });
            //Console.WriteLine(accountBalance[0].balance_decimal);
            //Console.WriteLine(accountBalance[0].symbol);

            var currencyBalance = await _eosioOaisis.ChainAPI.GetCurrencyBalanceAsync(account, "token.seeds", "SEEDS");
            return currencyBalance.balances[0];
        }

        public string GetBalance(string account)
        {
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/balancecheck.js
            //eos.getCurrencyBalance("token.seeds", account, 'SEEDS')

            var currencyBalance =  _eosioOaisis.ChainAPI.GetCurrencyBalance(account, "token.seeds", "SEEDS");
            return currencyBalance.balances[0];
        }


        public string PayWithSeeds(string fromAccount, string toAccount, int qty, string memo)
        {
            //Use standard TELOS/EOS Token API.Use Transfer action.
            //https://developers.eos.io/manuals/eosjs/latest/basic-usage/browser

            //string _code = "eosio.token", _action = "transfer", _memo = "";
            //TransferArgs _args = new TransferArgs() { from = "yatendra1", to = "yatendra1", quantity = "1.0000 EOS", memo = _memo };
            //var abiJsonToBin = chainAPI.GetAbiJsonToBin(_code, _action, _args);
            //logger.Info("For code {0}, action {1}, args {2} and memo {3} recieved bin {4}", _code, _action, _args, _memo, abiJsonToBin.binargs);

            //var abiBinToJson = chainAPI.GetAbiBinToJson(_code, _action, abiJsonToBin.binargs);
            //logger.Info("Received args json {0}", JsonConvert.SerializeObject(abiBinToJson.args));


            TransferArgs args = new TransferArgs() { from = fromAccount, to = toAccount, quantity = "1.0000 EOS", memo = memo };
           // var abiJsonToBin = _eosioOaisis.ChainAPI.GetAbiJsonToBin("eosio.token", "transfer", args);

            //prepare action object
            EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("transfer", fromAccount, "active", "eosio.token", args);

            var keypair = KeyManager.GenerateKeyPair();
            List<string> privateKeysInWIF = new List<string> { keypair.PrivateKey }; //TODO: Set Private Key

            
            

            //push transaction
            var transactionResult = _eosioOaisis.ChainAPI.PushTransaction(new[] { action }, privateKeysInWIF);
            // logger.Info(transactionResult.transaction_id);

            //transactionResult.processed
            return transactionResult.transaction_id;


            // string accountName = "eosio";
            //var abi = _eosioOaisis.ChainAPI.GetAbi(accountName);

            //abi.abi.actions[0].
            //abi.abi.tables

            //logger.Info("For account {0} recieved abi {1}", accountName, JsonConvert.SerializeObject(abi));
        }

        public string PayWithSeeds(Guid fromAvatarId, Guid toAvatarId, int qty, string memo, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            AvatarManager.AddKarmaToAvatar(fromAvatarId, KarmaTypePositive.BeASuperHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, ProviderType.SEEDSOASIS);
            AddKarmaForSeeds(fromAvatarId, toAvatarId, KarmaTypePositive.PayWithSeeds, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc);

            IAvatar fromAvatar = AvatarManager.LoadAvatar(fromAvatarId);
            IAvatar toAvatar = AvatarManager.LoadAvatar(fromAvatarId);
            return PayWithSeeds(fromAvatar.ProviderKey, toAvatar.ProviderKey, qty, memo);
        }
    }

        public string DonateSeeds(string fromTelosAccount, string toTelosAccount, int qty, string memo)
        {
            return PayWithSeeds(fromTelosAccount, toTelosAccount, qty, memo);
        }

        public string DonateSeeds(Guid fromAvatarId, Guid toAvatarId, int qty, string memo, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            AvatarManager.AddKarmaToAvatar(fromAvatarId, KarmaTypePositive.BeASuperHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, ProviderType.SEEDSOASIS);
            AddKarmaForSeeds(fromAvatarId, toAvatarId, KarmaTypePositive.DonateWithSeeds, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc);

            IAvatar fromAvatar = AvatarManager.LoadAvatar(fromAvatarId);
            IAvatar toAvatar = AvatarManager.LoadAvatar(fromAvatarId);
            return PayWithSeeds(fromAvatar.ProviderKey, toAvatar.ProviderKey, qty, memo);
        }

        private bool AddKarmaForSeeds(Guid fromAvatarId, Guid toAvatarId, KarmaTypePositive seedsKarmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDesc)
        {
            if (!ProviderManager.IsProviderRegistered(ProviderType.EOSOASIS))
            {
                throw new Exception("EOSIOOASIS Provider Not Registered. Please register and try again.")
            }

            //AvatarManager.AddKarmaToAvatar(fromAvatarId.Id, KarmaTypePositive.BeAHero);
            AvatarManager.AddKarmaToAvatar(fromAvatarId, seedsKarmaType, karmaSourceType, karmaSourceTitle, karmaSourceDesc, ProviderType.SEEDSOASIS);
            return true;
        }

        public string RewardWithSeeds(string fromAccount, string toAccount, int qty, string memo)
        {
            return PayWithSeeds(fromAccount, toAccount, qty, memo);
        }

        public string RewardWithSeeds(Guid fromAvatarId, Guid toAvatarId, int qty, string memo, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            AvatarManager.AddKarmaToAvatar(fromAvatarId, KarmaTypePositive.BeASuperHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, ProviderType.SEEDSOASIS);
            AddKarmaForSeeds(fromAvatarId, toAvatarId, KarmaTypePositive.RewardWithSeeds, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc);

            IAvatar fromAvatar = AvatarManager.LoadAvatar(fromAvatarId);
            IAvatar toAvatar = AvatarManager.LoadAvatar(fromAvatarId);
            return PayWithSeeds(fromAvatar.ProviderKey, toAvatar.ProviderKey, qty, memo);
    }

        public SendInviteResult SendInviteToJoinSeeds(string sponsor, string referrer, int transferQuantitiy, int sowQuantitiy)
        {
            //https://joinseeds.github.io/seeds-smart-contracts/onboarding.html
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/onboarding-helper.js
           
            string randomHex = GetRandomHexNumber(64); //16
            string inviteHash = GetSHA256Hash(randomHex);
            var keypair = KeyManager.GenerateKeyPair();
            List<string> privateKeysInWIF = new List<string> { keypair.PrivateKey };

            EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("invitefor", sponsor, "active", "join.seeds", new Invite() { sponsor = sponsor, referrer = referrer,  invite_hash = inviteHash, transfer_quantity = transferQuantitiy, sow_quantity = sowQuantitiy });
            var transactionResult = _eosioOaisis.ChainAPI.PushTransaction(new[] { action }, privateKeysInWIF);

            return new SendInviteResult() { TransactionId = transactionResult.transaction_id, InviteSecret = inviteHash };
        }

        public string AcceptInviteToJoinSeeds(string account, string inviteSecret)
        {
            //https://joinseeds.github.io/seeds-smart-contracts/onboarding.html
            //inviteSecret = inviteHash

            var keypair = KeyManager.GenerateKeyPair();
            List<string> privateKeysInWIF = new List<string> { keypair.PrivateKey };

            EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("accept", account, "active", "join.seeds", new Accept() { account = account, invite_secret = inviteSecret, publicKey = keypair.PublicKey });
            var transactionResult = _eosioOaisis.ChainAPI.PushTransaction(new[] { action }, privateKeysInWIF);

            return transactionResult.transaction_id;
        }

        public string GenerateSignInQRCode()
        {
            //https://github.com/JoinSEEDS/encode-transaction-service/blob/master/buildTransaction.js
            return "";
        }

        
        private static string GetRandomHexNumber(int digits)
        {
            byte[] buffer = new byte[digits / 2];
            _random.NextBytes(buffer);

            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());

            if (digits % 2 == 0)
                return result;

            return result + _random.Next(16).ToString("X");
        }

        private static string GetSHA256Hash(string value)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, value);
                
                /*
                Console.WriteLine($"The SHA256 hash of {value} is: {hash}.");
                Console.WriteLine("Verifying the hash...");

                if (VerifyHash(sha256Hash, value, hash))
                    Console.WriteLine("The hashes are the same.");
                else
                    Console.WriteLine("The hashes are not same.");
                */

                return hash;
            }
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        private static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            // Hash the input.
            var hashOfInput = GetHash(hashAlgorithm, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return comparer.Compare(hashOfInput, hash) == 0;
        }
    }
}
