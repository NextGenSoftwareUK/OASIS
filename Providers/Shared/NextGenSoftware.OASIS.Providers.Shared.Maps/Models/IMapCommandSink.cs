namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Models
{
    /// <summary>
    /// A live renderer (a Unity map component, a MapLibre/Google/HERE web view bridge)
    /// that applies map commands as they are produced. Passed to
    /// <c>IOASISMapProvider.Initialize</c>.
    /// </summary>
    public interface IMapCommandSink
    {
        /// <summary>Applies the command; returns false if the renderer rejected it.</summary>
        bool Apply(MapCommand command);
    }
}
