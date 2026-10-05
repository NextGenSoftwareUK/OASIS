using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public abstract class SemanticHolon : HolonBase, ISemanticHolon
    {
        public Guid ParentHolonId { get; set; }
        public IHolon ParentHolon { get; set; }
        public string ChildIdListCache { get; set; } //This will store the list of id's for the direct childen of this holon.
        public string AllChildIdListCache { get; set; } //This will store the list of id's for the ALL the childen of this holon (including all sub-childen).

        public IList<IHolon> Children { get; set; } = new List<IHolon>();

        public virtual IReadOnlyCollection<IHolon> AllChildren
        {
            get
            {
                return Children.AsReadOnly();
            }
        }
    }
}
