using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Params;
using EOSNewYork.EOSCore.Lib;
using EOSNewYork.EOSCore.Serialization;
using Newtonsoft.Json;

namespace EOSNewYork.EOSCore.Response.API
{
    public class TransactionResult : IEOAPI
    {
        public string id { get; set; }
        public TransactionResultInner trx { get; set; }
        public string block_time { get; set; }
        public DateTime block_time_datetime
        {
            get
            {
                return DateTime.SpecifyKind((DateTime.Parse(block_time)), DateTimeKind.Utc);
            }
        }
        public uint block_num { get; set; }
        public uint last_irreversible_block { get; set; }
        public List<JsonString> traces { get; set; }
        
        public EOSAPIMetadata GetMetaData()
        {
            var meta = new EOSAPIMetadata
            {
                uri = "/v1/history/get_transaction"
            };

            return meta;
        }
    }
    public class TransactionResultInner
    {
        public TransactionReceipt receipt { get; set; }
        public TransactionInner trx { get; set; }
    }
    public class TransactionReceipt
    {
        public string status { get; set; }
        public uint cpu_usage_us { get; set; }
        public uint net_usage_words { get; set; }
        [JsonConverter(typeof(TransactionReceiptTrxArray))]
        public TransactionReceiptTrx trx { get; set; }
    }
    public class TransactionReceiptTrx
    {
        public uint index { get; set; }
        public TransactionReceiptTrxInner trx { get; set; }
    }
    public class TransactionReceiptTrxInner
    {
        public List<string> signatures { get; set; }
        public string compression { get; set; }
        public string packed_context_free_data { get; set; }
        public string packed_trx { get; set; }
    }
}
