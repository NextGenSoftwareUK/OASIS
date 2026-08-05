using System.Collections.Generic;

namespace OGEditorSDK.MapFormat
{
    /// <summary>
    /// Implement this interface and ship as a DLL in the OASIS format-adapters folder
    /// to add support for a new map format. The OGMapFormatRegistry discovers it
    /// automatically via the OGMapFormatAdapterAttribute.
    /// </summary>
    public interface IOGMapFormatAdapter
    {
        // ── Identity ─────────────────────────────────────────────────────────

        /// <summary>Short stable identifier, e.g. "quake2", "doom", "goldsrc".</summary>
        string FormatId { get; }

        /// <summary>Human-readable name shown in the OASIS UI.</summary>
        string DisplayName { get; }

        /// <summary>File extensions this adapter handles, e.g. [".map", ".bsp"].</summary>
        string[] FileExtensions { get; }

        /// <summary>Geometry family — determines cross-family fidelity expectations.</summary>
        GeometryFamily Family { get; }

        // ── Read ──────────────────────────────────────────────────────────────

        /// <summary>Returns true if this adapter can read the given file.</summary>
        bool CanRead(string filePath);

        /// <summary>
        /// Read the map file and return a populated OGMapIR.
        /// Throw OGMapReadException on unrecoverable parse errors.
        /// </summary>
        OGMapIR Read(string filePath);

        // ── Write ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Write the OGMapIR to the destination format at outputPath.
        /// Call ValidateForWrite first and show any Error diagnostics to the user.
        /// </summary>
        void Write(OGMapIR map, string outputPath);

        /// <summary>
        /// Validate an OGMapIR before writing. Returns warnings and errors.
        /// All Error-severity diagnostics must be shown to the user before writing.
        /// </summary>
        IEnumerable<OGConversionDiagnostic> ValidateForWrite(OGMapIR map);

        // ── Fidelity ──────────────────────────────────────────────────────────

        /// <summary>
        /// Estimated fidelity of converting FROM the given source geometry family
        /// INTO this format's family. 1.0 = lossless; 0.5 = heavy manual cleanup needed.
        /// </summary>
        float ConversionFidelity(GeometryFamily sourceFamily);

        // ── Texture remapping (optional) ──────────────────────────────────────

        /// <summary>
        /// Map a texture name from a source format to this format's equivalent.
        /// Return null to keep the original name unchanged.
        /// Default implementation returns null (no remapping).
        /// </summary>
        string RemapTexture(string sourceTexture, string sourceFormatId);
    }
}
