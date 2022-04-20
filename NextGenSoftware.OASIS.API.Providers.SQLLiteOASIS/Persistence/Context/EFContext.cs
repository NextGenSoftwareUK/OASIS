using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Context
{
    public class DataContext : DbContext
    {
        //public DataContext()
        //{
        //}

        //public DataContext(DbContextOptions options) : base(options)
        //{

        //}
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(@"Data Source=D:\David\New\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS\Database\SQLLiteOASIS.db");
            }
        }
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);
        //}

        public DbSet<AvatarEntity> AvatarEntities { get; set; }
        public DbSet<HolonEntity> HolonEntities { get; set; }
        public DbSet<AvatarDetailEntity> AvatarDetailEntities { get; set; }

    }
}
