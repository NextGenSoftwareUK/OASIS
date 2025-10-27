using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.Table
{
    public class GlobalRow : IEOSTable
    {
        public string total_producer_vote_weight { get; set; }

        public EOSTableMetadata GetMetaData()
        {
            var meta = new EOSTableMetadata
            {
                primaryKey = "",
                contract = "eosio",
                scope = "eosio",
                table = "global"
            };
            return meta;
        }
    }
}
