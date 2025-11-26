
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class OASISSettings
    {
        public string Secret { get; set; }
        public EmailSettings Email { get; set; }
        public StorageProviderSettings StorageProviders { get; set; }
    }

    public class StorageProviderSettings
    {
        public string DefaultProviders { get; set; }

        public HoloOASISProviderSettings HoloOASIS { get; set; }
        public MongoDBOASISProviderSettings MongoDBOASIS { get; set; }

        public EOSIOASISProviderSettings EOSIOOASIS { get; set; }

        public ThreeFoldOASISProviderSettings ThreeFoldOASIS { get; set; }

        public EthereumOASISProviderSettings EthereumOASIS { get; set; }
        public StarknetOASISProviderSettings StarknetOASIS { get; set; }

        public SQLLiteDBOASISSettings SQLLiteDBOASIS { get; set; }
    }

    public class EmailSettings
    {
        public string EmailFrom { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
    }

    public class ProviderSettingsBase
    {
        public string ConnectionString { get; set; }
    }

    public class HoloOASISProviderSettings : ProviderSettingsBase
    {

    }

    public class MongoDBOASISProviderSettings : ProviderSettingsBase
    {
        public string DBName { get; set; }
    }

    public class EOSIOASISProviderSettings : ProviderSettingsBase
    {
    }

    public class ThreeFoldOASISProviderSettings : ProviderSettingsBase
    {

    }

    public class EthereumOASISProviderSettings : ProviderSettingsBase
    {

    }

    public class SQLLiteDBOASISSettings : ProviderSettingsBase
    {
    }

    public class StarknetOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcUrl { get; set; }
        public string Network { get; set; }
    }
}
