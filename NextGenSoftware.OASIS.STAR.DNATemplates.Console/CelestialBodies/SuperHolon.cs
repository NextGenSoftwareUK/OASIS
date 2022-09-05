
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.STAR.TestHarness.Genesis.Interfaces;

namespace NextGenSoftware.OASIS.STAR.TestHarness.Genesis
{
    public class SuperHolon : Holon, ISuperHolon
    {

        [CustomOASISProperty]
        public string SuperTestString { get; set; }

        [CustomOASISProperty]
        public int SuperTestInt { get; set; }

        [CustomOASISProperty]
        public bool SuperTestBool { get; set; }

    }
}
