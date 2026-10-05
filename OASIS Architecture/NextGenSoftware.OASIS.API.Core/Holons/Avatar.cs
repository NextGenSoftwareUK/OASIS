using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    // Lightweight version of the AvatarDetail object used for SSO. If people need the extended Avatar info they can load the AvatarDetail object.
    // If people need to add/subreact karma from the Avatar then they need to use the AvatarDetail object, same with if they need to query their KarmaAkasicRecords (karma audit).
    //public class Avatar : HolonBase, IAvatar
    public class Avatar : SemanticHolon, IAvatar
    {
        public Avatar()
        {
            this.HolonType = HolonType.Avatar;
        }

        public Dictionary<ProviderType, List<IProviderWallet>> ProviderWallets { get; set; } = new Dictionary<ProviderType, List<IProviderWallet>>();

        //TODO: Want to replace this with ProviderWallets above ASAP...
        //public Dictionary<ProviderType, string> ProviderPrivateKey { get; set; } = new Dictionary<ProviderType, string>();  //Unique private key used by each provider (part of private/public key pair).
        //public Dictionary<ProviderType, List<string>> ProviderPublicKey { get; set; } = new Dictionary<ProviderType, List<string>>();
        public Dictionary<ProviderType, string> ProviderUsername { get; set; } = new Dictionary<ProviderType, string>();  // This is only really needed when we need to store BOTH a id and username for a provider (ProviderUniqueStorageKey on Holon already stores either id/username etc).                                                                                                            // public Dictionary<ProviderType, string> ProviderId { get; set; } = new Dictionary<ProviderType, string>(); // The ProviderUniqueStorageKey property on the base Holon object can store ids, usernames, etc that uniqueliy identity that holon in the provider (although the Guid is globally unique we still need to map the Holons the unique id/username/etc for each provider).
       // public Dictionary<ProviderType, List<string>> ProviderWalletAddress { get; set; } = new Dictionary<ProviderType, List<string>>();


        //public new Guid Id 
        //{
        //    get
        //    {
        //        return base.Id;
        //    }
        //    set
        //    {
        //        base.Id = value;
        //    }
        //}

        //Needed to work around bug in WebAPI where base class properties are not returned/serialized (it cannot be the same property name or it breaks Mongo for some unknown reason!)
        public Guid AvatarId
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
            }
        }

        public new string Name
        {
            get
            {
                return FullName;
            }
        }
        public string FullName
        {
            get
            {
                return string.Concat(FirstName, " ", LastName).Trim();
            }
        }

        public string FullNameWithTitle
        {
            get
            {
                return string.Concat(Title, " ", FirstName, " ", LastName).Trim();
            }
        }

        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public EnumValue<AvatarType> AvatarType { get; set; }
        public bool AcceptTerms { get; set; }
        public string VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public string ResetToken { get; set; }
        public string JwtToken { get; set; }

        [JsonIgnore] // refresh token is returned in http only cookie
        public string RefreshToken { get; set; }

        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }
        public DateTime? LastBeamedIn { get; set; }
        public DateTime? LastBeamedOut { get; set; }
        public bool IsBeamedIn { get; set; }

        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
       // public string Image2D { get; set; }
        
        /*
        public int Karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        public int XP { get; set; }
        public int Level
        {
            get
            {
                if (this.Karma > 0 && this.Karma < 100)
                    return 1;

                if (this.Karma >= 100 && this.Karma < 200)
                    return 2;

                if (this.Karma >= 200 && this.Karma < 300)
                    return 3;

                if (this.Karma >= 777)
                    return 99;

                //TODO: Add all the other levels here, all the way up to 100 for now! ;=)

                return 1; //Default.
            }
        }*/

        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }

        public async Task<OASISResult<IAvatar>> SaveAsync(AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            return await AvatarManager.Instance.SaveAvatarAsync(this, autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, waitForAutoReplicationResult, providerType);
        }
        public OASISResult<IAvatar> Save(AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            return AvatarManager.Instance.SaveAvatar(this, autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, waitForAutoReplicationResult, providerType);
        }

        public async Task<OASISResult<IAvatar>> BeamOutAsync(AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            return await AvatarManager.Instance.BeamOutAsync(this, autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, waitForAutoReplicationResult, providerType);
        }
        public OASISResult<IAvatar> BeamOut(AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            return AvatarManager.Instance.BeamOut(this, autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, waitForAutoReplicationResult, providerType);
        }

        //public OASISResult<bool> SaveProviderWallets(ProviderType providerType = ProviderType.Default)
        //{
        //    return AvatarManager.Instance.SaveProviderWallets(this, providerType);
        //}

        //TODO: Implement async version ASAP...
        //public Task<OASISResult<bool>> SaveProviderWalletsAsync(ProviderType providerType = ProviderType.Default)
        //{
        //    return await AvatarManager.Instance.SaveProviderWalletsAsync(this, providerType);
        //}

        /*
        private int GetKarmaForType(KarmaTypePositive karmaType)
        {
            switch (karmaType)
            {
                case KarmaTypePositive.BeAHero:
                    return 7;

                case KarmaTypePositive.BeASuperHero:
                    return 8;

                case KarmaTypePositive.BeATeamPlayer:
                    return 5;

                case KarmaTypePositive.BeingDetermined:
                    return 5;

                case KarmaTypePositive.BeingFast:
                    return 5;

                case KarmaTypePositive.ContributingTowardsAGoodCauseAdministrator:
                    return 3;

                case KarmaTypePositive.ContributingTowardsAGoodCauseSpeaker:
                    return 8;

                case KarmaTypePositive.ContributingTowardsAGoodCauseContributor:
                    return 5;

                case KarmaTypePositive.ContributingTowardsAGoodCauseCreatorOrganiser:
                    return 10;

                case KarmaTypePositive.ContributingTowardsAGoodCauseFunder:
                    return 8;

                case KarmaTypePositive.ContributingTowardsAGoodCausePeacefulProtesterActivist:
                    return 5;

                case KarmaTypePositive.ContributingTowardsAGoodCauseSharer:
                    return 3;

                case KarmaTypePositive.HelpingAnimals:
                    return 5;

                case KarmaTypePositive.HelpingTheEnvironment:
                    return 5;

                case KarmaTypePositive.Other:
                    return 2;

                case KarmaTypePositive.OurWorld:
                    return 5;

                case KarmaTypePositive.SelfHelpImprovement:
                    return 2;

                //TODO: Finish...

                default:
                    return 0;
            }

        }

        private int GetKarmaForType(KarmaTypeNegative karmaType)
        {
            switch (karmaType)
            {
                case KarmaTypeNegative.AttackPhysciallyOtherPersonOrPeople:
                    return 10;

                case KarmaTypeNegative.AttackVerballyOtherPersonOrPeople:
                    return 5;

                case KarmaTypeNegative.BeingSelfish:
                    return 3;

                case KarmaTypeNegative.DisrespectPersonOrPeople:
                    return 4;

                case KarmaTypeNegative.DropLitter:
                    return 9;

                case KarmaTypeNegative.HarmingAnimals:
                    return 10;

                case KarmaTypeNegative.HarmingChildren:
                    return 9;

                case KarmaTypeNegative.HarmingNature:
                    return 10;

                case KarmaTypeNegative.NotTeamPlayer:
                    return 3;

                case KarmaTypeNegative.NutritionEatDiary:
                    return 6;

                case KarmaTypeNegative.NutritionEatDrinkUnhealthy:
                    return 3;

                case KarmaTypeNegative.NutritionEatMeat:
                    return 7;

                case KarmaTypeNegative.Other:
                    return 1;

                case KarmaTypeNegative.OurWorldAttackOtherPlayer:
                    return 7;

                case KarmaTypeNegative.OurWorldBeSelfish:
                    return 4;

                case KarmaTypeNegative.OurWorldDisrespectOtherPlayer:
                    return 5;

                case KarmaTypeNegative.OurWorldDropLitter:
                    return 7;

                case KarmaTypeNegative.OurWorldNotTeamPlayer:
                    return 3;

                default:
                    return 0;
            }
        }


        // A record of all the karma the user has earnt/lost along with when and where from.
        public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }

        public async Task<KarmaAkashicRecord> KarmaEarntAsync(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = AddKarmaToAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                await SaveAsync();

            return record;
        }

        public KarmaAkashicRecord KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = AddKarmaToAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                Save();

            return record;
        }

        public async Task<KarmaAkashicRecord> KarmaLostAsync(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = RemoveKarmaFromAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                await SaveAsync();

            return record;
        }

        public KarmaAkashicRecord KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = RemoveKarmaFromAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                Save();

            return record;
        }

        private KarmaAkashicRecord AddKarmaToAkashicRecord(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, int karmaOverride = 0)
        {
            int karma = GetKarmaForType(karmaType);

            if (karmaType == KarmaTypePositive.Other)
                karma = karmaOverride;

            this.Karma += karma;

            KarmaAkashicRecord record = new KarmaAkashicRecord
            {
                AvatarId = Id,
                Date = DateTime.Now,
                Karma = karma,
                TotalKarma = this.Karma,
                Provider = ProviderManager.CurrentStorageProviderType,
                KarmaSourceTitle = karamSourceTitle,
                KarmaSourceDesc = karmaSourceDesc,
                WebLink = webLink,
                KarmaSource = new EnumValue<KarmaSourceType>(karmaSourceType),
                KarmaEarntOrLost = new EnumValue<KarmaEarntOrLost>(KarmaEarntOrLost.Earnt),
                KarmaTypeNegative = new EnumValue<KarmaTypeNegative>(KarmaTypeNegative.None),
                KarmaTypePositive = new EnumValue<KarmaTypePositive>(karmaType),
            };

            if (this.KarmaAkashicRecords == null)
                this.KarmaAkashicRecords = new List<KarmaAkashicRecord>();

            this.KarmaAkashicRecords.Add(record);
            return record;
        }

        private KarmaAkashicRecord RemoveKarmaFromAkashicRecord(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, int karmaOverride = 0)
        {
            int karma = GetKarmaForType(karmaType);

            if (karmaType == KarmaTypeNegative.Other)
                karma = karmaOverride;

            this.Karma -= karma;

            KarmaAkashicRecord record = new KarmaAkashicRecord
            {
                AvatarId = Id,
                Date = DateTime.Now,
                Karma = karma,
                TotalKarma = this.Karma,
                Provider = ProviderManager.CurrentStorageProviderType,
                KarmaSourceTitle = karamSourceTitle,
                KarmaSourceDesc = karmaSourceDesc,
                WebLink = webLink,
                KarmaSource = new EnumValue<KarmaSourceType>(karmaSourceType),
                KarmaEarntOrLost = new EnumValue<KarmaEarntOrLost>(KarmaEarntOrLost.Lost),
                KarmaTypeNegative = new EnumValue<KarmaTypeNegative>(karmaType),
                KarmaTypePositive = new EnumValue<KarmaTypePositive>(KarmaTypePositive.None),
            };

            if (this.KarmaAkashicRecords == null)
                this.KarmaAkashicRecords = new List<KarmaAkashicRecord>();

            this.KarmaAkashicRecords.Add(record);
            return record;
        }*/
    }
}