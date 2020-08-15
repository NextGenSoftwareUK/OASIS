using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.Table
{

    public class BancorConnectorSettingsConstructorSettings
    {
        public string accountName;
    }

    public class BancorConnectorSettingsRow : IEOSTable
    {
        BancorConnectorSettingsConstructorSettings settings;

        public BancorConnectorSettingsRow(BancorConnectorSettingsConstructorSettings settings)
        {
            this.settings = settings;
        }

        public string smart_contract { get; set; }
        public string smart_currency { get; set; }
        public bool smart_enabled { get; set; }
        public bool enabled { get; set; }
        public string network { get; set; }
        public bool require_balance { get; set; }
        public UInt64 max_fee { get; set; }
        public UInt64 fee { get; set; }

        public string smart_currency_symbol
        {
            get
            {
                return smart_currency.Split(' ')[1];
            }
        }


        public EOSTableMetadata GetMetaData()
        {
            var meta = new EOSTableMetadata
            {
                primaryKey = "smart_contract",
                contract = this.settings.accountName,
                scope = this.settings.accountName,
                table = "settings",
                key_type = "name"
            };
            return meta;
        }
    }
}
