using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.MongoDb.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.MongoDb.Provider;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Repository;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Service;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol
{
    public static class ServiceDependencyProvider
    {
        public static void RegisterDependencies(this IServiceCollection serviceCollection, string connectionString, string databaseName)
        {
            // Middleware Dependency Injection
            serviceCollection.AddSingleton<IEntityProvider<Test>>(x =>
                new MongoDbProvider<Test>(connectionString, databaseName));
            serviceCollection.AddSingleton<IEntityProvider<Avatar>>(x =>
                new MongoDbProvider<Avatar>(connectionString, databaseName));
            serviceCollection.AddSingleton<IEntityProvider<AvatarDetail>>(x =>
               new MongoDbProvider<AvatarDetail>(connectionString, databaseName));
            serviceCollection.AddSingleton<IEntityProvider<Holon>>(x =>
               new MongoDbProvider<Holon>(connectionString, databaseName));
            serviceCollection.AddSingleton<IEntityProvider<HolonBase>>(x =>
               new MongoDbProvider<HolonBase>(connectionString, databaseName));
            serviceCollection.AddSingleton<IEntityProvider<SearchData>>(x =>
               new MongoDbProvider<SearchData>(connectionString, databaseName));

            // Repository Dependency Injection
            serviceCollection.AddSingleton<ITestRepository, TestRepository>();
            serviceCollection.AddSingleton<IAvatarRepository, AvatarRepository>();
            serviceCollection.AddSingleton<IAvatarDetailRepository, AvatarDetailRepository>();
            serviceCollection.AddSingleton<IHolonRepository, HolonRepository>();
            serviceCollection.AddSingleton<IHolonBaseRepository, HolonBaseRepository>();
            serviceCollection.AddSingleton<ISearchDataRepository, SearchDataRepository>();

            // Service Dependency Injection
            serviceCollection.AddSingleton<ITestService, TestService>();
            serviceCollection.AddSingleton<IAvatarService, AvatarService>();
            serviceCollection.AddSingleton<IAvatarDetailService, AvatarDetailService>();
            serviceCollection.AddSingleton<IHolonService, HolonService>();
            serviceCollection.AddSingleton<IHolonBaseService, HolonBaseService>();
            serviceCollection.AddSingleton<ISearchDataService, SearchDataService>();
        }
    }
}