using System;
using System.Collections.Generic;
using OGEditorSDK;

namespace OGEditorSDK.MapFormat
{
    /// <summary>
    /// Remaps entity classnames between game formats via the OASIS thing type system.
    /// OASIS entities (thing type >= 5000) are preserved by thing type across all formats.
    /// Non-OASIS entities with no mapping are preserved as info_oasis_unknown.
    /// </summary>
    public static class OGEntityRemapper
    {
        public static OGMapIR Remap(OGMapIR map, string srcFormatId, string dstFormatId,
            List<OGConversionDiagnostic> diagnostics = null)
        {
            if (srcFormatId == dstFormatId) return map;

            foreach (var e in map.PointEntities)
                RemapEntity(e, srcFormatId, dstFormatId, diagnostics);
            foreach (var e in map.BrushEntities)
                RemapEntity(e, srcFormatId, dstFormatId, diagnostics);

            return map;
        }

        private static void RemapEntity(OGEntity e, string src, string dst,
            List<OGConversionDiagnostic> diagnostics)
        {
            // If already tagged with an OASIS thing type, use that
            if (e.OASISThingType > 0)
            {
                var dstClass = OGEntityMappings.ThingTypeToDstClassname(e.OASISThingType, dst);
                if (dstClass != null)
                    e.Classname = dstClass;
                return;
            }

            // Try to find an OASIS thing type for this classname
            var thingType = OGEntityMappings.ClassnameToDstThingType(e.Classname, src);
            if (thingType > 0)
            {
                e.OASISThingType = thingType;
                var dstClass = OGEntityMappings.ThingTypeToDstClassname(thingType, dst);
                if (dstClass != null)
                {
                    e.Classname = dstClass;
                    return;
                }
            }

            // No mapping — preserve as unknown but don't destroy the entity
            diagnostics?.Add(new OGConversionDiagnostic(DiagnosticSeverity.Warning,
                $"No {dst} equivalent for entity '{e.Classname}' — preserved as info_oasis_unknown")
            {
                EntityClass = e.Classname,
                Location    = e.Origin
            });

            // Stash original classname in a key so it can be round-tripped
            e.Keys["oasis_original_classname"] = e.Classname;
            e.Keys["oasis_original_format"]    = src;
            e.Classname = "info_oasis_unknown";
        }
    }
}
