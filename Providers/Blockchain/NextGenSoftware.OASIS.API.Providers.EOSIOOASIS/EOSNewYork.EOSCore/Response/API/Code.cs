using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Params;
using EOSNewYork.EOSCore.Lib;
using Newtonsoft.Json;

namespace EOSNewYork.EOSCore.Response.API
{
    public class Code : IEOAPI
    {
        public string account_name { get; set; }
        public string code_hash { get; set; }
        public string wast { get; set; }
        public string wasm { get; set; }
        public AbiInner abi { get; set; }
        
        public EOSAPIMetadata GetMetaData()
        {
            var meta = new EOSAPIMetadata
            {
                uri = "/v1/chain/get_code"
            };

            return meta;
        }
    }
}
