using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public static class DependencyInjection
    {
        public static IHostBuilder AddInfrastructure()
        {
            IHostBuilder builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IAvatarDetailRepository, AvtarDetailRepository>();
                services.AddScoped<IAvatarRepository, AvatarRepository>();
                services.AddScoped<IHolonRepository, HolonRepository>();
            });
            return builder;
        }
    }
}
