using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class AvatarManager : OASISManager
    {
        public static IAvatar LoggedInAvatar { get; set; }
        private ProviderManagerConfig _config;
        
        public List<IOASISStorageProvider> OASISStorageProviders { get; set; }
        
        public ProviderManagerConfig Config
        {
            get
            {
                if (_config == null)
                    _config = new ProviderManagerConfig();

                return _config;
            }
        }

        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

       //TODO: Not sure we want to pass the OASISDNA here?
        public AvatarManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            
        }

        // TODO: Not sure if we want to move methods from the AvatarService in WebAPI here?
        // For integration with STAR and others like Unity can just call the REST API service?
        // What advantage is there to making it native through dll's? Would be slightly faster than having to make a HTTP request/response round trip...
        // BUT would STILL need to call out to a OASIS Storage Provider so depending if that was also running locally is how fast it would be...
        // For now just easier to call the REST API service from STAR... can come back to this later... :)
        public OASISResult<IAvatar> Authenticate(string username, string password, string ipAddress)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            result = LoadAvatar(username, false);

            if (result.Result == null)
            {
                result.IsError = true;
                result.Message = "This avatar does not exist. Please contact support or create a new avatar.";
            }
            else
            {
                if (result.Result.DeletedDate != DateTime.MinValue)
                {
                    result.IsError = true;
                    result.Message = "This avatar has been deleted. Please contact support or create a new avatar.";
                }

                // TODO: Implement Activate/Deactivate methods in AvatarManager & Providers...
                if (!result.Result.IsActive)
                {
                    result.IsError = true;
                    result.Message = "This avatar is no longer active. Please contact support or create a new avatar.";
                }

                //if (!result.Result.IsVerified && OASISDNA.OASIS.Security.DoesAvatarNeedToBeVerifiedBeforeLogin)
                if (!result.Result.IsVerified && OASISDNA.OASIS.Security.SendVerificationEmail)
                {
                    result.IsError = true;
                    result.Message = "Avatar has not been verified. Please check your email.";
                }

                if (!BC.Verify(password, result.Result.Password))
                {
                    result.IsError = true;
                    result.Message = "Email or password is incorrect";
                }
            }

            //TODO: Come back to this.
            //if (OASISDNA.OASIS.Security.AvatarPassword.)

            if (result.Result != null & !result.IsError)
            {
                var jwtToken = GenerateJWTToken(result.Result);
                var refreshToken = generateRefreshToken(ipAddress);

                result.Result.RefreshTokens.Add(refreshToken);
                result.Result.JwtToken = jwtToken;
                result.Result.RefreshToken = refreshToken.Token;
                result.Result.LastBeamedIn = DateTime.Now;
                result.Result.IsBeamedIn = true;

                LoggedInAvatar = result.Result;
                OASISResult<IAvatar> saveAvatarResult = SaveAvatar(result.Result);

                if (!saveAvatarResult.IsError && saveAvatarResult.IsSaved)
                {
                    result.Result = HideAuthDetails(saveAvatarResult.Result);
                    result.IsSaved = true;
                }
                else
                {
                    result.Message = saveAvatarResult.Message;
                    result.IsError = saveAvatarResult.IsError;
                    result.IsSaved = saveAvatarResult.IsSaved;
                }
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> AuthenticateAsync(string username, string password, string ipAddress)
        {
            OASISResult<IAvatar> result = await LoadAvatarAsync(username, false);

            if (result.Result == null)
            {
                result.IsError = true;
                result.Message = "This avatar does not exist. Please contact support or create a new avatar.";
            }
            else
            {
                if (result.Result.DeletedDate != DateTime.MinValue)
                {
                    result.IsError = true;
                    result.Message = "This avatar has been deleted. Please contact support or create a new avatar.";
                }

                // TODO: Implement Activate/Deactivate methods in AvatarManager & Providers...
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

                if (!BC.Verify(password, result.Result.Password))
                {
                    result.IsError = true;
                    result.Message = "Email or password is incorrect";
                }
            }

            //TODO: Come back to this.
            //if (OASISDNA.OASIS.Security.AvatarPassword.)

            if (result.Result != null & !result.IsError)
            {
                var jwtToken = GenerateJWTToken(result.Result);
                var refreshToken = generateRefreshToken(ipAddress);

                result.Result.RefreshTokens.Add(refreshToken);
                result.Result.JwtToken = jwtToken;
                result.Result.RefreshToken = refreshToken.Token;
                result.Result.LastBeamedIn = DateTime.Now;
                result.Result.IsBeamedIn = true;

                LoggedInAvatar = result.Result;
                OASISResult<IAvatar> saveAvatarResult = SaveAvatar(result.Result);

                if (!saveAvatarResult.IsError && saveAvatarResult.IsSaved)
                {
                    result.Result = HideAuthDetails(saveAvatarResult.Result);
                    result.IsSaved = true;
                }
                else
                {
                    result.Message = saveAvatarResult.Message;
                    result.IsError = saveAvatarResult.IsError;
                    result.IsSaved = saveAvatarResult.IsSaved;
                }
            }

            return result;
        }

        public OASISResult<IAvatar> Register(string avatarTitle, string firstName, string lastName, string email, string password, AvatarType avatarType, string origin, OASISType createdOASISType, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            OASISResult<IAvatar> result = PrepareToRegisterAvatar(avatarTitle, firstName, lastName, email, password, avatarType, origin, createdOASISType);

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
                            result = AvatarRegistered(result, origin);
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

            return result;
        }

        public async Task<OASISResult<IAvatar>> RegisterAsync(string avatarTitle, string firstName, string lastName, string email, string password, AvatarType avatarType, string origin, OASISType createdOASISType, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            OASISResult<IAvatar> result = PrepareToRegisterAvatar(avatarTitle, firstName, lastName, email, password, avatarType, origin, createdOASISType);

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
                            result = AvatarRegistered(result, origin);
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

            return result;
        }

        public OASISResult<bool> VerifyEmail(string token)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            IAvatar avatar = LoadAllAvatarsWithPasswords().FirstOrDefault(x => x.VerificationToken == token);

            if (avatar == null)
            {
                result.Result = false;
                result.IsError = true;
                result.Message = "Verification Failed";
            }
            else
            {
                result.Result = true;
                avatar.Verified = DateTime.UtcNow;
                avatar.VerificationToken = null;
                //avatar.IsNewHolon = false;
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

            return result;
        }

        public IEnumerable<IAvatar> LoadAllAvatarsWithPasswords(ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            IEnumerable<IAvatar> avatars = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllAvatars().Result;
            return avatars;
        }

        public IEnumerable<IAvatar> LoadAllAvatars(bool hideAuthDetails = true, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            IEnumerable<IAvatar> avatars = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllAvatars().Result;

            if (hideAuthDetails)
                avatars = HideAuthDetails(avatars);

            return avatars;
        }

        public async Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync(bool hideAuthDetails = true, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            IEnumerable<IAvatar> avatars = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllAvatarsAsync().Result.Result;

            if (hideAuthDetails)
                avatars = HideAuthDetails(avatars);

            return avatars;
        }

        public OASISResult<IAvatar> LoadAvatar(Guid id, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                    result = providerResult.Result.LoadAvatar(id, version);
                else
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (!providerResult.IsError && providerResult.Result != null)
                                result = providerResult.Result.LoadAvatar(id, version);
                            else
                            {
                                result.IsError = true;
                                result.Message = providerResult.Message;
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
                        }
                    }

                    if (result.Result != null && !result.IsError)
                        break;
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));

            else if (hideAuthDetails)
                result.Result = HideAuthDetails(result.Result);

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                    result = await providerResult.Result.LoadAvatarAsync(id, version);
                else
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }

                if (result.IsError || result.Result == null)
                {
                    if (string.IsNullOrEmpty(result.Message))
                        result.Message = "Avatar Not Found.";
                    
                    ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Reason: ", result.Message));
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (!providerResult.IsError && providerResult.Result != null)
                                result = await providerResult.Result.LoadAvatarAsync(id, version);
                            else
                            {
                                result.IsError = true;
                                result.Message = providerResult.Message;
                            }

                            if (result.IsError || result.Result == null)
                                ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Reason: ", result.Message));
                        }
                        catch (Exception ex)
                        {
                            ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
            {
                if (result.WarningCount > 0)
                    result.Message = string.Concat("The avatar with id ", id, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));

                if (hideAuthDetails)
                    result.Result = HideAuthDetails(result.Result);
            } 

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }

        public OASISResult<IAvatar> LoadAvatar(string username, string password, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                    result = providerResult.Result.LoadAvatar(username, password, version);
                else
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (!providerResult.IsError && providerResult.Result != null)
                                result = providerResult.Result.LoadAvatar(username, password, version);
                            else
                            {
                                result.IsError = true;
                                result.Message = providerResult.Message;
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()));

            else if (hideAuthDetails)
                result.Result = HideAuthDetails(result.Result);

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                    result = await providerResult.Result.LoadAvatarAsync(username, password, version);
                else
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (!providerResult.IsError && providerResult.Result != null)
                                result = await providerResult.Result.LoadAvatarAsync(username, password, version);
                            else
                            {
                                result.IsError = true;
                                result.Message = providerResult.Message;
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()));

            else if (hideAuthDetails)
                result.Result = HideAuthDetails(result.Result);

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }

        //TODO: Replicate Auto-Fail over and Auto-Replication code for all Avatar, HolonManager methods etc...
        public OASISResult<IAvatar> LoadAvatar(string username, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                    result = providerResult.Result.LoadAvatar(username, version);
                else
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (!providerResult.IsError && providerResult.Result != null)
                                result = providerResult.Result.LoadAvatar(username, version);
                            else
                            {
                                result.IsError = true;
                                result.Message = providerResult.Message;
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()));

            else if (hideAuthDetails)
                result.Result = HideAuthDetails(result.Result);

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                    result = await providerResult.Result.LoadAvatarAsync(username, version);
                else
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (!providerResult.IsError && providerResult.Result != null)
                                result = await providerResult.Result.LoadAvatarAsync(username, version);
                            else
                            {
                                result.IsError = true;
                                result.Message = providerResult.Message;
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()));

            else if (hideAuthDetails)
                result.Result = HideAuthDetails(result.Result);

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }

        public OASISResult<IAvatar> LoadAvatarByEmail(string email, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                    result = providerResult.Result.LoadAvatarByEmail(email, version);
                else
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (!providerResult.IsError && providerResult.Result != null)
                                result = providerResult.Result.LoadAvatarByEmail(email, version);
                            else
                            {
                                result.IsError = true;
                                result.Message = providerResult.Message;
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", email, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()));

            else if (hideAuthDetails)
                result.Result = HideAuthDetails(result.Result);

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                    result = await providerResult.Result.LoadAvatarByEmailAsync(email, version);
                else
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (!providerResult.IsError && providerResult.Result != null)
                                result = await providerResult.Result.LoadAvatarByEmailAsync(email, version);
                            else
                            {
                                result.IsError = true;
                                result.Message = providerResult.Message;
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", email, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()));

            else if (hideAuthDetails)
                result.Result = HideAuthDetails(result.Result);

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                    result = providerResult.Result.LoadAvatarDetail(id, version);
                else
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar detail ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (!providerResult.IsError && providerResult.Result != null)
                                result = providerResult.Result.LoadAvatarDetail(id, version);
                            else
                            {
                                result.IsError = true;
                                result.Message = providerResult.Message;
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar detail ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()));

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }

        public async Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
        {
            //TODO: Need to handle return of OASISResult properly...
            var detail = await ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAvatarDetailAsync(id);
            return detail.Result;
        }

        public async Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string email)
        {
            //TODO: Need to handle return of OASISResult properly...
            var detail = await ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAvatarDetailByEmailAsync(email);
            return detail.Result;
        }

        public IAvatarDetail LoadAvatarDetailByEmail(string email)
        {
            //TODO: Need to handle return of OASISResult properly...
            var detail = ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAvatarDetailByEmail(email);
            return detail.Result;
        }
        
        public async Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string username)
        {
            //TODO: Need to handle return of OASISResult properly...
            var detail = await ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAvatarDetailByUsernameAsync(username);
            return detail.Result;
        }

        public IAvatarDetail LoadAvatarDetailByUsername(string username)
        {
            //TODO: Need to handle return of OASISResult properly...
            var detail = ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAvatarDetailByUsername(username);
            return detail.Result;
        }

        public IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
        {
            //TODO: Need to handle return of OASISResult properly...
            var details = ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAllAvatarDetails();
            return details.Result;
        }

        public async Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
        {
            //TODO: Need to handle return of OASISResult properly...
            var details = await ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAllAvatarDetailsAsync();
            return details.Result;
        }

        
        /*
        public async Task<IAvatar> SaveAvatarAsync(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            IAvatar savedAvatar = null;

            try
            {
                int removingDays = OASISDNA.OASIS.Security.RemoveOldRefreshTokensAfterXDays;
                int removeQty = avatar.RefreshTokens.RemoveAll(token => (DateTime.Today - token.Created).TotalDays > removingDays);

                //TODO: Need to handle return of OASISResult properly...
                savedAvatar = await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.SaveAvatarAsync(PrepareAvatarForSaving(avatar));
            }
            catch (Exception ex)
            {
                savedAvatar = null;
                LoggingManager.Log(string.Concat("Error saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), LogType.Error);
            }

            if (savedAvatar == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            //TODO: Need to handle return of OASISResult properly...
                            savedAvatar = await ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveAvatarAsync(avatar);

                            if (savedAvatar != null)
                                break;
                        }
                        catch (Exception ex)
                        {
                            savedAvatar = null;
                            LoggingManager.Log(string.Concat("Error saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", type.Name, ". Error Message: ", ex.ToString()), LogType.Error);
                            //If the next provider errors then just continue to the next provider.
                        }
                    }
                }
            }

            if (ProviderManager.IsAutoReplicationEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            //TODO: Need to handle return of OASISResult properly...
                            await ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveAvatarAsync(avatar);
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Log(string.Concat("Error replicating avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", type.Name, ". Error Message: ", ex.ToString()), LogType.Error);
                        }
                    }
                }
            }

            ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            return savedAvatar;
        }*/

        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                int removingDays = OASISDNA.OASIS.Security.RemoveOldRefreshTokensAfterXDays;
                int removeQty = avatar.RefreshTokens.RemoveAll(token => (DateTime.Today - token.Created).TotalDays > removingDays);

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                {
                    result = await providerResult.Result.SaveAvatarAsync(PrepareAvatarForSaving(avatar));
                    
                    if (result != null && !result.IsError && result.Result != null)
                        result.IsSaved = true;
                    else
                        ErrorHandling.HandleError(ref result, result.Message);
                }
                else
                    ErrorHandling.HandleError(ref result, providerResult.Message);
            }
            catch (Exception ex)
            {
                result.Result = null;
                ErrorHandling.HandleWarning(ref result, string.Concat("Error saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);
                            
                            if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                            {
                                result = await providerResult.Result.SaveAvatarAsync(avatar);
                                result.IsSaved = true;
                                break;
                            }
                            else
                                ErrorHandling.HandleWarning(ref result, providerResult.Message);
                        }
                        catch (Exception ex)
                        {
                            result.Result = null;
                            ErrorHandling.HandleWarning(ref result, string.Concat("Error saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", type.Name, ". Error Message: ", ex.ToString()));
                            //If the next provider errors then just continue to the next provider.
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", avatar.Name, " with id ", avatar.Id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()));

            if (ProviderManager.IsAutoReplicationEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            //TODO: Implement better error handling/logging like used elsewhere in HolonManager and CelestialBody/CelestialSpace etc.
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                            {
                                result = await providerResult.Result.SaveAvatarAsync(avatar);
                                result.IsSaved = true;
                            }
                            else
                                ErrorHandling.HandleWarning(ref result, providerResult.Message);
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Log(string.Concat("Error replicating avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", type.Name, ". Error Message: ", ex.ToString()), LogType.Error);
                        }
                    }
                }
            }

            ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            return result;
        }

        public OASISResult<IAvatar> SaveAvatar(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                int removingDays = OASISDNA.OASIS.Security.RemoveOldRefreshTokensAfterXDays;
                int removeQty = avatar.RefreshTokens.RemoveAll(token => (DateTime.Today - token.Created).TotalDays > removingDays);

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                {
                    result = providerResult.Result.SaveAvatar(PrepareAvatarForSaving(avatar));
                    result.IsSaved = true;
                }
                else
                    ErrorHandling.HandleError(ref result, providerResult.Message);
            }
            catch (Exception ex)
            {
                LoggingManager.Log(string.Concat("Error saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), LogType.Error);
                result.Result = null;
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                            {
                                result = providerResult.Result.SaveAvatar(avatar);
                                result.IsSaved = true;
                                break;
                            }
                            else
                                ErrorHandling.HandleError(ref result, providerResult.Message);
                        }
                        catch (Exception ex)
                        {
                            result.Result = null;
                            LoggingManager.Log(string.Concat("Error saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", type.Name, ". Error Message: ", ex.ToString()), LogType.Error);
                            //If the next provider errors then just continue to the next provider.
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", avatar.Name, " with id ", avatar.Id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()));

            if (ProviderManager.IsAutoReplicationEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                            {
                                result = providerResult.Result.SaveAvatar(avatar);
                                result.IsSaved = true;
                            }
                            else
                                ErrorHandling.HandleWarning(ref result, providerResult.Message);
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Log(string.Concat("Error replicating avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", type.Name, ". Error Message: ", ex.ToString()), LogType.Error);
                        }
                    }
                }
            }

            ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            return result;
        }

        public OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                {
                    result = providerResult.Result.SaveAvatarDetail(PrepareAvatarDetailForSaving(avatar));
                    result.IsSaved = true;
                }
                else
                    ErrorHandling.HandleError(ref result, providerResult.Message);
            }
            catch (Exception ex)
            {
                result.Result = null;
                LoggingManager.Log(string.Concat("Error saving AvatarDetail ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), LogType.Error);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                            {
                                result = providerResult.Result.SaveAvatarDetail(avatar);
                                result.IsSaved = true;
                                break;
                            }
                            else
                                ErrorHandling.HandleError(ref result, providerResult.Message);
                        }
                        catch (Exception ex)
                        {
                            result.Result = null;
                            //If the next provider errors then just continue to the next provider.
                            LoggingManager.Log(string.Concat("Error saving AvatarDetail ", avatar.Name, " with id ", avatar.Id, " for provider ", type.Name, ". Error Message: ", ex.ToString()), LogType.Error);
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", avatar.Name, " with id ", avatar.Id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()));

            if (ProviderManager.IsAutoReplicationEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                            {
                                result = providerResult.Result.SaveAvatarDetail(avatar);
                                result.IsSaved = true;
                            }
                            else
                                ErrorHandling.HandleWarning(ref result, providerResult.Message);
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Log(string.Concat("Error replicating AvatarDetail ", avatar.Name, " with id ", avatar.Id, " for provider ", type.Name, ". Error Message: ", ex.ToString()), LogType.Error);
                        }
                    }
                }
            }

            ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                {
                    result = await providerResult.Result.SaveAvatarDetailAsync(PrepareAvatarDetailForSaving(avatar));
                    result.IsSaved = true;
                }
                else
                    ErrorHandling.HandleError(ref result, providerResult.Message);
            }
            catch (Exception ex)
            {
                result.Result = null;
                LoggingManager.Log(string.Concat("Error saving AvatarDetail ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), LogType.Error);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                            {
                                result = await providerResult.Result.SaveAvatarDetailAsync(avatar);
                                result.IsSaved = true;
                                break;
                            }
                            else
                                ErrorHandling.HandleError(ref result, providerResult.Message);
                        }
                        catch (Exception ex)
                        {
                            result.Result = null;
                            //If the next provider errors then just continue to the next provider.
                            LoggingManager.Log(string.Concat("Error saving AvatarDetail ", avatar.Name, " with id ", avatar.Id, " for provider ", type.Name, ". Error Message: ", ex.ToString()), LogType.Error);
                        }
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail, ", avatar.Name, " with id ", avatar.Id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()));

            if (ProviderManager.IsAutoReplicationEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                            {
                                result = await providerResult.Result.SaveAvatarDetailAsync(avatar);
                                result.IsSaved = true;
                            }
                            else
                                ErrorHandling.HandleWarning(ref result, providerResult.Message);
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Log(string.Concat("Error replicating AvatarDetail ", avatar.Name, " with id ", avatar.Id, " for provider ", type.Name, ". Error Message: ", ex.ToString()), LogType.Error);
                        }
                    }
                }
            }

            ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            return result;
        }


        //TODO: Need to refactor methods below to match the new above ones.
        public OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            return ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.DeleteAvatar(id, softDelete);
        }

        public async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in DeleteAvatarAsync method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<bool> result = new OASISResult<bool>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                result = await providerResult.Result.DeleteAvatarAsync(id, softDelete);

                if (result != null && !result.IsError && result.Result)
                {
                    result.Message = "Avatar Successfully Deleted.";
                    result.IsSaved = true;
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}");
            }
            else
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}");

            return result;

            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            //return await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.DeleteAvatarAsync(id, softDelete).Result;
        }
        
        public async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string userName, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in DeleteAvatarByUsernameAsync method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<bool> result = new OASISResult<bool>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                result = await providerResult.Result.DeleteAvatarByUsernameAsync(userName, softDelete);

                if (result != null && !result.IsError && result.Result)
                {
                    result.Message = "Avatar Successfully Deleted.";
                    result.IsSaved = true;
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}");
            }
            else
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}");

            return result;

            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            //return await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.DeleteAvatarByUsernameAsync(userName, softDelete);
        }
        
        public async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in DeleteAvatarByEmailAsync method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<bool> result = new OASISResult<bool>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                result = await providerResult.Result.DeleteAvatarByEmailAsync(email, softDelete);

                if (result != null && !result.IsError && result.Result)
                {
                    result.Message = "Avatar Successfully Deleted.";
                    result.IsSaved = true;
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}");
            }
            else
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}");

            return result;

            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            //return await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.DeleteAvatarByEmailAsync(email, softDelete);
        }

        public async Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(IAvatarDetail avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in AddKarmaToAvatarAsync method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                result = await providerResult.Result.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                if (result != null && !result.IsError && result.Result != null)
                {
                    result.Message = "Karma Successfully Added To Avatar.";
                    result.IsSaved = true;
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}");
            }
            else
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}");

            return result.Result;

            //TODO: Need to handle return of OASISResult properly..
            ////TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc....
            //return await ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }
        public async Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(Guid avatarId, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in AddKarmaToAvatarAsync method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                OASISResult<IAvatarDetail> avatarResult = await providerResult.Result.LoadAvatarDetailAsync(avatarId);

                if(avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    result = await providerResult.Result.AddKarmaToAvatarAsync(avatarResult.Result, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        result.Message = "Karma Successfully Added To Avatar.";
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}");
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: Avatar Not Found. Error Details: {avatarResult.Message}");
            }
            else
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}");

            return result.Result;

            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            //IAvatarDetail avatar = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatarDetail(avatarId);
            //return await ProviderManager.CurrentStorageProvider.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatarDetail avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            return new OASISResult<KarmaAkashicRecord>(ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.AddKarmaToAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink).Result);
        }
        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(Guid avatarId, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in AddKarmaToAvatar method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                OASISResult<IAvatarDetail> avatarResult = providerResult.Result.LoadAvatarDetail(avatarId);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    result = providerResult.Result.AddKarmaToAvatar(avatarResult.Result, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        result.Message = "Karma Successfully Added To Avatar.";
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}");
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: Avatar Not Found. Error Details: {avatarResult.Message}");
            }
            else
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}");

            return result;

            /*
            //TODO: Need to handle return of OASISResult properly...
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();
            OASISResult<IAvatarDetail> avatarResult = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatarDetail(avatarId);
            //IAvatar avatar = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatar(avatarId);

            if (avatarResult != null )
            {
                result.Result = ProviderManager.CurrentStorageProvider.AddKarmaToAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                if (result.Result != null)
                    result.Message = "Karma Successfully Added To Avatar.";
            }
            else
            {
                result.IsError = true;
                result.Message = "Avatar Not Found";
            }

            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...

            return result;
            */
        }

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(IAvatarDetail avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            //return await ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink).Result;

            string errorMessage = "Error in RemoveKarmaFromAvatarAsync method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                result = await providerResult.Result.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                if (result != null && !result.IsError && result.Result != null)
                {
                    result.Message = "Karma Successfully Removed From Avatar.";
                    result.IsSaved = true;
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}");
            }
            else
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}");

            return result.Result;
        }

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(Guid avatarId, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in RemoveKarmaFromAvatarAsync method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                OASISResult<IAvatarDetail> avatarResult = await providerResult.Result.LoadAvatarDetailAsync(avatarId);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    result = await providerResult.Result.RemoveKarmaFromAvatarAsync(avatarResult.Result, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        result.Message = "Karma Successfully Removed From Avatar.";
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}");
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: Avatar Not Found. Error Details: {avatarResult.Message}");
            }
            else
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}");

            return result.Result;

            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            //IAvatarDetail avatar = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.LoadAvatarDetail(avatarId);
            //return await ProviderManager.CurrentStorageProvider.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        public KarmaAkashicRecord RemoveKarmaFromAvatar(IAvatarDetail avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            //return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.RemoveKarmaFromAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

            string errorMessage = "Error in RemoveKarmaFromAvatar method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                result = providerResult.Result.RemoveKarmaFromAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                if (result != null && !result.IsError && result.Result != null)
                {
                    result.Message = "Karma Successfully Removed From Avatar.";
                    result.IsSaved = true;
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}");
            }
            else
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}");

            return result.Result;
        }

        public OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(Guid avatarId, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error in RemoveKarmaFromAvatar method in AvatarManager.";
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            if (providerResult != null && !providerResult.IsError && providerResult.Result != null)
            {
                OASISResult<IAvatarDetail> avatarResult = providerResult.Result.LoadAvatarDetail(avatarId);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    result = providerResult.Result.RemoveKarmaFromAvatar(avatarResult.Result, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        result.Message = "Karma Successfully Removed From Avatar.";
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {result.Message}");
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: Avatar Not Found. Error Details: {avatarResult.Message}");
            }
            else
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {providerResult.Message}");

            return result;
        }

        public bool CheckIfEmailIsAlreadyInUse(string email)
        {
           // OASISResult<IAvatar> avatarResult = await LoadAvatarByEmailAsync(email);


            //return LoadAllAvatars().Any(x => x.Email == email);
            return LoadAvatarByEmail(email).Result != null;
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

        private IAvatarDetail PrepareAvatarDetailForSaving(IAvatarDetail avatar)
        {
            //if (string.IsNullOrEmpty(avatar.Username))
            //    avatar.Username = avatar.Email;

            if (avatar.Id == Guid.Empty)
            {
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
            }
            else
            {
                avatar.IsActive = true;
                avatar.CreatedDate = DateTime.Now;

                if (LoggedInAvatar != null)
                    avatar.CreatedByAvatarId = LoggedInAvatar.Id;
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

        public IAvatar HideAuthDetails(IAvatar avatar, bool hidePassword = true, bool hidePrivateKeys = true, bool hideVerificationToken = true, bool hideRefreshTokens = true)
        {
            if (OASISDNA.OASIS.Security.HideVerificationToken || hideVerificationToken)
              avatar.VerificationToken = null; 

            if (hidePassword)
                avatar.Password = null;

            if (hidePrivateKeys)
                avatar.ProviderPrivateKey = null;

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

        private void sendAlreadyRegisteredEmail(string email, string origin)
        {
            string message;

            if (!string.IsNullOrEmpty(origin))
                message = $@"<p>If you don't know your password please visit the <a href=""{origin}/avatar/forgot-password"">forgot password</a> page.</p>";
            else
                message = "<p>If you don't know your password you can reset it via the <code>/avatar/forgot-password</code> api route.</p>";

            if (!EmailManager.IsInitialized)
                EmailManager.Initialize(OASISDNA);

            EmailManager.Send(
                to: email,
                subject: "OASIS Sign-up Verification - Email Already Registered",
                html: $@"<h4>Email Already Registered</h4>
                         <p>Your email <strong>{email}</strong> is already registered.</p>
                         {message}"
            );
        }

        private void sendVerificationEmail(IAvatar avatar, string origin)
        {
            string message;

            if (!string.IsNullOrEmpty(origin))
            {
                var verifyUrl = $"{origin}/avatar/verify-email?token={avatar.VerificationToken}";
                message = $@"<p>Please click the below link to verify your email address:</p>
                             <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";
            }
            else
            {
                message = $@"<p>Please use the below token to verify your email address with the <code>/avatar/verify-email</code> api route:</p>
                             <p><code>{avatar.VerificationToken}</code></p>";
            }

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

        private OASISResult<IAvatar> PrepareToRegisterAvatar(string avatarTitle, string firstName, string lastName, string email, string password, AvatarType avatarType, string origin, OASISType createdOASISType)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            if (!ValidationHelper.IsValidEmail(email))
            {
                result.IsError = true;
                result.Message = "The email is not valid.";
                return result;
            }

            //TODO: {PERFORMANCE} Add this method to the providers so more efficient.
            if (CheckIfEmailIsAlreadyInUse(email))
            {
                // send already registered error in email to prevent account enumeration
                sendAlreadyRegisteredEmail(email, origin);
                result.IsError = true;
                result.Message = "Avatar Already Registered.";
                return result;
            }

            result.Result = new Avatar() { Id = Guid.NewGuid(), IsNewHolon = true, FirstName = firstName, LastName = lastName, Password = password, Title = avatarTitle, Email = email, AvatarType = new EnumValue<AvatarType>(avatarType), CreatedOASISType = new EnumValue<OASISType>(createdOASISType) };

            //result.Result.CreatedDate = DateTime.UtcNow;
            result.Result.VerificationToken = randomTokenString();

            // hash password
            result.Result.Password = BC.HashPassword(password);

            return result;
        }

        private OASISResult<IAvatarDetail> PrepareToRegisterAvatarDetail(Guid avatarId, string username, string email, OASISType createdOASISType, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            IAvatarDetail avatarDetail = new AvatarDetail() { Id = avatarId, IsNewHolon = true, Email = email, Username = username, CreatedOASISType = new EnumValue<OASISType>(createdOASISType), STARCLIColour = cliColour, FavouriteColour = favColour };

            // TODO: Temp! Remove later!
            if (email == "davidellams@hotmail.com")
            {
                avatarDetail.Karma = 777777;
                avatarDetail.XP = 2222222;

                avatarDetail.GeneKeys.Add(new GeneKey() { Name = "Expectation", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });
                avatarDetail.GeneKeys.Add(new GeneKey() { Name = "Invisibility", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });
                avatarDetail.GeneKeys.Add(new GeneKey() { Name = "Rapture", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });

                avatarDetail.HumanDesign.Type = "Generator";
                avatarDetail.Inventory.Add(new InventoryItem() { Name = "Magical Armour" });
                avatarDetail.Inventory.Add(new InventoryItem() { Name = "Mighty Wizard Sword" });

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
                avatarDetail.Stats.Staminia.Current = 99;
                avatarDetail.Stats.Staminia.Max = 99;

                avatarDetail.SuperPowers.AstralProjection = 99;
                avatarDetail.SuperPowers.BioLocatation = 88;
                avatarDetail.SuperPowers.Flight = 99;
                avatarDetail.SuperPowers.FreezeBreath = 88;
                avatarDetail.SuperPowers.HeatVision = 99;
                avatarDetail.SuperPowers.Invulerability = 99;
                avatarDetail.SuperPowers.SuperSpeed = 99;
                avatarDetail.SuperPowers.SuperStrength = 99;
                avatarDetail.SuperPowers.XRayVision = 99;
                avatarDetail.SuperPowers.Teleportation = 99;
                avatarDetail.SuperPowers.Telekineseis = 99;

                avatarDetail.Skills.Computers = 99;
                avatarDetail.Skills.Engineering = 99;
            }

            //avatarDetail.CreatedDate = DateTime.UtcNow;

            result.Result = avatarDetail;
            return result;
        }

        private OASISResult<IAvatar> AvatarRegistered(OASISResult<IAvatar> result, string origin)
        {
            if (OASISDNA.OASIS.Security.SendVerificationEmail)
                sendVerificationEmail(result.Result, origin);

            result.Result = HideAuthDetails(result.Result);
            result.IsSaved = true;
            result.Message = "Avatar Created Successfully. Please check your email for the verification email. You will not be able to log in till you have verified your email. Thank you.";

            return result;
        }

        //TODO: Want to try and get all methods above to route through some generic function like this ASAP...
        private OASISResult<IAvatar> LoadAvatar(Func<string, int, OASISResult<IAvatar>> avatarLoadFunc, string param1, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                    result = avatarLoadFunc(param1, version);
                else
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Error loading avatar ", param1, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
            }

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value);

                            if (!providerResult.IsError && providerResult.Result != null)
                                result = avatarLoadFunc(param1, version);
                            else
                            {
                                result.IsError = true;
                                result.Message = providerResult.Message;
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorHandling.HandleError(ref result, string.Concat("Error loading avatar ", param1, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
                        }
                    }
                }
            }

            if (result.Result == null)
            {
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", param1, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else if (hideAuthDetails)
                result.Result = HideAuthDetails(result.Result);

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }
    }
}
