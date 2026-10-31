using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

/// <summary>Shared graph contract fixture for native SDK/provider tests.</summary>
public sealed class NamedChildOwner : Holon
{
    public List<Holon> Components { get; set; } = new();
    public override ReadOnlyCollection<IHolon> AllChildren
        => Children.Concat<IHolon>(Components).ToList().AsReadOnly();
    public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()
        => base.CaptureChildCollections().Concat(new[] { new HolonChildCollection(nameof(Components), Components,
            rows => Components = rows.Cast<Holon>().ToList()) }).ToArray();
}
