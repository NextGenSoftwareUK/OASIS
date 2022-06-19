using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context
{
    public class DataContext : DbContext
    {
        public DbSet<AvatarEntity> Avatars { get; set; }
        public DbSet<AvatarDetailEntity> AvatarDetails { get; set; }
        public DbSet<HolonEntity> Holons { get; set; }

        private string GetDefaultDbPath()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            return Path.Join(path, "OASISSqlLiteDb.db");
        }

        private string GetDefaultDbConnectionString()
        {
            return $"Data Source={GetDefaultDbPath()}";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(GetDefaultDbConnectionString());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Unique Constrains

            modelBuilder.Entity<AvatarEntity>()
                .HasIndex(x => new {x.Id, x.AvatarId, x.Email})
                .IsUnique();

            modelBuilder.Entity<AvatarDetailEntity>()
                .HasIndex(x => new {x.Id, x.Email})
                .IsUnique();

            modelBuilder.Entity<HolonEntity>()
                .HasIndex(x => new {x.HolonId})
                .IsUnique();

            #endregion

            #region Multiple Primary Keys

            modelBuilder.Entity<AvatarEntity>()
                .HasKey(x => new {x.Id, x.AvatarId});

            modelBuilder.Entity<AvatarDetailEntity>()
                .HasKey(x => new {x.Id});

            modelBuilder.Entity<HolonEntity>()
                .HasKey(x => new {x.HolonId});

            #endregion

            #region Indexes

            modelBuilder.Entity<AvatarEntity>()
                .HasIndex(x => x.Id, "idx_avatar_id");

            modelBuilder.Entity<AvatarDetailEntity>()
                .HasIndex(x => x.Id, "idx_avatar_dtl_id");

            modelBuilder.Entity<HolonEntity>()
                .HasIndex(x => x.HolonId, "idx_holon_id");

            #endregion

            base.OnModelCreating(modelBuilder);
        }
    }
}