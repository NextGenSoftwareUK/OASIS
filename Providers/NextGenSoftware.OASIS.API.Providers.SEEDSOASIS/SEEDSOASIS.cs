using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using EOSNewYork.EOSCore.ActionArgs;
using EOSNewYork.EOSCore.Response.API;
using EOSNewYork.EOSCore.Utilities;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.Membranes;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS
{
    public class SEEDSOASIS : OASISProvider, IOASISApplicationProvider
    {
        private static Random _random = new Random();
        private AvatarManager _avatarManager = null;
        private KeyManager _keyManager = null;

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

        public TelosOASIS.TelosOASIS TelosOASIS { get; }


        private AvatarManager AvatarManager
        {
            get
            {
                if (_avatarManager == null)
                {
                    if (TelosOASIS != null)
                        _avatarManager = new AvatarManager(TelosOASIS);
                    else
                    {
                        if (!ProviderManager.Instance.IsProviderRegistered(Core.Enums.ProviderType.TelosOASIS))
                            throw new Exception("TelosOASIS Provider Not Registered. Please register and try again.");
                        else
                            throw new Exception("TelosOASIS Provider Is Registered But Was Not Injected Into SEEDSOASIS Provider.");
                    }
                }

                return _avatarManager;
            }
        }

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(this, AvatarManager);

                return _keyManager;
            }
        }

        public SEEDSOASIS(TelosOASIS.TelosOASIS telosOASIS)
       // public SEEDSOASIS(string telosConnectionString)
        {
            this.ProviderName = "SEEDSOASIS";
            this.ProviderDescription = "SEEDS Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SEEDSOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Application);
            TelosOASIS = telosOASIS;

           // TelosOASIS = new TelosOASIS.TelosOASIS(telosConnectionString);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            if (!TelosOASIS.IsProviderActivated)
                await TelosOASIS.ActivateProviderAsync();

            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> ActivateProvider()
        {
            if (!TelosOASIS.IsProviderActivated)
                TelosOASIS.ActivateProvider();

            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            if (TelosOASIS.IsProviderActivated)
                await TelosOASIS.DeActivateProviderAsync();

            _keyManager = null;
            _avatarManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            if (TelosOASIS.IsProviderActivated)
                TelosOASIS.DeActivateProvider();

            _keyManager = null;
            _avatarManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public async Task<string> GetBalanceAsync(string telosAccountName)
        {
            return await TelosOASIS.GetBalanceAsync(telosAccountName, "token.seeds", "SEEDS");
        }

        public string GetBalanceForTelosAccount(string telosAccountName)
        {
            return TelosOASIS.GetBalanceForTelosAccount(telosAccountName, "token.seeds", "SEEDS");
        }

        public string GetBalanceForAvatar(Guid avatarId)
        {
            return TelosOASIS.GetBalanceForAvatar(avatarId, "token.seeds", "SEEDS");
        }

        public async Task<TableRows> GetAllOrganisationsAsync()
        {
            TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return rows;
        }

        public TableRows GetAllOrganisations()
        {
            TableRows rows = TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRows("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return rows;
        }

        public string GetOrganisation(string orgName)
        {
            TableRows rows = TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRows("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);

            //TODO: Come back to this...
            //for (int i = 0; i < rows.rows.Count; i++)
            //{
            //    int orgNameBegins = rows.rows[i].ToString().IndexOf("org_name");
            //    string orgName = rows.rows[i].ToString().Substring(orgNameBegins + 10, 12);
            //}

            string json = JsonConvert.SerializeObject(rows);

            

            //rows.rows.Where(x => x.)

            return JsonConvert.SerializeObject(rows);
        }

        public async Task<string> GetAllOrganisationsAsJSONAsync()
        {
            TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return JsonConvert.SerializeObject(rows);
        }

        public string GetAllOrganisationsAsJSON()
        {
            TableRows rows = TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRows("orgs.seeds", "orgs.seeds", "organization", "true", 0, -1, 99999);
            return JsonConvert.SerializeObject(rows);
        }

        public OASISResult<string> PayWithSeedsUsingTelosAccount(string fromTelosAccountName, string fromTelosAccountPrivateKey, string toTelosAccountName, int quanitity, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null, string memo = null)
        {
            return PayWithSeeds(fromTelosAccountName, fromTelosAccountPrivateKey, toTelosAccountName, quanitity, KarmaTypePositive.PayWithSeeds, KarmaTypePositive.BeAHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink, memo);
        }

        public OASISResult<string> PayWithSeedsUsingAvatar(Guid fromAvatarId, Guid toAvatarId, int quanitity, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null, string memo = null)
        {
            //TODO: Add support for multiple accounts later.
            return PayWithSeedsUsingTelosAccount(TelosOASIS.GetTelosAccountNamesForAvatar(fromAvatarId)[0], TelosOASIS.GetTelosAccountPrivateKeyForAvatar(toAvatarId), TelosOASIS.GetTelosAccountNamesForAvatar(toAvatarId)[0], quanitity, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink, memo);
        }

        public OASISResult<string> DonateWithSeedsUsingTelosAccount(string fromTelosAccountName, string fromTelosAccountPrivateKey, string toTelosAccountName, int quanitity, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null, string memo = null)
        {
            return PayWithSeeds(fromTelosAccountName, fromTelosAccountPrivateKey, toTelosAccountName, quanitity, KarmaTypePositive.DonateWithSeeds, KarmaTypePositive.BeASuperHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink, memo);
        }

        public OASISResult<string> DonateWithSeedsUsingAvatar(Guid fromAvatarId, Guid toAvatarId, int quanitity, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null, string memo = null)
        {
            //TODO: Add support for multiple accounts later.
            return DonateWithSeedsUsingTelosAccount(TelosOASIS.GetTelosAccountNamesForAvatar(fromAvatarId)[0], TelosOASIS.GetTelosAccountPrivateKeyForAvatar(toAvatarId), TelosOASIS.GetTelosAccountNamesForAvatar(toAvatarId)[0], quanitity, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink, memo);
        }

        public OASISResult<string> RewardWithSeedsUsingTelosAccount(string fromTelosAccountName, string fromTelosAccountPrivateKey, string toTelosAccountName, int quanitity, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null, string memo = null)
        {
            return PayWithSeeds(fromTelosAccountName, fromTelosAccountPrivateKey, toTelosAccountName, quanitity, KarmaTypePositive.RewardWithSeeds, KarmaTypePositive.BeASuperHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink, memo);
        }

        public OASISResult<string> RewardWithSeedsUsingAvatar(Guid fromAvatarId, Guid toAvatarId, int quanitity, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null, string memo = null)
        {
            //TODO: Add support for multiple accounts later.
            return RewardWithSeedsUsingTelosAccount(TelosOASIS.GetTelosAccountNamesForAvatar(fromAvatarId)[0], TelosOASIS.GetTelosAccountPrivateKeyForAvatar(toAvatarId), TelosOASIS.GetTelosAccountNamesForAvatar(toAvatarId)[0], quanitity, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink, memo);
        }

        public OASISResult<SendInviteResult> SendInviteToJoinSeedsUsingTelosAccount(string sponsorTelosAccountName, string sponsorTelosAccountNamePrivateKey, string referrerTelosAccountName, int transferQuantitiy, int sowQuantitiy, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null)
        {
            OASISResult<SendInviteResult> result = new OASISResult<SendInviteResult>();

            try
            {
                result.Result = SendInviteToJoinSeeds(sponsorTelosAccountName, sponsorTelosAccountNamePrivateKey, referrerTelosAccountName, transferQuantitiy, sowQuantitiy);
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = string.Concat("Error occured pushing the transaction onto the EOSIO chain. Error Message: ", ex.ToString());
                OASISErrorHandling.HandleError(ref result, result.Message);
            }

            // If there was no error then now add the karma.
            if (!result.IsError && !string.IsNullOrEmpty(result.Result.TransactionId))
            {
                try
                {
                    AddKarmaForSeeds(TelosOASIS.GetAvatarIdForTelosAccountName(sponsorTelosAccountName), KarmaTypePositive.SendInviteToJoinSeeds, KarmaTypePositive.BeAHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink);
                }
                catch (Exception ex)
                {
                    result.IsError = true;
                    result.Message = string.Concat("Error occured adding karma points to account ", sponsorTelosAccountName, ". Was attempting to add points for SendInviteToJoinSeeds & BeAHero. KarmaSource Type: ", Enum.GetName(receivingKarmaFor), ". Karma Source: ", appWebsiteServiceName, ", Karma Source Desc: ", appWebsiteServiceDesc, ", Website Link: ", appWebsiteServiceLink, ". Error Message: ", ex.ToString());
                    OASISErrorHandling.HandleError(ref result, result.Message);
                }
            }
            else
            {
                if (!result.IsError)
                {
                    result.IsError = true;
                    result.Message = "Unknown error occured pushing the transaction onto the EOSIO chain.";
                    OASISErrorHandling.HandleError(ref result, result.Message);
                }
            }

            return result;
        }

        public OASISResult<SendInviteResult> SendInviteToJoinSeedsUsingAvatar(Guid sponsorAvatarId, Guid referrerAvatarId, int transferQuantitiy, int sowQuantitiy, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null)
        {
            //TODO: Add support for multiple accounts later.
            return SendInviteToJoinSeedsUsingTelosAccount(TelosOASIS.GetTelosAccountNamesForAvatar(sponsorAvatarId)[0], TelosOASIS.GetTelosAccountPrivateKeyForAvatar(sponsorAvatarId), TelosOASIS.GetTelosAccountNamesForAvatar(referrerAvatarId)[0], transferQuantitiy, sowQuantitiy, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink);
        }

        public OASISResult<string> AcceptInviteToJoinSeedsUsingTelosAccount(string telosAccountName, string inviteSecret, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                result.Result = AcceptInviteToJoinSeeds(telosAccountName, inviteSecret);
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = string.Concat("Error occured pushing the transaction onto the EOSIO chain. Error Message: ", ex.ToString());
                OASISErrorHandling.HandleError(ref result, result.Message);
            }

            // If there was no error then now add the karma.
            if (!result.IsError && !string.IsNullOrEmpty(result.Result))
            {
                try
                {
                    AddKarmaForSeeds(TelosOASIS.GetAvatarIdForTelosAccountName(telosAccountName), KarmaTypePositive.AcceptInviteToJoinSeeds, KarmaTypePositive.BeAHero, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink);
                }
                catch (Exception ex)
                {
                    result.IsError = true;
                    result.Message = string.Concat("Error occured adding karma points to account ", telosAccountName, ". Was attempting to add points for AcceptInviteToJoinSeeds & BeAHero. KarmaSource Type: ", Enum.GetName(receivingKarmaFor), ". Karma Source: ", appWebsiteServiceName, ", Karma Source Desc: ", appWebsiteServiceDesc, ", Website Link: ", appWebsiteServiceLink, ". Error Message: ", ex.ToString());
                    OASISErrorHandling.HandleError(ref result, result.Message);
                }
            }
            else
            {
                if (!result.IsError)
                {
                    result.IsError = true;
                    result.Message = "Unknown error occured pushing the transaction onto the EOSIO chain.";
                    OASISErrorHandling.HandleError(ref result, result.Message);
                }
            }

            return result;
        }

        public OASISResult<string> AcceptInviteToJoinSeedsUsingAvatar(Guid avatarId, string inviteSecret, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null)
        {
            //TODO: Add support for multiple accounts later.
            return AcceptInviteToJoinSeedsUsingTelosAccount(TelosOASIS.GetTelosAccountNamesForAvatar(avatarId)[0], inviteSecret, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink);
        }

        public string GenerateSignInQRCode(string telosAccountName)
        {
            //https://github.com/JoinSEEDS/encode-transaction-service/blob/master/buildTransaction.js
            return "";
        }

        public string GenerateSignInQRCodeForAvatar(Guid avatarId)
        {
            //TODO: Add support for multiple accounts later.
            return GenerateSignInQRCode(TelosOASIS.GetTelosAccountNamesForAvatar(avatarId)[0]);
        }

        private OASISResult<string> PayWithSeeds(string fromTelosAccountName, string fromTelosAccountPrivateKey, string toTelosAccountName, int quanitity, KarmaTypePositive seedsKarmaType, KarmaTypePositive seedsKarmaHeroType, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null, string memo = null)
        {
            // TODO: Make generic and apply to all other calls...
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                result.Result = PayWithSeeds(fromTelosAccountName, fromTelosAccountPrivateKey, toTelosAccountName, quanitity, memo);
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = string.Concat("Error occured pushing the transaction onto the EOSIO chain. Error Message: ", ex.ToString());
                OASISErrorHandling.HandleError(ref result, result.Message);
            }

            // If there was no error then now add the karma.
            if (!result.IsError && !string.IsNullOrEmpty(result.Result))
            {
                try
                {
                    AddKarmaForSeeds(TelosOASIS.GetAvatarIdForTelosAccountName(fromTelosAccountName), seedsKarmaType, seedsKarmaHeroType, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink);
                }
                catch (Exception ex)
                {
                    result.IsError = true;
                    result.Message = string.Concat("Error occured adding karma points to account ", fromTelosAccountName, ". Was attempting to add points for ", Enum.GetName(seedsKarmaType), " & ", Enum.GetName(seedsKarmaHeroType), ". KarmaSource Type: ", Enum.GetName(receivingKarmaFor), ". Karma Source: ", appWebsiteServiceName, ", Karma Source Desc: ", appWebsiteServiceDesc, ", Website Link: ", appWebsiteServiceLink, ". Error Message: ", ex.ToString());
                    OASISErrorHandling.HandleError(ref result, result.Message);
                }
            }
            else
            {
                if (!result.IsError)
                {
                    result.IsError = true;
                    result.Message = "Unknown error occured pushing the transaction onto the EOSIO chain.";
                    OASISErrorHandling.HandleError(ref result, result.Message);
                }
            }

            return result;
        }

        private string PayWithSeeds(string fromTelosAccountName, string fromTelosAccountPrivateKey, string toTelosAccountName, int quanitity, string memo)
        {
            return PayWithSeeds(fromTelosAccountName, fromTelosAccountPrivateKey, toTelosAccountName, ConvertTokenToSEEDSFormat(quanitity), memo);
        }

        private string PayWithSeeds(string fromTelosAccountName, string fromTelosAccountPrivateKey, string toTelosAccountName, string quanitity, string memo)
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
            TransferArgs args = new TransferArgs() { from = fromTelosAccountName, to = toTelosAccountName, quantity = quanitity, memo = memo };
            // var abiJsonToBin = EOSIOOASIS.ChainAPI.GetAbiJsonToBin("eosio.token", "transfer", args);

            //prepare action object
            //EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("transfer", fromTelosAccountName, "active", "eosio.token", args);
            //EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("transfer", fromTelosAccountName, "active", "seed.seeds", args);
            EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("transfer", fromTelosAccountName, "active", "token.seeds", args);

            var keypair = KeyManager.GenerateKeyPair(Core.Enums.ProviderType.SEEDSOASIS).Result; //TODO: Handle OASISResult properly.
            //List<string> privateKeysInWIF = new List<string> { keypair.PrivateKey }; //TODO: Set Private Key
            List<string> privateKeysInWIF = new List<string> { fromTelosAccountPrivateKey }; 

            //push transaction
            var transactionResult = TelosOASIS.EOSIOOASIS.ChainAPI.PushTransaction(new[] { action }, privateKeysInWIF);


            // logger.Info(transactionResult.transaction_id);

            //transactionResult.processed
            return transactionResult.transaction_id;


            // string accountName = "eosio";
            //var abi = EOSIOOASIS.ChainAPI.GetAbi(accountName);

            //abi.abi.actions[0].
            //abi.abi.tables

            //logger.Info("For account {0} recieved abi {1}", accountName, JsonConvert.SerializeObject(abi));
        }

        private SendInviteResult SendInviteToJoinSeeds(string sponsorTelosAccountName, string sponsorTelosAccountNamePrivateKey, string referrerTelosAccountName, int transferQuantitiy, int sowQuantitiy)
        {
            //https://joinseeds.github.io/seeds-smart-contracts/onboarding.html
            //https://github.com/JoinSEEDS/seeds-smart-contracts/blob/master/scripts/onboarding-helper.js

            string randomHex = GetRandomHexNumber(64); //16
            string inviteHash = GetSHA256Hash(randomHex);
            var keypair = KeyManager.GenerateKeyPair(Core.Enums.ProviderType.SEEDSOASIS).Result; //TODO: Handle OASISResult properly.
            //List<string> privateKeysInWIF = new List<string> { keypair.PrivateKey }; //TODO: Set Private Key
            List<string> privateKeysInWIF = new List<string> { sponsorTelosAccountNamePrivateKey }; 

            EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("invitefor", sponsorTelosAccountName, "active", "join.seeds", new Invite() { sponsor = sponsorTelosAccountName, referrer = referrerTelosAccountName, invite_hash = inviteHash, transfer_quantity = ConvertTokenToSEEDSFormat(transferQuantitiy), sow_quantity = ConvertTokenToSEEDSFormat(sowQuantitiy) });
            var transactionResult = TelosOASIS.EOSIOOASIS.ChainAPI.PushTransaction(new[] { action }, privateKeysInWIF);

            return new SendInviteResult() { TransactionId = transactionResult.transaction_id, InviteSecret = inviteHash };
        }

        private string AcceptInviteToJoinSeeds(string telosAccountName, string inviteSecret)
        {
            //https://joinseeds.github.io/seeds-smart-contracts/onboarding.html
            //inviteSecret = inviteHash

            //TODO: Handle OASISResult properly.
            var keypair = KeyManager.GenerateKeyPair(Core.Enums.ProviderType.SEEDSOASIS).Result; 
            List<string> privateKeysInWIF = new List<string> { keypair.PrivateKey };

            EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("accept", telosAccountName, "active", "join.seeds", new Accept() { account = telosAccountName, invite_secret = inviteSecret, publicKey = keypair.PublicKey });
            var transactionResult = TelosOASIS.EOSIOOASIS.ChainAPI.PushTransaction(new[] { action }, privateKeysInWIF);

            return transactionResult.transaction_id;
        }

        private bool AddKarmaForSeeds(Guid avatarId, KarmaTypePositive seedsKarmaType, KarmaTypePositive seedsKarmaHeroType, KarmaSourceType receivingKarmaFor, string appWebsiteServiceName, string appWebsiteServiceDesc, string appWebsiteServiceLink = null)
        {
            //TODO: Add new karma methods OASIS.API.CORE that allow bulk/batch karma to be added in one call (maybe use params?)
            bool karmaHeroResult = !AvatarManager.AddKarmaToAvatar(avatarId, seedsKarmaHeroType, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink, Core.Enums.ProviderType.SEEDSOASIS).IsError;
            bool karmaSeedsResult = AvatarManager.AddKarmaToAvatar(avatarId, seedsKarmaType, receivingKarmaFor, appWebsiteServiceName, appWebsiteServiceDesc, appWebsiteServiceLink, Core.Enums.ProviderType.SEEDSOASIS).IsError;
            return karmaHeroResult && karmaSeedsResult;
        }

        private string ConvertTokenToSEEDSFormat(int amount)
        {
            //return string.Concat(Math.Round(amount, 4).ToString().PadRight(4, '0'), " SEEDS");
            return string.Concat(amount, ".0000 SEEDS");
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

        #region Serialization Methods

        /// <summary>
        /// Parse SEEDS blockchain response to Avatar object
        /// </summary>
        private Avatar ParseSEEDSToAvatar(string seedsJson)
        {
            try
            {
                // Deserialize the complete Avatar object from SEEDS JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(seedsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromSEEDS(seedsJson);
            }
        }

        /// <summary>
        /// Create Avatar from SEEDS response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromSEEDS(string seedsJson)
        {
            try
            {
                // Extract basic information from SEEDS JSON response
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = ExtractSEEDSProperty(seedsJson, "account") ?? "seeds_user",
                    Email = ExtractSEEDSProperty(seedsJson, "email") ?? "user@seeds.example",
                    FirstName = ExtractSEEDSProperty(seedsJson, "first_name"),
                    LastName = ExtractSEEDSProperty(seedsJson, "last_name"),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                
                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Extract property value from SEEDS JSON response
        /// </summary>
        private string ExtractSEEDSProperty(string seedsJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for SEEDS properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(seedsJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to SEEDS blockchain format
        /// </summary>
        private string ConvertAvatarToSEEDS(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with SEEDS blockchain structure
                var seedsData = new
                {
                    account = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(seedsData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        /// <summary>
        /// Convert Holon to SEEDS blockchain format
        /// </summary>
        private string ConvertHolonToSEEDS(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with SEEDS blockchain structure
                var seedsData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(seedsData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        #endregion
    }
}
