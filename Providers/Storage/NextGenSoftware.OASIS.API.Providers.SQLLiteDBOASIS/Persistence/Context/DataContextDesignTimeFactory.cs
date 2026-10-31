using Microsoft.EntityFrameworkCore.Design;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

// Model scaffolding never selects the user's default local database or starts provider activation.
public sealed class DataContextDesignTimeFactory:IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)=>new("Data Source=:memory:");
}
