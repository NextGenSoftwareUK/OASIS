using BuildingBlocks.Extensions;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, svc) =>
    {
        svc.AddDbContext<DataContext>(c => c.UseNpgsql(ctx.Configuration.GetDefaultConnectionString()));
        svc.AddLogging(logging => logging.AddConsole());

        svc.AddScoped<Migrator>();
    });

using IHost host = hostBuilder.Build();

try
{
    await host.StartAsync();

    Migrator migrator = host.Services.GetRequiredService<Migrator>();
    await migrator.MigrateAsync();
}
finally
{
    await host.StopAsync();
}