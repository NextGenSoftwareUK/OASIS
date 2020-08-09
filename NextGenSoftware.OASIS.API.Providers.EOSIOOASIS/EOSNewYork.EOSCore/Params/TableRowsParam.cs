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
    public class TableRowsParam
    {
        public string scope { get; set; }
        public string code { get; set; }
        public string table { get; set; }        
        public string json { get; set; }        
        public int lower_bound { get; set; }        
        public int upper_bound { get; set; }        
        public int limit { get; set; }        
    }
}
