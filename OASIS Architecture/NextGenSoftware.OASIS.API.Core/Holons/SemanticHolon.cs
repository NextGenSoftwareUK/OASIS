using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public abstract class SemanticHolon : HolonBase, ISemanticHolon
    {
        public Guid ParentHolonId { get; set; } //Primary parent holon.
        public Guid ParentHolonId2 { get; set; } //For use if they need quick access to multiple parent holons.
        public Guid ParentHolonId3 { get; set; } //For use if they need quick access to multiple parent holons.
        public List<Guid> ParentHolonIds { get; set; } //Used if they need more than 3 parent holons.
        public IHolon ParentHolon { get; set; }
        public IHolon ParentHolon2 { get; set; }
        public IHolon ParentHolon3 { get; set; }
        public string ChildIdListCache { get; set; } //This will store the list of id's for the direct childen of this holon.
        public string AllChildIdListCache { get; set; } //This will store the list of id's for the ALL the childen of this holon (including all sub-childen).

        public IList<Guid> ChildrenIds { get; set; } = new List<Guid>();
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
