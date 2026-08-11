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
    public partial class AvatarManager : OASISManager
    {
        private const string LIVE_OASISSITE = "https://oportal.oasisomniverse.one";

        private string OASISWebSiteURL
        {
            get
            {
                if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.OASISWebSiteURL))
                    OASISDNA.OASIS.Email.OASISWebSiteURL = LIVE_OASISSITE;

                return OASISDNA.OASIS.Email.OASISWebSiteURL;
            }
        }

        private void SendPasswordResetEmail(IAvatar avatar, string returnUrl = null)
        {
            var baseUrl = !string.IsNullOrWhiteSpace(returnUrl) ? returnUrl.TrimEnd('/') : $"{OASISWebSiteURL}/avatar/reset-password";
            var resetUrl = $"{baseUrl}?token={avatar.ResetToken}";

            string message = $@"
                <!DOCTYPE html>
                <html>
                <body style='margin:0; padding:0; background-color:#000000; font-family:Arial, sans-serif;'>
                    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#000000; padding:40px 0;'>
                        <tr>
                            <td align='center'>
                                <table width='500' cellpadding='0' cellspacing='0' style='background-color:#1a1a1a; border-radius:12px; padding:40px;'>
                                    <tr>
                                        <td align='left' style='padding-bottom:24px;'>
                                            <img src='https://portal.oasisomniverse.one/assets/img/OASISLogoEmail.jpg' 
                                                 alt='OASIS' 
                                                 width='120'
                                                 style='display:block; background-color:#1a1a1a; border-radius:4px;' />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='color:#ffffff; font-size:22px; font-weight:bold; padding-bottom:16px;'>
                                            Reset Your Password
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='color:#ffffff; font-size:15px; padding-bottom:28px; line-height:1.6;'>
                                            We received a request to reset your password. Click the button below to choose a new one. 
                                            This link will be valid for <strong style='color:#ffffff;'>1 day</strong>.
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='padding-bottom:28px;'>
                                            <a href='{resetUrl}' 
                                               style='background-color:#ffffff; color:#000000; padding:14px 28px; 
                                                      border-radius:6px; text-decoration:none; font-weight:bold; font-size:15px;'>
                                                Reset Password
                                            </a>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='color:#ffffff; font-size:13px; padding-bottom:20px; line-height:1.6;'>
                                            Or copy and paste this link into your browser:<br/>
                                            <a href='{resetUrl}' style='color:#4a9eff;'>{resetUrl}</a>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='color:#ffffff; font-size:13px; padding-bottom:20px; line-height:1.6;'>
                                            If you didn't request a password reset you can safely ignore this email — your password will not be changed.
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='border-top:1px solid #333333; padding-top:20px; color:#ffffff; font-size:13px;'>
                                            OASIS · Our World
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";


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

            Task.Run(() => EmailManager.SendAsync(avatar.Email,
                "OASIS - Reset Password",
                $@"<h4>Reset Password</h4>
                         {message}")).GetAwaiter().GetResult();
        }

        //private void SendAlreadyRegisteredEmail(string email, string message)
        //{
        //    message = String.Concat($"<p>{message}</p>", $@"<p>If you don't know your password please visit the <a href=""{OASISWebSiteURL}/avatar/forgot-password"">forgot password</a> page.</p>");

        //    //if (!string.IsNullOrEmpty(origin))
        //    //    message = $@"<p>If you don't know your password please visit the <a href=""{origin}/avatar/forgot-password"">forgot password</a> page.</p>";
        //    //else
        //    //    message = "<p>If you don't know your password you can reset it via the <code>/avatar/forgot-password</code> api route.</p>";

        //    if (!EmailManager.IsInitialized)
        //        EmailManager.Initialize(OASISDNA);


        //    //EmailManager.Send(
        //    //    to: email,
        //    //    subject: "OASIS Sign-up Verification - Email Already Registered",
        //    //    html: $@"<h4>Email Already Registered</h4>{message}"
        //    //    //html: $@"<h4>Email Already Registered</h4>
        //    //    //         <p>Your email <strong>{email}</strong> is already registered.</p>
        //    //    //         {message}"
        //    //);

        //    Task.Run(() => EmailManager.SendAsync(
        //        to: email,
        //        subject: "OASIS Sign-up Verification - Email Already Registered",
        //        html: $@"<h4>Email Already Registered</h4>{message}")).GetAwaiter().GetResult();

        //    //html: $@"<h4>Email Already Registered</h4>
        //    //         <p>Your email <strong>{email}</strong> is already registered.</p>
        //    //         {message}"
        //    //);
        //}

        private void SendAlreadyRegisteredEmail(string email, string message)
        {
            string html = $@"
                <!DOCTYPE html>
                <html>
                <body style='margin:0; padding:0; background-color:#000000; font-family:Arial, sans-serif;'>
                    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#000000; padding:40px 0;'>
                        <tr>
                            <td align='center'>
                                <table width='500' cellpadding='0' cellspacing='0' style='background-color:#1a1a1a; border-radius:12px; padding:40px;'>
                                    <tr>
                                        <td align='left' style='padding-bottom:24px;'>
                                            <img src='https://portal.oasisomniverse.one/assets/img/OASISLogoEmail.jpg' 
                                                 alt='OASIS' 
                                                 width='120'
                                                 style='display:block; background-color:#1a1a1a; border-radius:4px;' />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='color:#ffffff; font-size:22px; font-weight:bold; padding-bottom:16px;'>
                                            Email Already Registered
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='color:#ffffff; font-size:15px; padding-bottom:28px; line-height:1.6;'>
                                            Sorry, the email <a href='mailto:{email}' style='color:#4a9eff;'>{email}</a> 
                                            is already in use, please use another one.
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='padding-bottom:28px;'>
                                            <a href='{OASISWebSiteURL}/avatar/forgot-password' 
                                               style='background-color:#ffffff; color:#000000; padding:14px 28px; 
                                                      border-radius:6px; text-decoration:none; font-weight:bold; font-size:15px;'>
                                                Forgot password?
                                            </a>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='border-top:1px solid #333333; padding-top:20px; color:#ffffff; font-size:13px;'>
                                            OASIS · Our World
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            //string html = $@"
            //<div style='background-color:#000000; padding:30px; text-align:center; font-family:Arial, sans-serif;'>
            //    <img src='{OASISWebSiteURL}/assets/img/OASISLogo.jpg' 
            //         alt='OASIS' 
            //         style='width:200px; margin-bottom:20px;' />

            //    <h4 style='color:#ffffff;'>Email Already Registered</h4>

            //    <p style='color:#ffffff;'>{message}</p>

            //    <p style='color:#ffffff;'>
            //        If you don't know your password please visit the 
            //        <a href='{OASISWebSiteURL}/avatar/forgot-password' style='color:#ffffff;'>
            //            forgot password
            //        </a> 
            //        page.
            //    </p>
            //</div>";

            //string html = $@"
            //<div style='background-color:#000000; padding:30px; text-align:center; font-family:Arial, sans-serif;'>
            //    <img src='data:image/jpeg;base64,{EmailManager.LogoBase64}' 
            //         alt='OASIS' 
            //         style='width:200px; margin-bottom:20px;' />

            //    <h4 style='color:#ffffff;'>Email Already Registered</h4>

            //    <p style='color:#ffffff;'>{message}</p>

            //    <p style='color:#ffffff;'>
            //        If you don't know your password please visit the 
            //        <a href='{OASISWebSiteURL}/avatar/forgot-password' style='color:#ffffff;'>
            //            forgot password
            //        </a> 
            //        page.
            //    </p>
            //</div>";

            if (!EmailManager.IsInitialized)
                EmailManager.Initialize(OASISDNA);

            Task.Run(() => EmailManager.SendAsync(
                to: email,
                subject: "OASIS Sign-up Verification - Email Already Registered",
                html: html
            )).GetAwaiter().GetResult();
        }

        private void SendVerificationEmail(IAvatar avatar)
        {
            var verifyUrl = $"{OASISWebSiteURL}/avatar/verify-email?token={avatar.VerificationToken}";

            string message = $@"
                <!DOCTYPE html>
                <html>
                <body style='margin:0; padding:0; background-color:#000000; font-family:Arial, sans-serif;'>
                    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#000000; padding:40px 0;'>
                        <tr>
                            <td align='center'>
                                <table width='500' cellpadding='0' cellspacing='0' style='background-color:#1a1a1a; border-radius:12px; padding:40px;'>
                                    <tr>
                                        <td align='left' style='padding-bottom:24px;'>
                                            <img src='https://portal.oasisomniverse.one/assets/img/OASISLogoEmail.jpg' 
                                                 alt='OASIS' 
                                                 width='120'
                                                 style='display:block; background-color:#1a1a1a; border-radius:4px;' />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='color:#ffffff; font-size:22px; font-weight:bold; padding-bottom:16px;'>
                                            Verify Your Email Address
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='color:#ffffff; font-size:15px; padding-bottom:28px; line-height:1.6;'>
                                            Thanks for signing up! Please click the button below to verify your email address and activate your account.
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='padding-bottom:28px;'>
                                            <a href='{verifyUrl}' 
                                               style='background-color:#ffffff; color:#000000; padding:14px 28px; 
                                                      border-radius:6px; text-decoration:none; font-weight:bold; font-size:15px;'>
                                                Verify Email
                                            </a>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='color:#ffffff; font-size:13px; padding-bottom:20px; line-height:1.6;'>
                                            Or copy and paste this link into your browser:<br/>
                                            <a href='{verifyUrl}' style='color:#4a9eff;'>{verifyUrl}</a>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='border-top:1px solid #333333; padding-top:20px; color:#ffffff; font-size:13px;'>
                                            OASIS · Our World
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            //var verifyUrl = $"{OASISWebSiteURL}/avatar/verify-email?token={avatar.VerificationToken}";
            //string message = $@"<p>Please click the below link to verify your email address:</p>
            //                 <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";

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

            //EmailManager.Send(
            //    to: avatar.Email,
            //    subject: "OASIS Sign-up Verification - Verify Email",
            //    //html: $@"<h4>Verify Email</h4>
            //    html: $@"<h4>Verify Email</h4>
            //             <p>Thanks for registering!</p>
            //             <p>Welcome to the OASIS!</p>
            //             <p>Ready Player One?</p>
            //             {message}"
            //);

            //Task.Run(() => EmailManager.SendAsync(
            //   to: avatar.Email,
            //   subject: "OASIS Sign-up Verification - Verify Email",
            //   //html: $@"<h4>Verify Email</h4>
            //   html: $@"<h4>Verify Email</h4>
            //             <p>Thanks for registering!</p>
            //             <p>Welcome to the OASIS!</p>
            //             <p>Ready Player One?</p>
            //             {message}")).GetAwaiter().GetResult();

            Task.Run(() => EmailManager.SendAsync(
              to: avatar.Email,
              subject: "OASIS Sign-up Verification - Verify Email",
              //html: $@"<h4>Verify Email</h4>
              html: message)).GetAwaiter().GetResult();
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
                OASISResult<IProviderWallet> walletResult = WalletManager.Instance.CreateWalletWithoutSaving(result.Result.Id, $"Default {provider.ProviderType.Name} Wallet", $"Default wallet for chain {provider.ProviderType.Name}", provider.ProviderType.Value, isDefaultWallet: true);

                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                {
                    if (!result.Result.ProviderWallets.ContainsKey(provider.ProviderType.Value) )
                        result.Result.ProviderWallets[provider.ProviderType.Value] = new List<IProviderWallet>();

                    if (result.Result.ProviderWallets[provider.ProviderType.Value] == null)
                        result.Result.ProviderWallets[provider.ProviderType.Value] = new List<IProviderWallet>();

                    result.Result.ProviderWallets[provider.ProviderType.Value].Add(walletResult.Result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured creating default wallet for provider/chain {walletResult.Message}");
            }

            //TODO: Fix this properly later! For some reason was causing an error in Azure cloud but seemed fine everywhere else including AWS etc! Its to do with not being able to save the wallets locally, which is not what we want on a server anyway! lol
            //OASISResult<bool> saveWalletsResult = await WalletManager.Instance.SaveProviderWalletsForAvatarByIdAsync(result.Result.Id, result.Result.ProviderWallets);
            
            //if (!(saveWalletsResult != null && saveWalletsResult.Result != null && !saveWalletsResult.IsError))
            //    OASISErrorHandling.HandleError(ref result, $"Error occured saving the default wallets. Reason: {saveWalletsResult.Message}");

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

    }
}
