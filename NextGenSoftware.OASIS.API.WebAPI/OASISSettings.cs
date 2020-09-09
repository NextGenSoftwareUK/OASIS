
namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class OASISSettings
    {
        public StorageProviders StorageProviders { get; set; }
    }

    public class StorageProviders
    {
        public string DefaultProviders { get; set; }

        public HoloOASISProviderSettings HoloOASIS { get; set; }
        public MongoDBOASISProviderSettings MongoDBOASIS { get; set; }

        public EOSIOASISProviderSettings EOSIOOASIS { get; set; }

        public ThreeFoldOASISProviderSettings ThreeFoldOASIS { get; set; }

        public EthereumOASISProviderSettings EthereumOASIS { get; set; }
    }

    public class HoloOASISProviderSettings
    {
        public string ConnectionString { get; set; }
    }

    public class MongoDBOASISProviderSettings
    {
        public string DBName { get; set; }
        public string ConnectionString { get; set; }
    }

    public class EOSIOASISProviderSettings
    {
        public string ConnectionString { get; set; }
    }

    public class ThreeFoldOASISProviderSettings
    {
        public string ConnectionString { get; set; }
    }

    public class EthereumOASISProviderSettings
    {
        public string ConnectionString { get; set; }
    }
}
