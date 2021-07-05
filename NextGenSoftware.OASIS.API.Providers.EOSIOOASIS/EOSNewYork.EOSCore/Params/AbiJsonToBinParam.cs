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
    public class AbiJsonToBinParam
    {
        public string code { get; set; }
        public string action { get; set; }
        public object args { get; set; }
    }
}
