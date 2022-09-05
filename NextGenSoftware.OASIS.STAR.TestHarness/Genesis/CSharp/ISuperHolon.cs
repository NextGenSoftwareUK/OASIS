
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;

namespace NextGenSoftware.OASIS.STAR.TestHarness.Genesis.Interfaces
{
    public interface ISuperHolon : IHolon
    {

        [CustomOASISProperty]
        public string SuperTestString { get; set; }

        [CustomOASISProperty]
        public int SuperTestInt { get; set; }

        [CustomOASISProperty]
        public bool SuperTestBool { get; set; }

    }
}
