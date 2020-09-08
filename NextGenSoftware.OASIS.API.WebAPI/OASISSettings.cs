
namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class OASISSettings
    {
        public StorageProviders StorageProviders { get; set; }
    }

    public class StorageProviders
    {
        public string DefaultProvider { get; set; }

        public HoloOASISProviderSettings HoloOASIS { get; set; }
        public MongoOASISProviderSettings MongoOASIS { get; set; }
    }

    public class HoloOASISProviderSettings
    {
        public string ConnectionString { get; set; }
    }

    public class MongoOASISProviderSettings
    {
        public string DBName { get; set; }
        public string ConnectionString { get; set; }
    }
}
