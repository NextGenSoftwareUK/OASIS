
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.STAR.TestHarness.Genesis.Interfaces;

namespace NextGenSoftware.OASIS.STAR.TestHarness.Genesis
{
    public class SuperTest : Holon, ISuperTest
    {

        [CustomOASISProperty]
        public string TestString { get; set; }

        [CustomOASISProperty]
        public int TestInt { get; set; }

        [CustomOASISProperty]
        public bool TestBool { get; set; }

    }
}
