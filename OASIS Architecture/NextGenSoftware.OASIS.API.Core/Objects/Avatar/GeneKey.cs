

using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class GeneKey : IGeneKey
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Shadow { get; set; }
        public string Gift { get; set; }
        public string Sidhi { get; set; }
    }
}