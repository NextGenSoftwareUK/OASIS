using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.Table
{
    public class UnusedaccntsWhitelistRow : IEOSTable
    {
        public string account { get; set; }
        public string eth_address { get; set; }

        public EOSTableMetadata GetMetaData()
        {
            var meta = new EOSTableMetadata
            {
                primaryKey = "account",
                contract = "unusedaccnts",
                scope = "unusedaccnts",
                table = "whitelist",
                key_type = "name"
            };
            return meta;
        }
    }
}
