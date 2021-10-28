using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    public abstract class ZomeParam
    {
        public string ParamName { get; set; }
        //public object ParamValue { get; set; }
        public bool IsStruct { get; set; }
    }
}
