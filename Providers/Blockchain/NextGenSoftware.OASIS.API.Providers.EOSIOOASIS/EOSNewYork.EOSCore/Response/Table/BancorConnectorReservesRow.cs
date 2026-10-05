using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.Table
{

    public class BancorConnectorReservesConstructorSettings
    {
        public string accountName;
    }

    public class BancorConnectorReservesRow : IEOSTable
    {
        BancorConnectorReservesConstructorSettings settings;

        public BancorConnectorReservesRow(BancorConnectorReservesConstructorSettings settings)
        {
            this.settings = settings;
        }

        public string contract { get; set; }
        public string currency { get; set; }
        public bool enabled { get; set; }
        public UInt64 ratio { get; set; }

        public int currency_precision
        {
            get
            {
                var numeric = currency.Split(' ')[0];
                var prec = numeric.Split('.')[1];
                return prec.Length;
            }
        }

        public string symbol
        {
            get
            {
                return currency.Split(' ')[1];
            }
        }


        public EOSTableMetadata GetMetaData()
        {
            var meta = new EOSTableMetadata
            {
                primaryKey = "contract",
                contract = this.settings.accountName,
                scope = this.settings.accountName,
                table = "reserves",
                key_type = "name"
            };
            return meta;
        }
    }
}
