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
    public class Abi : IEOAPI
    {
        public string account_name { get; set; }
        public AbiInner abi { get; set; }
        
        public EOSAPIMetadata GetMetaData()
        {
            var meta = new EOSAPIMetadata
            {
                uri = "/v1/chain/get_abi"
            };

            return meta;
        }
    }
    public class AbiInner
    {
        public string version { get; set; }
        public List<CustomType> types { get; set; }
        public List<Struct> structs { get; set; }
        public List<ActionType> actions { get; set; }
        public List<Table> tables { get; set; }
        public List<string> recardian_clauses { get; set; }
        public List<string> error_messages { get; set; }
        public List<string> abi_extensions { get; set; }
    }
    public class CustomType
    {
        public string new_type_name { get; set; }
        public string type { get; set; }
    }
    public class Struct
    {
        public string name { get; set; }
        [JsonProperty(PropertyName="base")]
        public string base_struct { get; set; }
        public List<Field> fields { get; set; }
    }
    public class Field
    {
        public string name { get; set; }
        public string type { get; set; }
    }
    public class ActionType
    {
        public string name { get; set; }
        public string type { get; set; }
        public string ricardian_contract { get; set; }
    }
    public class Table
    {
        public string name { get; set; }
        public string index_type { get; set; }
        public List<string> key_names { get; set; }
        public List<string> key_types { get; set; }
        public string type { get; set; }
    }
}
