using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context
{
    public class DataContext : DbContext
    {
        public DbSet<AvatarModel> Avatars { get; set; }
        public DbSet<AvatarDetailModel> AvatarDetails { get; set; }
        public DbSet<HolonModel> Holons { get; set; }

        //private string _defaultDbPath => "OASISSqlLiteDb.db";

        //private string GetDefaultDbConnectionString()
        //{
        //    return $"Data Source={_defaultDbPath}";
        //}

        private string DbPath = "";
        private string _connectionString = "";

        public DataContext(string connectionString)
        {
            _connectionString = connectionString;
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);

            //Default DB Path if no connectionstring is passed in.
            DbPath = $"{path}{System.IO.Path.DirectorySeparatorChar}Database{System.IO.Path.DirectorySeparatorChar}OASISSqlLiteDb.sqlite";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // connect to sqlite database
            if (string.IsNullOrEmpty(_connectionString))
                options.UseSqlite($"Data Source={DbPath}");
            else
                options.UseSqlite(_connectionString);
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlite(GetDefaultDbConnectionString());
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Child Entities Setting Up

             modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.HeartRates)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.Gifts)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.InventoryItems)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.GeneKeys)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.Spells)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.Achievements)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.KarmaAkashicRecords)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.ProviderPrivateKey)
                .WithOne()
                .HasForeignKey(ob => ob.OwnerId);
            
            modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.ProviderPublicKey)
                .WithOne()
                .HasForeignKey(ob => ob.OwnerId);
            
            modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.ProviderWalletAddress)
                .WithOne()
                .HasForeignKey(ob => ob.OwnerId);
            
             modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.MetaData)
                .WithOne()
                .HasForeignKey(ob => ob.OwnerId);

             modelBuilder.Entity<AvatarDetailModel>()
                .HasMany(avatar => avatar.AvatarChakras)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarChakraModel>()
                .HasMany(ac => ac.GiftsUnlocked)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarChakraId);

            modelBuilder.Entity<AvatarDetailModel>()
                .HasOne<AvatarStatsModel>(avatar => avatar.Stats)
                .WithOne()
                .HasForeignKey<AvatarStatsModel>(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarDetailModel>()
                .HasOne<AvatarAuraModel>(avatar => avatar.Aura)
                .WithOne()
                .HasForeignKey<AvatarAuraModel>(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarDetailModel>()
                .HasOne<AvatarHumanDesignModel>(avatar => avatar.HumanDesign)
                .WithOne()
                .HasForeignKey<AvatarHumanDesignModel>(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarDetailModel>()
                .HasOne<AvatarSkillsModel>(avatar => avatar.Skills)
                .WithOne()
                .HasForeignKey<AvatarSkillsModel>(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarDetailModel>()
                .HasOne<AvatarAttributesModel>(avatar => avatar.Attributes)
                .WithOne()
                .HasForeignKey<AvatarAttributesModel>(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarDetailModel>()
                .HasOne<AvatarSuperPowersModel>(avatar => avatar.SuperPowers)
                .WithOne()
                .HasForeignKey<AvatarSuperPowersModel>(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarChakraModel>()
                .HasOne<CrystalModel>(c => c.Crystal)
                .WithOne()
                .HasForeignKey<CrystalModel>(ob => ob.AvatarChakraId);

            modelBuilder.Entity<PlanetModel>()
                .HasMany(planet => planet.Moons)
                .WithOne()
                .HasForeignKey(ob => ob.ParentPlanetId);
            
            modelBuilder.Entity<SolarSystemModel>()
                .HasMany(ss => ss.Planets)
                .WithOne()
                .HasForeignKey(ob => ob.SolarSystemId);
            
            modelBuilder.Entity<SolarSystemModel>()
                .HasMany(ss => ss.Asteroids)
                .WithOne()
                .HasForeignKey(ob => ob.SolarSystemId);
            
            modelBuilder.Entity<SolarSystemModel>()
                .HasMany(ss => ss.Comets)
                .WithOne()
                .HasForeignKey(ob => ob.SolarSystemId);
            
            modelBuilder.Entity<SolarSystemModel>()
                .HasMany(ss => ss.Meteroids)
                .WithOne()
                .HasForeignKey(ob => ob.SolarSystemId);
            
            modelBuilder.Entity<GalaxyModel>()
                .HasMany(galaxy => galaxy.SolarSystems)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyId);
            
            modelBuilder.Entity<GalaxyModel>()
                .HasMany(galaxy => galaxy.Nebulas)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyId);
            
            modelBuilder.Entity<GalaxyModel>()
                .HasMany(galaxy => galaxy.Stars)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyId);
            
            modelBuilder.Entity<GalaxyModel>()
                .HasMany(galaxy => galaxy.Planets)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyId);
            
            modelBuilder.Entity<GalaxyModel>()
                .HasMany(galaxy => galaxy.Asteroids)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyId);
            
            modelBuilder.Entity<GalaxyModel>()
                .HasMany(galaxy => galaxy.Comets)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyId);
            
            modelBuilder.Entity<GalaxyModel>()
                .HasMany(galaxy => galaxy.Meteroids)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyId);

            modelBuilder.Entity<GalaxyClusterModel>()
                .HasMany(galaxy => galaxy.Galaxies)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyClusterId);
            
            modelBuilder.Entity<GalaxyClusterModel>()
                .HasMany(galaxy => galaxy.SolarSystems)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyClusterId);
            
            modelBuilder.Entity<GalaxyClusterModel>()
                .HasMany(galaxy => galaxy.Stars)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyClusterId);
            
            modelBuilder.Entity<GalaxyClusterModel>()
                .HasMany(galaxy => galaxy.Planets)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyClusterId);
            
            modelBuilder.Entity<GalaxyClusterModel>()
                .HasMany(galaxy => galaxy.Asteroids)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyClusterId);
            
            modelBuilder.Entity<GalaxyClusterModel>()
                .HasMany(galaxy => galaxy.Comets)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyClusterId);
            
            modelBuilder.Entity<GalaxyClusterModel>()
                .HasMany(galaxy => galaxy.Meteroids)
                .WithOne()
                .HasForeignKey(ob => ob.GalaxyClusterId);

            modelBuilder.Entity<UniverseModel>()
                .HasMany(universe => universe.GalaxyClusters)
                .WithOne()
                .HasForeignKey(ob => ob.UniverseId);
            
            modelBuilder.Entity<UniverseModel>()
                .HasMany(universe => universe.SolarSystems)
                .WithOne()
                .HasForeignKey(ob => ob.UniverseId);
            
            modelBuilder.Entity<UniverseModel>()
                .HasMany(universe => universe.Nebulas)
                .WithOne()
                .HasForeignKey(ob => ob.UniverseId);
            
            modelBuilder.Entity<UniverseModel>()
                .HasMany(universe => universe.Stars)
                .WithOne()
                .HasForeignKey(ob => ob.UniverseId);
            
            modelBuilder.Entity<UniverseModel>()
                .HasMany(universe => universe.Planets)
                .WithOne()
                .HasForeignKey(ob => ob.UniverseId);
            
            modelBuilder.Entity<UniverseModel>()
                .HasMany(universe => universe.Asteroids)
                .WithOne()
                .HasForeignKey(ob => ob.UniverseId);
            
            modelBuilder.Entity<UniverseModel>()
                .HasMany(universe => universe.Comets)
                .WithOne()
                .HasForeignKey(ob => ob.UniverseId);
            
            modelBuilder.Entity<UniverseModel>()
                .HasMany(universe => universe.Meteroids)
                .WithOne()
                .HasForeignKey(ob => ob.UniverseId);
            #endregion
            
            base.OnModelCreating(modelBuilder);
        }
    }
}