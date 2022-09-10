
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;

namespace NextGenSoftware.OASIS.STAR.TestHarness.Genesis.Interfaces
{
    public interface ISuperTest2 : IHolon
    {

        [CustomOASISProperty]
        public string TestString { get; set; }

        [CustomOASISProperty]
        public int TestInt { get; set; }

        [CustomOASISProperty]
        public bool TestBool { get; set; }

    }
}
