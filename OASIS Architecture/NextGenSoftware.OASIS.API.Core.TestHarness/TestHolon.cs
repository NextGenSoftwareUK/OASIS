using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.TestHarness
{
    public class TestHolon : Holon
    {
        public Guid Id { get; set; }
        
        
        public static string Name { get; set; }

        [CustomOASISProperty]
        public string Description { get; set; }
       
    }
}
