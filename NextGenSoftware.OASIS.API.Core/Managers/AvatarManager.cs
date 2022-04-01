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

            try
            {
                result = LoadAvatar(username, false);

                if (result.Result == null)
                {
                    result.IsError = true;
                    result.Message = $"This avatar does not exist. Please contact support or create a new avatar. Error Details: {result.Message}";
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
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in Authenticate method in AvatarManager. Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> AuthenticateAsync(string username, string password, string ipAddress)
        {
            OASISResult<IAvatar> result = await LoadAvatarAsync(username, false);

            try
            {
                if (result.Result == null)
                {
                    result.IsError = true;
                    result.Message = $"This avatar does not exist. Please contact support or create a new avatar. Error Details: {result.Message}";
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
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in AuthenticateAsync method in AvatarManager. Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatar> Register(string avatarTitle, string firstName, string lastName, string email, string password, AvatarType avatarType, string origin, OASISType createdOASISType, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                result = PrepareToRegisterAvatar(avatarTitle, firstName, lastName, email, password, avatarType, origin, createdOASISType);

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
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in Register method in AvatarManager. Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> RegisterAsync(string avatarTitle, string firstName, string lastName, string email, string password, AvatarType avatarType, string origin, OASISType createdOASISType, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                result = PrepareToRegisterAvatar(avatarTitle, firstName, lastName, email, password, avatarType, origin, createdOASISType);


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
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in RegisterAsync method in AvatarManager. Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<bool> VerifyEmail(string token)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
                OASISResult<IEnumerable<IAvatar>> avatarsResult = LoadAllAvatars();

                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    IAvatar avatar = avatarsResult.Result.FirstOrDefault(x => x.VerificationToken == token);

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
                }
                else
                    ErrorHandling.HandleError(ref result, $"Error in VerifyEmail loading all avatars. Reason: {avatarsResult.Message}");

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
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in VerifyEmail method in AvatarManager. Error Message: ", ex.ToString()), ex);
                result.Result = false;
            }

            return result;
        }

        public OASISResult<IAvatar> LoadAvatar(Guid id, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = LoadAvatarForProvider(id, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarForProvider(id, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar ", id, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with id ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await LoadAvatarForProviderAsync(id, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarForProviderAsync(id, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar ", id, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with id ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatar> LoadAvatar(string username, string password, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = LoadAvatarForProvider(username, password, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarForProvider(username, password, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await LoadAvatarForProviderAsync(username, password, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarForProviderAsync(username, password, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        //TODO: Replicate Auto-Fail over and Auto-Replication code for all Avatar, HolonManager methods etc...
        public OASISResult<IAvatar> LoadAvatar(string username, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = LoadAvatarForProvider(username, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarForProvider(username, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await LoadAvatarForProviderAsync(username, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarForProviderAsync(username, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatar> LoadAvatarByEmail(string email, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = LoadAvatarByEmailForProvider(email, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarByEmailForProvider(email, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar with email ", email, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar with email ", email, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with email ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await LoadAvatarByEmailForProviderAsync(email, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarByEmailForProviderAsync(email, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar with email ", email, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar with email ", email, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with email ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = LoadAvatarDetailForProvider(id, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarDetailForProvider(id, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar with id ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar detail with id ", id, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";   
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with id ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await LoadAvatarDetailForProviderAsync(id, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarDetailForProviderAsync(id, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with id ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar detail with id ", id, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with id ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await LoadAvatarDetailByEmailForProviderAsync(email, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarDetailByEmailForProviderAsync(email, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with email ", email, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar detail with email ", email, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with email ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = LoadAvatarDetailByEmailForProvider(email, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarDetailByEmailForProvider(email, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with email ", email, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar detail with email ", email, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";   
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with email ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await LoadAvatarDetailByUsernameForProviderAsync(username, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarDetailByEmailForProviderAsync(username, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with username ", username, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar detail with username ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = LoadAvatarDetailByUsernameForProvider(username, providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarDetailByEmailForProvider(username, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with username ", username, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar detail with username ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        //public IEnumerable<IAvatar> LoadAllAvatarsWithPasswords(ProviderType provider = ProviderType.Default)
        //{
        //    //TODO: Need to handle return of OASISResult properly...
        //    IEnumerable<IAvatar> avatars = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllAvatars().Result;
        //    return avatars;
        //}

        //public async Task<IEnumerable<IAvatar>> LoadAllAvatarsWithPasswordsAsync(ProviderType provider = ProviderType.Default)
        //{
        //    //TODO: Need to handle return of OASISResult properly...
        //    IEnumerable<IAvatar> avatars = await ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllAvatarsAsync();
        //    return avatars;
        //}

        public OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = LoadAllAvatarsForProvider(providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAllAvatarsForProvider(type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatars. Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("All avatars loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatars Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatars for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await LoadAllAvatarsForProviderAsync(providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAllAvatarsForProviderAsync(type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatars. Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("All avatars loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatars Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatars for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = LoadAllAvatarDetailsForProvider(providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAllAvatarDetailsForProvider(type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatar details. Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("All avatar details loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Details Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatar details for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await LoadAllAvatarDetailsForProviderAsync(providerType, version);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAllAvatarDetailsForProviderAsync(type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatar details. Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("All avatar details loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Details Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatar details for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                int removingDays = OASISDNA.OASIS.Security.RemoveOldRefreshTokensAfterXDays;
                int removeQty = avatar.RefreshTokens.RemoveAll(token => (DateTime.Today - token.Created).TotalDays > removingDays);

                result = await SaveAvatarForProviderAsync(avatar, providerType);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await SaveAvatarForProviderAsync(avatar, type.Value);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar ", avatar.Name, " with id ", avatar.Id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Saved.";   
                }

                //TODO: Need to move into background thread ASAP!
                if (ProviderManager.IsAutoReplicationEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            result = await SaveAvatarForProviderAsync(avatar, type.Value);
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
                result.Result = null;
            }

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

                result = SaveAvatarForProvider(avatar, providerType);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = SaveAvatarForProvider(avatar, type.Value);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar ", avatar.Name, " with id ", avatar.Id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Saved.";
                }

                //TODO: Need to move into background thread ASAP!
                if (ProviderManager.IsAutoReplicationEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            result = SaveAvatarForProvider(avatar, type.Value);
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = SaveAvatarDetailForProvider(avatar, providerType);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = SaveAvatarDetailForProvider(avatar, type.Value);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar detail ", avatar.Name, " with id ", avatar.Id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar detail ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Saved."; 
                }

                //TODO: Need to move into background thread ASAP!
                if (ProviderManager.IsAutoReplicationEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            result = SaveAvatarDetailForProvider(avatar, type.Value);
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar detail ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await SaveAvatarDetailForProviderAsync(avatar, providerType);

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await SaveAvatarDetailForProviderAsync(avatar, type.Value);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar detail ", avatar.Name, " with id ", avatar.Id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar detail ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Saved.";
                }

                //TODO: Need to move into background thread ASAP!
                if (ProviderManager.IsAutoReplicationEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            result = await SaveAvatarDetailForProviderAsync(avatar, type.Value);
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar detail ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), true, false, false, false, true, ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = DeleteAvatarForProvider(id, softDelete, providerType);

                if ((!result.Result || result.IsError) && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = DeleteAvatarForProvider(id, softDelete, type.Value);

                            if (!result.IsError && result.Result)
                                break;
                        }
                    }
                }

                if (result.IsError || !result.Result)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar with id ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar with id ", id, " was successfully deleted for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Deleted.";
                }

                //TODO: Need to move into background thread ASAP!
                if (ProviderManager.IsAutoReplicationEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            result = DeleteAvatarForProvider(id, softDelete, type.Value);
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar with id ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = false;
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await DeleteAvatarForProviderAsync(id, softDelete, providerType);

                if ((!result.Result || result.IsError) && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await DeleteAvatarForProviderAsync(id, softDelete, type.Value);

                            if (!result.IsError && result.Result)
                                break;
                        }
                    }
                }

                if (result.IsError || !result.Result)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar with id ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar with id ", id, " was successfully deleted for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Deleted.";
                }

                //TODO: Need to move into background thread ASAP!
                if (ProviderManager.IsAutoReplicationEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            result = await DeleteAvatarForProviderAsync(id, softDelete, type.Value);
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar with id ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = false;
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string userName, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await DeleteAvatarByUsernameForProviderAsync(userName, softDelete, providerType);

                if ((!result.Result || result.IsError) && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await DeleteAvatarByUsernameForProviderAsync(userName, softDelete, type.Value);

                            if (!result.IsError && result.Result)
                                break;
                        }
                    }
                }

                if (result.IsError || !result.Result)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar with username ", userName, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar with username ", userName, " was successfully deleted for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Deleted.";
                }

                //TODO: Need to move into background thread ASAP!
                if (ProviderManager.IsAutoReplicationEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            result = await DeleteAvatarByUsernameForProviderAsync(userName, softDelete, type.Value);
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar with userName ", userName, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = false;
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = await DeleteAvatarByEmailForProviderAsync(email, softDelete, providerType);

                if ((!result.Result || result.IsError) && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await DeleteAvatarByEmailForProviderAsync(email, softDelete, type.Value);

                            if (!result.IsError && result.Result)
                                break;
                        }
                    }
                }

                if (result.IsError || !result.Result)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar with email ", email, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar with email ", email, " was successfully deleted for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Deleted.";
                }

                //TODO: Need to move into background thread ASAP!
                if (ProviderManager.IsAutoReplicationEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            result = await DeleteAvatarByEmailForProviderAsync(email, softDelete, type.Value);
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar with email ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = false;
            }

            return result;
        }

        public OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                result = DeleteAvatarByEmailForProvider(email, softDelete, providerType);

                if ((!result.Result || result.IsError) && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = DeleteAvatarByEmailForProvider(email, softDelete, type.Value);

                            if (!result.IsError && result.Result)
                                break;
                        }
                    }
                }

                if (result.IsError || !result.Result)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar with email ", email, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        result.Message = string.Concat("The avatar with email ", email, " was successfully deleted for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));
                    else
                        result.Message = "Avatar Successfully Deleted.";
                }

                //TODO: Need to move into background thread ASAP!
                if (ProviderManager.IsAutoReplicationEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            result = DeleteAvatarByEmailForProvider(email, softDelete, type.Value);
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar with email ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
                result.Result = false;
            }

            return result;
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

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
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

        /*
        //TODO: Want to try and get all methods above to route through some generic function like this ASAP...
        private async Task<OASISResult<IAvatar>> LoadAvatarAsync(Func<string, int, Task<OASISResult<IAvatar>>> avatarLoadFunc, string param1, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            result = await LoadAvatarForProviderAsync(avatarLoadFunc, param1, providerType, version);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = await LoadAvatarForProviderAsync(avatarLoadFunc, param1, type.Value, version);

                        if (!result.IsError && result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
                ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", param1, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), ".\n\nError Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
            {
                if (result.WarningCount > 0)
                    result.Message = string.Concat("The avatar ", param1, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "\n\n.Error Details:\n", OASISResultHelper.BuildInnerMessageError(result.InnerMessages));

                if (hideAuthDetails)
                    result.Result = HideAuthDetails(result.Result);
            }

            // Set the current provider back to the original provider.
            ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return result;
        }

        private async Task<OASISResult<IAvatar>> LoadAvatarForProviderAsync(Func<string, int, Task<OASISResult<IAvatar>>> avatarLoadFunc, string param1, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

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

                            ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", param1, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Reason: ", result.Message));
                        }
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", param1, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Reason: timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat("Error loading avatar ", param1, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". There was an error setting the provider. Reason:", providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat("Unknown error occured loading avatar ", param1, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), ex);
            }

            return result;
        }*/

        private OASISResult<IAvatar> LoadAvatarForProvider(Guid id, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            //TODO: IMPLEMENT DIFFERENT TIMEOUT MECHNISM FOR NON-ASYNC METHODS? OR JUST CALL THE ASYNC VERSION?
            return LoadAvatarForProviderAsync(id, providerType, version).Result;
        }

        private async Task<OASISResult<IAvatar>> LoadAvatarForProviderAsync(Guid id, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            string errorMessage = $"Error in LoadAvatarForProviderAsync method in AvatarManager loading avatar with id {id} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarAsync(id, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Avatar Not Found.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        private OASISResult<IAvatar> LoadAvatarForProvider(string username, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            //TODO: IMPLEMENT DIFFERENT TIMEOUT MECHNISM FOR NON-ASYNC METHODS? OR JUST CALL THE ASYNC VERSION?
            return LoadAvatarForProviderAsync(username, providerType, version).Result;
        }

        private async Task<OASISResult<IAvatar>> LoadAvatarForProviderAsync(string username, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            string errorMessage = $"Error in LoadAvatarForProviderAsync method in AvatarManager loading avatar with username {username} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarAsync(username, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Avatar Not Found.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        private OASISResult<IAvatar> LoadAvatarForProvider(string username, string password, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            return LoadAvatarForProviderAsync(username, password).Result;
        }

        private async Task<OASISResult<IAvatar>> LoadAvatarForProviderAsync(string username, string password, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            string errorMessage = $"Error in LoadAvatarForProviderAsync method in AvatarManager loading avatar with username {username} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarAsync(username, password, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Avatar Not Found.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        private OASISResult<IAvatar> LoadAvatarByEmailForProvider(string email, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            return LoadAvatarByEmailForProviderAsync(email, providerType, version).Result;
        }

        private async Task<OASISResult<IAvatar>> LoadAvatarByEmailForProviderAsync(string email, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            string errorMessage = $"Error in LoadAvatarByEmailForProviderAsync method in AvatarManager loading avatar with email {email} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarByEmailAsync(email, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Avatar Not Found.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        private OASISResult<IAvatarDetail> LoadAvatarDetailForProvider(Guid id, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            return LoadAvatarDetailForProviderAsync(id, providerType, version).Result;
        }

        private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailForProviderAsync(Guid id, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            string errorMessage = $"Error in LoadAvatarDetailForProviderAsync method in AvatarManager loading avatar detail with id {id} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarDetailAsync(id, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Avatar Detail Not Found.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        private OASISResult<IAvatarDetail> LoadAvatarDetailByEmailForProvider(string email, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            return LoadAvatarDetailByEmailForProviderAsync(email, providerType, version).Result;
        }

        private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailForProviderAsync(string email, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            string errorMessage = $"Error in LoadAvatarDetailByUsernameForProviderAsync method in AvatarManager loading avatar detail with email {email} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarDetailByEmailAsync(email, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Avatar Detail Not Found.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        private OASISResult<IAvatarDetail> LoadAvatarDetailByUsernameForProvider(string username, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            return LoadAvatarDetailByUsernameForProviderAsync(username, providerType, version).Result;
        }

        private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameForProviderAsync(string username, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            string errorMessage = $"Error in LoadAvatarDetailByUsernameForProviderAsync method in AvatarManager loading avatar detail with username {username} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAvatarDetailByUsernameAsync(username, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Avatar Detail Not Found.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        private OASISResult<IEnumerable<IAvatar>> LoadAllAvatarsForProvider(ProviderType providerType = ProviderType.Default, int version = 0)
        {
            return LoadAllAvatarsForProviderAsync(providerType, version).Result;
        }



        private async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsForProviderAsync(ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            string errorMessage = $"Error in LoadAllAvatarsForProviderAsync method in AvatarManager loading all avatars for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAllAvatarsAsync(version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "No Avatars Were Found.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        private OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsForProvider(ProviderType providerType = ProviderType.Default, int version = 0)
        {
            return LoadAllAvatarDetailsForProviderAsync(providerType, version).Result;
        }

        private async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsForProviderAsync(ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            string errorMessage = $"Error in LoadAllAvatarDetailsForProviderAsync method in AvatarManager loading all avatar details for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadAllAvatarDetailsAsync(version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "No Avatar Details Were Found.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> SaveAvatarForProviderAsync(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            string errorMessage = $"Error in SaveAvatarDetailForProviderAsync method in AvatarManager saving avatar detail with name {avatar.Name}, username {avatar.Username} and id {avatar.Id} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

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

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                        else
                            result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        public OASISResult<IAvatar> SaveAvatarForProvider(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            return SaveAvatarForProviderAsync(avatar, providerType).Result;


            //OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            //ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            //try
            //{
            //    OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

            //    if (!providerResult.IsError && providerResult.Result != null)
            //    {



            //        ////Func<IAvatar, OASISResult<IAvatar>> longMethod = providerResult.Result.SaveAvatar(avatar);
            //        //Action<OASISResult<IAvatar>> longMethod = providerResult.Result.SaveAvatar(avatar);
            //        //var task = providerResult.Result.SaveAvatar(avatar);
            //        //object monitorSync = new object();
            //        //bool timedOut;

            //        //lock (monitorSync)
            //        //{
            //        //    task.BeginInvoke(monitorSync, null, null);
            //        //    timedOut = !Monitor.Wait(monitorSync, TimeSpan.FromSeconds(30)); // waiting 30 secs
            //        //}
            //        //if (timedOut)
            //        //{
            //        //    // it timed out.
            //        //}

            //            //var task = providerResult.Result.SaveAvatar(avatar);

            //            //if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
            //            //{
            //            //    result = task.Result;

            //            //    if (result.IsError || result.Result == null)
            //            //    {
            //            //        if (string.IsNullOrEmpty(result.Message))
            //            //            result.Message = "Unknown.";

            //            //        ErrorHandling.HandleWarning(ref result, string.Concat("Error saving avatar for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Reason: ", result.Message));
            //            //    }
            //            //}
            //            //else
            //            //    ErrorHandling.HandleWarning(ref result, string.Concat("Error saving avatar for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Reason: timeout occured."));
            //        }
            //        else
            //        ErrorHandling.HandleWarning(ref result, string.Concat("Error saving avatar for provider ", ProviderManager.CurrentStorageProviderType.Name, ". There was an error setting the provider. Reason:", providerResult.Message));
            //}
            //catch (Exception ex)
            //{
            //    ErrorHandling.HandleWarning(ref result, string.Concat("Unknown error occured saving avatar for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()));
            //}

            //return result;
        }

        public async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailForProviderAsync(IAvatarDetail avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            string errorMessage = $"Error in SaveAvatarDetailForProviderAsync method in AvatarManager saving avatar detail with name {avatar.Name}, username {avatar.Username} and id {avatar.Id} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.SaveAvatarDetailAsync(avatar);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || result.Result == null)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Unknown.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                        else
                            result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        public OASISResult<IAvatarDetail> SaveAvatarDetailForProvider(IAvatarDetail avatar, ProviderType providerType = ProviderType.Default)
        {
            return SaveAvatarDetailForProviderAsync(avatar, providerType).Result;
        }

        public OASISResult<bool> DeleteAvatarForProvider(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            return DeleteAvatarForProviderAsync(id, softDelete, providerType).Result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarForProviderAsync(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            //TODO: Make ALL OASIS Methods follow this pattern.
            string errorMessage = $"Error in DeleteAvatarForProviderAsync method in AvatarManager deleting avatar with id {id} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.DeleteAvatarAsync(id, softDelete);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || !result.Result)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Unknown.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                        else
                            result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        public OASISResult<bool> DeleteAvatarByEmailForProvider(string email, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            return DeleteAvatarByEmailForProviderAsync(email, softDelete, providerType).Result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByEmailForProviderAsync(string email, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            //TODO: Make ALL OASIS Methods follow this pattern.
            string errorMessage = $"Error in DeleteAvatarByEmailForProviderAsync method in AvatarManager deleting avatar with email {email} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.DeleteAvatarAsync(email, softDelete);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || !result.Result)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Unknown.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                        else
                            result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        public OASISResult<bool> DeleteAvatarByUsernameForProvider(string username, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            return DeleteAvatarByUsernameForProviderAsync(username, softDelete, providerType).Result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByUsernameForProviderAsync(string username, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            //TODO: Make ALL OASIS Methods follow this pattern.
            string errorMessage = $"Error in DeleteAvatarByUsernameForProviderAsync method in AvatarManager deleting avatar with username {username} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.DeleteAvatarAsync(username, softDelete);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds)) == task)
                    {
                        result = task.Result;

                        if (result.IsError || !result.Result)
                        {
                            if (string.IsNullOrEmpty(result.Message))
                                result.Message = "Unknown.";

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                        else
                            result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }

        //TODO: Wish there was a way we could make a generic way to pass a Func in with DIFFERENT params AND return types?! :) Would save a LOT of code above! ;-) lol
        /*
        public async Task<OASISResult<IAvatar>> CallOASISProviderMethodAsync(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            string errorMessage = $"Error in SaveAvatarDetailForProviderAsync method in AvatarManager saving avatar detail with name {avatar.Name}, username {avatar.Username} and id {avatar.Id} for provider {ProviderManager.CurrentStorageProviderType.Name}. Reason: ";

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

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

                            ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, result.Message));
                        }
                        else
                            result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.ToString()), ex);
            }

            return result;
        }*/
    }
}
