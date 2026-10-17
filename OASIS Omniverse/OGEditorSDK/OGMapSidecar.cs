// OGMapSidecar — per-map JSON sidecar that records cross-game portal destinations and
// cross-game entity placements for the OASIS Omniverse kernel to read at runtime.
//
// Sidecar file lives alongside the map file: oasis_{mapBaseName}.json
// Hand-serialised JSON — no external dependencies, compatible with .NET Standard 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace OGEditorSDK
{
    /// <summary>A cross-game portal entry written into the map sidecar.</summary>
    public sealed class PortalEntry
    {
        public int    ThingId         { get; set; }
        public double X               { get; set; }
        public double Y               { get; set; }
        public string DestinationGame { get; set; }
        public string DestinationMap  { get; set; }
        public double DestinationX    { get; set; }
        public double DestinationY    { get; set; }
        public double DestinationZ    { get; set; }
    }

    /// <summary>A cross-game entity placed in this map (from another OGame's catalog).</summary>
    public sealed class CrossGameEntityEntry
    {
        public int    ThingId     { get; set; }
        public int    ThingType   { get; set; }
        public string SourceGame  { get; set; }
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Reads and writes the per-map OASIS sidecar JSON.
    /// Thread-safety: callers must serialize concurrent writes to the same sidecar path.
    /// </summary>
    public static class OGMapSidecar
    {
        /// <summary>Return the sidecar path for a given map file path.</summary>
        public static string GetSidecarPath(string mapFilePath)
        {
            string dir  = Path.GetDirectoryName(mapFilePath) ?? ".";
            string name = Path.GetFileNameWithoutExtension(mapFilePath);
            return Path.Combine(dir, "oasis_" + name + ".json");
        }

        /// <summary>Append a portal entry to the sidecar (creates the file if absent).</summary>
        public static void AppendPortal(string mapFilePath, PortalEntry entry)
        {
            var sidecar = Load(mapFilePath);
            sidecar.Portals.Add(entry);
            Save(mapFilePath, sidecar);
        }

        /// <summary>Append a cross-game entity entry to the sidecar (creates the file if absent).</summary>
        public static void AppendCrossGameEntity(string mapFilePath, CrossGameEntityEntry entry)
        {
            var sidecar = Load(mapFilePath);
            sidecar.CrossGameEntities.Add(entry);
            Save(mapFilePath, sidecar);
        }

        // ── Read ──────────────────────────────────────────────────────────────────

        public static OGSidecarData Load(string mapFilePath)
        {
            string path = GetSidecarPath(mapFilePath);
            if (!File.Exists(path)) return new OGSidecarData();
            string json = File.ReadAllText(path, Encoding.UTF8);
            return Parse(json);
        }

        // ── Write ─────────────────────────────────────────────────────────────────

        public static void Save(string mapFilePath, OGSidecarData data)
        {
            string path = GetSidecarPath(mapFilePath);
            File.WriteAllText(path, Serialise(data), Encoding.UTF8);
        }

        // ── Serialisation (hand-rolled — no System.Text.Json / Newtonsoft dep) ────

        private static string Serialise(OGSidecarData d)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"portals\": [");
            for (int i = 0; i < d.Portals.Count; i++)
            {
                var p = d.Portals[i];
                sb.Append("    {");
                sb.Append(" \"thingId\": "        + p.ThingId + ",");
                sb.Append(" \"x\": "              + p.X.ToString("F3") + ",");
                sb.Append(" \"y\": "              + p.Y.ToString("F3") + ",");
                sb.Append(" \"destinationGame\": \"" + Esc(p.DestinationGame) + "\",");
                sb.Append(" \"destinationMap\": \""  + Esc(p.DestinationMap)  + "\",");
                sb.Append(" \"destinationX\": "  + p.DestinationX.ToString("F3") + ",");
                sb.Append(" \"destinationY\": "  + p.DestinationY.ToString("F3") + ",");
                sb.Append(" \"destinationZ\": "  + p.DestinationZ.ToString("F3"));
                sb.Append(" }");
                if (i < d.Portals.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ],");
            sb.AppendLine("  \"crossGameEntities\": [");
            for (int i = 0; i < d.CrossGameEntities.Count; i++)
            {
                var e = d.CrossGameEntities[i];
                sb.Append("    {");
                sb.Append(" \"thingId\": "    + e.ThingId + ",");
                sb.Append(" \"thingType\": "  + e.ThingType + ",");
                sb.Append(" \"sourceGame\": \"" + Esc(e.SourceGame) + "\",");
                sb.Append(" \"displayName\": \"" + Esc(e.DisplayName) + "\"");
                sb.Append(" }");
                if (i < d.CrossGameEntities.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.Append("}");
            return sb.ToString();
        }

        private static string Esc(string s) =>
            (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        // ── Minimal JSON parser (Regex-based, sufficient for our own output) ───────

        private static OGSidecarData Parse(string json)
        {
            var d = new OGSidecarData();
            foreach (Match pm in Regex.Matches(json, @"\{[^{}]*""thingId""[^{}]*""destinationGame""[^{}]*\}"))
            {
                d.Portals.Add(new PortalEntry
                {
                    ThingId         = Int("thingId", pm.Value),
                    X               = Dbl("x", pm.Value),
                    Y               = Dbl("y", pm.Value),
                    DestinationGame = Str("destinationGame", pm.Value),
                    DestinationMap  = Str("destinationMap", pm.Value),
                    DestinationX    = Dbl("destinationX", pm.Value),
                    DestinationY    = Dbl("destinationY", pm.Value),
                    DestinationZ    = Dbl("destinationZ", pm.Value),
                });
            }
            foreach (Match em in Regex.Matches(json, @"\{[^{}]*""thingId""[^{}]*""sourceGame""[^{}]*\}"))
            {
                d.CrossGameEntities.Add(new CrossGameEntityEntry
                {
                    ThingId     = Int("thingId", em.Value),
                    ThingType   = Int("thingType", em.Value),
                    SourceGame  = Str("sourceGame", em.Value),
                    DisplayName = Str("displayName", em.Value),
                });
            }
            return d;
        }

        private static int    Int(string key, string json) { var m = Regex.Match(json, "\"" + key + "\":\\s*(-?\\d+)"); return m.Success ? int.Parse(m.Groups[1].Value) : 0; }
        private static double Dbl(string key, string json) { var m = Regex.Match(json, "\"" + key + "\":\\s*(-?[\\d.]+)"); return m.Success ? double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0; }
        private static string Str(string key, string json) { var m = Regex.Match(json, "\"" + key + "\":\\s*\"([^\"]*)\""); return m.Success ? m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\") : ""; }
    }

    /// <summary>In-memory representation of one map's OASIS sidecar.</summary>
    public sealed class OGSidecarData
    {
        public List<PortalEntry>          Portals           { get; } = new List<PortalEntry>();
        public List<CrossGameEntityEntry> CrossGameEntities { get; } = new List<CrossGameEntityEntry>();
    }
}
