using EOSNewYork.EOSCore.Lib;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.EOSIOClasses
{
    public class EOSIOConfigTableRow: IEOSTable
    {
        public string contract_name{ get; set; }
        public string contract_version{ get; set; }
        public string admin{ get; set; }
        public int total_accounts { get; set; }
        public string oasis_account { get; set; }

        public EOSTableMetadata GetMetaData()
        {
            var meta = new EOSTableMetadata
            {
                primaryKey = "contract_name",
                contract = "oasis",
                scope = "oasis",
                table = "config",
                key_type = "string"
            };
            return meta;
        }
    }
}
