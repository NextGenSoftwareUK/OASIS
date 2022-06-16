using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.Table
{
    public class ProducerRow : IEOSTable
    {
        //public long last_claim_time { get; set; }
        public string last_claim_time { get; set; }
        public string owner { get; set; }
        public string total_votes { get; set; }
        public string producer_key { get; set; }
        public bool is_active { get; set; }
        public long unpaid_blocks { get; set; }
        public string url { get; set; }

        public double total_votes_long
        {
            get
            {
                return double.Parse(total_votes, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /*
        public DateTime last_claim_time_DateTime
        {
            get
            {
                DateTime last = DateTime.MinValue;
                if(last_claim_time > 0)
                    last = Utilities.EOSUtility.FromUnixTime(last_claim_time, true);

                return last;
            }
        }
        */

        public DateTime last_claim_time_DateTime
        {
            get
            {
                DateTime last = DateTime.MinValue;
                
                last = DateTime.Parse(last_claim_time, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

                return last;
            }
        }



        public EOSTableMetadata GetMetaData()
        {

            var meta = new EOSTableMetadata
            {
                primaryKey = "owner",
                contract = "eosio",
                scope = "eosio",
                table = "producers",
                key_type = "name"
            };

            return meta;
        }

    }
}
