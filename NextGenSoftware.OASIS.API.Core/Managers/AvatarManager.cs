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
        private static Dictionary<string, string> _avatarIdToProviderKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _avatarUsernameToProviderKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _avatarIdToProviderPrivateKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, Guid> _providerKeyToAvatarIdLookup = new Dictionary<string, Guid>();
        private static Dictionary<string, IAvatar> _providerKeyToAvatarLookup = new Dictionary<string, IAvatar>();

        public static IAvatar LoggedInAvatar { get; set; }
        private ProviderManagerConfig _config;

       // public OASISDNA OASISDNA { get; set; }
        
        public List<IOASISStorage> OASISStorageProviders { get; set; }
        
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
        public AvatarManager(IOASISStorage OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            
        }

        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        //public AvatarManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
        //{
            
        //}

        // TODO: Not sure if we want to move methods from the AvatarService in WebAPI here?
        // For integration with STAR and others like Unity can just call the REST API service?
        // What advantage is there to making it native through dll's? Would be slightly faster than having to make a HTTP request/response round trip...
        // BUT would STILL need to call out to a OASIS Storage Provider so depending if that was also running locally is how fast it would be...
        // For now just easier to call the REST API service from STAR... can come back to this later... :)
        public OASISResult<IAvatar> Authenticate(string username, string password, string ipAddress)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            result.Result = LoadAvatar(username);

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

                if (!result.Result.IsVerified && OASISDNA.OASIS.Security.DoesAvatarNeedToBeVerifiedBeforeLogin)
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
                var jwtToken = generateJwtToken(result.Result);
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
                    result.Result = RemoveAuthDetails(saveAvatarResult.Result);
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
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            result.Result = await LoadAvatarAsync(username);

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
                var jwtToken = generateJwtToken(result.Result);
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
                    result.Result = RemoveAuthDetails(saveAvatarResult.Result);
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
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
           // IEnumerable<IAvatar> avatars = LoadAllAvatars();

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

            //IAvatar avatar = new Avatar() { FirstName = firstName, LastName = lastName, Password = password, Title = avatarTitle, Email = email, AvatarType = new EnumValue<AvatarType>(avatarType), CreatedOASISType = new EnumValue<OASISType>(createdOASISType), STARCLIColour = cliColour, FavouriteColour = favColour };
            IAvatar avatar = new Avatar() { FirstName = firstName, LastName = lastName, Password = password, Title = avatarTitle, Email = email, AvatarType = new EnumValue<AvatarType>(avatarType), CreatedOASISType = new EnumValue<OASISType>(createdOASISType) };

            /*
            // TODO: Temp! Remove later!
            if (email == "davidellams@hotmail.com")
            {
                avatar.Karma = 777777;
                avatar.XP = 2222222;

                avatar.GeneKeys.Add(new GeneKey() { Name = "Expectation", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });
                avatar.GeneKeys.Add(new GeneKey() { Name = "Invisibility", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });
                avatar.GeneKeys.Add(new GeneKey() { Name = "Rapture", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });

                avatar.HumanDesign.Type = "Generator";
                avatar.Inventory.Add(new InventoryItem() { Name = "Magical Armour" });
                avatar.Inventory.Add(new InventoryItem() { Name = "Mighty Wizard Sword" });

                avatar.Spells.Add(new Spell() { Name = "Super Spell" });
                avatar.Spells.Add(new Spell() { Name = "Super Speed Spell" });
                avatar.Spells.Add(new Spell() { Name = "Super Srength Spell" });

                avatar.Achievements.Add(new Achievement() { Name = "Becoming Superman!" });
                avatar.Achievements.Add(new Achievement() { Name = "Completing STAR!" });

                avatar.Gifts.Add(new AvatarGift() { GiftType = KarmaTypePositive.BeASuperHero });

                avatar.Aura.Brightness = 99;
                avatar.Aura.Level = 77;
                avatar.Aura.Progress = 88;
                avatar.Aura.Size = 10;
                avatar.Aura.Value = 777;

                avatar.Chakras.Root.Level = 77;
                avatar.Chakras.Root.Progress = 99;
                avatar.Chakras.Root.XP = 8783;

                avatar.Attributes.Dexterity = 99;
                avatar.Attributes.Endurance = 99;
                avatar.Attributes.Intelligence = 99;
                avatar.Attributes.Magic = 99;
                avatar.Attributes.Speed = 99;
                avatar.Attributes.Strength = 99;
                avatar.Attributes.Toughness = 99;
                avatar.Attributes.Vitality = 99;
                avatar.Attributes.Wisdom = 99;

                avatar.Stats.Energy.Current = 99;
                avatar.Stats.Energy.Max = 99;
                avatar.Stats.HP.Current = 99;
                avatar.Stats.HP.Max = 99;
                avatar.Stats.Mana.Current = 99;
                avatar.Stats.Mana.Max = 99;
                avatar.Stats.Staminia.Current = 99;
                avatar.Stats.Staminia.Max = 99;

                avatar.SuperPowers.AstralProjection = 99;
                avatar.SuperPowers.BioLocatation = 88;
                avatar.SuperPowers.Flight = 99;
                avatar.SuperPowers.FreezeBreath = 88;
                avatar.SuperPowers.HeatVision = 99;
                avatar.SuperPowers.Invulerability = 99;
                avatar.SuperPowers.SuperSpeed = 99;
                avatar.SuperPowers.SuperStrength = 99;
                avatar.SuperPowers.XRayVision = 99;
                avatar.SuperPowers.Teleportation = 99;
                avatar.SuperPowers.Telekineseis = 99;

                avatar.Skills.Computers = 99;
                avatar.Skills.Engineering = 99;
            }*/

            // first registered account is an admin
            //var isFirstAccount = _context.Accounts.Count() == 0;

            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!

            //TODO: Not sure if this is a good idea or not? Currently you can register as a wizard (admin) or normal user.
            // The normal register screen will create user types but if logged in as a wizard, then they can create other wizards.
            //var isFirstAccount = avatars.Count() == 0;
            //avatar.AvatarType = isFirstAccount ? AvatarType.Wizard : AvatarType.User;

            avatar.CreatedDate = DateTime.UtcNow;
            avatar.VerificationToken = randomTokenString();

            // hash password
            avatar.Password = BC.HashPassword(password);

            // save account
            //  _context.Accounts.Add(account);
            // _context.SaveChanges();
            //AvatarManager.SaveAvatarAsync(avatar);

            //TODO: Get async version working ASAP! :)

            OASISResult<IAvatar> saveAvatarResult = SaveAvatar(avatar);
            avatar = saveAvatarResult.Result;

            if (!saveAvatarResult.IsError && saveAvatarResult.IsSaved)
            {
                sendVerificationEmail(avatar, origin);
                result.Result = RemoveAuthDetails(avatar);
                result.IsSaved = true;
            }
            else
            {
                result.Message = saveAvatarResult.Message;
                result.IsError = saveAvatarResult.IsError;
                result.IsSaved = saveAvatarResult.IsSaved;
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
            IEnumerable<IAvatar> avatars = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllAvatars();
            return avatars;
        }

        public IEnumerable<IAvatar> LoadAllAvatars(ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            IEnumerable<IAvatar> avatars = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllAvatars();

            foreach (IAvatar avatar in avatars)
                avatar.Password = null;

            return avatars;
        }

        public async Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync(ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            IEnumerable<IAvatar> avatars = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllAvatarsAsync().Result;

            foreach (IAvatar avatar in avatars)
                avatar.Password = null;

            return avatars;
        }

        public async Task<IAvatar> LoadAvatarAsync(string username, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            IAvatar avatar = await ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatarAsync(username);
            // avatar.Password = null;
            return avatar;
        }
        
        public async Task<IAvatar> LoadAvatarByUsernameAsync(string username, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            var avatar = await ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatarByUsernameAsync(username);
            return avatar;
        }

        public async Task<IAvatar> LoadAvatarByEmailAsync(string email, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            var avatar = await ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatarByEmailAsync(email);
            return avatar;
        }

        public async Task<IAvatar> LoadAvatarAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            IAvatar avatar = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.LoadAvatarAsync(id).Result;
            // avatar.Password = null;
            return avatar;
        }

        public async Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
        {
            //TODO: Need to handle return of OASISResult properly...
            var detail = await ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAvatarDetailAsync(id);
            return detail;
        }
        
        public async Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string email)
        {
            //TODO: Need to handle return of OASISResult properly...
            var detail = await ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAvatarDetailByEmailAsync(email);
            return detail;
        }
        
        public async Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string username)
        {
            //TODO: Need to handle return of OASISResult properly...
            var detail = await ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAvatarDetailByUsernameAsync(username);
            return detail;
        }

        public async Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailAsync()
        {
            //TODO: Need to handle return of OASISResult properly...
            var details = await ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAllAvatarDetailsAsync();
            return details;
        }

        public IAvatarDetail LoadAvatarDetail(Guid id)
        {
            //TODO: Need to handle return of OASISResult properly...
            var detail =  ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAvatarDetail(id);
            return detail;
        }

        public IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
        {
            //TODO: Need to handle return of OASISResult properly...
            var details = ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAllAvatarDetails();
            return details;
        }

        public async Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
        {
            //TODO: Need to handle return of OASISResult properly...
            var details = await ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default).Result.LoadAllAvatarDetailsAsync();
            return details;
        }

        public IAvatar LoadAvatar(Guid id, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            IAvatar avatar = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.LoadAvatar(id);
            //avatar.Password = null;
            return avatar;
        }

        public async Task<IAvatar> LoadAvatarAsync(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.LoadAvatarAsync(username, password).Result;
        }

        public IAvatar LoadAvatar(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.LoadAvatar(username, password);
        }

        //TODO: Replicate Auto-Fail over and Auto-Replication code for all Avatar, HolonManager methods etc...
        public IAvatar LoadAvatar(string username, ProviderType providerType = ProviderType.Default)
        {
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            IAvatar avatar = null;

            try
            {
                //TODO: Need to handle return of OASISResult properly...
                avatar = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.LoadAvatar(username);
            }
            catch (Exception ex)
            {
                avatar = null;
                LoggingManager.Log(string.Concat("Error loading avatar ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), LogType.Error);
            }

            if (avatar == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            //TODO: Need to handle return of OASISResult properly...
                            avatar = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.LoadAvatar(username);
                            needToChangeBack = true;

                            if (avatar != null)
                                break;
                        }
                        catch (Exception ex)
                        {
                            avatar = null;
                            LoggingManager.Log(string.Concat("Error loading avatar ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name, ". Error Message: ", ex.ToString()), LogType.Error);
                        }
                    }
                }
            }

            if (avatar == null)
            {
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            // Set the current provider back to the original provider.
            if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return avatar;
        }

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
        }

        public OASISResult<IAvatar> SaveAvatar(IAvatar avatar, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                int removingDays = OASISDNA.OASIS.Security.RemoveOldRefreshTokensAfterXDays;
                int removeQty = avatar.RefreshTokens.RemoveAll(token => (DateTime.Today - token.Created).TotalDays > removingDays);

                //TODO: Need to handle return of OASISResult properly...
                result.Result = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.SaveAvatar(PrepareAvatarForSaving(avatar));
                result.IsSaved = true;
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
                            //TODO: Need to handle return of OASISResult properly...
                            result.Result = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveAvatar(avatar);
                            result.IsSaved = true;

                        if (result.Result != null)
                                break;
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
            {
                result.IsError = true;
                result.Message = string.Concat("All Registered OASIS Providers In The AutoFailOverList Failed To Save The Avatar. Providers in list are ", ProviderManager.GetProviderAutoFailOverListAsString());
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
                            ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveAvatar(avatar);
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
                //TODO: Need to handle return of OASISResult properly...
                result.Result = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.SaveAvatarDetail(PrepareAvatarDetailForSaving(avatar));
                result.IsSaved = true;
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
                            //TODO: Need to handle return of OASISResult properly...
                            result.Result = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveAvatarDetail(avatar);
                            result.IsSaved = true;

                            if (result.Result != null)
                                break;
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
            {
                result.IsError = true;
                result.Message = string.Concat("All Registered OASIS Providers In The AutoFailOverList Failed To Save The AvatarDetail. Providers in list are ", ProviderManager.GetProviderAutoFailOverListAsString());
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
                            ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveAvatarDetail(avatar);
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
                //TODO: Need to handle return of OASISResult properly...
                result.Result = await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.SaveAvatarDetailAsync(PrepareAvatarDetailForSaving(avatar));
                result.IsSaved = true;
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
                            //TODO: Need to handle return of OASISResult properly...
                            result.Result = await ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveAvatarDetailAsync(avatar);
                            result.IsSaved = true;

                            if (result.Result != null)
                                break;
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
            {
                result.IsError = true;
                result.Message = string.Concat("All Registered OASIS Providers In The AutoFailOverList Failed To Save The AvatarDetail. Providers in list are ", ProviderManager.GetProviderAutoFailOverListAsString());
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
                            await ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveAvatarDetailAsync(avatar);
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
        public bool DeleteAvatar(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            return ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.DeleteAvatar(id, softDelete);
        }

        public async Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            return await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.DeleteAvatarAsync(id, softDelete);
        }
        
        public async Task<bool> DeleteAvatarByUsernameAsync(string userName, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            return await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.DeleteAvatarByUsernameAsync(userName, softDelete);
        }
        
        public async Task<bool> DeleteAvatarByEmailAsync(string email, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            return await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.DeleteAvatarByEmailAsync(email, softDelete);
        }

        public async Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(IAvatarDetail avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly..
            ////TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc....
            return await ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }
        public async Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(Guid avatarId, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            IAvatarDetail avatar = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatarDetail(avatarId);
            return await ProviderManager.CurrentStorageProvider.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatarDetail avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement Delete like HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            return new OASISResult<KarmaAkashicRecord>(ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.AddKarmaToAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink));
        }
        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(Guid avatarId, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();
            IAvatarDetail avatar = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatarDetail(avatarId);
            
            if (avatar != null)
                result.Result = ProviderManager.CurrentStorageProvider.AddKarmaToAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
            else
            {
                result.IsError = true;
                result.Message = "Avatar Not Found";
            }

            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...

            return result;
        }

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(IAvatarDetail avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            return await ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(Guid avatarId, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            IAvatarDetail avatar = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatarDetail(avatarId);
            return await ProviderManager.CurrentStorageProvider.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        public KarmaAkashicRecord RemoveKarmaFromAvatar(IAvatarDetail avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType provider = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.RemoveKarmaFromAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        public OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(Guid avatarId, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null, ProviderType provider = ProviderType.Default)
        {
            OASISResult<KarmaAkashicRecord> result = new OASISResult<KarmaAkashicRecord>();

            //TODO: Need to handle return of OASISResult properly...
            IAvatarDetail avatar = ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAvatarDetail(avatarId);

            if (avatar != null)
                result.Result = ProviderManager.CurrentStorageProvider.RemoveKarmaFromAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
            else
            {
                result.IsError = true;
                result.Message = "Avatar Not Found";
            }

            //TODO: Need to implement like avove and HolonManager does to include error handling, auto replication, auto failed over, logging, etc...

            return result;
        }

        // Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
        public IAvatarDetail LinkProviderKeyToAvatar(Guid avatarId, ProviderType providerTypeToLinkTo, string providerKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            IAvatarDetail avatar = ProviderManager.SetAndActivateCurrentStorageProvider(providerToLoadAvatarFrom).Result.LoadAvatarDetail(avatarId);
            avatar.ProviderKey[providerTypeToLinkTo] = providerKey;
            avatar = avatar.Save();
            return avatar;
        }

        // Private key for a public/private keypair.
        public IAvatarDetail LinkProviderPrivateKeyToAvatar(Guid avatarId, ProviderType providerTypeToLinkTo, string providerPrivateKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            //TODO: Need to handle return of OASISResult properly...
            IAvatarDetail avatar = ProviderManager.SetAndActivateCurrentStorageProvider(providerToLoadAvatarFrom).Result.LoadAvatarDetail(avatarId);
            avatar.ProviderPrivateKey[providerTypeToLinkTo] = StringCipher.Encrypt(providerPrivateKey);
            avatar = avatar.Save();
            return avatar;
        }

        public string GetProviderKeyForAvatar(Guid avatarId, ProviderType providerType)
        {
            string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (!_avatarIdToProviderKeyLookup.ContainsKey(key))
            {
                IAvatar avatar = LoadAvatar(avatarId);
                GetProviderKeyForAvatar(avatar, providerType, key, _avatarIdToProviderKeyLookup);
            }

            return _avatarIdToProviderKeyLookup[key];
        }

        public string GetProviderKeyForAvatar(string avatarUsername, ProviderType providerType)
        {
            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (!_avatarUsernameToProviderKeyLookup.ContainsKey(key))
            {
                IAvatar avatar = LoadAvatar(avatarUsername);
                GetProviderKeyForAvatar(avatar, providerType, key, _avatarUsernameToProviderKeyLookup);
            }

            return _avatarUsernameToProviderKeyLookup[key];
        }

        //TODO: COME BACK TO THIS! EVENTUALLY NEED TO MAKE ALL AVATAR FUNCTIONS ACCEPT EITHER AVATAR ID OR AVATAR USERNAME...
        private string GetProviderKeyForAvatar(IAvatar avatar, ProviderType providerType, string key, Dictionary<string, string> dictionaryCache)
        {
            if (avatar != null)
            {
                if (avatar.ProviderKey.ContainsKey(providerType))
                    dictionaryCache[key] = avatar.ProviderKey[providerType];
                else
                    throw new InvalidOperationException(string.Concat("The avatar with id ", avatar.Id, " and username ", avatar.Username, " was not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderKeyToAvatar method on the AvatarManager or avatar REST API."));
            }
            else
                throw new InvalidOperationException(string.Concat("The avatar with id ", avatar.Id, " and username ", avatar.Username, " was not found."));

            return dictionaryCache[key];
        }

        public string GetPrivateProviderKeyForAvatar(Guid avatarId, ProviderType providerType)
        {
            string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (LoggedInAvatar.Id != avatarId)
                throw new InvalidOperationException("You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

            if (!_avatarIdToProviderPrivateKeyLookup.ContainsKey(key))
            {
                IAvatarDetail avatar = LoadAvatarDetail(avatarId);

                if (avatar != null)
                {
                    if (avatar.ProviderPrivateKey.ContainsKey(providerType))
                        _avatarIdToProviderPrivateKeyLookup[key] = avatar.ProviderPrivateKey[providerType];
                    else
                        throw new InvalidOperationException(string.Concat("The avatar with id ", avatarId, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                    throw new InvalidOperationException(string.Concat("The avatar with id ", avatarId, " was not found."));
            }

            return StringCipher.Decrypt(_avatarIdToProviderPrivateKeyLookup[key]);
        }

        public Guid GetAvatarIdForProviderKey(string providerKey, ProviderType providerType)
        {
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerKeyToAvatarIdLookup.ContainsKey(key))
                _providerKeyToAvatarIdLookup[key] = GetAvatarForProviderKey(providerKey, providerType).Id;

            return _providerKeyToAvatarIdLookup[key];
        }

        //public string GetAvatarUsernameForProviderKey(string providerKey, ProviderType providerType)
        //{
        //    // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
        //    // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

        //    string key = string.Concat(Enum.GetName(providerType), providerKey);

        //    if (!_providerKeyToAvatarIdLookup.ContainsKey(key))
        //        _providerKeyToAvatarIdLookup[key] = GetAvatarForProviderKey(providerKey, providerType).Id;

        //    return _providerKeyToAvatarIdLookup[key];
        //}

        //TODO: Think will remove this if there is no good use case for it?
        public IAvatar GetAvatarForProviderKey(string providerKey, ProviderType providerType)
        {
            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerKeyToAvatarLookup.ContainsKey(key))
            {
                IAvatar avatar = LoadAllAvatars().FirstOrDefault(x => x.ProviderKey.ContainsKey(providerType) && x.ProviderKey[providerType] == providerKey);

                if (avatar != null)
                    _providerKeyToAvatarIdLookup[key] = avatar.Id;
                else
                    throw new InvalidOperationException(string.Concat("The provider Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderKeyToAvatar method on the AvatarManager or avatar REST API."));
            }

            return _providerKeyToAvatarLookup[key];
        }

        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderKeysForAvatar(Guid avatarId)
        {
            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();
            IAvatar avatar = LoadAvatar(avatarId);

            if (avatar != null)
                result.Result = avatar.ProviderKey;
            else
            {
                result.IsError = true;
                result.Message = string.Concat("No avatar was found for the id ", avatarId);
                //throw new InvalidOperationException(string.Concat("No avatar was found for the id ", avatarId));
                // NOTE: Would rather return OASISResult's rather than throw exceptions because less overhead (exceptions return a full stack trace).
                // TODO: Eventually need OASISResult's implemented for ALL OASIS functions (this includes replacing all exceptions where possible).
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, string>> GetAllPrivateProviderKeysForAvatar(Guid avatarId)
        {
            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();

            if (LoggedInAvatar.Id != avatarId)
            {
                result.IsError = true;
                result.Message = "ERROR: You can only retreive your own private keys, not another persons avatar.";
            }
            else
            {
                IAvatarDetail avatar = LoadAvatarDetail(avatarId);

                if (avatar != null)
                {
                    result.Result = avatar.ProviderPrivateKey;

                    // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                    foreach (ProviderType providerType in result.Result.Keys)
                        result.Result[providerType] = StringCipher.Decrypt(result.Result[providerType]);
                }
                else
                {
                    result.IsError = true;
                    result.Message = string.Concat("No avatar was found for the id ", avatarId);
                    //throw new InvalidOperationException(string.Concat("No avatar was found for the id ", avatarId));
                    // NOTE: Would rather return OASISResult's rather than throw exceptions because less overhead (exceptions return a full stack trace).
                    // TODO: Eventually need OASISResult's implemented for ALL OASIS functions (this includes replacing all exceptions where possible).
                }
            }

            return result;
        }

        public bool CheckIfEmailIsAlreadyInUse(string email)
        {
            return LoadAllAvatars().Any(x => x.Email == email);
        }

        private IAvatar PrepareAvatarForSaving(IAvatar avatar)
        {
            if (string.IsNullOrEmpty(avatar.Username))
                avatar.Username = avatar.Email;

            // TODO: I think it's best to include audit stuff here so the providers do not need to worry about it?
            // Providers could always override this behaviour if they choose...
            if (avatar.Id != Guid.Empty)
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

        private IAvatarDetail PrepareAvatarDetailForSaving(IAvatarDetail avatar)
        {
            if (string.IsNullOrEmpty(avatar.Username))
                avatar.Username = avatar.Email;

            // TODO: I think it's best to include audit stuff here so the providers do not need to worry about it?
            // Providers could always override this behaviour if they choose...
            if (avatar.Id != Guid.Empty)
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

        private string generateJwtToken(IAvatar account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(OASISDNA.OASIS.Security.Secret);
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

        private IAvatar RemoveAuthDetails(IAvatar avatar)
        {
            //  avatar.VerificationToken = null; //TODO: Put back in when LIVE!

            avatar.Password = null;
            // avatar.RefreshToken = null;
            //avatar.RefreshTokens = null;

            return avatar;
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
    }
}
