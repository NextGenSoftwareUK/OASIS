using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.Table
{
    public class VoterRow : IEOSTable
    {
        public string owner { get; set; }
        public string proxy { get; set; }
        public Int64 staked { get; set; }
        public double last_vote_weight { get; set; }
        public double proxied_vote_weight { get; set; }
        public List<string> producers { get; set; }
        public double last_vote_weight_for_this_account_only
        {
            get
            {
                return last_vote_weight - proxied_vote_weight;
            }
        }
        public string voterDescription
        {
            get
            {
                string name = owner;
                if (!string.IsNullOrEmpty(proxy))
                    name = name + "(proxyvia:" + proxy + ")";
                return name;
            }
        }

        public EOSTableMetadata GetMetaData()
        {
            var meta = new EOSTableMetadata
            {
                primaryKey = "owner",
                contract = "eosio",
                scope = "eosio",
                table = "voters",
                key_type = "name"
            };
            return meta;
        }
    }
}
