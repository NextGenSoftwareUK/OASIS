using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface ISemanticHolon : IHolonBase
    {
        IHolon ParentHolon { get; set; }
        Guid ParentHolonId { get; set; }
        IList<IHolon> Children { get; set; } //Allows any holon to add any number of custom child holons to it.
        IReadOnlyCollection<IHolon> AllChildren { get; } //Readonly collection of all the total children including all the zomes, celestialbodies, celestialspaces, moons, holons, planets, stars etc belong to the holon.
        string ChildIdListCache { get; set; } //This will store the list of id's for the direct childen of this holon.
        string AllChildIdListCache { get; set; } //This will store the list of id's for the ALL the childen of this holon (including all sub-childen).
    }
}
