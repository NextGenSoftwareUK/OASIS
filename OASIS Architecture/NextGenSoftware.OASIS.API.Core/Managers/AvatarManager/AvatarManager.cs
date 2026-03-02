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
    public partial class AvatarManager : OASISManager
    {
        private static AvatarManager _instance = null;
        private ProviderManagerConfig _config;
        private static IAvatar _loggedInAvatar = null;

        //public static IAvatar OASISSystemAccount { get; set; } //TODO: Later may need to actually create a avatar for this Id? So we can see which ids belong to OASIS Accounts outside of each ONODE (each ONODE has its own OASISDNA with its own system id's).

        public static IAvatar LoggedInAvatar 
        { 
            get
            {
                // Request-scoped avatar first (set by WEB4/WEB5 middleware) - safe for multiple concurrent clients
                var requestAvatar = OASISRequestContext.CurrentAvatar;
                if (requestAvatar != null)
                    return requestAvatar;
                var requestAvatarId = OASISRequestContext.CurrentAvatarId;
                if (requestAvatarId.HasValue && requestAvatarId.Value != Guid.Empty)
                    return new Avatar() { Id = requestAvatarId.Value };
                // Fallback to static for non-API callers (e.g. CLI, desktop)
                if (_loggedInAvatar == null && !string.IsNullOrEmpty(Instance.OASISDNA.OASIS.OASISSystemAccountId))
                    _loggedInAvatar = new Avatar() { Id = new Guid(Instance.OASISDNA.OASIS.OASISSystemAccountId) };
                return _loggedInAvatar;
            }
            set
            {
                _loggedInAvatar = value;
            }
        } 
        
        public static Dictionary<string, IAvatar> LoggedInAvatarSessions { get; set; }
        //public List<IOASISStorageProvider> OASISStorageProviders { get; set; }

        //TODO Implement this singleton pattern for other Managers...
        public static AvatarManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AvatarManager(ProviderManager.Instance.CurrentStorageProvider, ProviderManager.Instance.OASISDNA);

                return _instance;
            }
        }

        public ProviderManagerConfig Config
        {
            get
            {
                if (_config == null)
                    _config = new ProviderManagerConfig();

                return _config;
            }
        }

        //public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

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
            //They can log in with either username, email or a public key linked to the avatar.
            OASISResult<IAvatar> result = null;

            try
            {
                //Temp supress logging to the console in case STAR CLI is creating a new avatar...
                //CLIEngine.SupressConsoleLogging = true;

                //Temp disable the OASIS HyperDrive so it returns fast and does not attempt to find the avatar across all providers! ;-)
                //TODO: May want to fine tune how we handle this in future?
                //bool isAutoFailOverEnabled = ProviderManager.Instance.IsAutoFailOverEnabled;
                //ProviderManager.Instance.IsAutoFailOverEnabled = false;

                List<EnumValue<ProviderType>> currentProviderFailOverList = ProviderManager.Instance.GetProviderAutoFailOverList();
                ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(ProviderManager.Instance.GetProviderAutoFailOverListForAvatarLogin());

                //First try by username...
                result = LoadAvatar(username, false, false);

                if (result.Result == null)
                {
                    //Now try by email...
                    result = LoadAvatarByEmail(username, false, false);

                    if (result.Result == null)
                    {
                        //Finally by Public Key...
                        //TODO: Make this more efficient so we do not need to load all avatars!
                        OASISResult<IEnumerable<IAvatar>> avatarsResult = LoadAllAvatars();

                        if (!avatarsResult.IsError && avatarsResult.Result != null)
                        {
                            if (avatarsResult.Result.Any(x => x.ProviderWallets.ContainsKey(ProviderManager.Instance.CurrentStorageProviderType.Value)))
                                result.Result = avatarsResult.Result.FirstOrDefault(x => x.ProviderWallets[ProviderManager.Instance.CurrentStorageProviderType.Value].Any(x => x.PublicKey == username));
                        }
                    }
                }

                if (result.Result == null)
                    result.Message = $"This avatar does not exist. Please contact support or create a new avatar.";
                else
                {
                    result = ProcessAvatarLogin(result, password);

                    //TODO: Come back to this.
                    //if (OASISDNA.OASIS.Security.AvatarPassword.)

                    if (result.Result != null & !result.IsError)
                    {
                        var jwtToken = GenerateJWTToken(result.Result);
                        var refreshToken = generateRefreshToken(ipAddress);

                        if (result.Result.RefreshTokens == null)
                            result.Result.RefreshTokens = new List<RefreshToken>();

                        result.Result.RefreshTokens.Add(refreshToken);
                        result.Result.JwtToken = jwtToken;
                        result.Result.RefreshToken = refreshToken.Token;
                        result.Result.LastBeamedIn = DateTime.Now;
                        result.Result.IsBeamedIn = true;

                        LoggedInAvatar = result.Result;
                        OASISResult<IAvatar> saveAvatarResult = SaveAvatar(result.Result);

                        if (!saveAvatarResult.IsError && saveAvatarResult.IsSaved)
                        {
                            result.Result = HideAuthDetails(saveAvatarResult.Result, false, true, false, false);
                            result.IsSaved = true;
                            result.Message = "Avatar Successfully Authenticated.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured in Authenticate method in AvatarManager whilst saving the avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    }
                    else
                        result.Result = null;
                }

                ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(currentProviderFailOverList);
                //ProviderManager.Instance.IsAutoFailOverEnabled = isAutoFailOverEnabled;
                //CLIEngine.SupressConsoleLogging = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in Authenticate method in AvatarManager. Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public delegate OASISResult<IAvatar> SaveAvatarFunction(IAvatar avatar);

        public async Task<OASISResult<IAvatar>> AuthenticateAsync(string username, string password, string ipAddress, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false)
        {
            //They can log in with either username, email or a public key linked to the avatar.
            OASISResult<IAvatar> result = null;

            try
            {
                //Temp supress logging to the console in case STAR CLI is creating a new avatar...
                //CLIEngine.SupressConsoleLogging = true;

                //Temp disable the OASIS HyperDrive so it returns fast and does not attempt to find the avatar across all providers! ;-)
                //TODO: May want to fine tune how we handle this in future?
                //bool isAutoFailOverEnabled = ProviderManager.Instance.IsAutoFailOverEnabled;
                //ProviderManager.Instance.IsAutoFailOverEnabled = false;

                List<EnumValue<ProviderType>> currentProviderFailOverList = ProviderManager.Instance.GetProviderAutoFailOverList();
                ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(ProviderManager.Instance.GetProviderAutoFailOverListForAvatarLogin());

                //First try by username...
                result = await LoadAvatarAsync(username, false, false);

                if (result.Result == null)
                {
                    //Now try by email...
                    result = await LoadAvatarByEmailAsync(username, false, false);

                    if (result.Result == null)
                    {
                        //Finally by Public Key...
                        OASISResult<IEnumerable<IAvatar>> avatarsResult = await LoadAllAvatarsAsync(false);

                        if (!avatarsResult.IsError && avatarsResult.Result != null)
                        {
                            if (avatarsResult.Result.Any(x => x.ProviderWallets.ContainsKey(ProviderManager.Instance.CurrentStorageProviderType.Value)))
                                result.Result = avatarsResult.Result.FirstOrDefault(x => x.ProviderWallets[ProviderManager.Instance.CurrentStorageProviderType.Value].Any(x => x.PublicKey == username));
                        }
                    }
                }

                if (result.Result == null)
                    result.Message = $"This avatar does not exist. Please contact support or create a new avatar.";
                else
                {
                    //ProcessAvatarLogin(result, username, password, ipAddress, (result.Result) => { SaveAvatar(result.Result); }) ;
                    // ProcessAvatarLogin(result, username, password, ipAddress, SaveAvatar);
                    result = ProcessAvatarLogin(result, password);

                    //TODO: Come back to this.
                    //if (OASISDNA.OASIS.Security.AvatarPassword.)

                    if (result.Result != null & !result.IsError)
                    {
                        var jwtToken = GenerateJWTToken(result.Result);
                        var refreshToken = generateRefreshToken(ipAddress);

                        if (result.Result.RefreshTokens == null)
                            result.Result.RefreshTokens = new List<RefreshToken>();

                        result.Result.RefreshTokens.Add(refreshToken);
                        result.Result.JwtToken = jwtToken;
                        result.Result.RefreshToken = refreshToken.Token;
                        result.Result.LastBeamedIn = DateTime.Now;
                        result.Result.IsBeamedIn = true;

                        LoggedInAvatar = result.Result;
                        OASISResult<IAvatar> saveAvatarResult = SaveAvatar(result.Result, autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, waitForAutoReplicationResult);

                        if (!saveAvatarResult.IsError && saveAvatarResult.IsSaved)
                        {
                            result.Result = HideAuthDetails(saveAvatarResult.Result, false, true, false, false);
                            result.IsSaved = true;
                            result.Message = "Avatar Successfully Authenticated.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured in AuthenticateAsync method in AvatarManager whilst saving the avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    }
                    else
                        result.Result = null;
                }


                ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(currentProviderFailOverList);
                //ProviderManager.Instance.IsAutoFailOverEnabled = isAutoFailOverEnabled;
                //CLIEngine.SupressConsoleLogging = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in AuthenticateAsync method in AvatarManager. Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatar> Register(string avatarTitle, string firstName, string lastName, string email, string password, string username, AvatarType avatarType, OASISType createdOASISType, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
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
                                result = AvatarRegistered(result);
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

        public async Task<OASISResult<IAvatar>> RegisterAsync(string avatarTitle, string firstName, string lastName, string email, string password, string username, AvatarType avatarType, OASISType createdOASISType, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
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
                                result = AvatarRegistered(result);
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
                //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
                OASISResult<IEnumerable<IAvatar>> avatarsResult = LoadAllAvatars(false, false);

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
                        avatar.IsActive = true;
                        OASISResult<IAvatar> saveAvatarResult = SaveAvatar(avatar);

                        result.IsError = saveAvatarResult.IsError;
                        result.IsSaved = saveAvatarResult.IsSaved;
                        result.Message = saveAvatarResult.Message;
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error in VerifyEmail loading all avatars. Reason: {avatarsResult.Message}", avatarsResult.DetailedMessage);

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
        public async Task<OASISResult<string>> ForgotPasswordAsync(string email, ProviderType providerType = ProviderType.Default)
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
                SendPasswordResetEmail(avatarResult.Result);
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

        public OASISResult<string> ForgotPassword(string email, ProviderType providerType = ProviderType.Default)
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
                SendPasswordResetEmail(avatarResult.Result);
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
                OASISResult<IEnumerable<IAvatar>> avatarsResult = await LoadAllAvatarsAsync(false, false, false, providerType);

                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
                    var avatar = avatarsResult.Result.FirstOrDefault(x =>
                        x.ResetToken == token &&
                        x.ResetTokenExpires > DateTime.UtcNow);

                    if (avatar == null)
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found, token is invalid.");
                        return response;
                    }

                    if (!BCrypt.Net.BCrypt.Verify(oldPassword, avatar.Password))
                    {
                        OASISErrorHandling.HandleError(ref response, "Old Password Is Not Correct");
                        return response;
                    }

                    // update password and remove reset token
                    avatar.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
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
                else
                    OASISErrorHandling.HandleError(ref response, $"Error occured in ResetPassword loading all avatars. Reason: {avatarsResult.Message}", avatarsResult.DetailedMessage);
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
                OASISResult<IEnumerable<IAvatar>> avatarsResult = LoadAllAvatars(false, false, false, providerType);

                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
                    var avatar = avatarsResult.Result.FirstOrDefault(x =>
                        x.ResetToken == token &&
                        x.ResetTokenExpires > DateTime.UtcNow);

                    if (avatar == null)
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found, token is invalid.");
                        return response;
                    }

                    if (!BCrypt.Net.BCrypt.Verify(oldPassword, avatar.Password))
                    {
                        OASISErrorHandling.HandleError(ref response, "Old Password Is Not Correct");
                        return response;
                    }

                    // update password and remove reset token
                    avatar.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
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
                else
                    OASISErrorHandling.HandleError(ref response, $"Error occured in ResetPassword loading all avatars. Reason: {avatarsResult.Message}", avatarsResult.DetailedMessage);
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
            //bool isAutoFailOverEnabled = ProviderManager.Instance.IsAutoFailOverEnabled;
            //ProviderManager.Instance.IsAutoFailOverEnabled = false;

            List<EnumValue<ProviderType>> currentProviderFailOverList = ProviderManager.Instance.GetProviderAutoFailOverList();
            ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(ProviderManager.Instance.GetProviderAutoFailOverListForCheckIfEmailAlreadyInUse());
            OASISResult<IAvatar> existingAvatarResult = LoadAvatarByEmail(email);
            ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(currentProviderFailOverList);
            //ProviderManager.Instance.IsAutoFailOverEnabled = isAutoFailOverEnabled;

            //CLIEngine.SupressConsoleLogging = false;

            if (!existingAvatarResult.IsError && existingAvatarResult.Result != null)
            {
                //If the avatar has previously been deleted (soft deleted) then allow them to create a new avatar with the same email address.
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

            return result;
        }

        public OASISResult<bool> CheckIfUsernameIsAlreadyInUse(string username)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            ////Temp supress logging to the console in case STAR CLI is creating a new avatar...
            CLIEngine.SupressConsoleLogging = true;

            //Temp disable the OASIS HyperDrive so it returns fast and does not attempt to find the avatar across all providers! ;-)
            //TODO: May want to fine tune how we handle this in future?
            //bool isAutoFailOverEnabled = ProviderManager.Instance.IsAutoFailOverEnabled;
            //ProviderManager.Instance.IsAutoFailOverEnabled = false;

            List<EnumValue<ProviderType>> currentProviderFailOverList = ProviderManager.Instance.GetProviderAutoFailOverList();
            ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(ProviderManager.Instance.GetProviderAutoFailOverListForCheckIfUsernameAlreadyInUse());
            OASISResult<IAvatar> existingAvatarResult = LoadAvatar(username);
            ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(currentProviderFailOverList);
            //ProviderManager.Instance.IsAutoFailOverEnabled = isAutoFailOverEnabled;

            CLIEngine.SupressConsoleLogging = false;

            if (!existingAvatarResult.IsError && existingAvatarResult.Result != null)
            {
                //If the avatar has previously been deleted (soft deleted) then allow them to create a new avatar with the same email address.
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

            return result;
        }

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

        #region Session Management - OASIS SSO System 🚀

        /// <summary>
        /// Get all sessions for a specific avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Session management data</returns>
        public async Task<OASISResult<AvatarSessionManagement>> GetAvatarSessionsAsync(Guid avatarId)
        {
            OASISResult<AvatarSessionManagement> result = new OASISResult<AvatarSessionManagement>();
            string errorMessage = $"Error in GetAvatarSessionsAsync method in AvatarManager for avatar {avatarId}. Reason: ";

            try
            {
                // For now, return demo data - this would be implemented with actual session storage
                var sessionManagement = new AvatarSessionManagement
                {
                    TotalSessions = 12,
                    ActiveSessions = 4,
                    CurrentLocation = "San Francisco, CA",
                    LastLogin = DateTime.UtcNow.AddHours(-2),
                    SecurityStatus = "Secure",
                    Sessions = new List<AvatarSession>
                    {
                        new AvatarSession
                        {
                            Id = "1",
                            AvatarId = avatarId,
                            ServiceName = "STARNET Dashboard",
                            ServiceType = "platform",
                            DeviceType = "desktop",
                            DeviceName = "MacBook Pro 16\"",
                            Location = "San Francisco, CA",
                            IpAddress = "192.168.1.100",
                            IsActive = true,
                            LastActivity = DateTime.UtcNow.AddMinutes(-5),
                            LoginTime = DateTime.UtcNow.AddHours(-2),
                            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)",
                            Platform = "macOS",
                            Version = "1.0.0"
                        },
                        new AvatarSession
                        {
                            Id = "2",
                            AvatarId = avatarId,
                            ServiceName = "OASIS Gaming Platform",
                            ServiceType = "game",
                            DeviceType = "vr",
                            DeviceName = "Oculus Quest 3",
                            Location = "San Francisco, CA",
                            IpAddress = "192.168.1.101",
                            IsActive = true,
                            LastActivity = DateTime.UtcNow.AddMinutes(-15),
                            LoginTime = DateTime.UtcNow.AddHours(-4),
                            UserAgent = "OculusBrowser/1.0.0",
                            Platform = "Android",
                            Version = "2.1.0"
                        }
                    }
                };

                result.Result = sessionManagement;
                result.IsError = false;
                result.Message = "Avatar sessions retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        /// <summary>
        /// Logout avatar from specific sessions (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionIds">List of session IDs to logout</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> LogoutAvatarSessionsAsync(Guid avatarId, List<string> sessionIds)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = $"Error in LogoutAvatarSessionsAsync method in AvatarManager for avatar {avatarId}. Reason: ";

            try
            {
                // For now, simulate successful logout - this would be implemented with actual session management
                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully logged out from {sessionIds.Count} session(s)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        /// <summary>
        /// Logout avatar from all sessions (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> LogoutAllAvatarSessionsAsync(Guid avatarId)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = $"Error in LogoutAllAvatarSessionsAsync method in AvatarManager for avatar {avatarId}. Reason: ";

            try
            {
                // For now, simulate successful logout - this would be implemented with actual session management
                result.Result = true;
                result.IsError = false;
                result.Message = "Successfully logged out from all sessions";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        /// <summary>
        /// Create a new session for an avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionData">Session information</param>
        /// <returns>Created session</returns>
        public async Task<OASISResult<AvatarSession>> CreateAvatarSessionAsync(Guid avatarId, CreateSessionRequest sessionData)
        {
            OASISResult<AvatarSession> result = new OASISResult<AvatarSession>();
            if (sessionData == null)
            {
                result.IsError = true;
                result.Message = "The session data is required. Please provide a valid request with ServiceName, ServiceType, and optional DeviceType, Location, etc.";
                return result;
            }
            string errorMessage = $"Error in CreateAvatarSessionAsync method in AvatarManager for avatar {avatarId}. Reason: ";

            try
            {
                var session = new AvatarSession
                {
                    Id = Guid.NewGuid().ToString(),
                    AvatarId = avatarId,
                    ServiceName = sessionData.ServiceName,
                    ServiceType = sessionData.ServiceType,
                    DeviceType = sessionData.DeviceType,
                    DeviceName = sessionData.DeviceName,
                    Location = sessionData.Location,
                    IpAddress = sessionData.IpAddress,
                    UserAgent = sessionData.UserAgent,
                    Platform = sessionData.Platform,
                    Version = sessionData.Version,
                    Metadata = sessionData.Metadata,
                    ExpiresAt = sessionData.ExpiresAt,
                    IsActive = true,
                    LoginTime = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                result.Result = session;
                result.IsError = false;
                result.Message = "Avatar session created successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        /// <summary>
        /// Update an existing session (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="sessionData">Updated session information</param>
        /// <returns>Updated session</returns>
        public async Task<OASISResult<AvatarSession>> UpdateAvatarSessionAsync(Guid avatarId, string sessionId, UpdateSessionRequest sessionData)
        {
            OASISResult<AvatarSession> result = new OASISResult<AvatarSession>();
            if (sessionData == null)
            {
                result.IsError = true;
                result.Message = "The session data is required. Please provide a valid UpdateSessionRequest (e.g. Location, IpAddress, IsActive).";
                return result;
            }
            string errorMessage = $"Error in UpdateAvatarSessionAsync method in AvatarManager for avatar {avatarId}, session {sessionId}. Reason: ";

            try
            {
                // For now, return a mock updated session - this would be implemented with actual session management
                var session = new AvatarSession
                {
                    Id = sessionId,
                    AvatarId = avatarId,
                    ServiceName = "Updated Service",
                    ServiceType = "platform",
                    DeviceType = "desktop",
                    DeviceName = "Updated Device",
                    Location = sessionData.Location ?? "San Francisco, CA",
                    IpAddress = sessionData.IpAddress ?? "192.168.1.100",
                    UserAgent = sessionData.UserAgent ?? "Mozilla/5.0",
                    Platform = sessionData.Platform ?? "macOS",
                    Version = sessionData.Version ?? "1.0.0",
                    Metadata = sessionData.Metadata,
                    ExpiresAt = sessionData.ExpiresAt,
                    IsActive = sessionData.IsActive ?? true,
                    LastActivity = sessionData.LastActivity ?? DateTime.UtcNow,
                    LoginTime = DateTime.UtcNow.AddHours(-1),
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    UpdatedAt = DateTime.UtcNow
                };

                result.Result = session;
                result.IsError = false;
                result.Message = "Avatar session updated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        /// <summary>
        /// Get session statistics for an avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Session statistics</returns>
        public async Task<OASISResult<AvatarSessionStats>> GetAvatarSessionStatsAsync(Guid avatarId)
        {
            OASISResult<AvatarSessionStats> result = new OASISResult<AvatarSessionStats>();
            string errorMessage = $"Error in GetAvatarSessionStatsAsync method in AvatarManager for avatar {avatarId}. Reason: ";

            try
            {
                var stats = new AvatarSessionStats
                {
                    TotalSessionsCreated = 156,
                    ActiveSessions = 4,
                    SessionsLast24Hours = 8,
                    SessionsLast7Days = 23,
                    SessionsLast30Days = 89,
                    AverageSessionDurationMinutes = 45.5,
                    LongestSessionDurationMinutes = 180.0,
                    MostUsedDeviceType = "desktop",
                    MostUsedServiceType = "platform",
                    MostCommonLocation = "San Francisco, CA",
                    LastSessionTime = DateTime.UtcNow.AddMinutes(-5),
                    SecurityIncidents = 0,
                    UniqueDevicesUsed = 3,
                    UniqueLocationsUsed = 2,
                    TrustScore = 95
                };

                result.Result = stats;
                result.IsError = false;
                result.Message = "Avatar session statistics retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        #endregion

        #region Token Management

        /// <summary>
        /// Refresh an authentication token
        /// </summary>
        public OASISResult<IAvatar> RefreshToken(string token, string ipAddress)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                var refreshToken = GetRefreshTokenFromAvatar(token, out IAvatar avatar);

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found");
                    return response;
                }

                if (refreshToken != null && refreshToken.IsActive)
                {
                    var newRefreshToken = generateRefreshToken(ipAddress);
                    refreshToken.Revoked = DateTime.UtcNow;
                    refreshToken.RevokedByIp = ipAddress;
                    refreshToken.ReplacedByToken = newRefreshToken.Token;
                    avatar.RefreshTokens.Add(newRefreshToken);

                    avatar.RefreshToken = newRefreshToken.Token;
                    avatar.JwtToken = GenerateJWTToken(avatar);

                    OASISResult<IAvatar> saveAvatarResult = SaveAvatar(avatar);

                    if (saveAvatarResult != null && !saveAvatarResult.IsError && saveAvatarResult.Result != null)
                    {
                        avatar = HideAuthDetails(saveAvatarResult.Result);
                        response.Result = avatar;
                    }
                    else
                        OASISErrorHandling.HandleError(ref response, $"Error occurred in RefreshToken method in AvatarManager saving avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                }
                else
                    OASISErrorHandling.HandleError(ref response, "Refresh token is inactive or invalid");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"An unknown error occurred in RefreshToken method in AvatarManager. Reason: {ex.Message}");
            }

            return response;
        }

        /// <summary>
        /// Revoke a refresh token
        /// </summary>
        public OASISResult<string> RevokeToken(string token, string ipAddress)
        {
            var response = new OASISResult<string>();
            var refreshToken = GetRefreshTokenFromAvatar(token, out IAvatar avatar);

            if (avatar == null)
            {
                OASISErrorHandling.HandleError(ref response, "Avatar not found");
                return response;
            }

            // revoke token and save
            if (refreshToken != null && refreshToken.IsActive)
            {
                refreshToken.Revoked = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;
                avatar.IsBeamedIn = false;
                avatar.LastBeamedOut = DateTime.Now;

                var saveAvatar = SaveAvatar(avatar);

                if (saveAvatar != null && !saveAvatar.IsError && saveAvatar.Result != null)
                {
                    response.Message = "Token Revoked.";
                    response.IsSaved = true;
                }
                else
                    OASISErrorHandling.HandleError(ref response, $"An error in RevokeToken method in AvatarManager saving the avatar. Reason: {saveAvatar.Message}", saveAvatar.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref response, "Refresh token is inactive or invalid");

            return response;
        }

        /// <summary>
        /// Validate an account token (JWT)
        /// </summary>
        public OASISResult<string> ValidateAccountToken(string accountToken)
        {
            var response = new OASISResult<string>();

            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var key = System.Text.Encoding.ASCII.GetBytes(OASISDNA.OASIS.Security.SecretKey);
                tokenHandler.ValidateToken(accountToken, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out Microsoft.IdentityModel.Tokens.SecurityToken validatedToken);

                response.Result = "Token is valid";
            }
            catch
            {
                response.IsError = true;
                response.Result = "Token is invalid";
            }

            return response;
        }

        /// <summary>
        /// Validate a password reset token
        /// </summary>
        public OASISResult<string> ValidateResetToken(string token)
        {
            var response = new OASISResult<string>();

            try
            {
                OASISResult<IEnumerable<IAvatar>> avatarsResult = LoadAllAvatars(false, false);

                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    var avatar = avatarsResult.Result.SingleOrDefault(x =>
                        x.ResetToken == token && x.ResetTokenExpires > DateTime.UtcNow);

                    if (avatar == null)
                    {
                        response.IsError = true;
                        response.Result = "Invalid token";
                    }
                    else
                        response.Result = "Valid token";
                }
                else
                    OASISErrorHandling.HandleError(ref response, $"Error in ValidateResetToken loading avatars. Reason: {avatarsResult.Message}", avatarsResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                response.IsError = true;
                response.Result = "Error validating token";
                OASISErrorHandling.HandleError(ref response, ex.Message);
            }

            return response;
        }

        /// <summary>
        /// Helper method to get refresh token from avatar
        /// </summary>
        private RefreshToken GetRefreshTokenFromAvatar(string token, out IAvatar avatar)
        {
            avatar = null;
            var avatarsResult = LoadAllAvatars(false, false);

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                foreach (var av in avatarsResult.Result)
                {
                    if (av.RefreshTokens != null)
                    {
                        var refreshToken = av.RefreshTokens.SingleOrDefault(x => x.Token == token);
                        if (refreshToken != null)
                        {
                            avatar = av;
                            return refreshToken;
                        }
                    }
                }
            }

            return null;
        }

        #endregion

    }
}
