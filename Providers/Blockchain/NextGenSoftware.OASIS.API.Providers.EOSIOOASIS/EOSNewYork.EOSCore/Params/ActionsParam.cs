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
    public class ActionsParam
    {
        public int pos { get; set; }
        public int offset { get; set; }
        public string account_name { get; set; }        
    }
}
