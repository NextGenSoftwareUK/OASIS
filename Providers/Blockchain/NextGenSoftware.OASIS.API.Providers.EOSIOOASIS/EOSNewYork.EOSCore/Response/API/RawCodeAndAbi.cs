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
    public class RawCodeAndAbi : IEOAPI
    {
        public string account_name { get; set; }
        public string wasm { get; set; }
        public string abi { get; set; }
        
        public EOSAPIMetadata GetMetaData()
        {
            var meta = new EOSAPIMetadata
            {
                uri = "/v1/chain/get_raw_code_and_abi"
            };

            return meta;
        }
    }
}
