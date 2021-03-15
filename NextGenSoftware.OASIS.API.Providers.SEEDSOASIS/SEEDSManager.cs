using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using EOSNewYork.EOSCore;
using EOSNewYork.EOSCore.ActionArgs;
using EOSNewYork.EOSCore.Params;
using EOSNewYork.EOSCore.Response.API;
using EOSNewYork.EOSCore.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.EOSIOClasses;


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

        private ChainAPI _chainAPI = new ChainAPI(ENDPOINT_TEST);

        public SEEDSManager()
        {
            
        }

        public async Task<string> GetUserAsync(string user)
        {
            var rows = await _chainAPI.GetTableRowsAsync("accounts", "accounts", "users", "true", user, user, 1, 3);

            if (rows.rows.Count == 0)
                return "";

            var row = (EOSIOAccountTableRow)rows.rows[0];
            //var Avatar = AvatarRow.ToAvatar();
           // Avatar.Password = StringCipher.Decrypt(Avatar.Password, OASIS_PASS_PHRASE);

            return "";
        }

        public string GetUser(string user)
        {
            //TODO: Finish this...
            var rows = _chainAPI.GetTableRows("accounts", "accounts", "users", "true", user, user, 1, 3);

            if (rows.rows.Count == 0)
                return "";

            var row = (EOSIOAccountTableRow)rows.rows[0];
            //var Avatar = AvatarRow.ToAvatar();
            // Avatar.Password = StringCipher.Decrypt(Avatar.Password, OASIS_PASS_PHRASE);



            var account = _chainAPI.GetAccount("yatendra1");
          //  logger.Info("{0} is currently the returned account name", account.account_name);
            string json = JsonConvert.SerializeObject(account);
           // logger.Info("{0}", json);


            return "";
        }

        public async Task<TableRows> GetAllOrganisationsAsync()
        {
            TableRows rows = await _chainAPI.GetTableRowsAsync("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return rows;
        }

        public TableRows GetAllOrganisations()
        {
            TableRows rows = _chainAPI.GetTableRows("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return rows;
        }

        public async Task<string> GetAllOrganisationsAsJSONAsync()
        {
            TableRows rows = await _chainAPI.GetTableRowsAsync("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return JsonConvert.SerializeObject(rows);
        }

        public string GetAllOrganisationsAsJSON()
        {
            TableRows rows = _chainAPI.GetTableRows("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return JsonConvert.SerializeObject(rows);
        }

        public async Task<string> GetBalanceAsync(string account)
        {
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/balancecheck.js
            //eos.getCurrencyBalance("token.seeds", account, 'SEEDS')

            //var accountBalance = tableAPI.GetTokenAccountBalance(new GetTokenAccountBalanceConstructorSettings() { accountName = "wozzawozza11", tokenContract = "epraofficial" });
            //Console.WriteLine(accountBalance[0].balance_decimal);
            //Console.WriteLine(accountBalance[0].symbol);

            var currencyBalance = await _chainAPI.GetCurrencyBalanceAsync(account, "token.seeds", "SEEDS");
            return currencyBalance.balances[0];
        }

        public string GetBalance(string account)
        {
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/balancecheck.js
            //eos.getCurrencyBalance("token.seeds", account, 'SEEDS')

            var currencyBalance =  _chainAPI.GetCurrencyBalance(account, "token.seeds", "SEEDS");
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
            var abiJsonToBin = _chainAPI.GetAbiJsonToBin("eosio.token", "transfer", args);

            //prepare action object
            EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("transfer", fromAccount, "active", "eosio.token", args);

            List<string> privateKeysInWIF = new List<string> { "" }; //TODO: Set Private Key

            //push transaction
            var transactionResult = _chainAPI.PushTransaction(new[] { action }, privateKeysInWIF);
            // logger.Info(transactionResult.transaction_id);

            //transactionResult.processed
            return transactionResult.transaction_id;


            // string accountName = "eosio";
            //var abi = _chainAPI.GetAbi(accountName);

            //abi.abi.actions[0].
            //abi.abi.tables

            //logger.Info("For account {0} recieved abi {1}", accountName, JsonConvert.SerializeObject(abi));
        }
        public void DonateSeeds()
        {
            //Use standard TELOS/EOS Token API.Use Transfer action.
            //https://developers.eos.io/manuals/eosjs/latest/basic-usage/browser
        }

        public void JoinSeeds(string sponsor, string inviteHash, int transferQuantitiy, int sowQuantitiy)
        {
            //https://joinseeds.github.io/seeds-smart-contracts/onboarding.html

            var abiJsonToBin = _chainAPI.GetAbiJsonToBin("eosio.token", "invite", new Invite() { sponsor = sponsor, invite_hash = inviteHash, transfer_quantity = transferQuantitiy, sow_quantity = sowQuantitiy });
        }

        public void GenerateSignInQRCode()
        {
            //https://github.com/JoinSEEDS/encode-transaction-service/blob/master/buildTransaction.js
        }
    }
}
