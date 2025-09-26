using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Params;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.API
{
    public class PushTransaction : IEOAPI
    {
        public string transaction_id { get; set; }
        public ProcessedTransaction processed { get; set; }
        
        public EOSAPIMetadata GetMetaData()
        {
            var meta = new EOSAPIMetadata
            {
                uri = "/v1/chain/push_transaction"
            };

            return meta;
        }
    }
    public class ProcessedTransaction 
    {
        public string id { get; set; }
        public TransactionReceipt receipt { get; set; }
        public uint elapsed { get; set; }   
        public uint net_usage { get; set; }
        public bool scheduled { get; set; }
        public List<ActionTrace> action_traces { get; set; }
        public object except { get; set; }
    }
    public class ActionTrace
    {
        public object receipt { get; set; }
        public Action act { get; set; }
        public uint elapsed { get; set; }
        public uint cpu_usage { get; set; }
        public string console { get; set; }
        public uint total_cpu_usage { get; set; }
        public string trx_id { get; set; }
    }
}
