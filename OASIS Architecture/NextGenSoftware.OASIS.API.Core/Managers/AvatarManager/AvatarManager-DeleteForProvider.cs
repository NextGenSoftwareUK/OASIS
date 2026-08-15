using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.CLI.Engine;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager
    {
        //public IAvatar HideAuthDetails(IAvatar avatar, bool hidePassword = true, bool hidePrivateKeys = true, bool hideVerificationToken = true, bool hideRefreshTokens = true)
        public IAvatar HideAuthDetails(IAvatar avatar, bool hidePassword = false, bool hidePrivateKeys = true, bool hideVerificationToken = false, bool hideRefreshTokens = false)
        {
            if (OASISDNA.OASIS.Security.HideVerificationToken || hideVerificationToken)
                avatar.VerificationToken = null;

            if (hidePassword)
                avatar.Password = null;

            if (hidePrivateKeys)
            {
                foreach (ProviderType providerType in avatar.ProviderWallets.Keys)
                {
                    foreach (ProviderWallet wallet in avatar.ProviderWallets[providerType])
                        wallet.PrivateKey = null;
                }
            }

            if (OASISDNA.OASIS.Security.HideRefreshTokens || hideRefreshTokens)
                avatar.RefreshTokens = null;

            return avatar;
        }

        public IEnumerable<IAvatar> HideAuthDetails(IEnumerable<IAvatar> avatars)
        {
            List<IAvatar> tempList = avatars.ToList();

            for (int i = 0; i < tempList.Count; i++)
                tempList[i] = HideAuthDetails(tempList[i]);

            return tempList;
        }

        public async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailForProviderAsync(IAvatarDetail avatar, OASISResult<IAvatarDetail> result, SaveMode saveMode, ProviderType providerType = ProviderType.Default)
        {
            string errorMessageTemplate = "Error in SaveAvatarDetailForProviderAsync method in AvatarManager saving avatar detail with name {0}, username {1} and id {2} for provider {3} for {4}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, avatar.Name, avatar.Username, avatar.Id, providerType, Enum.GetName(typeof(SaveMode), saveMode));

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                errorMessage = string.Format(errorMessageTemplate, avatar.Name, avatar.Username, avatar.Id, ProviderManager.Instance.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    HolonManager.Instance.ExtractCustomPropertiesToMetaData(avatar);
                    var task = providerResult.Result.SaveAvatarDetailAsync(avatar);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                    {
                        if (task.Result == null || (task.Result.IsError || task.Result.Result == null))
                        {
                            if (task.Result == null || (task.Result != null && string.IsNullOrEmpty(task.Result.Message) && saveMode != SaveMode.AutoReplication))
                                task.Result.Message = "Unknown.";

                            OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage, saveMode == SaveMode.AutoReplication);
                        }
                        else
                        {
                            result.IsSaved = true;
                            result.Result = task.Result.Result;
                        }
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."), saveMode == SaveMode.AutoReplication);
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage, saveMode == SaveMode.AutoReplication);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex, saveMode == SaveMode.AutoReplication);
            }

            return result;
        }

        public OASISResult<IAvatarDetail> SaveAvatarDetailForProvider(IAvatarDetail avatar, OASISResult<IAvatarDetail> result, SaveMode saveMode, ProviderType providerType = ProviderType.Default)
        {
            return SaveAvatarDetailForProviderAsync(avatar, result, saveMode, providerType).Result;
        }

        public OASISResult<bool> DeleteAvatarForProvider(Guid id, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            return DeleteAvatarForProviderAsync(id, result, saveMode, softDelete, providerType).Result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarForProviderAsync(Guid id, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessageTemplate = "Error in DeleteAvatarForProviderAsync method in AvatarManager deleting avatar with email {0} for provider {1} for {2}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, id, providerType, Enum.GetName(typeof(SaveMode), saveMode));

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                errorMessage = string.Format(errorMessageTemplate, id, ProviderManager.Instance.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.DeleteAvatarAsync(id, softDelete);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                    {
                        if (task.Result.IsError || !task.Result.Result)
                        {
                            if (string.IsNullOrEmpty(task.Result.Message) && saveMode != SaveMode.AutoReplication)
                                result.Message = "Unknown.";

                            OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage, saveMode == SaveMode.AutoReplication);
                        }
                        else
                        {
                            result.IsSaved = true;
                            result.Result = task.Result.Result;
                        }
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."), saveMode == SaveMode.AutoReplication);
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage, saveMode == SaveMode.AutoReplication);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex, saveMode == SaveMode.AutoReplication);
            }

            return result;
        }

        public OASISResult<bool> DeleteAvatarByEmailForProvider(string email, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            return DeleteAvatarByEmailForProviderAsync(email, result, saveMode, softDelete, providerType).Result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByEmailForProviderAsync(string email, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessageTemplate = "Error in DeleteAvatarByEmailForProviderAsync method in AvatarManager deleting avatar with email {0} for provider {1} for {2}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, email, providerType, Enum.GetName(typeof(SaveMode), saveMode));

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                errorMessage = string.Format(errorMessageTemplate, email, ProviderManager.Instance.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.DeleteAvatarByEmailAsync(email, softDelete);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                    {
                        if (task.Result.IsError || !task.Result.Result)
                        {
                            if (string.IsNullOrEmpty(task.Result.Message) && saveMode != SaveMode.AutoReplication)
                                result.Message = "Unknown.";

                            OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage, saveMode == SaveMode.AutoReplication);
                        }
                        else
                        {
                            result.IsSaved = true;
                            result.Result = task.Result.Result;
                        }
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."), saveMode == SaveMode.AutoReplication);
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage, saveMode == SaveMode.AutoReplication);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex, saveMode == SaveMode.AutoReplication);
            }

            return result;
        }

        public OASISResult<bool> DeleteAvatarByUsernameForProvider(string username, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            return DeleteAvatarByUsernameForProviderAsync(username, result, saveMode, softDelete, providerType).Result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByUsernameForProviderAsync(string username, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessageTemplate = "Error in DeleteAvatarByUsernameForProviderAsync method in AvatarManager deleting avatar with username {0} for provider {1} for {2}. Reason: ";
            string errorMessage = String.Format(errorMessageTemplate, username, providerType, Enum.GetName(typeof(SaveMode), saveMode));

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.DeleteAvatarByUsernameAsync(username, softDelete);
                    errorMessage = String.Format(errorMessageTemplate, username, ProviderManager.Instance.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                    {
                        if (task.Result.IsError || !task.Result.Result)
                        {
                            if (string.IsNullOrEmpty(task.Result.Message) && saveMode != SaveMode.AutoReplication)
                                task.Result.Message = "Unknown.";

                            OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage, saveMode == SaveMode.AutoReplication);
                        }
                        else
                        {
                            result.IsSaved = true;
                            result.Result = task.Result.Result;
                        }
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."), saveMode == SaveMode.AutoReplication);
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage, saveMode == SaveMode.AutoReplication);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex, saveMode == SaveMode.AutoReplication);
            }

            return result;
        }

        public void ShowKarmaThresholds()
        {
            LevelManager.GenerateLevelLookup(true);
        }

        public Dictionary<int, long> GetKarmaThresholds()
        {
            return LevelManager.LevelLookup;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> SearchAvatarsAsync(string searchTerm, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            //OASISResult<IEnumerable<IHolon>> searchResults = await HolonManager.Instance.SearchHolonsAsync(searchTerm, HolonType.Avatar, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResult<ISearchResults> searchResults = await SearchManager.Instance.SearchAsync(new SearchParams()
            {
                SearchGroups = new List<ISearchGroupBase>()
                            {
                                new SearchTextGroup()
                                {
                                    HolonType = HolonType.Avatar,
                                    SearchQuery = searchTerm,
                                    SearchAvatars = true,
                                    AvatarSearchParams = new SearchAvatarParams()
                                    {
                                        SearchAllFields = true
                                    }
                                }
                            }
            });

            if (searchResults != null && !searchResults.IsError && searchResults.Result != null)
                result.Result = searchResults.Result.SearchResultAvatars;

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(searchResults, result);
            //result.Result = Mapper.ConvertIHolonsToIAvatars(searchResults.Result);

            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> SearchAvatars(string searchTerm, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            //OASISResult<IEnumerable<IHolon>> searchResults = await HolonManager.Instance.SearchHolonsAsync(searchTerm, HolonType.Avatar, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResult<ISearchResults> searchResults = SearchManager.Instance.Search(new SearchParams()
            {
                SearchGroups = new List<ISearchGroupBase>()
                            {
                                new SearchTextGroup()
                                {
                                    HolonType = HolonType.Avatar,
                                    SearchQuery = searchTerm,
                                    SearchAvatars = true,
                                    AvatarSearchParams = new SearchAvatarParams()
                                    {
                                        SearchAllFields = true
                                    }
                                }
                            }
            });

            if (searchResults != null && !searchResults.IsError && searchResults.Result != null)
                result.Result = searchResults.Result.SearchResultAvatars;

            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(searchResults, result);
            //result.Result = Mapper.ConvertIHolonsToIAvatars(searchResults.Result);

            return result;
        }

        /*
       public OASISResult<bool> DeleteAvatarDetailForProvider(Guid id, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
       {
           return DeleteAvatarDetailForProviderAsync(id, result, saveMode, softDelete, providerType).Result;
       }

       public async Task<OASISResult<bool>> DeleteAvatarDetailForProviderAsync(Guid id, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
       {
           string errorMessageTemplate = "Error in DeleteAvatarDetailForProviderAsync method in AvatarManager deleting avatar detail with email {0} for provider {1} for {2}. Reason: ";
           string errorMessage = string.Format(errorMessageTemplate, id, providerType, Enum.GetName(typeof(SaveMode), saveMode));

           try
           {
               OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);
               errorMessage = string.Format(errorMessageTemplate, id, ProviderManager.Instance.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

               if (!providerResult.IsError && providerResult.Result != null)
               {
                   var task = providerResult.Result.DeleteAvatarDetailAsync(id, softDelete);

                   if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                   {
                       if (task.Result.IsError || !task.Result.Result)
                       {
                           if (string.IsNullOrEmpty(task.Result.Message) && saveMode != SaveMode.AutoReplication)
                               result.Message = "Unknown.";

                           OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage, saveMode == SaveMode.AutoReplication);
                       }
                       else
                       {
                           result.IsSaved = true;
                           result.Result = task.Result.Result;
                       }
                   }
                   else
                       OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."), saveMode == SaveMode.AutoReplication);
               }
               else
                   OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage, saveMode == SaveMode.AutoReplication);
           }
           catch (Exception ex)
           {
               OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex, saveMode == SaveMode.AutoReplication);
           }

           return result;
       }

       public OASISResult<bool> DeleteAvatarDetailByEmailForProvider(string email, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
       {
           return DeleteAvatarDetailByEmailForProviderAsync(email, result, saveMode, softDelete, providerType).Result;
       }

       public async Task<OASISResult<bool>> DeleteAvatarDetailByEmailForProviderAsync(string email, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
       {
           string errorMessageTemplate = "Error in DeleteAvatarDetailByEmailForProviderAsync method in AvatarManager deleting avatar with email {0} for provider {1} for {2}. Reason: ";
           string errorMessage = string.Format(errorMessageTemplate, email, providerType, Enum.GetName(typeof(SaveMode), saveMode));

           try
           {
               OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);
               errorMessage = string.Format(errorMessageTemplate, email, ProviderManager.Instance.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

               if (!providerResult.IsError && providerResult.Result != null)
               {
                   var task = providerResult.Result.DeleteAvatarDetailByEmailAsync(email, softDelete);

                   if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                   {
                       if (task.Result.IsError || !task.Result.Result)
                       {
                           if (string.IsNullOrEmpty(task.Result.Message) && saveMode != SaveMode.AutoReplication)
                               result.Message = "Unknown.";

                           OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage, saveMode == SaveMode.AutoReplication);
                       }
                       else
                       {
                           result.IsSaved = true;
                           result.Result = task.Result.Result;
                       }
                   }
                   else
                       OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."), saveMode == SaveMode.AutoReplication);
               }
               else
                   OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage, saveMode == SaveMode.AutoReplication);
           }
           catch (Exception ex)
           {
               OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex, saveMode == SaveMode.AutoReplication);
           }

           return result;
       }

       public OASISResult<bool> DeleteAvatarDetailByUsernameForProvider(string username, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
       {
           return DeleteAvatarDetailByUsernameForProviderAsync(username, result, saveMode, softDelete, providerType).Result;
       }

       public async Task<OASISResult<bool>> DeleteAvatarDetailByUsernameForProviderAsync(string username, OASISResult<bool> result, SaveMode saveMode, bool softDelete = true, ProviderType providerType = ProviderType.Default)
       {
           string errorMessageTemplate = "Error in DeleteAvatarDetailByUsernameForProviderAsync method in AvatarManager deleting avatar detail with username {0} for provider {1} for {2}. Reason: ";
           string errorMessage = String.Format(errorMessageTemplate, username, providerType, Enum.GetName(typeof(SaveMode), saveMode));

           try
           {
               OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

               if (!providerResult.IsError && providerResult.Result != null)
               {
                   var task = providerResult.Result.DeleteAvatarDetailByUsernameAsync(username, softDelete);
                   errorMessage = String.Format(errorMessageTemplate, username, ProviderManager.Instance.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

                   if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                   {
                       if (task.Result.IsError || !task.Result.Result)
                       {
                           if (string.IsNullOrEmpty(task.Result.Message) && saveMode != SaveMode.AutoReplication)
                               task.Result.Message = "Unknown.";

                           OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage, saveMode == SaveMode.AutoReplication);
                       }
                       else
                       {
                           result.IsSaved = true;
                           result.Result = task.Result.Result;
                       }
                   }
                   else
                       OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."), saveMode == SaveMode.AutoReplication);
               }
               else
                   OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage, saveMode == SaveMode.AutoReplication);
           }
           catch (Exception ex)
           {
               OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex, saveMode == SaveMode.AutoReplication);
           }

           return result;
       }*/

    }
}
