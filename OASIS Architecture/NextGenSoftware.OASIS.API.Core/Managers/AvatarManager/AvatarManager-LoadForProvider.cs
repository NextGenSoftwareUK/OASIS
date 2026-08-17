using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager
    {
        private OASISResult<IAvatar> AvatarRegistered(OASISResult<IAvatar> result, bool callerIsWizard = false, bool suppressVerificationEmail = false)
        {
            if (OASISDNA.OASIS.Email.SendVerificationEmail && !suppressVerificationEmail)
            {
                try
                {
                    SendVerificationEmail(result.Result);
                }
                catch (Exception emailEx)
                {
                    // Non-fatal: registration still succeeds if verification email fails to send
                    result.InnerMessages.Add($"Warning: Failed to send verification email: {emailEx.Message}");
                }
            }

            string verificationToken = callerIsWizard ? result.Result.VerificationToken : null;
            result.Result = HideAuthDetails(result.Result);
            if (callerIsWizard)
                result.Result.VerificationToken = verificationToken;

            result.IsSaved = true;
            result.Message = "Avatar Created Successfully. Please check your email for the verification email. You will not be able to log in till you have verified your email. Thank you.";

            return result;
        }

        /*
        //TODO: Want to try and get all methods above to route through some generic function like this ASAP...
        private async Task<OASISResult<IAvatar>> LoadAvatarAsync(Func<string, int, Task<OASISResult<IAvatar>>> avatarLoadFunc, string param1, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

            result = await LoadAvatarForProviderAsync(avatarLoadFunc, param1, providerType, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadAvatarForProviderAsync(avatarLoadFunc, param1, type.Value, version);

                        if (!result.IsError && result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
                OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", param1, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
            {
                if (result.WarningCount > 0)
                    result.Message = string.Concat("The avatar ", param1, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));

                if (hideAuthDetails)
                    result.Result = HideAuthDetails(result.Result);
            }

            // Set the current provider back to the original provider.
            ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }

        private async Task<OASISResult<IAvatar>> LoadAvatarForProviderAsync(Func<string, int, Task<OASISResult<IAvatar>>> avatarLoadFunc, string param1, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = avatarLoadFunc(param1, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Avatar Not Found.";

                            OASISErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", param1, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name, ". Reason: ", result.Message));
                        }
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", param1, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name, ". Reason: timeout occured."));
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", param1, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name, ". There was an error setting the provider. Reason:", providerResult.Message));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, string.Concat("Unknown error occured loading avatar ", param1, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
            }

            return result;
        }*/

        private OASISResult<IAvatar> LoadAvatarForProvider(Guid id, OASISResult<IAvatar> result, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            //TODO: IMPLEMENT DIFFERENT TIMEOUT MECHNISM FOR NON-ASYNC METHODS? OR JUST CALL THE ASYNC VERSION?
            return LoadAvatarForProviderAsync(id, result, providerType, version).Result;
        }

        private async Task<OASISResult<IAvatar>> LoadAvatarForProviderAsync(Guid id, OASISResult<IAvatar> result, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            string errorMessageTemplate = "Error in LoadAvatarForProviderAsync method in AvatarManager loading avatar with id {0} for provider {1}. Reason: ";
            string errorMessage = String.Format(errorMessageTemplate, id, providerType);

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                errorMessage = String.Format(errorMessageTemplate, id, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarAsync(id, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                    {
                        if (task.Result.IsError || task.Result.Result == null)
                        {
                            if (string.IsNullOrEmpty(task.Result.Message))
                                task.Result.Message = "Avatar Not Found.";

                            OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                        }
                        else
                        {
                            result.Result = task.Result.Result;

                            //If we are loading from a local storge provider then load the provider wallets (including their private keys stored ONLY on local storage).
                            //if (ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocal || ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                            //    result = await LoadProviderWalletsAsync(providerResult.Result, result, errorMessage);
                            //else
                            //    result.IsLoaded = true;
                        }
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        private OASISResult<IAvatar> LoadAvatarForProvider(string username, OASISResult<IAvatar> result, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            //TODO: IMPLEMENT DIFFERENT TIMEOUT MECHNISM FOR NON-ASYNC METHODS? OR JUST CALL THE ASYNC VERSION?
            return LoadAvatarForProviderAsync(username, result, providerType, version).Result;
        }

        private async Task<OASISResult<IAvatar>> LoadAvatarForProviderAsync(string username, OASISResult<IAvatar> result, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            string errorMessageTemplate = "Error in LoadAvatarForProviderAsync method in AvatarManager loading avatar with username {0} for provider {1}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, username, providerType);

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                errorMessage = string.Format(errorMessageTemplate, username, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    //var task = providerResult.Result.LoadAvatarAsync(username, version);
                    var task = providerResult.Result.LoadAvatarByUsernameAsync(username, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                    {
                        if (task.Result.IsError || task.Result.Result == null)
                        {
                            if (string.IsNullOrEmpty(task.Result.Message))
                                task.Result.Message = "Avatar Not Found.";

                            OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                        }
                        else
                        {
                            result.Result = task.Result.Result;

                            //If we are loading from a local storge provider then load the provider wallets (including their private keys stored ONLY on local storage).
                            //if (ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocal || ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                            //    result = await LoadProviderWalletsAsync(providerResult.Result, result, errorMessage);
                            //else
                            //    result.IsLoaded = true;
                        }
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        private OASISResult<IAvatar> LoadAvatarByEmailForProvider(string email, OASISResult<IAvatar> result, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            return LoadAvatarByEmailForProviderAsync(email, result, providerType, version).Result;
        }

        private async Task<OASISResult<IAvatar>> LoadAvatarByEmailForProviderAsync(string email, OASISResult<IAvatar> result, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            string errorMessageTemplate = "Error in LoadAvatarByEmailForProviderAsync method in AvatarManager loading avatar with email {0} for provider {1}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, email, providerType);

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                errorMessage = string.Format(errorMessageTemplate, email, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarByEmailAsync(email, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                    {
                        if (task.Result.IsError || task.Result.Result == null)
                        {
                            if (string.IsNullOrEmpty(task.Result.Message))
                                task.Result.Message = "Avatar Not Found.";

                            OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                        }
                        else
                        {
                            result.Result = task.Result.Result;

                            ////If we are loading from a local storge provider then load the provider wallets (including their private keys stored ONLY on local storage).
                            //if (ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocal || ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                            //    result = await LoadProviderWalletsAsync(providerResult.Result, result, errorMessage);
                            //else
                            //    result.IsLoaded = true;
                        }
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        private OASISResult<IAvatar> LoadAvatarByVerificationTokenForProvider(string token, ProviderType providerType = ProviderType.Default)
            => LoadAvatarByVerificationTokenForProviderAsync(token, providerType).Result;

        private async Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenForProviderAsync(string token, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = $"Error in LoadAvatarByVerificationTokenForProviderAsync for provider {providerType}. Reason: ";
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarByVerificationTokenAsync(token);
                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                        return task.Result;
                    return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, "timeout occured.") };
                }
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, providerResult.Message) };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, ex.Message), Exception = ex };
            }
        }

        private OASISResult<IAvatar> LoadAvatarByResetTokenForProvider(string token, ProviderType providerType = ProviderType.Default)
            => LoadAvatarByResetTokenForProviderAsync(token, providerType).Result;

        private async Task<OASISResult<IAvatar>> LoadAvatarByResetTokenForProviderAsync(string token, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = $"Error in LoadAvatarByResetTokenForProviderAsync for provider {providerType}. Reason: ";
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarByResetTokenAsync(token);
                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                        return task.Result;
                    return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, "timeout occured.") };
                }
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, providerResult.Message) };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, ex.Message), Exception = ex };
            }
        }

        private OASISResult<IAvatar> LoadAvatarByRefreshTokenForProvider(string token, ProviderType providerType = ProviderType.Default)
            => LoadAvatarByRefreshTokenForProviderAsync(token, providerType).Result;

        private async Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenForProviderAsync(string token, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = $"Error in LoadAvatarByRefreshTokenForProviderAsync for provider {providerType}. Reason: ";
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarByRefreshTokenAsync(token);
                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                        return task.Result;
                    return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, "timeout occured.") };
                }
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, providerResult.Message) };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, ex.Message), Exception = ex };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByPublicKeyForProvider(string publicKey, ProviderType providerType = ProviderType.Default)
            => LoadAvatarByPublicKeyForProviderAsync(publicKey, providerType).Result;

        public async Task<OASISResult<IAvatar>> LoadAvatarByPublicKeyForProviderAsync(string publicKey, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = $"Error in LoadAvatarByPublicKeyForProviderAsync for provider {providerType}. Reason: ";
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarByPublicKeyAsync(publicKey);
                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                        return task.Result;
                    return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, "timeout occured.") };
                }
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, providerResult.Message) };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, ex.Message), Exception = ex };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByProviderKeyForProvider(string providerKey, ProviderType providerType = ProviderType.Default)
            => LoadAvatarByProviderKeyForProviderAsync(providerKey, providerType).Result;

        public async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyForProviderAsync(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = $"Error in LoadAvatarByProviderKeyForProviderAsync for provider {providerType}. Reason: ";
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarByProviderKeyAsync(providerKey);
                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                        return task.Result;
                    return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, "timeout occured.") };
                }
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, providerResult.Message) };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, ex.Message), Exception = ex };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByPrivateKeyForProvider(string privateKey, ProviderType providerType = ProviderType.Default)
            => LoadAvatarByPrivateKeyForProviderAsync(privateKey, providerType).Result;

        public async Task<OASISResult<IAvatar>> LoadAvatarByPrivateKeyForProviderAsync(string privateKey, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = $"Error in LoadAvatarByPrivateKeyForProviderAsync for provider {providerType}. Reason: ";
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarByPrivateKeyAsync(privateKey);
                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                        return task.Result;
                    return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, "timeout occured.") };
                }
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, providerResult.Message) };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsError = true, Message = string.Concat(errorMessage, ex.Message), Exception = ex };
            }
        }

        /*
       private OASISResult<IAvatar> LoadAvatarByJwtTokenForProvider(string jwtToken, OASISResult<IAvatar> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           return LoadAvatarByJwtTokenForProviderAsync(jwtToken, result, providerType, version).Result;
       }

       private async Task<OASISResult<IAvatar>> LoadAvatarByJwtTokenForProviderAsync(string jwtToken, OASISResult<IAvatar> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           string errorMessageTemplate = "Error in LoadAvatarByJwtTokenForProviderAsync method in AvatarManager loading avatar with email {0} for provider {1}. Reason: ";
           string errorMessage = string.Format(errorMessageTemplate, jwtToken, providerType);

           try
           {
               OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);
               errorMessage = string.Format(errorMessageTemplate, jwtToken, ProviderManager.Instance.CurrentStorageProviderType.Name);

               if (!providerResult.IsError && providerResult.Result != null)
               {
                   var task = providerResult.Result.LoadAvatarByJwtTokenAsync(jwtToken, version);

                   if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                   {
                       if (task.Result.IsError || task.Result.Result == null)
                       {
                           if (string.IsNullOrEmpty(task.Result.Message))
                               task.Result.Message = "Avatar Not Found.";

                           OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                       }
                       else
                       {
                           result.Result = task.Result.Result;

                           ////If we are loading from a local storge provider then load the provider wallets (including their private keys stored ONLY on local storage).
                           //if (ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocal || ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                           //    result = await LoadProviderWalletsAsync(providerResult.Result, result, errorMessage);
                           //else
                           //    result.IsLoaded = true;
                       }
                   }
                   else
                       OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
               }
               else
                   OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
           }
           catch (Exception ex)
           {
               OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex);
           }

           return result;
       }*/

       private OASISResult<IAvatarDetail> LoadAvatarDetailForProvider(Guid id, OASISResult<IAvatarDetail> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           return LoadAvatarDetailForProviderAsync(id, result, providerType, version).Result;
       }

       private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailForProviderAsync(Guid id, OASISResult<IAvatarDetail> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           string errorMessageTemplate = "Error in LoadAvatarDetailForProviderAsync method in AvatarManager loading avatar detail with id {0} for provider {1}. Reason: ";
           string errorMessage = string.Format(errorMessageTemplate, id, providerType);

           try
           {
               OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
               errorMessage = string.Format(errorMessageTemplate, id, ProviderManager.Instance.CurrentStorageProviderType.Name);

               if (!providerResult.IsError && providerResult.Result != null)
               {
                   var task = providerResult.Result.LoadAvatarDetailAsync(id, version);

                   if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                   {
                       if (task.Result.IsError || task.Result.Result == null)
                       {
                           if (string.IsNullOrEmpty(task.Result.Message))
                               task.Result.Message = "Avatar Detail Not Found.";

                           OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                       }
                       else
                       {
                           result.IsLoaded = true;
                           result.Result = task.Result.Result;
                           if (result.Result != null)
                               result.Result = (IAvatarDetail)HolonManager.Instance.MapMetaData<AvatarDetail>(result.Result);
                           PromoteInventoryNftIdFromMetaData(result.Result);
                       }
                   }
                   else
                       OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
               }
               else
                   OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
           }
           catch (Exception ex)
           {
               OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex);
           }

           return result;
       }

       private OASISResult<IAvatarDetail> LoadAvatarDetailByEmailForProvider(string email, OASISResult<IAvatarDetail> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           return LoadAvatarDetailByEmailForProviderAsync(email, result, providerType, version).Result;
       }

       private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailForProviderAsync(string email, OASISResult<IAvatarDetail> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           string errorMessageTemplate = "Error in LoadAvatarDetailByEmailForProviderAsync method in AvatarManager loading avatar detail with email {0} for provider {1}. Reason: ";
           string errorMessage = string.Format(errorMessageTemplate, email, providerType);

           try
           {
               OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
               errorMessage = string.Format(errorMessageTemplate, email, ProviderManager.Instance.CurrentStorageProviderType.Name);

               if (!providerResult.IsError && providerResult.Result != null)
               {
                   var task = providerResult.Result.LoadAvatarDetailByEmailAsync(email, version);

                   if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                   {
                       if (task.Result.IsError || task.Result.Result == null)
                       {
                           if (string.IsNullOrEmpty(result.Message))
                               task.Result.Message = "Avatar Detail Not Found.";

                           OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                       }
                       else
                       {
                           result.IsLoaded = true;
                           result.Result = task.Result.Result;
                           if (result.Result != null)
                               result.Result = (IAvatarDetail)HolonManager.Instance.MapMetaData<AvatarDetail>(result.Result);
                           PromoteInventoryNftIdFromMetaData(result.Result);
                       }
                   }
                   else
                       OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
               }
               else
                   OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
           }
           catch (Exception ex)
           {
               OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex);
           }

           return result;
       }

       private OASISResult<IAvatarDetail> LoadAvatarDetailByUsernameForProvider(string username, OASISResult<IAvatarDetail> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           return LoadAvatarDetailByUsernameForProviderAsync(username, result, providerType, version).Result;
       }

       private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameForProviderAsync(string username, OASISResult<IAvatarDetail> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           string errorMessageTemplate = "Error in LoadAvatarDetailByUsernameForProviderAsync method in AvatarManager loading avatar detail with username {0} for provider {1}. Reason: ";
           string errorMessage = string.Format(errorMessageTemplate, username, providerType);

           try
           {
               OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
               errorMessage = string.Format(errorMessageTemplate, username, ProviderManager.Instance.CurrentStorageProviderType.Name);

               if (!providerResult.IsError && providerResult.Result != null)
               {
                   var task = providerResult.Result.LoadAvatarDetailByUsernameAsync(username, version);

                   if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                   {
                       if (task.Result.IsError || task.Result.Result == null)
                       {
                           if (string.IsNullOrEmpty(task.Result.Message))
                               task.Result.Message = "Avatar Detail Not Found.";

                           OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                       }
                       else
                       {
                           result.IsLoaded = true;
                           result.Result = task.Result.Result;
                           if (result.Result != null)
                               result.Result = (IAvatarDetail)HolonManager.Instance.MapMetaData<AvatarDetail>(result.Result);
                           PromoteInventoryNftIdFromMetaData(result.Result);
                       }
                   }
                   else
                       OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
               }
               else
                   OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
           }
           catch (Exception ex)
           {
               OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex);
           }

           return result;
       }

       private OASISResult<IEnumerable<IAvatar>> LoadAllAvatarsForProvider(OASISResult<IEnumerable<IAvatar>> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           return LoadAllAvatarsForProviderAsync(result, providerType, version).Result;
       }

       private async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsForProviderAsync(OASISResult<IEnumerable<IAvatar>> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           string errorMessageTemplate = "Error in LoadAllAvatarsForProviderAsync method in AvatarManager loading all avatar details for provider {0}. Reason: ";
           string errorMessage = string.Format(errorMessageTemplate, providerType);

           try
           {
               OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
               errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

               if (!providerResult.IsError && providerResult.Result != null)
               {
                   var task = providerResult.Result.LoadAllAvatarsAsync(version);

                   if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                   {
                       if (task.Result.IsError || task.Result.Result == null)
                       {
                           if (string.IsNullOrEmpty(task.Result.Message))
                               task.Result.Message = "No Avatars Were Found.";

                           OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                       }
                       else
                       {
                           result.IsLoaded = true;
                           result.Result = task.Result.Result;
                       }
                   }
                   else
                       OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."), task.Result.DetailedMessage);
               }
               else
                   OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
           }
           catch (Exception ex)
           {
               OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex);
           }

           return result;
       }

       private OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsForProvider(OASISResult<IEnumerable<IAvatarDetail>> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           return LoadAllAvatarDetailsForProviderAsync(result, providerType, version).Result;
       }

       private async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsForProviderAsync(OASISResult<IEnumerable<IAvatarDetail>> result, ProviderType providerType = ProviderType.Default, int version = 0)
       {
           string errorMessageTemplate = "Error in LoadAllAvatarDetailsForProviderAsync method in AvatarManager loading all avatar details for provider {0}. Reason: ";
           string errorMessage = string.Format(errorMessageTemplate, providerType);

           try
           {
               OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
               errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

               if (!providerResult.IsError && providerResult.Result != null)
               {
                   var task = providerResult.Result.LoadAllAvatarDetailsAsync(version);

                   if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                   {
                       if (task.Result.IsError || task.Result.Result == null)
                       {
                           if (string.IsNullOrEmpty(task.Result.Message))
                               task.Result.Message = "No Avatar Details Were Found.";

                           OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                       }
                       else
                       {
                           result.IsLoaded = true;
                           result.Result = task.Result.Result;
                       }
                   }
                   else
                       OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
               }
               else
                   OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
           }
           catch (Exception ex)
           {
               OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message));
           }

           return result;
       }

    }
}
