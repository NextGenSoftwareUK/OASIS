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
    public class Authorization
    {
        [JsonConverter(typeof(AccountName))]
        public AccountName actor { get; set; }
        [JsonConverter(typeof(PermissionName))]
        public PermissionName permission { get; set; }
    }
}
