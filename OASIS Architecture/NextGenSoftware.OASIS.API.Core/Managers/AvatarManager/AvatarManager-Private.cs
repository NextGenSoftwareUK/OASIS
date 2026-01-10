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
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        private const string LIVE_OASISSITE = "https://oasisweb4.one";

        private string OASISWebSiteURL
        {
            get
            {
                if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.OASISWebSiteURL))
                    OASISDNA.OASIS.Email.OASISWebSiteURL = LIVE_OASISSITE;

                return OASISDNA.OASIS.Email.OASISWebSiteURL;
            }
        }

        private void SendPasswordResetEmail(IAvatar avatar)
        {
            string message;

            var resetUrl = $"{OASISWebSiteURL}/avatar/reset-password?token={avatar.ResetToken}";
            message =
                $@"<p>Please click the below link to reset your password, the link will be valid for 1 day:</p>
                             <p><a href=""{resetUrl}"">{resetUrl}</a></p>";


            //if (!string.IsNullOrEmpty(origin))
            //{
            //    var resetUrl = $"{OASISDNA.OASIS.Email.VerificationWebSiteURL}/avatar/reset-password?token={avatar.ResetToken}";
            //    message =
            //        $@"<p>Please click the below link to reset your password, the link will be valid for 1 day:</p>
            //                 <p><a href=""{resetUrl}"">{resetUrl}</a></p>";
            //}
            //else
            //{
            //    message =
            //        $@"<p>Please use the below token to reset your password with the <code>/avatar/reset-password</code> api route:</p>
            //                 <p><code>{avatar.ResetToken}</code></p>";
            //}

            if (!EmailManager.IsInitialized)
                EmailManager.Initialize(OASISDNA);

            EmailManager.Send(
                avatar.Email,
                "OASIS - Reset Password",
                $@"<h4>Reset Password</h4>
                         {message}"
            );
        }

        private void SendAlreadyRegisteredEmail(string email, string message)
        {
            message = String.Concat($"<p>{message}</p>", $@"<p>If you don't know your password please visit the <a href=""{OASISWebSiteURL}/avatar/forgot-password"">forgot password</a> page.</p>");

            //if (!string.IsNullOrEmpty(origin))
            //    message = $@"<p>If you don't know your password please visit the <a href=""{origin}/avatar/forgot-password"">forgot password</a> page.</p>";
            //else
            //    message = "<p>If you don't know your password you can reset it via the <code>/avatar/forgot-password</code> api route.</p>";

            if (!EmailManager.IsInitialized)
                EmailManager.Initialize(OASISDNA);

            EmailManager.Send(
                to: email,
                subject: "OASIS Sign-up Verification - Email Already Registered",
                html: $@"<h4>Email Already Registered</h4>{message}"
                //html: $@"<h4>Email Already Registered</h4>
                //         <p>Your email <strong>{email}</strong> is already registered.</p>
                //         {message}"
            );
        }

        private void SendVerificationEmail(IAvatar avatar)
        {
            var verifyUrl = $"{OASISWebSiteURL}/avatar/verify-email?token={avatar.VerificationToken}";
            string message = $@"<p>Please click the below link to verify your email address:</p>
                             <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";

            //if (!string.IsNullOrEmpty(OASISDNA.OASIS.Email.VerificationWebSiteURL))
            //{
            //    var verifyUrl = $"{OASISDNA.OASIS.Email.VerificationWebSiteURL}/avatar/verify-email?token={avatar.VerificationToken}";
            //    message = $@"<p>Please click the below link to verify your email address:</p>
            //                 <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";
            //}
            //else
            //{
            //    message = $@"<p>Please use the below token to verify your email address with the <code>/avatar/verify-email</code> api route:</p>
            //                 <p><code>{avatar.VerificationToken}</code></p>";
            //}

            if (!EmailManager.IsInitialized)
                EmailManager.Initialize(OASISDNA);

            EmailManager.Send(
                to: avatar.Email,
                subject: "OASIS Sign-up Verification - Verify Email",
                //html: $@"<h4>Verify Email</h4>
                html: $@"<h4>Verify Email</h4>
                         <p>Thanks for registering!</p>
                         <p>Welcome to the OASIS!</p>
                         <p>Ready Player One?</p>
                         {message}"
            );
        }

        private async Task<OASISResult<IAvatar>> PrepareToRegisterAvatarAsync(string avatarTitle, string firstName, string lastName, string email, string password, string username, AvatarType avatarType, OASISType createdOASISType)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            if (!ValidationHelper.IsValidEmail(email))
            {
                result.IsError = true;
                result.Message = "The email is not valid.";
                return result;
            }

            OASISResult<bool> checkIfEmailExistsResult = CheckIfEmailIsAlreadyInUse(email);
            
            if (checkIfEmailExistsResult.Result)
            {
                result.IsError = true;
                result.Message = checkIfEmailExistsResult.Message;
                return result;
            }

            result.Result = new Avatar() 
            { 
                Id = Guid.NewGuid(), 
                IsNewHolon = true, FirstName = firstName, 
                LastName = lastName, Password = password, 
                Title = avatarTitle, Email = email, 
                AvatarType = new EnumValue<AvatarType>(avatarType), 
                CreatedOASISType = new EnumValue<OASISType>(createdOASISType),
                CreatedDate = DateTime.Now
            };
            //result.Result.Username = result.Result.Email; //Default the username to their email (they can change this later in Avatar Profile screen).

            result.Result.ProviderWallets = new Dictionary<ProviderType, List<IProviderWallet>>();

            foreach (IOASISBlockchainStorageProvider provider in ProviderManager.Instance.GetAllBlockchainProviders())
            {
                OASISResult<IProviderWallet> walletResult = WalletManager.Instance.CreateWalletWithoutSaving(result.Result.Id, $"Default {Enum.GetName(typeof(ProviderType), provider)} Wallet", $"Default wallet for chain {Enum.GetName(typeof(ProviderType), provider)}", provider.ProviderType.Value, isDefaultWallet: true);

                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                    result.Result.ProviderWallets[provider.ProviderType.Value].Add(walletResult.Result);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured creating default wallet for provider/chain {walletResult.Message}");
            }

            OASISResult<bool> saveWalletsResult = await WalletManager.Instance.SaveProviderWalletsForAvatarByIdAsync(result.Result.Id, result.Result.ProviderWallets);
            
            if (!(saveWalletsResult != null && saveWalletsResult.Result != null && !saveWalletsResult.IsError))
                OASISErrorHandling.HandleError(ref result, $"Error occured saving the default wallets. Reason: {saveWalletsResult.Message}");

            result.Result.CreatedByAvatarId = result.Result.Id;
            OASISResult<bool> checkIfUsernameExistsResult = CheckIfUsernameIsAlreadyInUse(email);

            if (checkIfUsernameExistsResult.Result)
            {
                result.IsError = true;
                result.Message = checkIfUsernameExistsResult.Message;
                return result;
            }

            result.Result.Username = username;
            result.Result.VerificationToken = randomTokenString();

            // hash password
            result.Result.Password = BC.HashPassword(password);
            return result;
        }

        private OASISResult<IAvatarDetail> PrepareToRegisterAvatarDetail(Guid avatarId, string username, string email, OASISType createdOASISType, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            IAvatarDetail avatarDetail = new AvatarDetail() 
            { 
                Id = avatarId, IsNewHolon = true, 
                Email = email, Username = username, 
                CreatedOASISType = new EnumValue<OASISType>(createdOASISType), 
                STARCLIColour = cliColour, 
                FavouriteColour = favColour,
                CreatedDate = DateTime.Now,
                CreatedByAvatarId = avatarId
            };

            // TODO: Temp! Remove later!
            if (email == "davidellams@hotmail.com")
            {
                avatarDetail.Karma = 777777;
                avatarDetail.XP = 2222222;

                avatarDetail.GeneKeys.Add(new GeneKey() { Name = "Expectation", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });
                avatarDetail.GeneKeys.Add(new GeneKey() { Name = "Invisibility", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });
                avatarDetail.GeneKeys.Add(new GeneKey() { Name = "Rapture", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });

                avatarDetail.HumanDesign.Type = "Generator";
                //avatarDetail.Inventory.Add(new InventoryItem() { Name = "Magical Armour" });
                //avatarDetail.Inventory.Add(new InventoryItem() { Name = "Mighty Wizard Sword" });

                avatarDetail.Spells.Add(new Spell() { Name = "Super Spell" });
                avatarDetail.Spells.Add(new Spell() { Name = "Super Speed Spell" });
                avatarDetail.Spells.Add(new Spell() { Name = "Super Srength Spell" });

                avatarDetail.Achievements.Add(new Achievement() { Name = "Becoming Superman!" });
                avatarDetail.Achievements.Add(new Achievement() { Name = "Completing STAR!" });

                avatarDetail.Gifts.Add(new AvatarGift() { GiftType = KarmaTypePositive.BeASuperHero });

                avatarDetail.Aura.Brightness = 99;
                avatarDetail.Aura.Level = 77;
                avatarDetail.Aura.Progress = 88;
                avatarDetail.Aura.Size = 10;
                avatarDetail.Aura.Value = 777;

                avatarDetail.Chakras.Root.Level = 77;
                avatarDetail.Chakras.Root.Progress = 99;
                avatarDetail.Chakras.Root.XP = 8783;

                avatarDetail.Attributes.Dexterity = 99;
                avatarDetail.Attributes.Endurance = 99;
                avatarDetail.Attributes.Intelligence = 99;
                avatarDetail.Attributes.Magic = 99;
                avatarDetail.Attributes.Speed = 99;
                avatarDetail.Attributes.Strength = 99;
                avatarDetail.Attributes.Toughness = 99;
                avatarDetail.Attributes.Vitality = 99;
                avatarDetail.Attributes.Wisdom = 99;

                avatarDetail.Stats.Energy.Current = 99;
                avatarDetail.Stats.Energy.Max = 99;
                avatarDetail.Stats.HP.Current = 99;
                avatarDetail.Stats.HP.Max = 99;
                avatarDetail.Stats.Mana.Current = 99;
                avatarDetail.Stats.Mana.Max = 99;
                avatarDetail.Stats.Stamina.Current = 99;
                avatarDetail.Stats.Stamina.Max = 99;

                avatarDetail.SuperPowers.AstralProjection = 99;
                avatarDetail.SuperPowers.BioLocatation = 88;
                avatarDetail.SuperPowers.Flight = 99;
                avatarDetail.SuperPowers.FreezeBreath = 88;
                avatarDetail.SuperPowers.HeatVision = 99;
                avatarDetail.SuperPowers.Invulnerability = 99;
                avatarDetail.SuperPowers.SuperSpeed = 99;
                avatarDetail.SuperPowers.SuperStrength = 99;
                avatarDetail.SuperPowers.XRayVision = 99;
                avatarDetail.SuperPowers.Teleportation = 99;
                avatarDetail.SuperPowers.Telekinesis = 99;

                avatarDetail.Skills.Computers = 99;
                avatarDetail.Skills.Engineering = 99;
            }

            //avatarDetail.CreatedDate = DateTime.UtcNow;

            result.Result = avatarDetail;
            return result;
        }

        private OASISResult<IAvatar> AvatarRegistered(OASISResult<IAvatar> result)
        {
            if (OASISDNA.OASIS.Email.SendVerificationEmail)
                SendVerificationEmail(result.Result);

            result.Result = HideAuthDetails(result.Result);
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

       public async Task<OASISResult<IAvatar>> SaveAvatarForProviderAsync(IAvatar avatar, OASISResult<IAvatar> result, SaveMode saveMode, ProviderType providerType = ProviderType.Default)
       {
           string errorMessageTemplate = "Error in SaveAvatarDetailForProviderAsync method in AvatarManager saving avatar with name {0}, username {1} and id {2} for provider {3} for {4}. Reason: ";
           string errorMessage = string.Format(errorMessageTemplate, avatar.Name, avatar.Username, avatar.Id, providerType, Enum.GetName(typeof(SaveMode), saveMode));

           try
           {
               OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
               errorMessage = string.Format(errorMessageTemplate, avatar.Name, avatar.Username, avatar.Id, ProviderManager.Instance.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

               if (!providerResult.IsError && providerResult.Result != null)
               {
                   //Make sure private keys are ONLY stored locally.
                   if (ProviderManager.Instance.CurrentStorageProviderCategory.Value != ProviderCategory.StorageLocal && ProviderManager.Instance.CurrentStorageProviderCategory.Value != ProviderCategory.StorageLocalAndNetwork)
                   {
                       foreach (ProviderType proType in avatar.ProviderWallets.Keys)
                       {
                           foreach (IProviderWallet wallet in avatar.ProviderWallets[proType])
                               wallet.PrivateKey = null;
                       }
                   }
                   else
                   {
                       //We need to save the wallets (with private keys) seperatley to the local storage provider otherwise the next time a non local provider replicates to local it will overwrite the wallets and private keys (will be blank).
                       //TODO: The PrivateKeys are already encrypted but I want to add an extra layer of protection to encrypt the full wallet! ;-)
                       //TODO: Soon will also add a 3rd level of protection by quantum encrypting the keys/wallets... :)
                       /*
                       OASISResult<bool> walletsResult = await WalletManager.Instance.SaveProviderWalletsForAvatarByIdAsync(avatar.Id, avatar.ProviderWallets, providerType);

                       if (walletsResult.IsError || !walletsResult.Result)
                       {
                           if (string.IsNullOrEmpty(walletsResult.Message) && saveMode != SaveMode.AutoReplication)
                               walletsResult.Message = "Unknown error occured saving provider wallets.";

                           OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, walletsResult.Message), walletsResult.DetailedMessage, saveMode == SaveMode.AutoReplication);
                       }
                       */

    }

    var task = providerResult.Result.SaveAvatarAsync(avatar);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                    {
                        if (task.Result.IsError || task.Result.Result == null)
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

        private OASISResult<IAvatar> SaveAvatarForProvider(IAvatar avatar, OASISResult<IAvatar> result, SaveMode saveMode, ProviderType providerType = ProviderType.Default)
        {
            string errorMessageTemplate = "Error in SaveAvatarForProvider method in AvatarManager saving avatar with name {0}, username {1} and id {2} for provider {3} for {4}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, avatar.Name, avatar.Username, avatar.Id, providerType, Enum.GetName(typeof(SaveMode), saveMode));

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);
                errorMessage = string.Format(errorMessageTemplate, avatar.Name, avatar.Username, avatar.Id, ProviderManager.Instance.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    //Make sure private keys are ONLY stored locally.
                    if (ProviderManager.Instance.CurrentStorageProviderCategory.Value != ProviderCategory.StorageLocal && ProviderManager.Instance.CurrentStorageProviderCategory.Value != ProviderCategory.StorageLocalAndNetwork)
                    {
                        foreach (ProviderType proType in avatar.ProviderWallets.Keys)
                        {
                            foreach (IProviderWallet wallet in avatar.ProviderWallets[proType])
                                wallet.PrivateKey = null;
                        }
                    }
                    else
                    {
                        //TODO: Was going to load the private keys from the local storage and then restore any missing private keys before saving (in case they had been removed before saving to a non-local storage provider) but then there will be no way of knowing if the keys have been removed by the user (if they were then this would then incorrectly restore them again!).
                        //Commented out code was an alternative to saving the private keys seperatley as the next block below does...
                        //(result, IAvatar originalAvatar) = OASISResultHelper<IAvatar, IAvatar>.UnWrapOASISResult(ref result, LoadAvatar(avatar.Id, true, providerType), String.Concat(errorMessage, "Error loading avatar. Reason: {0}"));

                        //if (!result.IsError)
                        //{

                        //}


                        //We need to save the wallets (with private keys) seperatley to the local storage provider otherwise the next time a non local provider replicates to local it will overwrite the wallets and private keys (will be blank).
                        //TODO: The PrivateKeys are already encrypted but I want to add an extra layer of protection to encrypt the full wallet! ;-)
                        //TODO: Soon will also add a 3rd level of protection by quantum encrypting the keys/wallets... :)

                        //OASISResult<bool> walletsResult = WalletManager.Instance.SaveProviderWalletsForAvatarById(avatar.Id, avatar.ProviderWallets, providerType);

                        //if (walletsResult.IsError || !walletsResult.Result)
                        //{
                        //    if (string.IsNullOrEmpty(walletsResult.Message) && saveMode != SaveMode.AutoReplication)
                        //        walletsResult.Message = "Unknown error occured saving provider wallets.";

                        //    OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, walletsResult.Message), walletsResult.DetailedMessage, saveMode == SaveMode.AutoReplication);
                        //}
                    }

                    var task = Task.Run(() => providerResult.Result.SaveAvatar(avatar));

                    if (task.Wait(TimeSpan.FromSeconds(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)))
                    {
                        if (task.Result.IsError || task.Result.Result == null)
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


        private IAvatar PrepareAvatarForSaving(IAvatar avatar)
        {
            if (string.IsNullOrEmpty(avatar.Username))
                avatar.Username = avatar.Email;

            if (avatar.Id == Guid.Empty || avatar.CreatedDate == DateTime.MinValue)
            {
                if (avatar.Id == Guid.Empty)
                    avatar.Id = Guid.NewGuid();

                avatar.IsNewHolon = true;
            }
            else if (avatar.CreatedDate != DateTime.MinValue)
                avatar.IsNewHolon = false;

            // TODO: I think it's best to include audit stuff here so the providers do not need to worry about it?
            // Providers could always override this behaviour if they choose...
            if (!avatar.IsNewHolon)
            {
                avatar.ModifiedDate = DateTime.Now;

                if (LoggedInAvatar != null)
                    avatar.ModifiedByAvatarId = LoggedInAvatar.Id;

                avatar.Version++;
                avatar.PreviousVersionId = avatar.VersionId;
                avatar.VersionId = Guid.NewGuid();
            }
            else
            {
                avatar.IsActive = true;
                avatar.CreatedDate = DateTime.Now;

                if (LoggedInAvatar != null)
                    avatar.CreatedByAvatarId = LoggedInAvatar.Id;

                avatar.Version = 1;
                avatar.VersionId = Guid.NewGuid();
            }

            return avatar;
        }

        private string GenerateJWTToken(IAvatar account)
        {
            //TODO: Replace exception with OASISResult ASAP.
            if (string.IsNullOrEmpty(OASISDNA.OASIS.Security.SecretKey))
                throw new ArgumentNullException("OASISDNA.OASIS.Security.SecretKey", "OASISDNA.OASIS.Security.SecretKey is missing, please generate a unique secret key from two GUID's.");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(OASISDNA.OASIS.Security.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken generateRefreshToken(string ipAddress)
        {
            return new RefreshToken
            {
                Token = randomTokenString(),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }

        private string randomTokenString()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            // convert random bytes to hex string
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }

        //TODO: Get this working later to make methods above more generic like HolonManager is! :)
        //private async Task<T> CallAvatarMethod<T>(OASISResult<T> result, Task task)
        //{
        //    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
        //    {
        //        if (task.Result.IsError || !task.Result.Result)
        //        {
        //            if (string.IsNullOrEmpty(task.Result.Message) && saveMode != SaveMode.AutoReplication)
        //                task.Result.Message = "Unknown.";

        //            OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage, saveMode == SaveMode.AutoReplication);
        //        }
        //        else
        //        {
        //            result.IsSaved = true;
        //            result.Result = task.Result.Result;
        //        }
        //    }
        //    else
        //        OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."), saveMode == SaveMode.AutoReplication);

        //    return result;
        //}

        private OASISResult<IAvatar> ProcessAvatarLogin(OASISResult<IAvatar> result, string password)
        {
            if (result.Result != null)
            {
                if (result.Result.DeletedDate != DateTime.MinValue)
                {
                    result.IsError = true;
                    result.Message = $"This avatar was deleted on {result.Result.DeletedDate} by avatar with id {result.Result.DeletedByAvatarId}, please contact support (to either restore your old avatar or permanently delete your old avatar so you can then re-use your old email address to create a new avatar) or create a new avatar with a new email address.";
                }

                if (!result.Result.IsActive)
                {
                    result.IsError = true;
                    result.Message = "This avatar is no longer active. Please contact support or create a new avatar.";
                }

                if (!result.Result.IsVerified)
                {
                    result.IsError = true;
                    result.Message = "Avatar has not been verified. Please check your email.";
                }

                if (result.Result.Password != null)
                {
                    if (!BC.Verify(password, result.Result.Password))
                    {
                        result.IsError = true;
                        result.Message = "Email or password is incorrect";
                    }
                }
                else
                {
                    result.Result.Password = BC.HashPassword("changemenow!");
                    OASISResult<IAvatar> saveResult = SaveAvatar(result.Result);
                    result.IsError = true;

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        result.Message = "Avatar is corrupt, the password has been re-set to 'changemenow!', please change your password once you login!";
                    else
                        result.Message = "Avatar is corrupt, please re-set password or create new avatar.";
                }
            }

            return result;
        }

        private async Task<OASISResult<IAvatarDetail>> UpdateAvatarDetailAsync(IAvatarDetail avatarDetailOriginal, IAvatarDetail avatarDetailToUpdate, string errorMessage, bool appendChildObjects = false)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();

            //Initialize the mapper
            //var config = new MapperConfiguration(cfg =>
            //        cfg.CreateMap<AvatarDetail, AvatarDetail>() );

            //var mapper = new AutoMapper.Mapper(config);
            //avatarDetailOriginal = mapper.Map<AvatarDetail>(avatarDetail);

            //Unfortunatley AutoMapper didn't seem to work correctly and override existing values with null ones, etc.
            //TODO: Need to look into it later...
            //TODO: Need to also map the child complex objects/structs such as Gifts, Spells, Achievements, etc, etc...

            if (avatarDetailOriginal.Address != avatarDetailToUpdate.Address && !string.IsNullOrEmpty(avatarDetailToUpdate.Address))
                avatarDetailOriginal.Address = avatarDetailToUpdate.Address;

            if (avatarDetailOriginal.Town != avatarDetailToUpdate.Town && !string.IsNullOrEmpty(avatarDetailToUpdate.Town))
                avatarDetailOriginal.Town = avatarDetailToUpdate.Town;

            if (avatarDetailOriginal.County != avatarDetailToUpdate.County && !string.IsNullOrEmpty(avatarDetailToUpdate.County))
                avatarDetailOriginal.County = avatarDetailToUpdate.County;

            if (avatarDetailOriginal.Country != avatarDetailToUpdate.Country && !string.IsNullOrEmpty(avatarDetailToUpdate.Country))
                avatarDetailOriginal.Country = avatarDetailToUpdate.Country;

            if (avatarDetailOriginal.Postcode != avatarDetailToUpdate.Postcode && !string.IsNullOrEmpty(avatarDetailToUpdate.Postcode))
                avatarDetailOriginal.Postcode = avatarDetailToUpdate.Postcode;

            if (avatarDetailOriginal.Mobile != avatarDetailToUpdate.Mobile && !string.IsNullOrEmpty(avatarDetailToUpdate.Mobile))
                avatarDetailOriginal.Mobile = avatarDetailToUpdate.Mobile;

            if (avatarDetailOriginal.Landline != avatarDetailToUpdate.Landline && !string.IsNullOrEmpty(avatarDetailToUpdate.Landline))
                avatarDetailOriginal.Landline = avatarDetailToUpdate.Landline;

            if (avatarDetailOriginal.Email != avatarDetailToUpdate.Email && !string.IsNullOrEmpty(avatarDetailToUpdate.Email))
                avatarDetailOriginal.Email = avatarDetailToUpdate.Email;

            if (avatarDetailOriginal.Username != avatarDetailToUpdate.Username && !string.IsNullOrEmpty(avatarDetailToUpdate.Username))
                avatarDetailOriginal.Username = avatarDetailToUpdate.Username;

            if (avatarDetailOriginal.DOB != avatarDetailToUpdate.DOB && avatarDetailToUpdate.DOB != DateTime.MinValue)
                avatarDetailOriginal.DOB = avatarDetailToUpdate.DOB;

            if (avatarDetailOriginal.Karma != avatarDetailToUpdate.Karma && avatarDetailToUpdate.Karma > 0)
                avatarDetailOriginal.Karma = avatarDetailToUpdate.Karma;

            if (avatarDetailOriginal.XP != avatarDetailToUpdate.XP && avatarDetailToUpdate.XP > 0)
                avatarDetailOriginal.XP = avatarDetailToUpdate.XP;

            if (avatarDetailOriginal.STARCLIColour != avatarDetailToUpdate.STARCLIColour && avatarDetailToUpdate.STARCLIColour != ConsoleColor.Black)
                avatarDetailOriginal.STARCLIColour = avatarDetailToUpdate.STARCLIColour;

            if (avatarDetailOriginal.FavouriteColour != avatarDetailToUpdate.FavouriteColour && avatarDetailToUpdate.FavouriteColour != ConsoleColor.Black)
                avatarDetailOriginal.FavouriteColour = avatarDetailToUpdate.FavouriteColour;

            if (avatarDetailOriginal.Portrait != avatarDetailToUpdate.Portrait && !string.IsNullOrEmpty(avatarDetailToUpdate.Portrait))
                avatarDetailOriginal.Portrait = avatarDetailToUpdate.Portrait;

            if (avatarDetailOriginal.Model3D != avatarDetailToUpdate.Model3D && !string.IsNullOrEmpty(avatarDetailToUpdate.Model3D))
                avatarDetailOriginal.Model3D = avatarDetailToUpdate.Model3D;

            if (avatarDetailOriginal.UmaJson != avatarDetailToUpdate.UmaJson && !string.IsNullOrEmpty(avatarDetailToUpdate.UmaJson))
                avatarDetailOriginal.UmaJson = avatarDetailToUpdate.UmaJson;

            if (avatarDetailOriginal.Description != avatarDetailToUpdate.Description && !string.IsNullOrEmpty(avatarDetailToUpdate.Description))
                avatarDetailOriginal.Description = avatarDetailToUpdate.Description;

            if (avatarDetailOriginal.DimensionLevel != avatarDetailToUpdate.DimensionLevel)
                avatarDetailOriginal.DimensionLevel = avatarDetailToUpdate.DimensionLevel;

            if (avatarDetailToUpdate.Achievements.Count > 0)
            {
                if (!appendChildObjects)
                    avatarDetailOriginal.Achievements.Clear();

                ((List<IAchievement>)avatarDetailOriginal.Achievements).AddRange(avatarDetailToUpdate.Achievements);
            }

            if (avatarDetailOriginal.Attributes.Magic != avatarDetailToUpdate.Attributes.Magic && avatarDetailToUpdate.Attributes.Magic > 0)
                avatarDetailOriginal.Attributes.Magic = avatarDetailToUpdate.Attributes.Magic;

            if (avatarDetailOriginal.Attributes.Wisdom != avatarDetailToUpdate.Attributes.Wisdom && avatarDetailToUpdate.Attributes.Wisdom > 0)
                avatarDetailOriginal.Attributes.Wisdom = avatarDetailToUpdate.Attributes.Wisdom;

            if (avatarDetailOriginal.Attributes.Intelligence != avatarDetailToUpdate.Attributes.Intelligence && avatarDetailToUpdate.Attributes.Intelligence > 0)
                avatarDetailOriginal.Attributes.Intelligence = avatarDetailToUpdate.Attributes.Intelligence;

            //TODO: Finish...


            //TODO: Apply to all other properties. Use AutoMapper here instead! ;-)

            result = await SaveAvatarDetailAsync(avatarDetailOriginal);

            if (!result.IsError && result.Result != null)
            {
                OASISResult<IAvatar> avatarResult = await LoadAvatarAsync(result.Result.Id, false, false);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if ((!string.IsNullOrEmpty(avatarDetailToUpdate.Username) && avatarResult.Result.Username != avatarDetailToUpdate.Username) || (!string.IsNullOrEmpty(avatarDetailToUpdate.Email) && avatarResult.Result.Email != avatarDetailToUpdate.Email))
                    {
                        if (!string.IsNullOrEmpty(avatarDetailToUpdate.Username))
                            avatarResult.Result.Username = avatarDetailToUpdate.Username;

                        if (!string.IsNullOrEmpty(avatarDetailToUpdate.Email))
                            avatarResult.Result.Email = avatarDetailToUpdate.Email;

                        OASISResult<IAvatar> saveAvatarResult = await avatarResult.Result.SaveAsync();

                        if (!saveAvatarResult.IsError && saveAvatarResult.Result != null)
                        {
                            result.Message = "Avatar Detail & Avatar Updated Successfully";
                            result.IsSaved = true;
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}{saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    }
                    else
                    {
                        result.Message = "Avatar Detail Updated Successfully";
                        result.IsSaved = true;
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{avatarResult.Message}", avatarResult.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{result.Message}", result.DetailedMessage);

            return result;
        }

        private async Task<OASISResult<IAvatar>> LoadProviderWalletsAsync(OASISResult<IAvatar> result)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = null;

            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
            {
                walletsResult = await WalletManager.Instance.LoadProviderWalletsForAvatarByIdAsync(result.Result.Id, false, false, type.Value);

                if (!walletsResult.IsError && walletsResult.Result != null)
                {
                    result.Result.ProviderWallets = walletsResult.Result;
                    break;
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"Error occured in LoadProviderWalletsAsync in AvatarManager loading wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (walletsResult.IsError)
                OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load wallets for avatar with id ", result.Result.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
                result.IsLoaded = true;

            return result;
        }

        //private OASISResult<IAvatar> LoadProviderWallets(OASISResult<IAvatar> result)
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = null;

        //    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //    {
        //        walletsResult = WalletManager.Instance.LoadProviderWalletsForAvatarById(result.Result.Id, type.Value);

        //        if (!walletsResult.IsError && walletsResult.Result != null)
        //        {
        //            result.Result.ProviderWallets = walletsResult.Result;
        //            break;
        //        }
        //        else
        //            OASISErrorHandling.HandleWarning(ref result, $"Error occured in LoadProviderWallets in AvatarManager loading wallets for provider {type.Name}. Reason: {walletsResult.Message}");
        //    }

        //    if (walletsResult.IsError)
        //        OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load wallets for avatar with id ", result.Result.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
        //    else
        //        result.IsLoaded = true;

        //    return result;
        //}

        private OASISResult<IAvatar> LoadProviderWallets(OASISResult<IAvatar> result)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = 
                WalletManager.Instance.LoadProviderWalletsForAvatarById(result.Result.Id);

            //This use to be HandleError but if the local wallets could not be loaded for whatever reason such as it was not found (because this is a new avatar for example), then it should just continue and then a new one will be created.
            if (walletsResult.IsError || walletsResult.Result == null)
                OASISErrorHandling.HandleWarning(ref result, $"Error occured in LoadProviderWallets method in AvatarManager loading wallets for avatar {result.Result.Id}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage, walletsResult);
            else
            {
                result.Result.ProviderWallets = walletsResult.Result;
                result.IsLoaded = true;

                if (walletsResult.WarningCount > 0)
                {
                    result.InnerMessages.Add(walletsResult.Message);
                    result.InnerMessages.AddRange(walletsResult.InnerMessages);
                    result.IsWarning = true;
                    result.WarningCount += walletsResult.WarningCount;
                }
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IAvatar>>> LoadProviderWalletsForAllAvatarsAsync(OASISResult<IEnumerable<IAvatar>> result)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = null;
            bool errored = false;

            foreach (IAvatar avatar in result.Result)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    walletsResult = await WalletManager.Instance.LoadProviderWalletsForAvatarByIdAsync(avatar.Id, false, false, type.Value);

                    if (!walletsResult.IsError && walletsResult.Result != null)
                    {
                        avatar.ProviderWallets = walletsResult.Result;
                        break;
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, $"Error occured in LoadProviderWalletsForAllAvatarsAsync in AvatarManager loading wallets for avatar {avatar.Id} for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
                }

                if (walletsResult.IsError)
                {
                    errored = true;
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load wallets for avatar with id ", avatar.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                }
            }

            if (!errored)
            {
                result.IsLoaded = true;

                if (result.WarningCount > 0)
                    OASISErrorHandling.HandleWarning(ref result, string.Concat("All avatar wallets loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                    result.Message = "Avatars Successfully Loaded.";
            }
            else
                OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatar wallets. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));

            return result;
        }

        private OASISResult<IEnumerable<IAvatar>> LoadProviderWalletsForAllAvatars(OASISResult<IEnumerable<IAvatar>> result)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = null;
            bool errored = false;

            foreach (IAvatar avatar in result.Result)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    walletsResult = WalletManager.Instance.LoadProviderWalletsForAvatarById(avatar.Id, providerTypeToLoadFrom: type.Value);

                    if (!walletsResult.IsError && walletsResult.Result != null)
                    {
                        avatar.ProviderWallets = walletsResult.Result;
                        break;
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, $"Error occured in LoadProviderWalletsForAllAvatars in AvatarManager loading wallets for avatar {avatar.Id} for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
                }

                if (walletsResult.IsError)
                {
                    errored = true;
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load wallets for avatar with id ", avatar.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                }
            }

            if (!errored)
                result.IsLoaded = true;

            return result;
        }


        //private OASISResult<IAvatar> ProcessAvatarLogin(OASISResult<IAvatar> result, string username, string password, string ipAddress, Func<IAvatar, OASISResult<IAvatar>> saveFunc)
        //
        //private OASISResult<IAvatar> ProcessAvatarLogin(OASISResult<IAvatar> result, string username, string password, string ipAddress, Func<IAvatar, OASISResult<IAvatar>> saveFunc)
        //private OASISResult<IAvatar> ProcessAvatarLogin(OASISResult<IAvatar> result, string username, string password, string ipAddress, SaveAvatarFunction saveAvatarFunction)
        //{
        //    if (result.Result != null)
        //    {
        //        if (result.Result.DeletedDate != DateTime.MinValue)
        //        {
        //            result.IsError = true;
        //            result.Message = $"This avatar was deleted on {result.Result.DeletedDate} by avatar with id {result.Result.DeletedByAvatarId}, please contact support or create a new avatar with a new email address.";
        //        }

        //        if (!result.Result.IsActive)
        //        {
        //            result.IsError = true;
        //            result.Message = "This avatar is no longer active. Please contact support or create a new avatar.";
        //        }

        //        if (!result.Result.IsVerified)
        //        {
        //            result.IsError = true;
        //            result.Message = "Avatar has not been verified. Please check your email.";
        //        }

        //        if (!BC.Verify(password, result.Result.Password))
        //        {
        //            result.IsError = true;
        //            result.Message = "Email or password is incorrect";
        //        }
        //    }

        //    //TODO: Come back to this.
        //    //if (OASISDNA.OASIS.Security.AvatarPassword.)

        //    if (result.Result != null & !result.IsError)
        //    {
        //        var jwtToken = GenerateJWTToken(result.Result);
        //        var refreshToken = generateRefreshToken(ipAddress);

        //        result.Result.RefreshTokens.Add(refreshToken);
        //        result.Result.JwtToken = jwtToken;
        //        result.Result.RefreshToken = refreshToken.Token;
        //        result.Result.LastBeamedIn = DateTime.Now;
        //        result.Result.IsBeamedIn = true;

        //        LoggedInAvatar = result.Result;
        //        //OASISResult<IAvatar> saveAvatarResult = SaveAvatar(result.Result);
        //        //OASISResult<IAvatar> saveAvatarResult = saveFunc(result.Result);
        //        OASISResult<IAvatar> saveAvatarResult = saveAvatarFunction(result.Result);

        //        if (!saveAvatarResult.IsError && saveAvatarResult.IsSaved)
        //        {
        //            result.Result = HideAuthDetails(saveAvatarResult.Result);
        //            result.IsSaved = true;
        //            result.Message = "Avatar Successfully Authenticated.";
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"Error occured in AuthenticateAsync method in AvatarManager whilst saving the avatar. Reason: {saveAvatarResult.Message}");
        //    }
        //    else
        //        result.Result = null;

        //    return result;
        //}


        //TODO: Wish there was a way we could make a generic way to pass a Func in with DIFFERENT params AND return types?! :) Would save a LOT of code above! ;-) lol
        /*
        public async Task<OASISResult<IAvatar>> CallOASISProviderMethodAsync(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            string errorMessage = $"Error in SaveAvatarDetailForProviderAsync method in AvatarManager saving avatar detail with name {avatar.Name}, username {avatar.Username} and id {avatar.Id} for provider {ProviderManager.Instance.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.SaveAvatarAsync(avatar);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Unknown.";

                            OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                        else
                            result.IsSaved = true;
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }*/
    }
}
