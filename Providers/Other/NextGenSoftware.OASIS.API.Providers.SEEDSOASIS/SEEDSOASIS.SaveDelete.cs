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
//using NextGenSoftware.OASIS.API.Providers.TelosOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS
{
    public partial class SEEDSOASIS
    {
        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Use EOSIO SDK to construct and push action rather than raw RPC
                TransferArgs args = new TransferArgs() { from = avatar.Username, to = avatar.Username, quantity = "0.0000 SEEDS", memo = "SaveAvatar" };
                EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("saveavatar", SEEDS_EOSIO_ACCOUNT_TEST, "active", SEEDS_EOSIO_ACCOUNT_TEST, args);

                var keypairResult = KeyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.SEEDSOASIS);
                if (keypairResult.IsError || keypairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to generate SEEDS key pair: {keypairResult.Message}");
                    return response;
                }
                List<string> privateKeysInWIF = new List<string> { keypairResult.Result.PrivateKey };

                var transactionResult = TelosOASIS.EOSIOOASIS.ChainAPI.PushTransaction(new[] { action }, privateKeysInWIF);

                if (transactionResult != null)
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
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to SEEDS: {ex.Message}");
            }

            return response;
        }

        public OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return result;
                    }
                }

                EOSNewYork.EOSCore.Params.Action action = new ActionUtility(ENDPOINT_TEST).GetActionObject("upsertavatardetail", SEEDS_EOSIO_ACCOUNT_TEST, "active", SEEDS_EOSIO_ACCOUNT_TEST, new
                {
                    id = avatarDetail.Id.ToString(),
                    username = avatarDetail.Username ?? "",
                    email = avatarDetail.Email ?? "",
                    karma = avatarDetail.Karma,
                    xp = avatarDetail.XP,
                    model3d = avatarDetail.Model3D ?? "",
                    uma_json = avatarDetail.UmaJson ?? "",
                    portrait = avatarDetail.Portrait ?? "",
                    town = avatarDetail.Town ?? "",
                    county = avatarDetail.County ?? "",
                    dob = ((DateTimeOffset)avatarDetail.DOB).ToUnixTimeSeconds(),
                    address = avatarDetail.Address ?? "",
                    country = avatarDetail.Country ?? "",
                    postcode = avatarDetail.Postcode ?? "",
                    landline = avatarDetail.Landline ?? "",
                    mobile = avatarDetail.Mobile ?? "",
                    favourite_colour = (int)avatarDetail.FavouriteColour,
                    starcli_colour = (int)avatarDetail.STARCLIColour,
                    created_date = ((DateTimeOffset)avatarDetail.CreatedDate).ToUnixTimeSeconds(),
                    modified_date = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                    description = avatarDetail.Description ?? "SEEDS Avatar Detail",
                    is_active = avatarDetail.IsActive
                });

                var keypair = KeyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.SEEDSOASIS).Result; //TODO: Handle OASISResult properly.
                List<string> privateKeysInWIF = new List<string> { keypair.PrivateKey };

                var transactionResult = TelosOASIS.EOSIOOASIS.ChainAPI.PushTransaction(new[] { action }, privateKeysInWIF);

                if (transactionResult != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail saved to SEEDS blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to save avatar detail to SEEDS blockchain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to SEEDS: {ex.Message}");
            }

            return result;
        }

        public OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<bool> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.DeleteAvatarAsync(id, softDelete);
        }

        public OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(email, softDelete).Result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<bool> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.DeleteAvatarByEmailAsync(email, softDelete);
        }

        public OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(username, softDelete).Result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<bool> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.DeleteAvatarByUsernameAsync(username, softDelete);
        }

        public OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(Guid.Parse(providerKey), softDelete);
        }

        // Additional IOASISStorageProvider interface members
        public OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            // Try to parse providerKey as Guid first
            if (Guid.TryParse(providerKey, out var guid))
            {
                return await LoadAvatarAsync(guid, version);
            }
            
            // If not a Guid, return error or try to load by provider key
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "Provider key must be a valid Guid for SEEDS provider");
            return result;
        }

        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatarDetail avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDescription, string webLink)
        {
            return AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karmaSourceTitle, karmaSourceDescription, webLink).Result;
        }

        public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatarAsync(IAvatarDetail avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDescription, string webLink)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<KarmaAkashicRecord> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karmaSourceTitle, karmaSourceDescription, webLink);
        }

        public OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(IAvatarDetail avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDescription, string webLink)
        {
            return RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karmaSourceTitle, karmaSourceDescription, webLink).Result;
        }

        public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatarAsync(IAvatarDetail avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDescription, string webLink)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<KarmaAkashicRecord> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karmaSourceTitle, karmaSourceDescription, webLink);
        }

        public OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, sendKarma).Result;
        }

        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true)
        {
            // SEEDSOASIS delegates storage operations to TelosOASIS
            if (TelosOASIS == null)
            {
                return new OASISResult<IHolon> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, false);
        }

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int continueOnError = 0, bool sendKarma = true, bool reloadChildren = true)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, sendKarma, reloadChildren).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int continueOnError = 0, bool sendKarma = true, bool reloadChildren = true)
        {
            // SEEDSOASIS delegates storage operations to TelosOASIS
            if (TelosOASIS == null)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, false);
        }

        public OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, sendKarma, version).Result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            // SEEDSOASIS delegates storage operations to TelosOASIS
            if (TelosOASIS == null)
            {
                return new OASISResult<IHolon> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, false, version);
        }

        public OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, sendKarma, version).Result;
        }

        public Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return LoadHolonAsync(Guid.Parse(providerKey), loadChildren, recursive, maxChildDepth, continueOnError, sendKarma, version);
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, sendKarma, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            // SEEDSOASIS delegates storage operations to TelosOASIS
            if (TelosOASIS == null)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, sendKarma, version).Result;
        }

        public Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return LoadHolonsForParentAsync(Guid.Parse(providerKey), holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, sendKarma, version);
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, sendKarma, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            // SEEDSOASIS delegates storage operations to TelosOASIS
            if (TelosOASIS == null)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.LoadHolonsByMetaDataAsync(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, matchMode, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, sendKarma, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.LoadHolonsByMetaDataAsync(metaData, matchMode, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
        }

        public OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            return LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, sendKarma, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool sendKarma = true, int version = 0)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
        }

        public OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<IHolon> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.DeleteHolonAsync(id);
        }

        public OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            return DeleteHolonAsync(Guid.Parse(providerKey));
        }

        public OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<bool> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.ImportAsync(holons);
        }

        public OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(id, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.ExportAllDataForAvatarByIdAsync(id, version);
        }

        public OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(username, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.ExportAllDataForAvatarByUsernameAsync(username, version);
        }

        public OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(email, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.ExportAllDataForAvatarByEmailAsync(email, version);
        }

        public OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.ExportAllAsync(version);
        }

        public async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            if (TelosOASIS == null)
            {
                return new OASISResult<ISearchResults> { IsError = true, Message = "TelosOASIS provider not initialized" };
            }
            return await TelosOASIS.SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version);
        }

        OASISResult<ISearchResults> IOASISStorageProvider.Search(ISearchParams searchParams, bool loadChildren, bool recursive, int maxChildDepth, bool continueOnError, int version)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public event EventDelegates.StorageProviderError OnStorageProviderError;

        /// <summary>
        /// Parse SEEDS EOSIO table row to Avatar object
        /// </summary>
        private IAvatar ParseSEEDSToAvatar(JsonElement seedsData)
        {
            try
            {
                var seedsAccount = seedsData.TryGetProperty("account", out var account) ? account.GetString() : seedsData.TryGetProperty("id", out var idProp) ? idProp.GetString() : "unknown";
                var avatar = new Avatar
                {
                    Id = seedsData.TryGetProperty("id", out var id) && Guid.TryParse(id.GetString(), out var parsedId) ? parsedId : CreateDeterministicGuid($"{ProviderType.Value}:{seedsAccount}"),
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
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:seeds_error"),
                    Username = "seeds_user",
                    Email = "user@seeds.example"
                };
            }
        }

        /// <summary>
        /// Parse SEEDS EOSIO table row to AvatarDetail object
        /// </summary>
        private IAvatarDetail ParseSEEDSToAvatarDetail(JsonElement seedsData)
        {
            try
            {
                var seedsAccount = seedsData.TryGetProperty("account", out var account) ? account.GetString() : seedsData.TryGetProperty("id", out var idProp) ? idProp.GetString() : "unknown";
                var avatarDetail = new AvatarDetail
                {
                    Id = seedsData.TryGetProperty("id", out var id) && Guid.TryParse(id.GetString(), out var parsedId) ? parsedId : CreateDeterministicGuid($"{ProviderType.Value}:{seedsAccount}"),
                    Username = seedsData.TryGetProperty("username", out var username) ? username.GetString() : "seeds_user",
                    Email = seedsData.TryGetProperty("email", out var email) ? email.GetString() : "user@seeds.example",
                    Karma = seedsData.TryGetProperty("karma", out var karma) ? karma.GetInt64() : 0,
                    XP = seedsData.TryGetProperty("xp", out var xp) ? xp.GetInt32() : 0,
                    Model3D = seedsData.TryGetProperty("model3d", out var model3d) ? model3d.GetString() : "",
                    UmaJson = seedsData.TryGetProperty("uma_json", out var umaJson) ? umaJson.GetString() : "",
                    Portrait = seedsData.TryGetProperty("portrait", out var portrait) ? portrait.GetString() : "",
                    Town = seedsData.TryGetProperty("town", out var town) ? town.GetString() : "",
                    County = seedsData.TryGetProperty("county", out var county) ? county.GetString() : "",
                    DOB = seedsData.TryGetProperty("dob", out var dob) ? DateTimeOffset.FromUnixTimeSeconds(dob.GetInt64()).DateTime : DateTime.UtcNow,
                    Address = seedsData.TryGetProperty("address", out var address) ? address.GetString() : "",
                    Country = seedsData.TryGetProperty("country", out var country) ? country.GetString() : "",
                    Postcode = seedsData.TryGetProperty("postcode", out var postcode) ? postcode.GetString() : "",
                    Landline = seedsData.TryGetProperty("landline", out var landline) ? landline.GetString() : "",
                    Mobile = seedsData.TryGetProperty("mobile", out var mobile) ? mobile.GetString() : "",
                    FavouriteColour = seedsData.TryGetProperty("favourite_colour", out var favouriteColour) ? (ConsoleColor)favouriteColour.GetInt32() : ConsoleColor.White,
                    STARCLIColour = seedsData.TryGetProperty("starcli_colour", out var starcliColour) ? (ConsoleColor)starcliColour.GetInt32() : ConsoleColor.White,
                    CreatedDate = seedsData.TryGetProperty("created_date", out var createdDate) ? DateTimeOffset.FromUnixTimeSeconds(createdDate.GetInt64()).DateTime : DateTime.UtcNow,
                    ModifiedDate = seedsData.TryGetProperty("modified_date", out var modifiedDate) ? DateTimeOffset.FromUnixTimeSeconds(modifiedDate.GetInt64()).DateTime : DateTime.UtcNow,
                    Description = seedsData.TryGetProperty("description", out var description) ? description.GetString() : "SEEDS Avatar Detail",
                    IsActive = seedsData.TryGetProperty("is_active", out var isActive) ? isActive.GetBoolean() : true
                };

                return avatarDetail;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing SEEDS data to AvatarDetail: {ex.Message}");
                return new AvatarDetail
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:seeds_error"),
                    Username = "seeds_user",
                    Email = "user@seeds.example"
                };
            }
        }

        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }

    }
}
