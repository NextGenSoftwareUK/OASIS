using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using EOSNewYork.EOSCore.ActionArgs;
using EOSNewYork.EOSCore.Response.API;
using EOSNewYork.EOSCore.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
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
        private AvatarManager _avatarManager = null;
        private EOSIOOASIS.EOSIOOASIS _eosioOaisis = null;

        private AvatarManager AvatarManagerInstance
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

        public async Task<Account> GetTelosAccountAsync(string telosAccountName)
        {
            var account = await _eosioOaisis.ChainAPI.GetAccountAsync(telosAccountName);
            return account;
        }

        public Account GetTelosAccount(string telosAccountName)
        {
            var account = _eosioOaisis.ChainAPI.GetAccount(telosAccountName);
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

        public async Task<string> GetBalanceAsync(string telosAccountName)
        {
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/balancecheck.js
            //eos.getCurrencyBalance("token.seeds", account, 'SEEDS')

            //var accountBalance = tableAPI.GetTokenAccountBalance(new GetTokenAccountBalanceConstructorSettings() { accountName = "wozzawozza11", tokenContract = "epraofficial" });
            //Console.WriteLine(accountBalance[0].balance_decimal);
            //Console.WriteLine(accountBalance[0].symbol);

            //var currencyBalance = await _eosioOaisis.ChainAPI.GetCurrencyBalanceAsync(telosAccountName, "seeds.seeds", "SEEDS");
            var currencyBalance = await _eosioOaisis.ChainAPI.GetCurrencyBalanceAsync(telosAccountName, "token.seeds", "SEEDS");
            return currencyBalance.balances[0];
        }

        public string GetBalance(string telosAccountName)
        {
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/balancecheck.js
            //eos.getCurrencyBalance("token.seeds", account, 'SEEDS')

            var currencyBalance =  _eosioOaisis.ChainAPI.GetCurrencyBalance(telosAccountName, "token.seeds", "SEEDS");
            return currencyBalance.balances[0];
        }

        public string GetBalanceForAvatar(Guid avatarId)
        {
            return GetBalance(GetTelosAccountNameForAvatar(avatarId));
        }

        public string PayWithSeeds(string fromTelosAccountName, string toTelosAccountName, float qty, string memo)
        {
            return PayWithSeeds(fromTelosAccountName, toTelosAccountName, string.Concat(qty, " SEEDS"), memo);
        }

        public string PayWithSeeds(string fromTelosAccountName, string toTelosAccountName, string qty, string memo)
        {
            //Use standard TELOS/EOS Token API.Use Transfer action.
            //https://developers.eos.io/manuals/eosjs/latest/basic-usage/browser

            //string _code = "eosio.token", _action = "transfer", _memo = "";
            //TransferArgs _args = new TransferArgs() { from = "yatendra1", to = "yatendra1", quantity = "1.0000 EOS", memo = _memo };
            //var abiJsonToBin = chainAPI.GetAbiJsonToBin(_code, _action, _args);
            //logger.Info("For code {0}, action {1}, args {2} and memo {3} recieved bin {4}", _code, _action, _args, _memo, abiJsonToBin.binargs);

            //var abiBinToJson = chainAPI.GetAbiBinToJson(_code, _action, abiJsonToBin.binargs);
            //logger.Info("Received args json {0}", JsonConvert.SerializeObject(abiBinToJson.args));


            //TransferArgs args = new TransferArgs() { from = fromTelosAccountName, to = toTelosAccountName, quantity = "1.0000 EOS", memo = memo };
            TransferArgs args = new TransferArgs() { from = fromTelosAccountName, to = toTelosAccountName, quantity = qty, memo = memo };
            // var abiJsonToBin = _eosioOaisis.ChainAPI.GetAbiJsonToBin("eosio.token", "transfer", args);

            //prepare action object
            //EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("transfer", fromTelosAccountName, "active", "eosio.token", args);
            //EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("transfer", fromTelosAccountName, "active", "seed.seeds", args);
            EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("transfer", fromTelosAccountName, "active", "token.seeds", args);

            var keypair = KeyManager.GenerateKeyPair();
            //List<string> privateKeysInWIF = new List<string> { keypair.PrivateKey }; //TODO: Set Private Key
            List<string> privateKeysInWIF = new List<string> { "5KW2jynm7kHw9XfAJ6WKPFk9xfP6rrDB6ggpuk48i3sTZCwVnz4" }; //TODO: Set Private Key

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

        public string PayWithSeeds(Guid fromAvatarId, Guid toAvatarId, float qty, string memo, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            AvatarManagerInstance.AddKarmaToAvatar(fromAvatarId, KarmaTypePositive.BeASuperHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, ProviderType.SEEDSOASIS);
            AddKarmaForSeeds(fromAvatarId, KarmaTypePositive.PayWithSeeds, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc);
            return PayWithSeeds(GetTelosAccountNameForAvatar(fromAvatarId), GetTelosAccountNameForAvatar(toAvatarId), qty, memo);
        }
 
        public string DonateWithSeeds(string fromTelosAccount, string toTelosAccount, float qty, string memo)
        {
            return PayWithSeeds(fromTelosAccount, toTelosAccount, qty, memo);
        }

        public string DonateWithSeeds(Guid fromAvatarId, Guid toAvatarId, float qty, string memo, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            AvatarManagerInstance.AddKarmaToAvatar(fromAvatarId, KarmaTypePositive.BeASuperHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, ProviderType.SEEDSOASIS);
            AddKarmaForSeeds(fromAvatarId, KarmaTypePositive.DonateWithSeeds, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc);
            return PayWithSeeds(GetTelosAccountNameForAvatar(fromAvatarId), GetTelosAccountNameForAvatar(toAvatarId), qty, memo);
        }

        public string RewardWithSeeds(string fromTelosAccountName, string toTelosAccountName, float qty, string memo)
        {
            return PayWithSeeds(fromTelosAccountName, toTelosAccountName, qty, memo);
        }

        public string RewardWithSeeds(Guid fromAvatarId, Guid toAvatarId, float qty, string memo, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            AvatarManagerInstance.AddKarmaToAvatar(fromAvatarId, KarmaTypePositive.BeAHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, ProviderType.SEEDSOASIS);
            AddKarmaForSeeds(fromAvatarId, KarmaTypePositive.RewardWithSeeds, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc);
            return PayWithSeeds(GetTelosAccountNameForAvatar(fromAvatarId), GetTelosAccountNameForAvatar(toAvatarId), qty, memo);
        }

        public SendInviteResult SendInviteToJoinSeeds(string sponsorTelosAccountName, string referrerTelosAccountName, float transferQuantitiy, float sowQuantitiy)
        {
            //https://joinseeds.github.io/seeds-smart-contracts/onboarding.html
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/onboarding-helper.js
           
            string randomHex = GetRandomHexNumber(64); //16
            string inviteHash = GetSHA256Hash(randomHex);
            var keypair = KeyManager.GenerateKeyPair();
            List<string> privateKeysInWIF = new List<string> { keypair.PrivateKey };

            EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("invitefor", sponsorTelosAccountName, "active", "join.seeds", new Invite() { sponsor = sponsorTelosAccountName, referrer = referrerTelosAccountName,  invite_hash = inviteHash, transfer_quantity = transferQuantitiy, sow_quantity = sowQuantitiy });
            var transactionResult = _eosioOaisis.ChainAPI.PushTransaction(new[] { action }, privateKeysInWIF);

            return new SendInviteResult() { TransactionId = transactionResult.transaction_id, InviteSecret = inviteHash };
        }

        public SendInviteResult SendInviteToJoinSeeds(Guid sponsorAvatarId, Guid referrerAvatarId, float transferQuantitiy, float sowQuantitiy, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            AvatarManagerInstance.AddKarmaToAvatar(sponsorAvatarId, KarmaTypePositive.BeASuperHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, ProviderType.SEEDSOASIS);
            AddKarmaForSeeds(sponsorAvatarId, KarmaTypePositive.SendInviteToJoinSeeds, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc);
            return SendInviteToJoinSeeds(GetTelosAccountNameForAvatar(sponsorAvatarId), GetTelosAccountNameForAvatar(referrerAvatarId), transferQuantitiy, sowQuantitiy);
        }

        public string AcceptInviteToJoinSeeds(string telosAccountName, string inviteSecret)
        {
            //https://joinseeds.github.io/seeds-smart-contracts/onboarding.html
            //inviteSecret = inviteHash

            var keypair = KeyManager.GenerateKeyPair();
            List<string> privateKeysInWIF = new List<string> { keypair.PrivateKey };

            EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("accept", telosAccountName, "active", "join.seeds", new Accept() { account = telosAccountName, invite_secret = inviteSecret, publicKey = keypair.PublicKey });
            var transactionResult = _eosioOaisis.ChainAPI.PushTransaction(new[] { action }, privateKeysInWIF);

            return transactionResult.transaction_id;
        }

        public string AcceptInviteToJoinSeeds(Guid avatarId, string inviteSecret, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            AvatarManagerInstance.AddKarmaToAvatar(avatarId, KarmaTypePositive.BeAHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, ProviderType.SEEDSOASIS);
            AddKarmaForSeeds(avatarId, KarmaTypePositive.AcceptInviteToJoinSeeds, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc);
            return AcceptInviteToJoinSeeds(GetTelosAccountNameForAvatar(avatarId), inviteSecret);
        }

        public bool AddKarmaForSeeds(Guid avatarId, KarmaTypePositive seedsKarmaType, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc)
        {
            if (!ProviderManager.IsProviderRegistered(ProviderType.EOSOASIS))
            {
                throw new Exception("EOSIOOASIS Provider Not Registered. Please register and try again.");
            }

            AvatarManagerInstance.AddKarmaToAvatar(avatarId, seedsKarmaType, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, ProviderType.SEEDSOASIS);
            return true;
        }

        public string GetTelosAccountNameForAvatar(Guid avatarId)
        {
            return AvatarManagerInstance.LoadAvatar(avatarId).ProviderKey[ProviderType.TelosOASIS];
        }

        public Account GetTelosAccountForAvatar(Guid avatarId)
        {
            return GetTelosAccount(GetTelosAccountNameForAvatar(avatarId));
        }

        public Guid GetAvatarIdForTelosAccountName(string telosAccountName)
        {
            return AvatarManagerInstance.LoadAllAvatars().FirstOrDefault(x => x.ProviderKey[ProviderType.TelosOASIS] == telosAccountName).Id;
        }

        public IAvatar GetAvatarForTelosAccountName(string telosAccountName)
        {
            return AvatarManagerInstance.LoadAllAvatars().FirstOrDefault(x => x.ProviderKey[ProviderType.TelosOASIS] == telosAccountName);
        }

        public string GenerateSignInQRCode(string telosAccountName)
        {
            //https://github.com/JoinSEEDS/encode-transaction-service/blob/master/buildTransaction.js
            return "";
        }

        public string GenerateSignInQRCodeForAvatar(Guid avatarId)
        {
            return GenerateSignInQRCode(GetTelosAccountNameForAvatar(avatarId));
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
