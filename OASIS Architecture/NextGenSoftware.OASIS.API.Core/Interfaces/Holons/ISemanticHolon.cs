using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface ISemanticHolon : IHolonBase
    {
        public Guid ParentHolonId { get; set; } //Primary parent holon.
        public Guid ParentHolonId2 { get; set; } //For use if they need quick access to multiple parent holons.
        public Guid ParentHolonId3 { get; set; } //For use if they need quick access to multiple parent holons.
        public List<Guid> ParentHolonIds { get; set; } //Used if they need more than 3 parent holons.
        IHolon ParentHolon { get; set; }
        IHolon ParentHolon2 { get; set; }
        IHolon ParentHolon3 { get; set; }
        IList<IHolon> Children { get; set; } //Allows any holon to add any number of custom child holons to it.
        IReadOnlyCollection<IHolon> AllChildren { get; } //Readonly collection of all the total children including all the zomes, celestialbodies, celestialspaces, moons, holons, planets, stars etc belong to the holon.
        string ChildIdListCache { get; set; } //This will store the list of id's for the direct childen of this holon.
        string AllChildIdListCache { get; set; } //This will store the list of id's for the ALL the childen of this holon (including all sub-childen).
    }
}
