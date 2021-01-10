
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public class DataContext : DbContext
    {
        public DbSet<Avatar> Avatars { get; set; }
        private string _connectionString = "";
      //  private readonly IConfiguration Configuration;

        //public DataContext(IConfiguration configuration)
        public DataContext(string connectionString)
        {
            _connectionString = connectionString;
            //Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // connect to sqlite database
            //options.UseSqlite(Configuration.GetConnectionString("OASISSQLLiteDB"));
            options.UseSqlite(_connectionString);
        }
    }
}