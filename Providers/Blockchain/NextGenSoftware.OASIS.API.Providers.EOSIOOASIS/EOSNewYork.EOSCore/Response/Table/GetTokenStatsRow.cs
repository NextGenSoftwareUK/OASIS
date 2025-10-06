using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.Table
{

    public class GetTokenStatsConstructorSettings
    {
        public string tokenContract;
        public string symbol;
    }


    public class GetTokenStatsRow : IEOSTable
    {
        GetTokenStatsConstructorSettings settings;

        public GetTokenStatsRow(GetTokenStatsConstructorSettings settings)
        {
            this.settings = settings;
        }

        public decimal max_supply_decimal
        {
            get
            {
                return decimal.Parse(max_supply.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public decimal supply_decimal
        {
            get
            {
                return decimal.Parse(supply.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
            }
        }


        public string supply { get; set; }
        public string max_supply { get; set; }
        public string issuer { get; set; }


        public EOSTableMetadata GetMetaData()
        {
            var meta = new EOSTableMetadata
            {
                primaryKey = "",
                contract = this.settings.tokenContract,
                scope = this.settings.symbol,
                table = "stat",
                key_type = ""
            };
            return meta;
        }
    }
}
