namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Singleton
{
    public sealed class Cargo
    {
        private static Cargo _cargo;
        
        public static Cargo GetInstance()
        {
            return _cargo ??= new Cargo();
        }
    }
}