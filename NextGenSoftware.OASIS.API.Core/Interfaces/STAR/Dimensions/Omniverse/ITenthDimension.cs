
namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ITenthDimension : IDimension
    {
        //TODO: Eighth Dimension and above is at the Omiverse level so spans ALL Multiverses/Universes so not sure what we would have here? Needs more thought...
        public ISuperVerse SuperVerse { get; set; }
    }
}