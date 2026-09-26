using System;
using System.Linq;

namespace OGEditorSDK.MapFormat
{
    /// <summary>
    /// Main entry point for map format conversion.
    /// Pipeline: Read → Remap entities → Approximate geometry → Remap textures → Validate → Write
    /// </summary>
    public class OGMapConverter
    {
        private readonly OGMapFormatRegistry _registry;

        public OGMapConverter() : this(OGMapFormatRegistry.Instance) { }
        public OGMapConverter(OGMapFormatRegistry registry) { _registry = registry; }

        /// <summary>
        /// Convert a map file from one format to another.
        /// Returns a result with fidelity score and any diagnostics.
        /// </summary>
        public OGMapConversionResult Convert(
            string srcPath, string srcFormatId,
            string dstPath, string dstFormatId)
        {
            var result = new OGMapConversionResult
            {
                SourceFormat      = srcFormatId,
                DestinationFormat = dstFormatId,
                OutputPath        = dstPath
            };

            if (!_registry.TryGet(srcFormatId, out var srcAdapter))
            {
                result.Diagnostics.Add(new OGConversionDiagnostic(DiagnosticSeverity.Error,
                    $"Unknown source format '{srcFormatId}'."));
                return result;
            }
            if (!_registry.TryGet(dstFormatId, out var dstAdapter))
            {
                result.Diagnostics.Add(new OGConversionDiagnostic(DiagnosticSeverity.Error,
                    $"Unknown destination format '{dstFormatId}'."));
                return result;
            }

            result.Fidelity = dstAdapter.ConversionFidelity(srcAdapter.Family);

            // 1. Read
            OGMapIR ir;
            try { ir = srcAdapter.Read(srcPath); }
            catch (OGMapReadException ex)
            {
                result.Diagnostics.Add(new OGConversionDiagnostic(DiagnosticSeverity.Error,
                    $"Failed to read map: {ex.Message}"));
                return result;
            }

            // 2. Remap entity classnames via OASIS thing type system
            OGEntityRemapper.Remap(ir, srcFormatId, dstFormatId, result.Diagnostics);

            // 3. Approximate geometry if families differ
            if (srcAdapter.Family != dstAdapter.Family)
            {
                result.Diagnostics.Add(new OGConversionDiagnostic(DiagnosticSeverity.Warning,
                    $"Cross-family conversion ({srcAdapter.Family} → {dstAdapter.Family}). " +
                    $"Fidelity ~{result.Fidelity:P0}. Manual cleanup will be required."));
                OGGeometryApproximator.Approximate(ir, srcAdapter.Family, dstAdapter.Family);
            }

            // 4. Remap textures
            OGTextureRemapper.Remap(ir, srcAdapter, dstAdapter);

            // 5. Validate for destination format
            foreach (var d in dstAdapter.ValidateForWrite(ir))
                result.Diagnostics.Add(d);

            // 6. Write (skip only on hard errors)
            if (result.Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error))
            {
                try
                {
                    dstAdapter.Write(ir, dstPath);
                    result.Success = true;
                }
                catch (OGMapWriteException ex)
                {
                    result.Diagnostics.Add(new OGConversionDiagnostic(DiagnosticSeverity.Error,
                        $"Failed to write map: {ex.Message}"));
                }
            }

            return result;
        }

        /// <summary>
        /// Convenience overload: auto-detect source format from file extension.
        /// </summary>
        public OGMapConversionResult Convert(string srcPath, string dstPath, string dstFormatId)
        {
            var adapter = _registry.DetectFromFile(srcPath);
            if (adapter == null)
                throw new InvalidOperationException(
                    $"Cannot auto-detect format for '{srcPath}'. Specify srcFormatId explicitly.");
            return Convert(srcPath, adapter.FormatId, dstPath, dstFormatId);
        }

        /// <summary>Query fidelity without performing a conversion.</summary>
        public float GetFidelity(string srcFormatId, string dstFormatId)
        {
            if (!_registry.TryGet(srcFormatId, out var src)) return 0f;
            if (!_registry.TryGet(dstFormatId, out var dst)) return 0f;
            return dst.ConversionFidelity(src.Family);
        }
    }
}
