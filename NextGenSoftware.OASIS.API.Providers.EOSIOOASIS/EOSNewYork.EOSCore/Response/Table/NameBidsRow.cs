using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Utilities;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.Table
{
    public class NameBidsRow : IEOSTable
    {
        public string newname { get; set; }
        public string high_bidder { get; set; }
        public long high_bid { get; set; }
        public string last_bid_time { get; set; }
        public string last_bid_time_utc
        {
            get
            {
                return EOSUtility.FromUnixTime(long.Parse(last_bid_time) / 1000000).ToString();
            }
        }
        public EOSTableMetadata GetMetaData()
        {

            var meta = new EOSTableMetadata
            {
                primaryKey = "newname",
                contract = "eosio",
                scope = "eosio",
                table = "namebids"
            };

            return meta;
        }
    }
}
