using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context
{
    public class DataContext : DbContext
    {
        private readonly string _connectionString;
        private readonly string _dbPath = "";

        public DataContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DataContext(DbContextOptions options, string connectionString) : base(options)
        {
            _connectionString = connectionString;

            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);

            //Default DB Path if no connectionstring is passed in.
            _dbPath = $"{path}{System.IO.Path.DirectorySeparatorChar}Database{System.IO.Path.DirectorySeparatorChar}SQLLiteDBOASIS.sqlite";

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // connect to sqlite database
            if (string.IsNullOrEmpty(_connectionString))
                optionsBuilder.UseSqlite($"Data Source={_dbPath}");
            else
                optionsBuilder.UseSqlite(_connectionString);

            //if (!optionsBuilder.IsConfigured)
            //{
            //    var directory = Directory.GetCurrentDirectory();
            //    optionsBuilder.UseSqlite(_connectionString);
            //    //optionsBuilder.UseSqlite(@"Data Source=" + directory + @"\Database\SQLLiteDBOASIS.db");
            //}
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.Achievements);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.Attributes);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.Aura);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Attributes);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Achievements);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Aura);

            modelBuilder.Entity<Avatar>().Ignore(t => t.AvatarType);
            modelBuilder.Entity<Chakra>().Ignore(t => t.Type);
            modelBuilder.Entity<Chakra>().Ignore(t => t.YogaPose);
            modelBuilder.Entity<Chakra>().Ignore(t => t.Element);
            modelBuilder.Entity<Crystal>().Ignore(t => t.Name);
            modelBuilder.Entity<Crystal>().Ignore(t => t.Type);
            modelBuilder.Entity<KarmaAkashicRecord>().Ignore(t => t.KarmaEarntOrLost);
            modelBuilder.Entity<KarmaAkashicRecord>().Ignore(t => t.KarmaTypeNegative);
            modelBuilder.Entity<KarmaAkashicRecord>().Ignore(t => t.KarmaTypePositive);
            modelBuilder.Entity<KarmaAkashicRecord>().Ignore(t => t.KarmaSource);
            modelBuilder.Entity<Avatar>().Ignore(t => t.CreatedProviderType);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.CreatedProviderType);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.CreatedProviderType);
            modelBuilder.Entity<HolonBase>().Ignore(t => t.CreatedProviderType);
            modelBuilder.Entity<AvatarEntity>().Ignore(t => t.CreatedProviderType);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.CreatedProviderType);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.CreatedProviderType);
            modelBuilder.Entity<KarmaAkashicRecord>().Ignore(t => t.Provider);
            modelBuilder.Entity<Achievement>().Ignore(t => t.Provider);
            modelBuilder.Entity<AvatarGift>().Ignore(t => t.Provider);
            //modelBuilder.Entity<Avatar>().Ignore(t => t.CreatedOASISType);
            //modelBuilder.Entity<AvatarDetail>().Ignore(t => t.CreatedOASISType);
            modelBuilder.Entity<HolonBase>().Ignore(t => t.CreatedOASISType);
            modelBuilder.Entity<AvatarEntity>().Ignore(t => t.CreatedOASISType);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.CreatedOASISType);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.CreatedOASISType);
            modelBuilder.Entity<HolonBase>().Ignore(t => t.CreatedByAvatar);
            modelBuilder.Entity<HolonBase>().Ignore(t => t.DeletedByAvatar);
            modelBuilder.Entity<HolonBase>().Ignore(t => t.ModifiedByAvatar);
            modelBuilder.Entity<HolonBase>().Ignore(t => t.Original);
            modelBuilder.Entity<Achievement>().Ignore(t => t.KarmaSource);
            modelBuilder.Entity<AvatarGift>().Ignore(t => t.KarmaSource);
            modelBuilder.Entity<Avatar>().Ignore(t => t.CreatedByAvatar);
            modelBuilder.Entity<Avatar>().Ignore(t => t.DeletedByAvatar);
            modelBuilder.Entity<Avatar>().Ignore(t => t.ModifiedByAvatar);
            modelBuilder.Entity<Avatar>().Ignore(t => t.Original);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.Children);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.Omniverse);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.Original);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentCelestialBody);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentCelestialSpace);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentDimension);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentGalaxy);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentGalaxyCluster);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentGrandSuperStar);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentGreatGrandSuperStar);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentHolon);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentMoon);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentMultiverse);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentMultiverse);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentOmniverse);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentPlanet);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentSolarSystem);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentStar);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentSuperStar);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentUniverse);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.ParentZome);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.Chakras);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.GeneKeys);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.HumanDesign);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.HumanDesign);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.GeneKeys);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Chakras);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Children);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Omniverse);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Original);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentCelestialBody);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentCelestialSpace);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentDimension);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentGalaxy);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentGalaxyCluster);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentGrandSuperStar);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentGreatGrandSuperStar);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentHolon);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentMoon);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentMultiverse);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentMultiverse);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentOmniverse);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentPlanet);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentSolarSystem);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentStar);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentSuperStar);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentUniverse);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentZome);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.Children);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.Original);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentCelestialBody);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentCelestialSpace);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentDimension);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentGalaxy);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentGalaxyCluster);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentGrandSuperStar);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentGreatGrandSuperStar);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentHolon);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentMoon);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentMultiverse);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentMultiverse);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentOmniverse);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentPlanet);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentSolarSystem);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentStar);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentSuperStar);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentUniverse);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentZome);

            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.Children);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.Original);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentCelestialBody);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentCelestialSpace);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentDimension);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentGalaxy);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentGalaxyCluster);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentGrandSuperStar);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentGreatGrandSuperStar);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentHolon);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentMoon);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentMultiverse);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentMultiverse);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentOmniverse);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentPlanet);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentSolarSystem);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentStar);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentSuperStar);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentUniverse);
            modelBuilder.Entity<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Ignore(t => t.ParentZome);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.Chakras);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Chakras);

            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.Gifts);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Gifts);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.Skills);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Skills);
            modelBuilder.Entity<Chakra>().Ignore(t => t.GiftsUnlocked);
            modelBuilder.Entity<AvatarStats>().Ignore(t => t.Energy);
            modelBuilder.Entity<AvatarStats>().Ignore(t => t.HP);
            modelBuilder.Entity<AvatarStats>().Ignore(t => t.Mana);
            modelBuilder.Entity<AvatarStats>().Ignore(t => t.Staminia);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.Stats);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Stats);
            modelBuilder.Entity<AvatarDetail>().Ignore(t => t.SuperPowers);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.SuperPowers);
            modelBuilder.Entity<Achievement>().HasNoKey();
            modelBuilder.Entity<AvatarAttributes>().HasNoKey();
            modelBuilder.Entity<AvatarAura>().HasNoKey();
            modelBuilder.Entity<AvatarChakras>().HasNoKey();
            modelBuilder.Entity<AvatarGift>().HasNoKey();
            modelBuilder.Entity<AvatarSuperPowers>().HasNoKey();
            modelBuilder.Entity<AvatarSkills>().HasNoKey();
            modelBuilder.Entity<AvatarStat>().HasNoKey();
            modelBuilder.Entity<AvatarStats>().HasNoKey();
            modelBuilder.Ignore<Chakra>();
            modelBuilder.Ignore<CrownChakra>();
            modelBuilder.Ignore<HeartChakra>();
            modelBuilder.Ignore<RootChakra>();
            modelBuilder.Ignore<SacralChakra>();
            modelBuilder.Ignore<SoloarPlexusChakra>();
            modelBuilder.Ignore<AvatarChakras>();
            modelBuilder.Ignore<ThirdEyeChakra>();
            modelBuilder.Ignore<ThroatChakra>();
            modelBuilder.Ignore<Crystal>();
            modelBuilder.Ignore<HeartRateEntry>();
            modelBuilder.Ignore<InventoryItem>();
            modelBuilder.Ignore<KarmaAkashicRecord>();
            modelBuilder.Ignore<Spell>();
            modelBuilder.Ignore<Dictionary<DimensionLevel, IHolon>>();
            modelBuilder.Ignore<Dictionary<DimensionLevel, Guid>>();
            modelBuilder.Ignore<Dictionary<ProviderType, Dictionary<string, string>>>();
            modelBuilder.Ignore<Dictionary<ProviderType, List<string>>>();
            modelBuilder.Ignore<Dictionary<ProviderType, string>>();
            modelBuilder.Ignore<Dictionary<string, string>>();
            modelBuilder.Ignore<ObservableCollection<IHolon>>();
            modelBuilder.Ignore<ObservableCollection<INode>>();
            modelBuilder.Ignore<HolonBase>();
            modelBuilder.Ignore<Avatar>();
            modelBuilder.Ignore<AvatarDetail>();
            modelBuilder.Ignore<AvatarDetailBase>();
            modelBuilder.Ignore<Core.Holons.Holon>();
            modelBuilder.Ignore<HolonBaseEntity>();
            modelBuilder.Ignore<HolonEntity>();
            modelBuilder.Ignore<EnumValue<ProviderType>>();
            modelBuilder.Ignore<EnumValue<OASISType>>();
            modelBuilder.Ignore<EnumValue<HolonType>>();
            modelBuilder.Ignore<EnumValue<AvatarType>>();
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.SuperPowers);

            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentOmniverse);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentMultiverse);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentUniverse);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentDimension);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.DimensionLevel);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.SubDimensionLevel);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentGalaxyCluster);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentGalaxy);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentSolarSystem);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentGreatGrandSuperStar);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentGrandSuperStar);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentSuperStar);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentStar);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentPlanet);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentMoon);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentCelestialSpace);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentCelestialBody);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentZome);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.ParentHolon);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.Children);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.Original);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.HolonType);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.IsNewHolon);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.IsSaving);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.VersionId);
            //modelBuilder.Entity<HolonEntity>().Ignore(t => t.PreviousVersionId);
            modelBuilder.Entity<HolonEntity>().Ignore(t => t.IsNewHolon);
            //modelBuilder.Entity<HolonBaseEntity>().Ignore(t => t.PreviousVersionId);
            //modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Country);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentOmniverse);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentMultiverse);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentUniverse);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentDimension);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.DimensionLevel);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.SubDimensionLevel);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentGalaxyCluster);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentGalaxy);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentSolarSystem);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentGreatGrandSuperStar);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentGrandSuperStar);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentSuperStar);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentStar);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentPlanet);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentMoon);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentCelestialSpace);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentCelestialBody);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentZome);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.ParentHolon);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Children);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Original);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.HolonType);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.IsNewHolon);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.IsSaving);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.VersionId);
            //modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.PreviousVersionId);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.IsNewHolon);

            modelBuilder.Entity<AvatarEntity>().Ignore(t => t.Original);
            modelBuilder.Entity<AvatarEntity>().Ignore(t => t.HolonType);
            modelBuilder.Entity<AvatarEntity>().Ignore(t => t.IsNewHolon);
            modelBuilder.Entity<AvatarEntity>().Ignore(t => t.IsSaving);
            modelBuilder.Entity<AvatarEntity>().Ignore(t => t.VersionId);
            //modelBuilder.Entity<AvatarEntity>().Ignore(t => t.PreviousVersionId);
            modelBuilder.Entity<AvatarEntity>().Ignore(t => t.IsNewHolon);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.County);
            //modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Country);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.FavouriteColour);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.STARCLIColour);
            //modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Address);
            //modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Town);
            //modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Postcode);
            //modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Mobile);
            //modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Landline);
            //modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.UmaJson);
            //modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Image2D);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.Model3D);
            modelBuilder.Entity<AvatarDetailEntity>().Ignore(t => t.HolonType);
            modelBuilder.Entity<AvatarEntity>().Ignore(t => t.HolonType);
            modelBuilder.Entity<AvatarEntity>().Ignore(t => t.AvatarId);








            base.OnModelCreating(modelBuilder);

        }

        public DbSet<AvatarEntity> AvatarEntities { get; set; }
        public DbSet<HolonEntity> HolonEntities { get; set; }
        public DbSet<AvatarDetailEntity> AvatarDetailEntities { get; set; }

    }
}
