
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.TestHarness.DNA
{
    //TODO: Replace base class with attribute.
    public class SuperZome : ZomeDNA
    {
        public class SuperTest : HolonDNA
        {
            public string TestString { get; set; }
            public int TestInt { get; set; }
            public bool TestBool { get; set; }
        }

        public class SuperHolon : HolonDNA
        {
            public string SuperTestString { get; set; }
            public int SuperTestInt { get; set; }
            public bool SuperTestBool { get; set; }
        }
    }
}