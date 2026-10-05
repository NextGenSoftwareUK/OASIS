using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;
using Newtonsoft.Json;

namespace EOSNewYork.EOSCore.Response.Table
{
    public class RammarketRow : IEOSTable
    {
        public string supply { get; set; }
        [JsonProperty(PropertyName = "base")]
        public BaseWeightBalance base_ { get; set; }
        public QuoteWeightBalance quote { get; set; }

        public double supply_double
        {
            get
            {
                return double.Parse(supply.Replace(" RAMCORE", ""), System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public EOSTableMetadata GetMetaData()
        {
            var meta = new EOSTableMetadata
            {
                primaryKey = "",
                contract = "eosio",
                scope = "eosio",
                table = "rammarket"
            };
            return meta;
        }
    }

    public class BaseWeightBalance
    {
        public string balance;
        public double weight;

        public long balance_long
        {
            get
            {
                return long.Parse(balance.Replace(" RAM",""), System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    public class QuoteWeightBalance
    {
        public string balance;
        public double weight;

        public double balance_double
        {
            get
            {
                return double.Parse(balance.Replace(" EOS",""), System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
