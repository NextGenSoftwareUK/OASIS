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
        public OASISResult<IAvatar> Register(string avatarTitle, string firstName, string lastName, string email, string password, string username, AvatarType avatarType, OASISType createdOASISType, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green, bool callerIsWizard = false, bool suppressVerificationEmail = false)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                result = PrepareToRegisterAvatarAsync(avatarTitle, firstName, lastName, email, password, username, avatarType, createdOASISType).Result;

                if (result != null && !result.IsError && result.Result != null)
                {
                    // AvatarDetail needs to have the same unique ID as Avatar so the records match (they will have unique/different provider keys per each provider)
                    OASISResult<IAvatarDetail> avatarDetailResult = PrepareToRegisterAvatarDetail(result.Result.Id, result.Result.Username, result.Result.Email, createdOASISType, cliColour, favColour);

                    if (avatarDetailResult != null && !avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    {
                        OASISResult<IAvatar> saveAvatarResult = SaveAvatar(result.Result);

                        if (!saveAvatarResult.IsError && saveAvatarResult.IsSaved)
                        {
                            result.Result = saveAvatarResult.Result;
                            OASISResult<IAvatarDetail> saveAvatarDetailResult = SaveAvatarDetail(avatarDetailResult.Result);

                            if (saveAvatarDetailResult != null && !saveAvatarDetailResult.IsError && saveAvatarDetailResult.Result != null)
                                result = AvatarRegistered(result, callerIsWizard, suppressVerificationEmail);
                            else
                            {
                                result.Message = saveAvatarDetailResult.Message;
                                result.IsError = saveAvatarDetailResult.IsError;
                                result.IsSaved = saveAvatarDetailResult.IsSaved;
                            }
                        }
                        else
                        {
                            result.Message = saveAvatarResult.Message;
                            result.IsError = saveAvatarResult.IsError;
                            result.IsSaved = saveAvatarResult.IsSaved;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in Register method in AvatarManager. Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> RegisterAsync(string avatarTitle, string firstName, string lastName, string email, string password, string username, AvatarType avatarType, OASISType createdOASISType, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green, bool callerIsWizard = false, bool suppressVerificationEmail = false)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                result = await PrepareToRegisterAvatarAsync(avatarTitle, firstName, lastName, email, password, username, avatarType, createdOASISType);

                if (result != null && !result.IsError && result.Result != null)
                {
                    // AvatarDetail needs to have the same unique ID as Avatar so the records match (they will have unique/different provider keys per each provider)
                    OASISResult<IAvatarDetail> avatarDetailResult = PrepareToRegisterAvatarDetail(result.Result.Id, result.Result.Username, result.Result.Email, createdOASISType, cliColour, favColour);

                    if (avatarDetailResult != null && !avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    {
                        OASISResult<IAvatar> saveAvatarResult = await SaveAvatarAsync(result.Result);

                        if (!saveAvatarResult.IsError && saveAvatarResult.IsSaved)
                        {
                            result.Result = saveAvatarResult.Result;
                            OASISResult<IAvatarDetail> saveAvatarDetailResult = await SaveAvatarDetailAsync(avatarDetailResult.Result);

                            if (saveAvatarDetailResult != null && !saveAvatarDetailResult.IsError && saveAvatarDetailResult.Result != null)
                                result = AvatarRegistered(result, callerIsWizard, suppressVerificationEmail);
                            else
                            {
                                result.Message = saveAvatarDetailResult.Message;
                                result.IsError = saveAvatarDetailResult.IsError;
                                result.IsSaved = saveAvatarDetailResult.IsSaved;
                            }
                        }
                        else
                        {
                            result.Message = saveAvatarResult.Message;
                            result.IsError = saveAvatarResult.IsError;
                            result.IsSaved = saveAvatarResult.IsSaved;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in RegisterAsync method in AvatarManager. Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatar> BeamOut(IAvatar avatar, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            if (avatar == null)
                return new OASISResult<IAvatar> { IsError = true, Message = "The avatar is required. Please provide a valid avatar object." };
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            avatar.LastBeamedOut = DateTime.Now;
            result = SaveAvatar(avatar, autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, waitForAutoReplicationResult, providerType);
            return result;
        }

        public async Task<OASISResult<IAvatar>> BeamOutAsync(IAvatar avatar, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            if (avatar == null)
                return new OASISResult<IAvatar> { IsError = true, Message = "The avatar is required. Please provide a valid avatar object." };
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            avatar.LastBeamedOut = DateTime.Now;
            result = await SaveAvatarAsync(avatar, autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, waitForAutoReplicationResult, providerType);
            return result;
        }

        public OASISResult<bool> VerifyEmail(string token)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatarResult = LoadAvatarByVerificationTokenForProvider(token);

                if (avatarResult.IsError)
                    OASISErrorHandling.HandleError(ref result, $"Error in VerifyEmail loading avatar by verification token. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
                else if (avatarResult.Result == null)
                {
                    result.Result = false;
                    result.IsError = true;
                    result.Message = "Verification Failed";
                }
                else
                {
                    IAvatar avatar = avatarResult.Result;
                    result.Result = true;
                    avatar.Verified = DateTime.UtcNow;
                    avatar.VerificationToken = null;
                    avatar.IsActive = true;
                    OASISResult<IAvatar> saveAvatarResult = SaveAvatar(avatar);

                    result.IsError = saveAvatarResult.IsError;
                    result.IsSaved = saveAvatarResult.IsSaved;
                    result.Message = saveAvatarResult.Message;
                }

                if (!result.IsError && result.IsSaved)
                {
                    result.Message = "Verification successful, you can now login";
                    result.Result = true;
                }
                else
                    result.Result = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in VerifyEmail method in AvatarManager. Error Message: ", ex.Message), ex);
                result.Result = false;
            }

            return result;
        }

        //public async Task<OASISResult<string>> ForgotPassword(ForgotPasswordRequest model)
        public async Task<OASISResult<string>> ForgotPasswordAsync(string email, ProviderType providerType = ProviderType.Default, string returnUrl = null)
        {
            var response = new OASISResult<string>();

            try
            {
                OASISResult<IAvatar> avatarResult = await LoadAvatarByEmailAsync(email, false, false, providerType);

                // always return ok response to prevent email enumeration
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error occured loading avatar in ForgotPassword, avatar not found. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
                    return response;
                }

                // create reset token that expires after 1 day
                avatarResult.Result.ResetToken = RandomTokenString();
                avatarResult.Result.ResetTokenExpires = DateTime.UtcNow.AddDays(24);

                var saveAvatar = SaveAvatar(avatarResult.Result, providerType: providerType);

                if (saveAvatar.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"An error occured saving the avatar in ForgotPassword method in AvatarService. Reason: {saveAvatar.Message}", saveAvatar.DetailedMessage);
                    return response;
                }

                // send email
                SendPasswordResetEmail(avatarResult.Result, returnUrl);
                response.Message = "Please check your email for password reset instructions";
                response.Result = response.Message;
            }
            catch (Exception e)
            {
                response.Exception = e;
                OASISErrorHandling.HandleError(ref response, $"An error occured in ForgotPassword method in AvatarService. Reason: {e.Message}");
            }

            return response;
        }

        public OASISResult<string> ForgotPassword(string email, ProviderType providerType = ProviderType.Default, string returnUrl = null)
        {
            var response = new OASISResult<string>();

            try
            {
                OASISResult<IAvatar> avatarResult = LoadAvatarByEmail(email, false, false, providerType);

                // always return ok response to prevent email enumeration
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error occured loading avatar in ForgotPassword, avatar not found. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
                    return response;
                }

                // create reset token that expires after 1 day
                avatarResult.Result.ResetToken = RandomTokenString();
                avatarResult.Result.ResetTokenExpires = DateTime.UtcNow.AddDays(24);

                var saveAvatar = SaveAvatar(avatarResult.Result, providerType: providerType);

                if (saveAvatar.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"An error occured saving the avatar in ForgotPassword method in AvatarService. Reason: {saveAvatar.Message}", saveAvatar.DetailedMessage);
                    return response;
                }

                // send email
                SendPasswordResetEmail(avatarResult.Result, returnUrl);
                response.Message = "Please check your email for password reset instructions";
                response.Result = response.Message;
            }
            catch (Exception e)
            {
                response.Exception = e;
                OASISErrorHandling.HandleError(ref response, $"An error occured in ForgotPassword method in AvatarService. Reason: {e.Message}");
            }

            return response;
        }

        public async Task<OASISResult<string>> ResetPasswordAsync(string token, string oldPassword, string newPassword, ProviderType providerType = ProviderType.Default)
        {
            var response = new OASISResult<string>();

            try
            {
                OASISResult<IAvatar> avatarResult = await LoadAvatarByResetTokenForProviderAsync(token, providerType);

                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error occured in ResetPassword loading avatar by reset token. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
                    return response;
                }

                var avatar = avatarResult.Result?.ResetTokenExpires > DateTime.UtcNow ? avatarResult.Result : null;

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found, token is invalid.");
                    return response;
                }

                // oldPassword is optional when authenticating via a reset token
                var pwdSettings = OASISDNAManager.OASISDNA?.OASIS?.Security?.AvatarPassword;
                if (!string.IsNullOrEmpty(oldPassword) && !PasswordEncryptionHelper.VerifyPassword(oldPassword, avatar.Password, pwdSettings))
                {
                    OASISErrorHandling.HandleError(ref response, "Old Password Is Not Correct");
                    return response;
                }

                // update password and remove reset token
                avatar.Password = PasswordEncryptionHelper.HashPassword(newPassword, pwdSettings);
                avatar.PasswordReset = DateTime.UtcNow;
                avatar.ResetToken = null;
                avatar.ResetTokenExpires = null;

                var saveAvatarResult = await SaveAvatarAsync(avatar, providerType: providerType);

                if (saveAvatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref saveAvatarResult, $"Error occured in ResetPassword saving the avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    return response;
                }

                if (_loggedInAvatar.Id == avatar.Id)
                    _loggedInAvatar = avatar;

                response.Message = "Password reset successful, you can now login";
                response.Result = response.Message;
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                response.IsSaved = false;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public OASISResult<string> ResetPassword(string token, string oldPassword, string newPassword, ProviderType providerType = ProviderType.Default)
        {
            var response = new OASISResult<string>();

            try
            {
                OASISResult<IAvatar> avatarResult = LoadAvatarByResetTokenForProvider(token, providerType);

                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error occured in ResetPassword loading avatar by reset token. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
                    return response;
                }

                var avatar = avatarResult.Result?.ResetTokenExpires > DateTime.UtcNow ? avatarResult.Result : null;

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found, token is invalid.");
                    return response;
                }

                // oldPassword is optional when authenticating via a reset token
                var pwdSettings = OASISDNAManager.OASISDNA?.OASIS?.Security?.AvatarPassword;
                if (!string.IsNullOrEmpty(oldPassword) && !PasswordEncryptionHelper.VerifyPassword(oldPassword, avatar.Password, pwdSettings))
                {
                    OASISErrorHandling.HandleError(ref response, "Old Password Is Not Correct");
                    return response;
                }

                // update password and remove reset token
                avatar.Password = PasswordEncryptionHelper.HashPassword(newPassword, pwdSettings);
                avatar.PasswordReset = DateTime.UtcNow;
                avatar.ResetToken = null;
                avatar.ResetTokenExpires = null;

                var saveAvatarResult = SaveAvatar(avatar, providerType: providerType);

                if (saveAvatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref saveAvatarResult, $"Error occured in ResetPassword saving the avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    return response;
                }

                response.Message = "Password reset successful, you can now login";
                response.Result = response.Message;
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                response.IsSaved = false;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }
        public string RandomTokenString()
        {
            using var rngCryptoServiceProvider = new System.Security.Cryptography.RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            // convert random bytes to hex string
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }

        //TODO: Finish moving Update methods and ALL AvatarService methods here ASAP!
        //Update also needs to be able to update ANY avatar property, currently it is only email, name, etc.

        /*
        public async Task<OASISResult<IAvatar>> Update(Guid id, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            string errorMessage = "Error in Update method in Avatar Service. Reason: ";

            try
            {
                response = await AvatarManager.LoadAvatarAsync(id, false);

                if (response.IsError || response.Result == null)
                    OASISErrorHandling.HandleError(ref response, $"{errorMessage}{response.Message}", response.DetailedMessage);
                else
                    response = await Update(response.Result, avatar);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"{errorMessage}Unknown Error Occured. See DetailedMessage for more info.", ex.Message, ex);
            }

            return response;
        }

        public async Task<OASISResult<IAvatar>> UpdateByEmail(string email, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            string errorMessage = "Error in UpdateByEmail method in Avatar Service. Reason: ";

            try
            {
                response = await AvatarManager.LoadAvatarByEmailAsync(email);

                if (response.IsError || response.Result == null)
                    OASISErrorHandling.HandleError(ref response, $"{errorMessage}{response.Message}", response.DetailedMessage);
                else
                    response = await Update(response.Result, avatar);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"{errorMessage}Unknown Error Occured. See DetailedMessage for more info.", ex.Message, ex);
            }

            return response;
        }

        public async Task<OASISResult<IAvatar>> UpdateByUsername(string username, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            string errorMessage = "Error in UpdateByUsername method in Avatar Service. Reason: ";

            try
            {
                response = await AvatarManager.LoadAvatarAsync(username);

                if (response.IsError || response.Result == null)
                    OASISErrorHandling.HandleError(ref response, $"{errorMessage}{response.Message}", response.DetailedMessage);
                else
                    response = await Update(response.Result, avatar);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"{errorMessage}Unknown Error Occured. See DetailedMessage for more info.", ex.Message, ex);
            }

            return response;
        }*/

        public OASISResult<bool> CheckIfEmailIsAlreadyInUse(string email, bool sendMail = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            //Temp supress logging to the console in case STAR CLI is creating a new avatar...
            //CLIEngine.SupressConsoleLogging = true;

            //Temp disable the OASIS HyperDrive so it returns fast and does not attempt to find the avatar across all providers! ;-)
            //TODO: May want to fine tune how we handle this in future?
            bool isAutoFailOverEnabled = ProviderManager.Instance.IsAutoFailOverEnabled;
            ProviderManager.Instance.IsAutoFailOverEnabled = false;

            try
            {
                // Use the currently active provider directly — bypasses HyperDrive failover so no DNA config needed and the check is fast.
                OASISResult<IAvatar> existingAvatarResult = LoadAvatarByEmail(email);

                if (!existingAvatarResult.IsError && existingAvatarResult.Result != null)
                {
                    if (existingAvatarResult.Result.DeletedDate != DateTime.MinValue)
                    {
                        result.Result = true;
                        OASISErrorHandling.HandleError(ref result, $"The avatar using email {email} was deleted on {existingAvatarResult.Result.DeletedDate} by avatar with id {existingAvatarResult.Result.DeletedByAvatarId}, please contact support (to either restore your old avatar or permanently delete your old avatar so you can then re-use your old email address to create a new avatar) or create a new avatar with a new email address.");
                    }
                    else
                    {
                        result.Result = true;
                        OASISErrorHandling.HandleError(ref result, $"Sorry, the email {email} is already in use, please use another one.");
                    }
                }
                else
                    result.Message = $"Email {email} not in use.";

                if (result.Result && sendMail)
                    SendAlreadyRegisteredEmail(email, result.Message);
            }
            finally
            {
                ProviderManager.Instance.IsAutoFailOverEnabled = isAutoFailOverEnabled;
            }

            return result;
        }

        public OASISResult<bool> CheckIfUsernameIsAlreadyInUse(string username)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            ////Temp supress logging to the console in case STAR CLI is creating a new avatar...
            CLIEngine.SupressConsoleLogging = true;

            //Temp disable the OASIS HyperDrive so it returns fast and does not attempt to find the avatar across all providers! ;-)
            //TODO: May want to fine tune how we handle this in future?
            bool isAutoFailOverEnabled = ProviderManager.Instance.IsAutoFailOverEnabled;
            ProviderManager.Instance.IsAutoFailOverEnabled = false;

            try
            {
                // Use the currently active provider directly — bypasses HyperDrive failover so no DNA config needed and the check is fast.
                OASISResult<IAvatar> existingAvatarResult = LoadAvatar(username);

                CLIEngine.SupressConsoleLogging = false;

                if (!existingAvatarResult.IsError && existingAvatarResult.Result != null)
                {
                    if (existingAvatarResult.Result.DeletedDate != DateTime.MinValue)
                    {
                        result.Result = true;
                        OASISErrorHandling.HandleError(ref result, $"The avatar using username {username} was deleted on {existingAvatarResult.Result.DeletedDate} by avatar with id {existingAvatarResult.Result.DeletedByAvatarId}, please contact support (to either restore your old avatar or permanently delete your old avatar so you can then re-use your old email address to create a new avatar) or create a new avatar with a new email address.");
                    }
                    else
                    {
                        result.Result = true;
                        OASISErrorHandling.HandleError(ref result, $"Sorry, the username {username} is already in use, please use another one.");
                    }
                }
                else
                    result.Message = $"Username {username} not in use.";
            }
            finally
            {
                CLIEngine.SupressConsoleLogging = false;
                ProviderManager.Instance.IsAutoFailOverEnabled = isAutoFailOverEnabled;
            }

            return result;
        }

    }
}
