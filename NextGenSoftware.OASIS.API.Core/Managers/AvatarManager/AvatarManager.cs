using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        private static AvatarManager _instance = null;
        private ProviderManagerConfig _config;

        public static IAvatar LoggedInAvatar { get; set; }
        //public List<IOASISStorageProvider> OASISStorageProviders { get; set; }

        //TODO Implement this singleton pattern for other Managers...
        public static AvatarManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AvatarManager(ProviderManager.CurrentStorageProvider);

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
                            if (avatarsResult.Result.Any(x => x.ProviderWallets.ContainsKey(ProviderManager.CurrentStorageProviderType.Value)))
                                result.Result = avatarsResult.Result.FirstOrDefault(x => x.ProviderWallets[ProviderManager.CurrentStorageProviderType.Value].Any(x => x.PublicKey == username));
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
                            result.Message = "Avatar Successfully Authenticated.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured in Authenticate method in AvatarManager whilst saving the avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    }
                    else
                        result.Result = null;
                }
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
                            if (avatarsResult.Result.Any(x => x.ProviderWallets.ContainsKey(ProviderManager.CurrentStorageProviderType.Value)))
                                result.Result = avatarsResult.Result.FirstOrDefault(x => x.ProviderWallets[ProviderManager.CurrentStorageProviderType.Value].Any(x => x.PublicKey == username));
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

                        result.Result.RefreshTokens.Add(refreshToken);
                        result.Result.JwtToken = jwtToken;
                        result.Result.RefreshToken = refreshToken.Token;
                        result.Result.LastBeamedIn = DateTime.Now;
                        result.Result.IsBeamedIn = true;

                        LoggedInAvatar = result.Result;
                        OASISResult<IAvatar> saveAvatarResult = SaveAvatar(result.Result, autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, waitForAutoReplicationResult);

                        if (!saveAvatarResult.IsError && saveAvatarResult.IsSaved)
                        {
                            result.Result = HideAuthDetails(saveAvatarResult.Result);
                            result.IsSaved = true;
                            result.Message = "Avatar Successfully Authenticated.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured in AuthenticateAsync method in AvatarManager whilst saving the avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    }
                    else
                        result.Result = null;
                }
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
        public async Task<OASISResult<string>> ForgotPassword(string email)
        {
            var response = new OASISResult<string>();

            try
            {
                OASISResult<IAvatar> avatarResult = await LoadAvatarByEmailAsync(email, false, false);

                // always return ok response to prevent email enumeration
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error occured loading avatar in ForgotPassword, avatar not found. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
                    return response;
                }

                // create reset token that expires after 1 day
                avatarResult.Result.ResetToken = RandomTokenString();
                avatarResult.Result.ResetTokenExpires = DateTime.UtcNow.AddDays(24);

                var saveAvatar = SaveAvatar(avatarResult.Result);

                if (saveAvatar.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"An error occured saving the avatar in ForgotPassword method in AvatarService. Reason: {saveAvatar.Message}", saveAvatar.DetailedMessage);
                    return response;
                }

                // send email
                SendPasswordResetEmail(avatarResult.Result);
                response.Message = "Please check your email for password reset instructions";
            }
            catch (Exception e)
            {
                response.Exception = e;
                OASISErrorHandling.HandleError(ref response, $"An error occured in ForgotPassword method in AvatarService. Reason: {e.Message}");
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
            OASISResult<IAvatar> existingAvatarResult = LoadAvatarByEmail(email);

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
            OASISResult<IAvatar> existingAvatarResult = LoadAvatar(username);

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

        public IAvatar HideAuthDetails(IAvatar avatar, bool hidePassword = true, bool hidePrivateKeys = true, bool hideVerificationToken = true, bool hideRefreshTokens = true)
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
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
                errorMessage = string.Format(errorMessageTemplate, avatar.Name, avatar.Username, avatar.Id, ProviderManager.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.SaveAvatarDetailAsync(avatar);

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
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
                errorMessage = string.Format(errorMessageTemplate, id, ProviderManager.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

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
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
                errorMessage = string.Format(errorMessageTemplate, email, ProviderManager.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

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
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.DeleteAvatarByUsernameAsync(username, softDelete);
                    errorMessage = String.Format(errorMessageTemplate, username, ProviderManager.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

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
               OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
               errorMessage = string.Format(errorMessageTemplate, id, ProviderManager.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

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
               OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
               errorMessage = string.Format(errorMessageTemplate, email, ProviderManager.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

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
               OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

               if (!providerResult.IsError && providerResult.Result != null)
               {
                   var task = providerResult.Result.DeleteAvatarDetailByUsernameAsync(username, softDelete);
                   errorMessage = String.Format(errorMessageTemplate, username, ProviderManager.CurrentStorageProviderType.Name, Enum.GetName(typeof(SaveMode), saveMode));

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
