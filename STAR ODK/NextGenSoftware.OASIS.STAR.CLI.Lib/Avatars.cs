using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class Avatars
    {
        public Avatars(Guid avatarId)
        {

        }
        public string GetValidNewEmail(string message, bool checkIfEmailAlreadyInUse, ProviderType providerType = ProviderType.Default)
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

                    OASISResult<bool> checkIfEmailAlreadyInUseResult = STAR.OASISAPI.Avatars.CheckIfEmailIsAlreadyInUse(email);
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

        public string GetValidExistingEmail(string message, ProviderType providerType = ProviderType.Default)
        {
            bool emailValid = false;
            string email = "";

            while (!emailValid)
            {
                CLIEngine.ShowMessage(string.Concat("", message), true, true);
                email = Console.ReadLine();

                if (!ValidationHelper.IsValidEmail(email))
                    CLIEngine.ShowErrorMessage("That email is not valid. Please try again.");


                CLIEngine.ShowWorkingMessage("Checking if email already in use...");
                CLIEngine.SupressConsoleLogging = true;

                OASISResult<bool> checkIfEmailAlreadyInUseResult = STAR.OASISAPI.Avatars.CheckIfEmailIsAlreadyInUse(email);
                CLIEngine.SupressConsoleLogging = false;

                if (checkIfEmailAlreadyInUseResult.Result)
                {
                    emailValid = true;
                    CLIEngine.Spinner.Stop();
                    CLIEngine.ShowMessage("", false);
                }
                else
                    CLIEngine.ShowErrorMessage("Email Not Found");
            }

            return email;
        }

        public string GetValidUsername(string message, bool checkIfUsernameAlreadyInUse = true, ProviderType providerType = ProviderType.Default)
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

                    OASISResult<bool> checkIfUsernameAlreadyInUseResult = STAR.OASISAPI.Avatars.CheckIfUsernameIsAlreadyInUse(username);
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

        public bool CreateAvatar(ProviderType providerType = ProviderType.Default)
        {
            ConsoleColor favColour = ConsoleColor.Green;
            ConsoleColor cliColour = ConsoleColor.Green;

            CLIEngine.ShowMessage("");
            CLIEngine.ShowMessage("Please create an avatar below:", false);

            string title = CLIEngine.GetValidTitle("What is your title? ");
            string firstName = CLIEngine.GetValidInput("What is your first name? ");
            CLIEngine.ShowMessage(string.Concat("Nice to meet you ", firstName, ". :)"));
            string lastName = CLIEngine.GetValidInput(string.Concat("What is your last name ", firstName, "? "));
            string email = GetValidNewEmail("What is your email address? ", true);
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

        public async Task BeamInAvatar(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> beamInResult = null;

            while (beamInResult == null || (beamInResult != null && beamInResult.IsError))
            {
                if (!CLIEngine.GetConfirmation("Do you have an existing avatar? "))
                    CreateAvatar();
                else
                    CLIEngine.ShowMessage("", false);

                CLIEngine.ShowMessage("Please beam in below:");
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
                            OASISResult<bool> verifyEmailResult = STAR.OASISAPI.Avatars.VerifyEmail(token);

                            if (verifyEmailResult.IsError)
                                CLIEngine.ShowErrorMessage(verifyEmailResult.Message);
                            else
                            {
                                CLIEngine.ShowSuccessMessage("Verification successful, you can now beam in.");
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

        public void ShowAvatarStats()
        {
            ShowAvatarStats(STAR.BeamedInAvatar, STAR.BeamedInAvatarDetail);
        }

        public void ShowAvatarStats(IAvatar avatar, IAvatarDetail avatarDetail)
        {
            if (avatar.Email == "davidellams@hotmail.com")
            {
                avatarDetail.Chakras.Crown.XP = 874;
                avatarDetail.Chakras.Crown.Level = 8;
                avatarDetail.Chakras.ThirdEye.XP = 717;
                avatarDetail.Chakras.ThirdEye.Level = 7;
                avatarDetail.Chakras.Throat.XP = 515;
                avatarDetail.Chakras.Throat.Level = 5;
                avatarDetail.Chakras.Heart.XP = 777;
                avatarDetail.Chakras.Heart.Level = 7;
                avatarDetail.Chakras.SolarPlexus.Level = 9;
                avatarDetail.Chakras.SolarPlexus.XP = 896;
                avatarDetail.Chakras.Sacral.Level = 7;
                avatarDetail.Chakras.Sacral.XP = 707;
                avatarDetail.Chakras.Root.Level = 5;
                avatarDetail.Chakras.Root.XP = 555;

                avatarDetail.Aura.ColourGreen = 77;
                avatarDetail.Aura.ColourRed = 77;
                avatarDetail.Aura.ColourBlue = 77;

                avatarDetail.Attributes.Dexterity = 77;
                avatarDetail.Attributes.Strength = 2222;
                avatarDetail.Attributes.Vitality = 111;
                avatarDetail.Attributes.Wisdom = 888;
                avatarDetail.Attributes.Toughness = 777;
                avatarDetail.Attributes.Endurance = 99;
                avatarDetail.Attributes.Intelligence = 999;
                avatarDetail.Attributes.Magic = 777;
                avatarDetail.Attributes.Speed = 555;

                avatarDetail.Stats.Energy.Current = 77;
                avatarDetail.Stats.Energy.Max = 99;
                avatarDetail.Stats.HP.Current = 88;
                avatarDetail.Stats.HP.Max = 99;
                avatarDetail.Stats.Mana.Current = 77;
                avatarDetail.Stats.Mana.Max = 99;
                avatarDetail.Stats.Stamina.Current = 55;
                avatarDetail.Stats.Stamina.Max = 99;

                avatarDetail.Skills.Yoga = 77;
                avatarDetail.Skills.Science = 88;
                avatarDetail.Skills.Research = 77;
                avatarDetail.Skills.Mindfulness = 55;
                avatarDetail.Skills.Meditation = 55;
                avatarDetail.Skills.FireStarting = 77;


                //avatarDetail.Karma = 777777;
                //avatarDetail.XP = 2222222;

                //avatarDetail.GeneKeys.Add(new GeneKey() { Name = "Expectation", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });
                //avatarDetail.GeneKeys.Add(new GeneKey() { Name = "Invisibility", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });
                //avatarDetail.GeneKeys.Add(new GeneKey() { Name = "Rapture", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });

                //avatarDetail.HumanDesign.Type = "Generator";
                //avatarDetail.Inventory.Add(new InventoryItem() { Name = "Magical Armour" });
                //avatarDetail.Inventory.Add(new InventoryItem() { Name = "Mighty Wizard Sword" });

                //avatarDetail.Spells.Add(new Spell() { Name = "Super Spell" });
                //avatarDetail.Spells.Add(new Spell() { Name = "Super Speed Spell" });
                //avatarDetail.Spells.Add(new Spell() { Name = "Super Srength Spell" });

                //avatarDetail.Achievements.Add(new Achievement() { Name = "Becoming Superman!" });
                //avatarDetail.Achievements.Add(new Achievement() { Name = "Completing STAR!" });

                //avatarDetail.Gifts.Add(new AvatarGift() { GiftType = KarmaTypePositive.BeASuperHero });

                //avatarDetail.Aura.Brightness = 99;
                //avatarDetail.Aura.Level = 77;
                //avatarDetail.Aura.Progress = 88;
                //avatarDetail.Aura.Size = 10;
                //avatarDetail.Aura.Value = 777;

                //avatarDetail.Chakras.Root.Level = 77;
                //avatarDetail.Chakras.Root.Progress = 99;
                //avatarDetail.Chakras.Root.XP = 8783;



                //avatarDetail.SuperPowers.AstralProjection = 99;
                //avatarDetail.SuperPowers.BioLocatation = 88;
                //avatarDetail.SuperPowers.Flight = 99;
                //avatarDetail.SuperPowers.FreezeBreath = 88;
                //avatarDetail.SuperPowers.HeatVision = 99;
                //avatarDetail.SuperPowers.Invulerability = 99;
                //avatarDetail.SuperPowers.SuperSpeed = 99;
                //avatarDetail.SuperPowers.SuperStrength = 99;
                //avatarDetail.SuperPowers.XRayVision = 99;
                //avatarDetail.SuperPowers.Teleportation = 99;
                //avatarDetail.SuperPowers.Telekineseis = 99;

                //avatarDetail.Skills.Computers = 99;
                //avatarDetail.Skills.Engineering = 99;
            }


            Console.ForegroundColor = ConsoleColor.Green;
            CLIEngine.ShowMessage("", false);
            CLIEngine.ShowMessage($"Avatar {avatar.Username} Beamed In On {avatar.LastBeamedIn} And Last Beamed Out On {avatar.LastBeamedOut}.");
            Console.WriteLine("");

            //TODO: May need to hide their real name if the user is not Admin(Wizard).
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" AVATAR STATS:");
            Console.WriteLine("");
            Console.WriteLine(string.Concat(" Name:                              ", avatar.FullName));
            Console.WriteLine(string.Concat(" Created:                           ", avatar.CreatedDate));
            Console.WriteLine(string.Concat(" Karma:                             ", avatarDetail.Karma));
            Console.WriteLine(string.Concat(" Level:                             ", avatarDetail.Level));
            Console.WriteLine(string.Concat(" XP:                                ", avatarDetail.XP));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" CHAKRAS:");
            Console.WriteLine("");
            Console.WriteLine(string.Concat(" Crown XP:                          ", avatarDetail.Chakras.Crown.XP));
            Console.WriteLine(string.Concat(" Crown Level:                       ", avatarDetail.Chakras.Crown.Level));
            Console.WriteLine(string.Concat(" ThirdEye XP:                       ", avatarDetail.Chakras.ThirdEye.XP));
            Console.WriteLine(string.Concat(" ThirdEye Level:                    ", avatarDetail.Chakras.ThirdEye.Level));
            Console.WriteLine(string.Concat(" Throat XP:                         ", avatarDetail.Chakras.Throat.XP));
            Console.WriteLine(string.Concat(" Throat Level:                      ", avatarDetail.Chakras.Throat.Level));
            Console.WriteLine(string.Concat(" Heart XP:                          ", avatarDetail.Chakras.Heart.XP));
            Console.WriteLine(string.Concat(" Heart Level:                       ", avatarDetail.Chakras.Heart.Level));
            Console.WriteLine(string.Concat(" Solar Plexus XP:                  ", avatarDetail.Chakras.SolarPlexus.XP));
            Console.WriteLine(string.Concat(" Solar Plexus Level:               ", avatarDetail.Chakras.SolarPlexus.Level));
            Console.WriteLine(string.Concat(" Sacral XP:                         ", avatarDetail.Chakras.Sacral.XP));
            Console.WriteLine(string.Concat(" Sacral Level:                      ", avatarDetail.Chakras.Sacral.Level));

            Console.WriteLine(string.Concat(" Root Sanskrit Name:                ", avatarDetail.Chakras.Root.SanskritName));
            Console.WriteLine(string.Concat(" Root XP:                           ", avatarDetail.Chakras.Root.XP));
            Console.WriteLine(string.Concat(" Root Level:                        ", avatarDetail.Chakras.Root.Level));
            Console.WriteLine(string.Concat(" Root Progress:                     ", avatarDetail.Chakras.Root.Progress));
            // Console.WriteLine(string.Concat(" Root Color: ", Superavatar.Chakras.Root.Color.Name));
            Console.WriteLine(string.Concat(" Root Element:                      ", avatarDetail.Chakras.Root.Element.Name));
            Console.WriteLine(string.Concat(" Root Yoga Pose:                    ", avatarDetail.Chakras.Root.YogaPose.Name));
            Console.WriteLine(string.Concat(" Root What It Controls:             ", avatarDetail.Chakras.Root.WhatItControls));
            Console.WriteLine(string.Concat(" Root When It Develops:             ", avatarDetail.Chakras.Root.WhenItDevelops));
            Console.WriteLine(string.Concat(" Root Crystal Name:                 ", avatarDetail.Chakras.Root.Crystal.Name.Name));
            Console.WriteLine(string.Concat(" Root Crystal Amplification Level:  ", avatarDetail.Chakras.Root.Crystal.AmplifyicationLevel));
            Console.WriteLine(string.Concat(" Root Crystal Cleansing Level:      ", avatarDetail.Chakras.Root.Crystal.CleansingLevel));
            Console.WriteLine(string.Concat(" Root Crystal Energising Level:     ", avatarDetail.Chakras.Root.Crystal.EnergisingLevel));
            Console.WriteLine(string.Concat(" Root Crystal Grounding Level:      ", avatarDetail.Chakras.Root.Crystal.GroundingLevel));
            Console.WriteLine(string.Concat(" Root Crystal Protection Level:     ", avatarDetail.Chakras.Root.Crystal.ProtectionLevel));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" AURA:");
            Console.WriteLine("");
            Console.WriteLine(string.Concat(" Brightness:                        ", avatarDetail.Aura.Brightness));
            Console.WriteLine(string.Concat(" Size:                              ", avatarDetail.Aura.Size));
            Console.WriteLine(string.Concat(" Level:                             ", avatarDetail.Aura.Level));
            Console.WriteLine(string.Concat(" Value:                             ", avatarDetail.Aura.Value));
            Console.WriteLine(string.Concat(" Progress:                          ", avatarDetail.Aura.Progress));
            Console.WriteLine(string.Concat(" ColourRed:                         ", avatarDetail.Aura.ColourRed));
            Console.WriteLine(string.Concat(" ColourGreen:                       ", avatarDetail.Aura.ColourGreen));
            Console.WriteLine(string.Concat(" ColourBlue:                        ", avatarDetail.Aura.ColourBlue));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" ATTRIBUTES:");
            Console.WriteLine("");
            Console.WriteLine(string.Concat(" Strength:                          ", avatarDetail.Attributes.Strength));
            Console.WriteLine(string.Concat(" Speed:                             ", avatarDetail.Attributes.Speed));
            Console.WriteLine(string.Concat(" Dexterity:                         ", avatarDetail.Attributes.Dexterity));
            Console.WriteLine(string.Concat(" Intelligence:                      ", avatarDetail.Attributes.Intelligence));
            Console.WriteLine(string.Concat(" Magic:                             ", avatarDetail.Attributes.Magic));
            Console.WriteLine(string.Concat(" Wisdom:                            ", avatarDetail.Attributes.Wisdom));
            Console.WriteLine(string.Concat(" Toughness:                         ", avatarDetail.Attributes.Toughness));
            Console.WriteLine(string.Concat(" Vitality:                          ", avatarDetail.Attributes.Vitality));
            Console.WriteLine(string.Concat(" Endurance:                         ", avatarDetail.Attributes.Endurance));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" STATS:");
            Console.WriteLine("");
            Console.WriteLine(string.Concat(" HP:                                ", avatarDetail.Stats.HP.Current, "/", avatarDetail.Stats.HP.Max));
            Console.WriteLine(string.Concat(" Mana:                              ", avatarDetail.Stats.Mana.Current, "/", avatarDetail.Stats.Mana.Max));
            Console.WriteLine(string.Concat(" Energy:                            ", avatarDetail.Stats.Energy.Current, "/", avatarDetail.Stats.Energy.Max));
            Console.WriteLine(string.Concat(" Stamina:                          ", avatarDetail.Stats.Stamina.Current, "/", avatarDetail.Stats.Stamina.Max));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" SUPER POWERS:");
            Console.WriteLine("");
            Console.WriteLine(string.Concat(" Flight:                            ", avatarDetail.SuperPowers.Flight));
            Console.WriteLine(string.Concat(" Astral Projection:                 ", avatarDetail.SuperPowers.AstralProjection));
            Console.WriteLine(string.Concat(" Bio-Locatation:                    ", avatarDetail.SuperPowers.BioLocatation));
            Console.WriteLine(string.Concat(" Heat Vision:                       ", avatarDetail.SuperPowers.HeatVision));
            Console.WriteLine(string.Concat(" Invulnerability:                    ", avatarDetail.SuperPowers.Invulnerability));
            Console.WriteLine(string.Concat(" Remote Viewing:                    ", avatarDetail.SuperPowers.RemoteViewing));
            Console.WriteLine(string.Concat(" Super Speed:                       ", avatarDetail.SuperPowers.SuperSpeed));
            Console.WriteLine(string.Concat(" Super Strength:                    ", avatarDetail.SuperPowers.SuperStrength));
            Console.WriteLine(string.Concat(" Telekinesis:                      ", avatarDetail.SuperPowers.Telekinesis));
            Console.WriteLine(string.Concat(" XRay Vision:                       ", avatarDetail.SuperPowers.XRayVision));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" SKILLS:");
            Console.WriteLine("");
            Console.WriteLine(string.Concat(" Computers:                         ", avatarDetail.Skills.Computers));
            Console.WriteLine(string.Concat(" Engineering:                       ", avatarDetail.Skills.Engineering));
            Console.WriteLine(string.Concat(" Farming:                           ", avatarDetail.Skills.Farming));
            Console.WriteLine(string.Concat(" Fire Starting:                     ", avatarDetail.Skills.FireStarting));
            Console.WriteLine(string.Concat(" Fishing:                           ", avatarDetail.Skills.Fishing));
            Console.WriteLine(string.Concat(" Languages:                         ", avatarDetail.Skills.Languages));
            Console.WriteLine(string.Concat(" Meditation:                        ", avatarDetail.Skills.Meditation));
            Console.WriteLine(string.Concat(" Melee Combat:                     ", avatarDetail.Skills.MeleeCombat));
            Console.WriteLine(string.Concat(" Mindfulness:                       ", avatarDetail.Skills.Mindfulness));
            Console.WriteLine(string.Concat(" Negotiating:                       ", avatarDetail.Skills.Negotiating));
            Console.WriteLine(string.Concat(" Ranged Combat:                     ", avatarDetail.Skills.RangedCombat));
            Console.WriteLine(string.Concat(" Research:                          ", avatarDetail.Skills.Research));
            Console.WriteLine(string.Concat(" Science:                           ", avatarDetail.Skills.Science));
            Console.WriteLine(string.Concat(" Spell Casting:                     ", avatarDetail.Skills.SpellCasting));
            Console.WriteLine(string.Concat(" Translating:                       ", avatarDetail.Skills.Translating));
            Console.WriteLine(string.Concat(" Yoga:                              ", avatarDetail.Skills.Yoga));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" GIFTS:");
            Console.WriteLine("");

            foreach (AvatarGift gift in avatarDetail.Gifts)
                Console.WriteLine(string.Concat(" ", Enum.GetName(gift.GiftType), " earnt on ", gift.GiftEarnt.ToString()));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" SPELLS:");
            Console.WriteLine("");

            foreach (Spell spell in avatarDetail.Spells)
                Console.WriteLine(string.Concat(" ", spell.Name));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" INVENTORY:");
            Console.WriteLine("");

            foreach (InventoryItem inventoryItem in avatarDetail.Inventory)
                Console.WriteLine(string.Concat(" ", inventoryItem.Name));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" ACHIEVEMENTS:");
            Console.WriteLine("");

            foreach (Achievement achievement in avatarDetail.Achievements)
                Console.WriteLine(string.Concat(" ", achievement.Name));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" GENE KEYS:");
            Console.WriteLine("");

            foreach (GeneKey geneKey in avatarDetail.GeneKeys)
                Console.WriteLine(string.Concat(" ", geneKey.Name));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" HUMAN DESIGN:");
            Console.WriteLine("");
            Console.WriteLine(string.Concat(" Type: ", !string.IsNullOrEmpty(avatarDetail.HumanDesign.Type) ? avatarDetail.HumanDesign.Type : "Unknown"));
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        //public void EnableOrDisableAutoProviderList(Func<bool, List<ProviderType>, bool> funct, bool isEnabled, List<ProviderType> providerTypes, string workingMessage, string successMessage, string errorMessage)
        //{
        //    CLIEngine.ShowWorkingMessage(workingMessage);

        //    if (funct(isEnabled, providerTypes))
        //        CLIEngine.ShowSuccessMessage(successMessage);
        //    else
        //        CLIEngine.ShowErrorMessage(errorMessage);
        //}

        public void ShowAvatar(IAvatar avatar, IAvatarDetail avatarDetail, bool listMode = false)
        {
            if (avatar != null)
            {
                //CLIEngine.ShowSuccessMessage("Avatar Loaded Successfully");
                CLIEngine.ShowMessage($"Avatar ID:                   {avatar.Id}");
                CLIEngine.ShowMessage($"Avatar Name:                 {avatar.FullName}");
                CLIEngine.ShowMessage($"Avatar Username:             {avatar.Username}");
                CLIEngine.ShowMessage($"Avatar Type:                 {avatar.AvatarType.Name}");
                CLIEngine.ShowMessage($"Avatar Created Date:         {avatar.CreatedDate}");
                CLIEngine.ShowMessage($"Avatar Modified Date:         {avatar.ModifiedDate}");
                CLIEngine.ShowMessage($"Avatar Last Beamed In Date:  {avatar.LastBeamedIn}");
                CLIEngine.ShowMessage($"Avatar Last Beamed Out Date: {avatar.LastBeamedOut}");
                CLIEngine.ShowMessage(String.Concat("Avatar Is Active:            ", avatar.IsActive ? "True" : "False"));
                CLIEngine.ShowMessage(String.Concat("Avatar Is Beamed In:         ", avatar.IsBeamedIn ? "True" : "False"));
                CLIEngine.ShowMessage(String.Concat("Avatar Is Verified:          ", avatar.IsVerified ? "True" : "False"));
                //CLIEngine.ShowMessage($"Avatar Version: {avatar.Version}");

                if (!listMode && avatarDetail != null && CLIEngine.GetConfirmation($"Do you wish to view more detailed information?"))
                    ShowAvatarStats(avatar, avatarDetail);

                if (listMode && avatarDetail != null)
                    ShowAvatarStats(avatar, avatarDetail);
            }
            else
                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In!");
        }

        public async Task ShowAvatar(Guid id = new Guid())
        {
            if (id == Guid.Empty)
                id = CLIEngine.GetValidInputForGuid("What is the ID/GUID for the avatar you wish to view?");

            OASISResult<IAvatar> avatarResult = await STAR.OASISAPI.Avatars.LoadAvatarAsync(id);

            if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
            {
                OASISResult<IAvatarDetail> avatarDetailResult = await STAR.OASISAPI.Avatars.LoadAvatarDetailAsync(id);

                if (avatarDetailResult != null && !avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    ShowAvatar(avatarResult.Result, avatarDetailResult.Result);
                else
                    CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar Detail: {avatarDetailResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar: {avatarResult.Message}");
        }

        public async Task ShowAvatar(string idOrUsername)
        {
            Guid id = Guid.Empty;

            if (string.IsNullOrEmpty(idOrUsername))
                idOrUsername = CLIEngine.GetValidInput("What is the username or ID/GUID for the avatar you wish to view?");

            if (Guid.TryParse(idOrUsername, out id))
                await ShowAvatar(id);
            else
            {
                OASISResult<IAvatar> avatarResult = await STAR.OASISAPI.Avatars.LoadAvatarAsync(idOrUsername);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    OASISResult<IAvatarDetail> avatarDetailResult = await STAR.OASISAPI.Avatars.LoadAvatarDetailByUsernameAsync(idOrUsername);

                    if (avatarDetailResult != null && !avatarDetailResult.IsError && avatarDetailResult.Result != null)
                        ShowAvatar(avatarResult.Result, avatarDetailResult.Result);
                    else
                        CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar Detail: {avatarDetailResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar: {avatarResult.Message}");
            }
        }

        public async Task ShowAvatar()
        {
            await ShowAvatar("");
        }


        public async Task<OASISResult<bool>> ForgotPasswordAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string email = GetValidExistingEmail("Enter your email: ", providerType);
            ErrorHandling.HandleResponseWithDefaultErrorMessage(result, await STAR.OASISAPI.Avatars.ForgotPasswordAsync(email, providerType), "Error occured sending Forgot Password email. Reason: ", "Successfully Sent Forgot Password Email, Please Check Your Email.");

            if (result != null && result.Result != null && !result.IsError && CLIEngine.GetConfirmation("Would you like to enter the token you received in the email to reset your password now?"))
            {
                Console.WriteLine("");
                result = await ResetPasswordAsync(providerType);
            }

            return result;
        }

        public async Task<OASISResult<bool>> ResetPasswordAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string token = CLIEngine.GetValidInput("What is the token you received in the Forgotten Password email you received?");
            string oldPassword = CLIEngine.ReadPassword("Enter your old password: ");
            string newPassword = CLIEngine.GetValidPassword("Enter your new password: ");
            ErrorHandling.HandleResponseWithDefaultErrorMessage(result, await STAR.OASISAPI.Avatars.ResetPasswordAsync(token, oldPassword, newPassword, providerType), "ResetPasswordAsync", "Successfully Reset Password");

            if (result != null && result.Result != null && !result.IsError)
            {
                OASISResult<IAvatar> avatarResult = await STAR.OASISAPI.Avatars.LoadAvatarAsync(STAR.BeamedInAvatar.Id);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                    STAR.BeamedInAvatar = avatarResult.Result;
            }

            return result;
        }
        public async Task SearchAvatarsAsync(string searchTerm = "", ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                Console.WriteLine("");
                searchTerm = CLIEngine.GetValidInput("What do you want to search for (enter username, name, email etc)?");
            }
            else
                Console.WriteLine("");

            CLIEngine.ShowWorkingMessage("Searching Avatars...");
            ListAvatars(await STAR.OASISAPI.Avatars.SearchAvatarsAsync(searchTerm, providerType));
        }

        public void SearchAvatars(string searchTerm = "", ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                Console.WriteLine("");
                searchTerm = CLIEngine.GetValidInput("What is the name of the Avatar you wish to search for?");
            }

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Searching Avatars...");
            ListAvatars(STAR.OASISAPI.Avatars.SearchAvatars(searchTerm, providerType));
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> ListAvatarsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Listing Avatars...");
            return await ListAvatars(await STAR.OASISAPI.Avatars.LoadAllAvatarsAsync(providerType: providerType));
        }

        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> ListAvatarDetailsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            string errorMessage = "Error occured in ListAvatarDetailsAsync. Reason:";

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Listing Avatar Details...");
            OASISResult<IEnumerable<IAvatar>> avatarResults = await STAR.OASISAPI.Avatars.LoadAllAvatarsAsync(providerType: providerType);

            if (avatarResults != null && avatarResults.Result != null && !avatarResults.IsError)
            {
                OASISResult<IEnumerable<IAvatarDetail>> avatarDetailResults = await STAR.OASISAPI.Avatars.LoadAllAvatarDetailsAsync(providerType: providerType);

                if (avatarDetailResults != null && avatarDetailResults.Result != null && !avatarDetailResults.IsError)
                {
                    result = avatarDetailResults;
                    await ListAvatars(avatarResults, avatarDetailResults);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAllAvatarDetailsAsync. Reason: {avatarDetailResults.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAllAvatarsAsync. Reason: {avatarResults.Message}");

            return result;
        }

        private async Task<OASISResult<IEnumerable<IAvatar>>> ListAvatars(OASISResult<IEnumerable<IAvatar>> avatarsResult, OASISResult<IEnumerable<IAvatarDetail>> avatarDetailsResult = null)
        {
            if (avatarsResult != null)
            {
                if (!avatarsResult.IsError)
                {
                    if (avatarsResult.Result != null && avatarsResult.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (avatarsResult.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{avatarsResult.Result.Count()} Avatar Found:");
                        else
                            CLIEngine.ShowMessage($"{avatarsResult.Result.Count()} Avatars Found:");

                        CLIEngine.ShowDivider();

                        Dictionary<Guid, IAvatarDetail> avatarDetails = new Dictionary<Guid, IAvatarDetail>();
                        if (avatarDetailsResult != null && avatarDetailsResult.Result != null && !avatarDetailsResult.IsError)
                        {
                            foreach (IAvatarDetail avatarDetail in avatarDetailsResult.Result)
                            {
                                if (!avatarDetails.ContainsKey(avatarDetail.Id))
                                    avatarDetails.Add(avatarDetail.Id, avatarDetail);
                            }
                        }

                        foreach (IAvatar avatar in avatarsResult.Result)
                        {
                            ShowAvatar(avatar, avatarDetails.TryGetValue(avatar.Id, out IAvatarDetail? value) ? value : null, true);
                            CLIEngine.ShowDivider();
                        }
                    }
                    else
                        CLIEngine.ShowWarningMessage("No Avatar's Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading Avatar's. Reason: {avatarsResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading Avatar's.");

            return avatarsResult;
        }
    }
}