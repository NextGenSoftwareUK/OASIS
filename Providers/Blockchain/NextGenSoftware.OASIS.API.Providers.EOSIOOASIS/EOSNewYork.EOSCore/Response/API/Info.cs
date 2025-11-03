using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.API
{
    public class Info : IEOAPI
    {
        public string server_version { get; set; }
        public string chain_id { get; set; }
        public Int64 head_block_num { get; set; }
        public Int64 last_irreversible_block_num { get; set; }
        public string last_irreversible_block_id { get; set; }
        public string head_block_id { get; set; }
        public string head_block_time { get; set; }
        public string head_block_producer { get; set; }
        public Int64 virtual_block_cpu_limit { get; set; }
        public Int64 virtual_block_net_limit { get; set; }
        public Int64 block_cpu_limit { get; set; }
        public Int64 block_net_limit { get; set; }

        public EOSAPIMetadata GetMetaData()
        {
            var meta = new EOSAPIMetadata
            {
                uri = "v1/chain/get_info"
            };

            return meta;
        }
    }
}
