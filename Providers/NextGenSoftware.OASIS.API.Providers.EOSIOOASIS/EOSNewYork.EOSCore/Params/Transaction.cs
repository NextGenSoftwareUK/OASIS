using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore;
using EOSNewYork.EOSCore.Serialization;
using Newtonsoft.Json;

namespace EOSNewYork.EOSCore.Params
{
    public class Transaction
    {
        [JsonConverter(typeof(TimePointSec))]
        public TimePointSec expiration { get; set; }
        public ushort ref_block_num { get; set; }
        public uint ref_block_prefix { get; set; }
        [JsonConverter(typeof(UnsignedInt))]
        public UnsignedInt max_net_usage_words { get; set; } = new UnsignedInt();
        public byte max_cpu_usage_ms { get; set; }
        [JsonConverter(typeof(UnsignedInt))]
        public UnsignedInt delay_sec { get; set; } = new UnsignedInt();
        public Action[] context_free_actions { get; set; } = new Action[0];
        public Action[] actions { get; set; } = new Action[0];
        public Tuple<ushort, char[]>[] transaction_extensions { get; set; } = new Tuple<ushort, char[]>[0];
    }
}
