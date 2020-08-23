

using NextGenSoftware.Holochain.HoloNET.HDK.Core;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness.ProxyClasses
{
    //public class MyZome

    //TODO: Replace base class with attribute.
    public class MyZome : ProxyZomeBase
    //public class SuperZome : HolochainBaseZome
    //public class MyZome 
    {

        //public MyZome : base("NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness.GeneratedCode")
        //{

        //}

        public class SuperTest : HolonBase
        {
            public string TestString { get; set; }
            public int TestInt { get; set; }
            public bool TestBool { get; set; }
        }

        public class SuperHolon : HolonBase
        {
            public string SuperTestString { get; set; }
            public int SuperTestInt { get; set; }
            public bool SuperTestBool { get; set; }
        }
    }
}
