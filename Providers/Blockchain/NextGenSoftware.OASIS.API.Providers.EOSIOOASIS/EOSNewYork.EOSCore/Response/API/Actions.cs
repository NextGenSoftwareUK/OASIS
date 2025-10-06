using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Params;
using EOSNewYork.EOSCore.Lib;
using EOSNewYork.EOSCore.Serialization;

namespace EOSNewYork.EOSCore.Response.API
{
    public class Actions : IEOAPI
    {
        public List<OrderedActionResult> actions { get; set; }
        public uint last_irreversible_block { get; set; }
        public bool? time_limit_exeeded_error { get; set; }

        public EOSAPIMetadata GetMetaData()
        {
            var meta = new EOSAPIMetadata
            {
                uri = "/v1/history/get_actions"
            };

            return meta;
        }
    }

    public class OrderedActionResult
    {
        public ulong global_action_seq { get; set; }
        public ulong account_action_seq { get; set; }
        public ulong block_num { get; set; }
        public string block_time { get; set; }
        public DateTime block_time_datetime
        {
            get
            {
                return DateTime.SpecifyKind((DateTime.Parse(block_time)), DateTimeKind.Utc);
            }
        }
        public OrderedActionResult_action_trace action_trace { get; set; }
    }


    public class OrderedActionResult_action_trace
    {
        public OrderedActionResult_act act { get; set; }
        public string console { get; set; }
        public string cpu_usage { get; set; }
        public uint elapsedtotal_cpu_usage { get; set; } // Set us uint but have not confimed the format.
        public string trx_id { get; set; }
        public uint total_cpu_usage { get; set; } // Set us uint but have not confimed the format.
        public List<OrderedActionResult_action_trace> inline_traces { get; set; }
        public OrderedActionResult_receipt receipt { get; set; }
    }


    public class OrderedActionResult_act
    {
        public string account { get; set; }
        public List<OrderedActionResult_authorization> authorization { get; set; }
        public dynamic data { get; set; }
        public string hex_data { get; set; }
        public string name { get; set; }
    }

    public class OrderedActionResult_receipt
    {
        public ulong abi_sequence { get; set; } // Set us uint but have not confimed the format.
        public string act_digest { get; set; }
        //public String auth_sequence { get; set; } // Not sure how to handle this. Deserializer does not seem to like the structure. 
        public ulong code_sequence { get; set; } // Set us uint but have not confimed the format.
        public ulong global_sequence { get; set; } // Set us uint but have not confimed the format.
        public string receiver { get; set; }
        public ulong recv_sequence { get; set; } // Set us uint but have not confimed the format.
    }

    public class OrderedActionResult_authorization
    {
        public string actor { get; set; }
        public string permission { get; set; }
    }

}