using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS
{
    public static class DependencyInjection
    {
        public static IHostBuilder AddInfrastructure()
        {
            IHostBuilder builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IAvtarDetailRepository, AvtarDetailRepository>();
                services.AddScoped<IAvtarRepository, AvtarRepository>();
                services.AddScoped<IHolonRepository, HolonRepository>();
            });
            return builder;
        }
    }
}
