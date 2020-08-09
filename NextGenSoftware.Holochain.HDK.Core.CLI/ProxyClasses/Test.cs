
using NextGenSoftware.Holochain.NETHDK.Core;

namespace NextGenSoftware.Holochain.NETHDK.CLI.ProxyClasses
{
    public class TestZome : HolochainBaseZome
    {
        public class Test : HolochainBaseDataObject
        {
            public string TestString { get; set; }
            public int TestInt { get; set; }
            public bool TestBool { get; set; }
        }

        public class Test2 : HolochainBaseDataObject
        {
            public string TestString { get; set; }
            public int TestInt { get; set; }
            public bool TestBool { get; set; }
        }
    }
}
