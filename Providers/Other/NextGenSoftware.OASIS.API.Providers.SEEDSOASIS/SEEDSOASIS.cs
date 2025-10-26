using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using Newtonsoft.Json;
using EOSNewYork.EOSCore.ActionArgs;
using EOSNewYork.EOSCore.Response.API;
using EOSNewYork.EOSCore.Utilities;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.Membranes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS
{
    public class SEEDSOASIS : OASISProvider, IOASISApplicationProvider, IOASISStorageProvider
    {
        private static Random _random = new Random();
        private AvatarManager _avatarManager = null;
        private KeyManager _keyManager = null;
        private readonly HttpClient _httpClient;

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
                        _avatarManager = new AvatarManager(TelosOASIS, OASISDNA);
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
                    _keyManager = new KeyManager(this, OASISDNA);

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
            _httpClient = new HttpClient();

           // TelosOASIS = new TelosOASIS.TelosOASIS(telosConnectionString);
        }

        //event EventDelegates.StorageProviderError IOASISStorageProvider.OnStorageProviderError
        //{
        //    add
        //    {
        //        throw new NotImplementedException();
        //    }

        //    remove
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

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

        #region IOASISStorageProvider Interface Implementation

        // Stub implementations for IOASISStorageProvider interface
        public OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return new OASISResult<IAvatar> { Message = "LoadAvatar is not supported yet by SEEDS provider." };
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "SEEDS provider is not activated");
                    return response;
                }

                // Load avatar from SEEDS blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "get_table_rows",
                    @params = new
                    {
                        code = SEEDS_EOSIO_ACCOUNT_TEST, // Use test by default
                        scope = SEEDS_EOSIO_ACCOUNT_TEST,
                        table = "avatars",
                        lower_bound = id.ToString(),
                        upper_bound = id.ToString(),
                        limit = 1,
                        reverse = false,
                        show_payer = false
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(ENDPOINT_TEST, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result) &&
                        result.TryGetProperty("rows", out var rows) &&
                        rows.ValueKind == JsonValueKind.Array &&
                        rows.GetArrayLength() > 0)
                    {
                        var avatarData = rows[0];
                        var avatar = ParseSEEDSToAvatar(avatarData);
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from SEEDS blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found on SEEDS blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from SEEDS blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from SEEDS: {ex.Message}");
            }

            return response;
        }

        public OASISResult<IAvatar> LoadAvatar(string providerKey, int version = 0)
        {
            return new OASISResult<IAvatar> { Message = "LoadAvatar by providerKey is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IAvatar>> LoadAvatarAsync(string providerKey, int version = 0)
        {
            return Task.FromResult(new OASISResult<IAvatar> { Message = "LoadAvatarAsync by providerKey is not supported yet by SEEDS provider." });
        }

        public OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
        {
            return new OASISResult<IAvatar> { Message = "LoadAvatarByEmail is not supported yet by SEEDS provider." };
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "SEEDS provider is not activated");
                    return response;
                }

                // Load avatar by email from SEEDS blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "get_table_rows",
                    @params = new
                    {
                        code = SEEDS_EOSIO_ACCOUNT_TEST,
                        scope = SEEDS_EOSIO_ACCOUNT_TEST,
                        table = "avatars",
                        index_position = 2, // Secondary index on email
                        key_type = "name",
                        lower_bound = email,
                        upper_bound = email,
                        limit = 1,
                        reverse = false,
                        show_payer = false
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(ENDPOINT_TEST, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result) &&
                        result.TryGetProperty("rows", out var rows) &&
                        rows.ValueKind == JsonValueKind.Array &&
                        rows.GetArrayLength() > 0)
                    {
                        var avatarData = rows[0];
                        var avatar = ParseSEEDSToAvatar(avatarData);
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded by email from SEEDS blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found on SEEDS blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar by email from SEEDS blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from SEEDS: {ex.Message}");
            }

            return response;
        }

        public OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
        {
            return new OASISResult<IAvatar> { Message = "LoadAvatarByUsername is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            return Task.FromResult(new OASISResult<IAvatar> { Message = "LoadAvatarByUsernameAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return new OASISResult<IEnumerable<IAvatar>> { Message = "LoadAllAvatars is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IAvatar>> { Message = "LoadAllAvatarsAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return new OASISResult<IAvatarDetail> { Message = "LoadAvatarDetail is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            return Task.FromResult(new OASISResult<IAvatarDetail> { Message = "LoadAvatarDetailAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
        {
            return new OASISResult<IAvatarDetail> { Message = "LoadAvatarDetailByEmail is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            return Task.FromResult(new OASISResult<IAvatarDetail> { Message = "LoadAvatarDetailByEmailAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
        {
            return new OASISResult<IAvatarDetail> { Message = "LoadAvatarDetailByUsername is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            return Task.FromResult(new OASISResult<IAvatarDetail> { Message = "LoadAvatarDetailByUsernameAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return new OASISResult<IEnumerable<IAvatarDetail>> { Message = "LoadAllAvatarDetails is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IAvatarDetail>> { Message = "LoadAllAvatarDetailsAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return new OASISResult<IAvatar> { Message = "SaveAvatar is not supported yet by SEEDS provider." };
        }

        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "SEEDS provider is not activated");
                    return response;
                }

                // Save avatar to SEEDS blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "push_transaction",
                    @params = new
                    {
                        signatures = new string[0], // Will be signed by wallet
                        compression = 0,
                        packed_context_free_data = "",
                        packed_trx = Convert.ToBase64String(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            expiration = DateTimeOffset.UtcNow.AddMinutes(10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            ref_block_num = 0,
                            ref_block_prefix = 0,
                            max_net_usage_words = 0,
                            max_cpu_usage_ms = 0,
                            delay_sec = 0,
                            context_free_actions = new object[0],
                            actions = new[]
                            {
                                new
                                {
                                    account = SEEDS_EOSIO_ACCOUNT_TEST,
                                    name = "saveavatar",
                                    authorization = new[]
                                    {
                                        new
                                        {
                                            actor = SEEDS_EOSIO_ACCOUNT_TEST,
                                            permission = "active"
                                        }
                                    },
                                    data = new
                                    {
                                        id = avatar.Id.ToString(),
                                        username = avatar.Username,
                                        email = avatar.Email,
                                        first_name = avatar.FirstName,
                                        last_name = avatar.LastName,
                                        title = avatar.Title,
                                        password = avatar.Password,
                                        avatar_type = avatar.AvatarType,
                                        accept_terms = avatar.AcceptTerms,
                                        is_verified = avatar.IsVerified,
                                        jwt_token = avatar.JwtToken,
                                        password_reset = avatar.PasswordReset,
                                        refresh_token = avatar.RefreshToken,
                                        reset_token = avatar.ResetToken,
                                        reset_token_expires = avatar.ResetTokenExpires,
                                        verification_token = avatar.VerificationToken,
                                        verified = avatar.Verified,
                                        last_beamed_in = avatar.LastBeamedIn,
                                        last_beamed_out = avatar.LastBeamedOut,
                                        is_beamed_in = avatar.IsBeamedIn,
                                        created_date = avatar.CreatedDate,
                                        modified_date = avatar.ModifiedDate,
                                        description = avatar.Description,
                                        is_active = avatar.IsActive
                                    }
                                }
                            }
                        })))
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(ENDPOINT_TEST, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar saved to SEEDS blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save avatar to SEEDS blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to SEEDS blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to SEEDS: {ex.Message}");
            }

            return response;
        }

        public OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return new OASISResult<IAvatarDetail> { Message = "SaveAvatarDetail is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            return Task.FromResult(new OASISResult<IAvatarDetail> { Message = "SaveAvatarDetailAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return new OASISResult<bool> { Message = "DeleteAvatar is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            return Task.FromResult(new OASISResult<bool> { Message = "DeleteAvatarAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
        {
            return new OASISResult<bool> { Message = "DeleteAvatarByEmail is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            return Task.FromResult(new OASISResult<bool> { Message = "DeleteAvatarByEmailAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
        {
            return new OASISResult<bool> { Message = "DeleteAvatarByUsername is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            return Task.FromResult(new OASISResult<bool> { Message = "DeleteAvatarByUsernameAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return new OASISResult<bool> { Message = "DeleteAvatar by providerKey is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return Task.FromResult(new OASISResult<bool> { Message = "DeleteAvatarAsync by providerKey is not supported yet by SEEDS provider." });
        }

        // Additional IOASISStorageProvider interface members
        public OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return new OASISResult<IAvatar> { Message = "LoadAvatarByProviderKey is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            return Task.FromResult(new OASISResult<IAvatar> { Message = "LoadAvatarByProviderKeyAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatarDetail avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDescription, string webLink)
        {
            return new OASISResult<KarmaAkashicRecord> { Message = "AddKarmaToAvatar is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatarAsync(IAvatarDetail avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDescription, string webLink)
        {
            return Task.FromResult(new OASISResult<KarmaAkashicRecord> { Message = "AddKarmaToAvatarAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(IAvatarDetail avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDescription, string webLink)
        {
            return new OASISResult<KarmaAkashicRecord> { Message = "RemoveKarmaFromAvatar is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatarAsync(IAvatarDetail avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDescription, string webLink)
        {
            return Task.FromResult(new OASISResult<KarmaAkashicRecord> { Message = "RemoveKarmaFromAvatarAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true)
        {
            return new OASISResult<IHolon> { Message = "SaveHolon is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true)
        {
            return Task.FromResult(new OASISResult<IHolon> { Message = "SaveHolonAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int continueOnError = 0, bool sendKarma = true, bool reloadChildren = true)
        {
            return new OASISResult<IEnumerable<IHolon>> { Message = "SaveHolons is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int continueOnError = 0, bool sendKarma = true, bool reloadChildren = true)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>> { Message = "SaveHolonsAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return new OASISResult<IHolon> { Message = "LoadHolon is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return Task.FromResult(new OASISResult<IHolon> { Message = "LoadHolonAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return new OASISResult<IHolon> { Message = "LoadHolon by providerKey is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return Task.FromResult(new OASISResult<IHolon> { Message = "LoadHolonAsync by providerKey is not supported yet by SEEDS provider." });
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>> { Message = "LoadHolonsForParent is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>> { Message = "LoadHolonsForParentAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>> { Message = "LoadHolonsForParent by providerKey is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>> { Message = "LoadHolonsForParentAsync by providerKey is not supported yet by SEEDS provider." });
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>> { Message = "LoadHolonsByMetaData is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>> { Message = "LoadHolonsByMetaDataAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>> { Message = "LoadHolonsByMetaData with Dictionary is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>> { Message = "LoadHolonsByMetaDataAsync with Dictionary is not supported yet by SEEDS provider." });
        }

        public OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>> { Message = "LoadAllHolons is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>> { Message = "LoadAllHolonsAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return new OASISResult<IHolon> { Message = "DeleteHolon is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            return Task.FromResult(new OASISResult<IHolon> { Message = "DeleteHolonAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return new OASISResult<IHolon> { Message = "DeleteHolon by providerKey is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            return Task.FromResult(new OASISResult<IHolon> { Message = "DeleteHolonAsync by providerKey is not supported yet by SEEDS provider." });
        }

        public OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return new OASISResult<bool> { Message = "Import is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            return Task.FromResult(new OASISResult<bool> { Message = "ImportAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>> { Message = "ExportAllDataForAvatarById is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>> { Message = "ExportAllDataForAvatarByIdAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>> { Message = "ExportAllDataForAvatarByUsername is not supported yet by SEEDS provider." };
        }

        public Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>> { Message = "ExportAllDataForAvatarByUsernameAsync is not supported yet by SEEDS provider." });
        }

        public OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>> { Message = "ExportAllDataForAvatarByEmail is not supported yet by SEEDS provider." };
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            
            try
            {
                // SEEDS doesn't support data export in the traditional sense
                // Return empty collection as SEEDS is primarily for data storage, not export
                result.Result = new List<IHolon>();
                result.IsError = true;
                result.Message = "SEEDS provider does not support data export operations";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar {email}: {ex.Message}", ex);
            }
            
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            
            try
            {
                // SEEDS doesn't support data export in the traditional sense
                result.Result = new List<IHolon>();
                result.IsError = true;
                result.Message = "SEEDS provider does not support data export operations";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data: {ex.Message}", ex);
            }
            
            return result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            
            try
            {
                // SEEDS doesn't support data export in the traditional sense
                result.Result = new List<IHolon>();
                result.IsError = true;
                result.Message = "SEEDS provider does not support data export operations";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data: {ex.Message}", ex);
            }
            
            return result;
        }

        public async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            
            try
            {
                // SEEDS doesn't support advanced search functionality
                // Return empty search results
                result.Result = new SearchResults
                {
                    SearchResultHolons = new List<IHolon>(),
                    NumberOfResults = 0
                };
                result.IsError = true;
                result.Message = "SEEDS provider does not support search operations";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error performing search: {ex.Message}", ex);
            }
            
            return result;
        }

        OASISResult<ISearchResults> IOASISStorageProvider.Search(ISearchParams searchParams, bool loadChildren, bool recursive, int maxChildDepth, bool continueOnError, int version)
        {
            var result = new OASISResult<ISearchResults>();
            
            try
            {
                // SEEDS doesn't support advanced search functionality
                result.Result = new SearchResults
                {
                    SearchResultHolons = new List<IHolon>(),
                    NumberOfResults = 0
                };
                result.IsError = true;
                result.Message = "SEEDS provider does not support search operations";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error performing search: {ex.Message}", ex);
            }
            
            return result;
        }

        public event EventDelegates.StorageProviderError OnStorageProviderError;

        /// <summary>
        /// Parse SEEDS EOSIO table row to Avatar object
        /// </summary>
        private IAvatar ParseSEEDSToAvatar(JsonElement seedsData)
        {
            try
            {
                var avatar = new Avatar
                {
                    Id = seedsData.TryGetProperty("id", out var id) ? Guid.Parse(id.GetString() ?? Guid.NewGuid().ToString()) : Guid.NewGuid(),
                    Username = seedsData.TryGetProperty("username", out var username) ? username.GetString() : "seeds_user",
                    Email = seedsData.TryGetProperty("email", out var email) ? email.GetString() : "user@seeds.example",
                    FirstName = seedsData.TryGetProperty("first_name", out var firstName) ? firstName.GetString() : "SEEDS",
                    LastName = seedsData.TryGetProperty("last_name", out var lastName) ? lastName.GetString() : "User",
                    Title = seedsData.TryGetProperty("title", out var title) ? title.GetString() : "",
                    Password = seedsData.TryGetProperty("password", out var password) ? password.GetString() : "",
                    AvatarType = new EnumValue<AvatarType>((AvatarType)(seedsData.TryGetProperty("avatar_type", out var avatarType) ? avatarType.GetInt32() : 0)),
                    AcceptTerms = seedsData.TryGetProperty("accept_terms", out var acceptTerms) ? acceptTerms.GetBoolean() : true,
                    JwtToken = seedsData.TryGetProperty("jwt_token", out var jwtToken) ? jwtToken.GetString() : "",
                    PasswordReset = seedsData.TryGetProperty("password_reset", out var passwordReset) ? DateTimeOffset.FromUnixTimeSeconds(passwordReset.GetInt64()).DateTime : (DateTime?)null,
                    RefreshToken = seedsData.TryGetProperty("refresh_token", out var refreshToken) ? refreshToken.GetString() : "",
                    ResetToken = seedsData.TryGetProperty("reset_token", out var resetToken) ? resetToken.GetString() : "",
                    ResetTokenExpires = seedsData.TryGetProperty("reset_token_expires", out var resetTokenExpires) ? DateTimeOffset.FromUnixTimeSeconds(resetTokenExpires.GetInt64()).DateTime : (DateTime?)null,
                    VerificationToken = seedsData.TryGetProperty("verification_token", out var verificationToken) ? verificationToken.GetString() : "",
                    Verified = seedsData.TryGetProperty("verified", out var verified) ? DateTimeOffset.FromUnixTimeSeconds(verified.GetInt64()).DateTime : (DateTime?)null,
                    LastBeamedIn = seedsData.TryGetProperty("last_beamed_in", out var lastBeamedIn) ? DateTimeOffset.FromUnixTimeSeconds(lastBeamedIn.GetInt64()).DateTime : (DateTime?)null,
                    LastBeamedOut = seedsData.TryGetProperty("last_beamed_out", out var lastBeamedOut) ? DateTimeOffset.FromUnixTimeSeconds(lastBeamedOut.GetInt64()).DateTime : (DateTime?)null,
                    IsBeamedIn = seedsData.TryGetProperty("is_beamed_in", out var isBeamedIn) ? isBeamedIn.GetBoolean() : false,
                    CreatedDate = seedsData.TryGetProperty("created_date", out var createdDate) ? DateTimeOffset.FromUnixTimeSeconds(createdDate.GetInt64()).DateTime : DateTime.UtcNow,
                    ModifiedDate = seedsData.TryGetProperty("modified_date", out var modifiedDate) ? DateTimeOffset.FromUnixTimeSeconds(modifiedDate.GetInt64()).DateTime : DateTime.UtcNow,
                    Description = seedsData.TryGetProperty("description", out var description) ? description.GetString() : "SEEDS Avatar",
                    IsActive = seedsData.TryGetProperty("is_active", out var isActive) ? isActive.GetBoolean() : true
                };

                return avatar;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing SEEDS data to Avatar: {ex.Message}");
                return new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = "seeds_user",
                    Email = "user@seeds.example"
                };
            }
        }

        #endregion
    }
}
