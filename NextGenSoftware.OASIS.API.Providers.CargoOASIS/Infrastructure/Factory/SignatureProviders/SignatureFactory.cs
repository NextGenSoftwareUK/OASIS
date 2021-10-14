namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.SignatureProviders
{
    public static class SignatureFactory
    {
        public static ISignatureProvider GetMemoryCacheSignatureProvider()
        {
            return new Web3SignatureProvider();
        }
    }
}