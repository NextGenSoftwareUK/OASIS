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
    public class TableRows : IEOAPI
    {
        public List<object> rows { get; set; }
        public bool more { get; set; }
        
        public EOSAPIMetadata GetMetaData()
        {
            var meta = new EOSAPIMetadata
            {
                uri = "/v1/chain/get_table_rows"
            };

            return meta;
        }
    }
}
