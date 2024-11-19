using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static partial class STARCLI
    {
        public static string GetValidEmail(string message, bool checkIfEmailAlreadyInUse, ProviderType providerType = ProviderType.Default)
        {
            bool emailValid = false;
            string email = "";

            while (!emailValid)
            {
                CLIEngine.ShowMessage(string.Concat("", message), true, true);
                email = Console.ReadLine();

                if (!ValidationHelper.IsValidEmail(email))
                    CLIEngine.ShowErrorMessage("That email is not valid. Please try again.");

                else if (checkIfEmailAlreadyInUse)
                {
                    CLIEngine.ShowWorkingMessage("Checking if email already in use...");
                    CLIEngine.SupressConsoleLogging = true;

                    OASISResult<bool> checkIfEmailAlreadyInUseResult = STAR.OASISAPI.Avatar.CheckIfEmailIsAlreadyInUse(email);
                    CLIEngine.SupressConsoleLogging = false;

                    //if (!checkIfEmailAlreadyInUseResult.Result)
                    //{
                    //    emailValid = true;
                    //    CLIEngine.Spinner.Stop();
                    //    CLIEngine.ShowMessage("", false);
                    //}

                    //No need to show error message because the CheckIfEmailIsAlreadyInUse function already shows this! ;-)
                    if (checkIfEmailAlreadyInUseResult.Result)
                        CLIEngine.ShowErrorMessage(checkIfEmailAlreadyInUseResult.Message);
                    else
                    {
                        emailValid = true;
                        CLIEngine.Spinner.Stop();
                        CLIEngine.ShowMessage("", false);
                    }
                }
                else
                    emailValid = true;
            }

            return email;
        }

        public static string GetValidUsername(string message, bool checkIfUsernameAlreadyInUse = true, ProviderType providerType = ProviderType.Default)
        {
            bool usernameValid = false;
            string username = "";

            while (!usernameValid)
            {
                CLIEngine.ShowMessage(string.Concat("", message), true, true);
                username = Console.ReadLine();

                if (checkIfUsernameAlreadyInUse)
                {
                    CLIEngine.ShowWorkingMessage("Checking if username already in use...");
                    CLIEngine.SupressConsoleLogging = true;

                    OASISResult<bool> checkIfUsernameAlreadyInUseResult = STAR.OASISAPI.Avatar.CheckIfUsernameIsAlreadyInUse(username);
                    CLIEngine.SupressConsoleLogging = false;

                    //if (!checkIfUsernameAlreadyInUseResult.Result)
                    //{
                    //    usernameValid = true;
                    //    CLIEngine.Spinner.Stop();
                    //    CLIEngine.ShowMessage("", false);
                    //}

                    //No need to show error message because the CheckIfUsernameIsAlreadyInUse function already shows this! ;-)
                    if (checkIfUsernameAlreadyInUseResult.Result)
                        CLIEngine.ShowErrorMessage(checkIfUsernameAlreadyInUseResult.Message);
                    else
                    {
                        usernameValid = true;
                        CLIEngine.Spinner.Stop();
                        CLIEngine.ShowMessage("", false);
                    }
                }
                else
                    usernameValid = true;
            }

            return username;
        }

        public static bool CreateAvatar(ProviderType providerType = ProviderType.Default)
        {
            ConsoleColor favColour = ConsoleColor.Green;
            ConsoleColor cliColour = ConsoleColor.Green;

            CLIEngine.ShowMessage("");
            CLIEngine.ShowMessage("Please create an avatar below:", false);

            string title = CLIEngine.GetValidTitle("What is your title? ");
            string firstName = CLIEngine.GetValidInput("What is your first name? ");
            CLIEngine.ShowMessage(string.Concat("Nice to meet you ", firstName, ". :)"));
            string lastName = CLIEngine.GetValidInput(string.Concat("What is your last name ", firstName, "? "));
            string email = GetValidEmail("What is your email address? ", true);
            string username = GetValidUsername("What username would you like? ", true);
            CLIEngine.GetValidColour(ref favColour, ref cliColour);
            string password = CLIEngine.GetValidPassword();
            CLIEngine.ShowWorkingMessage("Creating Avatar...");

            CLIEngine.SupressConsoleLogging = true;
            OASISResult<IAvatar> createAvatarResult = Task.Run(async () => await STAR.CreateAvatarAsync(title, firstName, lastName, email, username, password, cliColour, favColour)).Result;
            //OASISResult<IAvatar> createAvatarResult = STAR.CreateAvatar(title, firstName, lastName, email, username, password, cliColour, favColour);
            CLIEngine.SupressConsoleLogging = false;
            CLIEngine.ShowMessage("");

            if (createAvatarResult.IsError)
            {
                CLIEngine.ShowErrorMessage(string.Concat("Error creating avatar. Error message: ", createAvatarResult.Message));
                return false;
            }
            else
            {
                CLIEngine.ShowSuccessMessage("Successfully Created Avatar. Please Check Your Email To Verify Your Account Before Logging In.");
                return true;
            }
        }

        public static async Task BeamInAvatar(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> beamInResult = null;

            while (beamInResult == null || (beamInResult != null && beamInResult.IsError))
            {
                if (!CLIEngine.GetConfirmation("Do you have an existing avatar? "))
                    CreateAvatar();
                else
                    CLIEngine.ShowMessage("", false);

                CLIEngine.ShowMessage("Please login below:");
                //string username = GetValidEmail("Username? ", false);
                string username = GetValidUsername("Username? ", false);
                string password = CLIEngine.ReadPassword("Password? ");
                CLIEngine.ShowWorkingMessage("Beaming In...");

                CLIEngine.SupressConsoleLogging = true;
                beamInResult = Task.Run(async () => await STAR.BeamInAsync(username, password)).Result;
                CLIEngine.SupressConsoleLogging = false;

                //CLIEngine.ShowWorkingMessage("Beaming In...");
                //beamInResult = Task.Run(async () => await STAR.BeamInAsync("davidellams@hotmail.com", "my-super-secret-password")).Result;
                //beamInResult = Task.Run(async () => await STAR.BeamInAsync("davidellams@hotmail.com", "new-super-secret-password")).Result;
                //beamInResult = Task.Run(async () => await STAR.BeamInAsync("davidellams@hotmail.com", "test!")).Result;

                //beamInResult = STAR.BeamIn("davidellams@hotmail.com", "my-super-secret-password");
                //beamInResult = STAR.BeamIn("davidellams@hotmail.com", "test!");
                //beamInResult = STAR.BeamIn("davidellams@gmail.com", "test!");

                CLIEngine.ShowMessage("");

                if (beamInResult.IsError)
                {
                    CLIEngine.ShowErrorMessage(string.Concat("Error Beaming in. Error Message: ", beamInResult.Message));

                    if (beamInResult.Message == "Avatar has not been verified. Please check your email.")
                    {
                        CLIEngine.ShowErrorMessage("Then either click the link in the email to activate your avatar or enter the validation token contained in the email below:", false);

                        bool validToken = false;
                        while (!validToken)
                        {
                            string token = CLIEngine.GetValidInput("Enter validation token: ");
                            CLIEngine.ShowWorkingMessage("Verifying Token...");
                            OASISResult<bool> verifyEmailResult = STAR.OASISAPI.Avatar.VerifyEmail(token);

                            if (verifyEmailResult.IsError)
                                CLIEngine.ShowErrorMessage(verifyEmailResult.Message);
                            else
                            {
                                CLIEngine.ShowSuccessMessage("Verification successful, you can now login");
                                validToken = true;
                            }
                        }
                    }
                }

                else if (STAR.BeamedInAvatar == null)
                    CLIEngine.ShowErrorMessage("Error Beaming In. Username/Password may be incorrect.");
            }

            CLIEngine.ShowSuccessMessage(string.Concat("Successfully Beamed In! Welcome back ", STAR.BeamedInAvatar.Username, ". Have a nice day! :) You Are Level ", STAR.BeamedInAvatarDetail.Level, " And Have ", STAR.BeamedInAvatarDetail.Karma, " Karma."));
            //CLIEngine.ShowSuccessMessage(string.Concat("Successfully Beamed In! Welcome back dellams. Have a nice day! :) You Are Level 77 And Have 777 Karma."));
            //ShowAvatarStats();
            //await ReadyPlayerOne();
        }

        public static void ShowAvatarStats()
        {
            ShowAvatarStats(STAR.BeamedInAvatar, STAR.BeamedInAvatarDetail);
        }

        public static void ShowAvatarStats(IAvatar avatar, IAvatarDetail avatarDetail)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            CLIEngine.ShowMessage("", false);
            CLIEngine.ShowMessage($"Avatar {avatar.Username} Beamed In On {avatar.LastBeamedIn} And Last Beamed Out On {avatar.LastBeamedOut}.");
            Console.WriteLine("");
            Console.WriteLine(string.Concat(" Name: ", avatar.FullName));
            Console.WriteLine(string.Concat(" Created: ", avatar.CreatedDate));
            Console.WriteLine(string.Concat(" Karma: ", avatarDetail.Karma));
            Console.WriteLine(string.Concat(" Level: ", avatarDetail.Level));
            Console.WriteLine(string.Concat(" XP: ", avatarDetail.XP));

            Console.WriteLine("");
            Console.WriteLine(" Chakras:");
            Console.WriteLine(string.Concat(" Crown XP: ", avatarDetail.Chakras.Crown.XP));
            Console.WriteLine(string.Concat(" Crown Level: ", avatarDetail.Chakras.Crown.Level));
            Console.WriteLine(string.Concat(" ThirdEye XP: ", avatarDetail.Chakras.ThirdEye.XP));
            Console.WriteLine(string.Concat(" ThirdEye Level: ", avatarDetail.Chakras.ThirdEye.Level));
            Console.WriteLine(string.Concat(" Throat XP: ", avatarDetail.Chakras.Throat.XP));
            Console.WriteLine(string.Concat(" Throat Level: ", avatarDetail.Chakras.Throat.Level));
            Console.WriteLine(string.Concat(" Heart XP: ", avatarDetail.Chakras.Heart.XP));
            Console.WriteLine(string.Concat(" Heart Level: ", avatarDetail.Chakras.Heart.Level));
            Console.WriteLine(string.Concat(" SoloarPlexus XP: ", avatarDetail.Chakras.SoloarPlexus.XP));
            Console.WriteLine(string.Concat(" SoloarPlexus Level: ", avatarDetail.Chakras.SoloarPlexus.Level));
            Console.WriteLine(string.Concat(" Sacral XP: ", avatarDetail.Chakras.Sacral.XP));
            Console.WriteLine(string.Concat(" Sacral Level: ", avatarDetail.Chakras.Sacral.Level));

            Console.WriteLine(string.Concat(" Root SanskritName: ", avatarDetail.Chakras.Root.SanskritName));
            Console.WriteLine(string.Concat(" Root XP: ", avatarDetail.Chakras.Root.XP));
            Console.WriteLine(string.Concat(" Root Level: ", avatarDetail.Chakras.Root.Level));
            Console.WriteLine(string.Concat(" Root Progress: ", avatarDetail.Chakras.Root.Progress));
            // Console.WriteLine(string.Concat(" Root Color: ", Superavatar.Chakras.Root.Color.Name));
            Console.WriteLine(string.Concat(" Root Element: ", avatarDetail.Chakras.Root.Element.Name));
            Console.WriteLine(string.Concat(" Root YogaPose: ", avatarDetail.Chakras.Root.YogaPose.Name));
            Console.WriteLine(string.Concat(" Root WhatItControls: ", avatarDetail.Chakras.Root.WhatItControls));
            Console.WriteLine(string.Concat(" Root WhenItDevelops: ", avatarDetail.Chakras.Root.WhenItDevelops));
            Console.WriteLine(string.Concat(" Root Crystal Name: ", avatarDetail.Chakras.Root.Crystal.Name.Name));
            Console.WriteLine(string.Concat(" Root Crystal AmplifyicationLevel: ", avatarDetail.Chakras.Root.Crystal.AmplifyicationLevel));
            Console.WriteLine(string.Concat(" Root Crystal CleansingLevel: ", avatarDetail.Chakras.Root.Crystal.CleansingLevel));
            Console.WriteLine(string.Concat(" Root Crystal EnergisingLevel: ", avatarDetail.Chakras.Root.Crystal.EnergisingLevel));
            Console.WriteLine(string.Concat(" Root Crystal GroundingLevel: ", avatarDetail.Chakras.Root.Crystal.GroundingLevel));
            Console.WriteLine(string.Concat(" Root Crystal ProtectionLevel: ", avatarDetail.Chakras.Root.Crystal.ProtectionLevel));

            Console.WriteLine("");
            Console.WriteLine(" Aurua:");
            Console.WriteLine(string.Concat(" Brightness: ", avatarDetail.Aura.Brightness));
            Console.WriteLine(string.Concat(" Size: ", avatarDetail.Aura.Size));
            Console.WriteLine(string.Concat(" Level: ", avatarDetail.Aura.Level));
            Console.WriteLine(string.Concat(" Value: ", avatarDetail.Aura.Value));
            Console.WriteLine(string.Concat(" Progress: ", avatarDetail.Aura.Progress));
            Console.WriteLine(string.Concat(" ColourRed: ", avatarDetail.Aura.ColourRed));
            Console.WriteLine(string.Concat(" ColourGreen: ", avatarDetail.Aura.ColourGreen));
            Console.WriteLine(string.Concat(" ColourBlue: ", avatarDetail.Aura.ColourBlue));

            Console.WriteLine("");
            Console.WriteLine(" Attributes:");
            Console.WriteLine(string.Concat(" Strength: ", avatarDetail.Attributes.Strength));
            Console.WriteLine(string.Concat(" Speed: ", avatarDetail.Attributes.Speed));
            Console.WriteLine(string.Concat(" Dexterity: ", avatarDetail.Attributes.Dexterity));
            Console.WriteLine(string.Concat(" Intelligence: ", avatarDetail.Attributes.Intelligence));
            Console.WriteLine(string.Concat(" Magic: ", avatarDetail.Attributes.Magic));
            Console.WriteLine(string.Concat(" Wisdom: ", avatarDetail.Attributes.Wisdom));
            Console.WriteLine(string.Concat(" Toughness: ", avatarDetail.Attributes.Toughness));
            Console.WriteLine(string.Concat(" Vitality: ", avatarDetail.Attributes.Vitality));
            Console.WriteLine(string.Concat(" Endurance: ", avatarDetail.Attributes.Endurance));

            Console.WriteLine("");
            Console.WriteLine(" Stats:");
            Console.WriteLine(string.Concat(" HP: ", avatarDetail.Stats.HP.Current, "/", avatarDetail.Stats.HP.Max));
            Console.WriteLine(string.Concat(" Mana: ", avatarDetail.Stats.Mana.Current, "/", avatarDetail.Stats.Mana.Max));
            Console.WriteLine(string.Concat(" Energy: ", avatarDetail.Stats.Energy.Current, "/", avatarDetail.Stats.Energy.Max));
            Console.WriteLine(string.Concat(" Staminia: ", avatarDetail.Stats.Staminia.Current, "/", avatarDetail.Stats.Staminia.Max));

            Console.WriteLine("");
            Console.WriteLine(" Super Powers:");
            Console.WriteLine(string.Concat(" Flight: ", avatarDetail.SuperPowers.Flight));
            Console.WriteLine(string.Concat(" Astral Projection: ", avatarDetail.SuperPowers.AstralProjection));
            Console.WriteLine(string.Concat(" Bio-Locatation: ", avatarDetail.SuperPowers.BioLocatation));
            Console.WriteLine(string.Concat(" Heat Vision: ", avatarDetail.SuperPowers.HeatVision));
            Console.WriteLine(string.Concat(" Invulerability: ", avatarDetail.SuperPowers.Invulerability));
            Console.WriteLine(string.Concat(" Remote Viewing: ", avatarDetail.SuperPowers.RemoteViewing));
            Console.WriteLine(string.Concat(" Super Speed: ", avatarDetail.SuperPowers.SuperSpeed));
            Console.WriteLine(string.Concat(" Super Strength: ", avatarDetail.SuperPowers.SuperStrength));
            Console.WriteLine(string.Concat(" Telekineseis: ", avatarDetail.SuperPowers.Telekineseis));
            Console.WriteLine(string.Concat(" XRay Vision: ", avatarDetail.SuperPowers.XRayVision));

            Console.WriteLine("");
            Console.WriteLine(" Skills:");
            Console.WriteLine(string.Concat(" Computers: ", avatarDetail.Skills.Computers));
            Console.WriteLine(string.Concat(" Engineering: ", avatarDetail.Skills.Engineering));
            Console.WriteLine(string.Concat(" Farming: ", avatarDetail.Skills.Farming));
            Console.WriteLine(string.Concat(" FireStarting: ", avatarDetail.Skills.FireStarting));
            Console.WriteLine(string.Concat(" Fishing: ", avatarDetail.Skills.Fishing));
            Console.WriteLine(string.Concat(" Languages: ", avatarDetail.Skills.Languages));
            Console.WriteLine(string.Concat(" Meditation: ", avatarDetail.Skills.Meditation));
            Console.WriteLine(string.Concat(" MelleeCombat: ", avatarDetail.Skills.MelleeCombat));
            Console.WriteLine(string.Concat(" Mindfulness: ", avatarDetail.Skills.Mindfulness));
            Console.WriteLine(string.Concat(" Negotiating: ", avatarDetail.Skills.Negotiating));
            Console.WriteLine(string.Concat(" RangeCombat: ", avatarDetail.Skills.RangeCombat));
            Console.WriteLine(string.Concat(" Research: ", avatarDetail.Skills.Research));
            Console.WriteLine(string.Concat(" Science: ", avatarDetail.Skills.Science));
            Console.WriteLine(string.Concat(" SpellCasting: ", avatarDetail.Skills.SpellCasting));
            Console.WriteLine(string.Concat(" Translating: ", avatarDetail.Skills.Translating));
            Console.WriteLine(string.Concat(" Yoga: ", avatarDetail.Skills.Yoga));

            Console.WriteLine("");
            Console.WriteLine(" Gifts:");

            foreach (AvatarGift gift in avatarDetail.Gifts)
                Console.WriteLine(string.Concat(" ", Enum.GetName(gift.GiftType), " earnt on ", gift.GiftEarnt.ToString()));

            Console.WriteLine("");
            Console.WriteLine(" Spells:");

            foreach (Spell spell in avatarDetail.Spells)
                Console.WriteLine(string.Concat(" ", spell.Name));

            Console.WriteLine("");
            Console.WriteLine(" Inventory:");

            foreach (InventoryItem inventoryItem in avatarDetail.Inventory)
                Console.WriteLine(string.Concat(" ", inventoryItem.Name));

            Console.WriteLine("");
            Console.WriteLine(" Achievements:");

            foreach (Achievement achievement in avatarDetail.Achievements)
                Console.WriteLine(string.Concat(" ", achievement.Name));

            Console.WriteLine("");
            Console.WriteLine(" Gene Keys:");

            foreach (GeneKey geneKey in avatarDetail.GeneKeys)
                Console.WriteLine(string.Concat(" ", geneKey.Name));

            Console.WriteLine("");
            Console.WriteLine(" Human Design:");
            Console.WriteLine(string.Concat(" Type: ", avatarDetail.HumanDesign.Type));
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        //public static void EnableOrDisableAutoProviderList(Func<bool, List<ProviderType>, bool> funct, bool isEnabled, List<ProviderType> providerTypes, string workingMessage, string successMessage, string errorMessage)
        //{
        //    CLIEngine.ShowWorkingMessage(workingMessage);

        //    if (funct(isEnabled, providerTypes))
        //        CLIEngine.ShowSuccessMessage(successMessage);
        //    else
        //        CLIEngine.ShowErrorMessage(errorMessage);
        //}

        public static void ShowAvatar(IAvatar avatar, IAvatarDetail avatarDetail)
        {
            if (avatar != null)
            {
                //CLIEngine.ShowSuccessMessage("Avatar Loaded Successfully");
                CLIEngine.ShowMessage($"Avatar ID: {avatar.Id}");
                CLIEngine.ShowMessage($"Avatar Name: {avatar.FullName}");
                CLIEngine.ShowMessage($"Avatar Username: {avatar.Username}");
                CLIEngine.ShowMessage($"Avatar Type: {avatar.AvatarType.Name}");
                CLIEngine.ShowMessage($"Avatar Created Date: {avatar.CreatedDate}");
                CLIEngine.ShowMessage($"Avatar Modifed Date: {avatar.ModifiedDate}");
                CLIEngine.ShowMessage($"Avatar Last Beamed In Date: {avatar.LastBeamedIn}");
                CLIEngine.ShowMessage($"Avatar Last Beamed Out Date: {avatar.LastBeamedOut}");
                CLIEngine.ShowMessage(String.Concat("Avatar Is Active: ", avatar.IsActive ? "True" : "False"));
                CLIEngine.ShowMessage(String.Concat("Avatar Is Beamed In: ", avatar.IsBeamedIn ? "True" : "False"));
                CLIEngine.ShowMessage(String.Concat("Avatar Is Verified: ", avatar.IsVerified ? "True" : "False"));
                CLIEngine.ShowMessage($"Avatar Version: {avatar.Version}");

                if (CLIEngine.GetConfirmation($"Do you wish to view more detailed information?"))
                    ShowAvatarStats(avatar, avatarDetail);
            }
            else
                CLIEngine.ShowErrorMessage("Error Loading Avatar.");
        }

        public static async Task ShowAvatar(Guid id = new Guid())
        {
            if (id == Guid.Empty)
                id = CLIEngine.GetValidInputForGuid("What is the ID/GUID for the avatar you wish to view?");

            OASISResult<IAvatar> avatarResult = await STAR.OASISAPI.Avatar.LoadAvatarAsync(id);

            if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
            {
                OASISResult<IAvatarDetail> avatarDetailResult = await STAR.OASISAPI.Avatar.LoadAvatarDetailAsync(id);

                if (avatarDetailResult != null && !avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    STARCLI.ShowAvatar(avatarResult.Result, avatarDetailResult.Result);
                else
                    CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar Detail: {avatarDetailResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar: {avatarResult.Message}");
        }

        public static async Task ShowAvatar(string idOrUsername)
        {
            Guid id = Guid.Empty;

            if (string.IsNullOrEmpty(idOrUsername))
                idOrUsername = CLIEngine.GetValidInput("What is the username or ID/GUID for the avatar you wish to view?");

            if (Guid.TryParse(idOrUsername, out id))
                await ShowAvatar(id);
            else
            {
                OASISResult<IAvatar> avatarResult = await STAR.OASISAPI.Avatar.LoadAvatarAsync(idOrUsername);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    OASISResult<IAvatarDetail> avatarDetailResult = await STAR.OASISAPI.Avatar.LoadAvatarDetailByUsernameAsync(idOrUsername);

                    if (avatarDetailResult != null && !avatarDetailResult.IsError && avatarDetailResult.Result != null)
                        STARCLI.ShowAvatar(avatarResult.Result, avatarDetailResult.Result);
                    else
                        CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar Detail: {avatarDetailResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar: {avatarResult.Message}");
            }
        }

        public static async Task ShowAvatar()
        {
            await ShowAvatar("");
        }
    }
}

