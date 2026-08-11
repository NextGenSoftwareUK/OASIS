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
            OASISResult<bool> jwtReady = OASISDNAManager.EnsureJwtSecretKeyReadyForAvatarAuth();
            if (jwtReady.IsError || OASISDNAManager.OASISDNA == null || string.IsNullOrEmpty(OASISDNAManager.OASISDNA.OASIS?.Security?.SecretKey))
                throw new ArgumentNullException("OASISDNA.OASIS.Security.SecretKey",
                    string.IsNullOrEmpty(jwtReady.Message)
                        ? "OASISDNA.OASIS.Security.SecretKey is missing and could not be generated. Check DNA/OASIS_DNA.json exists next to STAR CLI."
                        : jwtReady.Message);

            OASISDNA = OASISDNAManager.OASISDNA;
            try
            {
                ProviderManager.Instance.OASISDNA = OASISDNAManager.OASISDNA;
            }
            catch
            {
                // non-fatal
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(OASISDNA.OASIS.Security.SecretKey);
            var jwtMinutes = OASISDNA.OASIS.Security.JwtTokenExpirationMinutes;
            if (jwtMinutes <= 0) jwtMinutes = 15;
            var claims = new System.Collections.Generic.List<Claim>
            {
                new Claim("id", account.Id.ToString())
            };

            if (OASISDNA.OASIS?.Security?.DIDEnabled == true)
                claims.Add(new Claim("did", account.DID));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(jwtMinutes),
                Issuer = "OASIS",
                Audience = "OASIS",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken generateRefreshToken(string ipAddress)
        {
            var refreshDays = OASISDNA.OASIS.Security.RefreshTokenExpirationDays;
            if (refreshDays <= 0) refreshDays = 7;
            return new RefreshToken
            {
                Token = randomTokenString(),
                Expires = DateTime.UtcNow.AddDays(refreshDays),
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
                    var pwdSettings = OASISDNAManager.OASISDNA?.OASIS?.Security?.AvatarPassword;
                    if (!PasswordEncryptionHelper.VerifyPassword(password, result.Result.Password, pwdSettings))
                    {
                        result.IsError = true;
                        result.Message = "Email or password is incorrect";
                    }
                }
                else
                {
                    var pwdSettings = OASISDNAManager.OASISDNA?.OASIS?.Security?.AvatarPassword;
                    result.Result.Password = PasswordEncryptionHelper.HashPassword("changemenow!", pwdSettings);
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
            {
                var emailCheck = CheckIfEmailIsAlreadyInUse(avatarDetailToUpdate.Email, false);
                if (emailCheck.Result)
                {
                    OASISErrorHandling.HandleError(ref result, "Email '" + avatarDetailToUpdate.Email + "' is already registered to another account. Email not updated.");
                    return result;
                }
                else
                    avatarDetailOriginal.Email = avatarDetailToUpdate.Email;
            }

            if (avatarDetailOriginal.Username != avatarDetailToUpdate.Username && !string.IsNullOrEmpty(avatarDetailToUpdate.Username))
            {
                var usernameCheck = CheckIfUsernameIsAlreadyInUse(avatarDetailToUpdate.Username);
                if (usernameCheck.Result)
                {
                    OASISErrorHandling.HandleError(ref result, "Username '" + avatarDetailToUpdate.Username + "' is already taken. Username not updated.");
                    return result;
                }
                else
                    avatarDetailOriginal.Username = avatarDetailToUpdate.Username;
            }

            if (avatarDetailOriginal.DOB != avatarDetailToUpdate.DOB && avatarDetailToUpdate.DOB != DateTime.MinValue)
                avatarDetailOriginal.DOB = avatarDetailToUpdate.DOB;

            if (avatarDetailOriginal.Karma != avatarDetailToUpdate.Karma && avatarDetailToUpdate.Karma > 0)
                avatarDetailOriginal.Karma = avatarDetailToUpdate.Karma;

            if (avatarDetailOriginal.XP != avatarDetailToUpdate.XP && avatarDetailToUpdate.XP > 0)
                avatarDetailOriginal.XP = avatarDetailToUpdate.XP;

            /* Quest tracker state: always apply so GET avatar/current can restore after beam-in */
            avatarDetailOriginal.ActiveQuestId = avatarDetailToUpdate.ActiveQuestId;
            avatarDetailOriginal.ActiveObjectiveId = avatarDetailToUpdate.ActiveObjectiveId;

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

            if (avatarDetailOriginal.Attributes.Strength != avatarDetailToUpdate.Attributes.Strength && avatarDetailToUpdate.Attributes.Strength > 0)
                avatarDetailOriginal.Attributes.Strength = avatarDetailToUpdate.Attributes.Strength;

            if (avatarDetailOriginal.Attributes.Speed != avatarDetailToUpdate.Attributes.Speed && avatarDetailToUpdate.Attributes.Speed > 0)
                avatarDetailOriginal.Attributes.Speed = avatarDetailToUpdate.Attributes.Speed;

            if (avatarDetailOriginal.Attributes.Dexterity != avatarDetailToUpdate.Attributes.Dexterity && avatarDetailToUpdate.Attributes.Dexterity > 0)
                avatarDetailOriginal.Attributes.Dexterity = avatarDetailToUpdate.Attributes.Dexterity;

            if (avatarDetailOriginal.Attributes.Toughness != avatarDetailToUpdate.Attributes.Toughness && avatarDetailToUpdate.Attributes.Toughness > 0)
                avatarDetailOriginal.Attributes.Toughness = avatarDetailToUpdate.Attributes.Toughness;

            if (avatarDetailOriginal.Attributes.Magic != avatarDetailToUpdate.Attributes.Magic && avatarDetailToUpdate.Attributes.Magic > 0)
                avatarDetailOriginal.Attributes.Magic = avatarDetailToUpdate.Attributes.Magic;

            if (avatarDetailOriginal.Attributes.Wisdom != avatarDetailToUpdate.Attributes.Wisdom && avatarDetailToUpdate.Attributes.Wisdom > 0)
                avatarDetailOriginal.Attributes.Wisdom = avatarDetailToUpdate.Attributes.Wisdom;

            if (avatarDetailOriginal.Attributes.Intelligence != avatarDetailToUpdate.Attributes.Intelligence && avatarDetailToUpdate.Attributes.Intelligence > 0)
                avatarDetailOriginal.Attributes.Intelligence = avatarDetailToUpdate.Attributes.Intelligence;

            if (avatarDetailOriginal.Attributes.Vitality != avatarDetailToUpdate.Attributes.Vitality && avatarDetailToUpdate.Attributes.Vitality > 0)
                avatarDetailOriginal.Attributes.Vitality = avatarDetailToUpdate.Attributes.Vitality;

            if (avatarDetailOriginal.Attributes.Endurance != avatarDetailToUpdate.Attributes.Endurance && avatarDetailToUpdate.Attributes.Endurance > 0)
                avatarDetailOriginal.Attributes.Endurance = avatarDetailToUpdate.Attributes.Endurance;

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
                walletsResult = await WalletManager.Instance.LoadProviderWalletsForAvatarByIdAsync(result.Result.Id, false, false, false, type.Value);

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
                    walletsResult = await WalletManager.Instance.LoadProviderWalletsForAvatarByIdAsync(avatar.Id, false, false, false, type.Value);

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
