namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.TokenStorage
{
    public static class TokenStorageFactory
    {
        public static ITokenStorage GetMemoryCacheTokenStorage()
        {
            return new MemoryCacheTokenStorage();
        }
    }
}