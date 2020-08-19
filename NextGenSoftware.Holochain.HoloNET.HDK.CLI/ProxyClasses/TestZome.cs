

using NextGenSoftware.Holochain.HoloNET.HDK.Core;

namespace NextGenSoftware.Holochain.HoloNET.HDK.CLI.ProxyClasses
{
    public class MyZome : HolochainBaseZome
    {
        public class Test : HolochainBaseDataObject
        {
            public string TestString { get; set; }
            public int TestInt { get; set; }
            public bool TestBool { get; set; }
        }

        public class MyTestClass : HolochainBaseDataObject
        {
            public string TestString { get; set; }
            public int TestInt { get; set; }
            public bool TestBool { get; set; }
        }
    }
}
