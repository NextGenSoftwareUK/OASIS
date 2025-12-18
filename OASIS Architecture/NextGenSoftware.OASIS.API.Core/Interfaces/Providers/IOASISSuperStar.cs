
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISSuperStar : IOASISStorageProvider
    {
        /// <summary>
        /// Allows a provider to generate any native code/artifacts it needs for a given OAPP/celestial body.
        /// For example, the HoloOASIS provider can generate Rust DNA/zome code into the specified output folder
        /// using the supplied source/template information.
        /// </summary>
        /// <param name="celestialBody">The root celestial body for the OAPP (may be null for ZomesAndHolonsOnly).</param>
        /// <param name="outputFolder">The root folder where generated code for this OAPP is being written.</param>
        /// <param name="nativeSource">A provider-specific source payload (for HoloOASIS this is currently the Rust lib.rs buffer).</param>
        /// <returns>True if generation succeeded, otherwise false.</returns>
        bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource);
    }
}
