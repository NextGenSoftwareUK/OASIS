using System;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public class DataContext : DbContext
    {
        public DbSet<AvatarModel> Avatars { get; set; }
        public DbSet<HolonModel> Holons { get; set; }

        private string DbPath = "";

        public DataContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = $"{path}{System.IO.Path.DirectorySeparatorChar}SQLLiteDBOASIS.sqlite";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // connect to sqlite database
            options.UseSqlite($"Data Source={DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder){

            modelBuilder.Entity<RefreshTokenModel>().HasAlternateKey(rt => rt.Id);
            modelBuilder.Entity<AvatarAttributesModel>().HasAlternateKey(aa => aa.AvatarId);
            modelBuilder.Entity<AvatarAuraModel>().HasAlternateKey(aa => aa.AvatarId);
            modelBuilder.Entity<AvatarHumanDesignModel>().HasAlternateKey(hd => hd.AvatarId);
            modelBuilder.Entity<ProviderKeyModel>().HasAlternateKey(p => p.KeyId);
            modelBuilder.Entity<ProviderPrivateKeyModel>().HasAlternateKey(p => p.KeyId);
            modelBuilder.Entity<ProviderPublicKeyModel>().HasAlternateKey(p => p.KeyId);
            modelBuilder.Entity<ProviderWalletAddressModel>().HasAlternateKey(p => p.KeyId);
            modelBuilder.Entity<AvatarSkillsModel>().HasAlternateKey(ask => ask.AvatarId);
            modelBuilder.Entity<AvatarStatsModel>().HasAlternateKey(ast => ast.AvatarId);
            modelBuilder.Entity<AvatarSuperPowersModel>().HasAlternateKey(ast => ast.AvatarId);


            modelBuilder.Entity<AvatarModel>()
                .HasOne<AvatarStatsModel>(avatar => avatar.Stats)
                .WithOne()
                .HasForeignKey<AvatarStatsModel>(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarModel>()
                .HasOne<AvatarAuraModel>(avatar => avatar.Aura)
                .WithOne()
                .HasForeignKey<AvatarAuraModel>(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarModel>()
                .HasOne<AvatarHumanDesignModel>(avatar => avatar.HumanDesign)
                .WithOne()
                .HasForeignKey<AvatarHumanDesignModel>(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarModel>()
                .HasOne<AvatarSkillsModel>(avatar => avatar.Skills)
                .WithOne()
                .HasForeignKey<AvatarSkillsModel>(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarModel>()
                .HasOne<AvatarAttributesModel>(avatar => avatar.Attributes)
                .WithOne()
                .HasForeignKey<AvatarAttributesModel>(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarModel>()
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
            

            modelBuilder.Entity<HolonModel>()
                .HasMany(universe => universe.Childrens)
                .WithOne()
                .HasForeignKey(ob => ob.ParentHolonId);
            
            modelBuilder.Entity<HolonModel>()
                .HasMany(avatar => avatar.ProviderKey)
                .WithOne()
                .HasForeignKey(ob => ob.ParentId);



            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.HeartRates)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.Gifts)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.RefreshTokens)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.InventoryItems)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.GeneKeys)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.Spells)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);

            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.Achievements)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.KarmaAkashicRecords)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.ProviderKey)
                .WithOne()
                .HasForeignKey(ob => ob.ParentId);


            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.ProviderPrivateKey)
                .WithOne()
                .HasForeignKey(ob => ob.ParentId);
            
            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.ProviderPublicKey)
                .WithOne()
                .HasForeignKey(ob => ob.ParentId);
            
            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.ProviderWalletAddress)
                .WithOne()
                .HasForeignKey(ob => ob.ParentId);
            
            modelBuilder.Entity<AvatarModel>()
                .HasMany(avatar => avatar.AvatarChakras)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarId);
            
            modelBuilder.Entity<AvatarChakraModel>()
                .HasMany(ac => ac.GiftsUnlocked)
                .WithOne()
                .HasForeignKey(ob => ob.AvatarChakraId);
        }
    }
}