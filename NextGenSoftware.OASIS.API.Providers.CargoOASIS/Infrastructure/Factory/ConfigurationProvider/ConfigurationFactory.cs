namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.ConfigurationProvider
{
    public static class ConfigurationFactory
    {
        public static IConfigurationProvider GetLocalStorageConfigurationProvider()
        {
            return new LocalStorageConfigurationProvider();
        }
    }
}