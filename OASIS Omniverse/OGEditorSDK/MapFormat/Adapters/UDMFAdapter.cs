using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace OGEditorSDK.MapFormat.Adapters
{
    /// <summary>
    /// Adapter for the Universal Doom Map Format (UDMF) text-based Sector2D map format.
    /// Supports reading and writing the standard doom/doom2 UDMF namespace.
    /// </summary>
    public class UDMFAdapter : IOGMapFormatAdapter
    {
        public string         FormatId       => "udmf";
        public string         DisplayName    => "UDMF";
        public string[]       FileExtensions => new[] { ".udmf" };
        public GeometryFamily Family         => GeometryFamily.Sector2D;

        // ── CanRead ───────────────────────────────────────────────────────────

        public bool CanRead(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            try
            {
                // UDMF is text; read enough to find "namespace"
                using var sr = new StreamReader(filePath, Encoding.UTF8);
                var buf = new char[512];
                int read = sr.Read(buf, 0, buf.Length);
                string head = new string(buf, 0, read);
                return head.IndexOf("namespace", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        // ── Read ──────────────────────────────────────────────────────────────

        public OGMapIR Read(string filePath)
        {
            string text = File.ReadAllText(filePath, Encoding.UTF8);
            var tokens  = Tokenize(text, filePath);
            int pos     = 0;

            // Raw parsed lists (indexed)
            var rawVerts   = new List<Dictionary<string, string>>();
            var rawSectors = new List<Dictionary<string, string>>();
            var rawSides   = new List<Dictionary<string, string>>();
            var rawLines   = new List<Dictionary<string, string>>();
            var rawThings  = new List<Dictionary<string, string>>();

            while (pos < tokens.Count)
            {
                string tok = tokens[pos];
                if (tok == "namespace")
                {
                    // namespace = "doom"; — skip to ';'
                    while (pos < tokens.Count && tokens[pos] != ";") pos++;
                    pos++; // skip ';'
                    continue;
                }

                string blockType = tok.ToLowerInvariant();
                pos++;
                if (pos >= tokens.Count || tokens[pos] != "{") { pos++; continue; }
                pos++; // skip '{'

                var fields = ParseBlock(tokens, ref pos);

                switch (blockType)
                {
                    case "vertex":  rawVerts.Add(fields);   break;
                    case "sector":  rawSectors.Add(fields); break;
                    case "sidedef": rawSides.Add(fields);   break;
                    case "linedef": rawLines.Add(fields);   break;
                    case "thing":   rawThings.Add(fields);  break;
                    // skip unknown block types
                }
            }

            // Build OGSector list
            var sectors = new List<OGSector>(rawSectors.Count);
            foreach (var f in rawSectors)
            {
                sectors.Add(new OGSector
                {
                    FloorHeight   = GetFloat(f, "heightfloor",   0),
                    CeilingHeight = GetFloat(f, "heightceiling", 128),
                    FloorTexture  = GetStr(f,  "texturefloor",  "FLOOR4_8"),
                    CeilingTexture= GetStr(f,  "textureceiling","CEIL3_5"),
                    LightLevel    = GetInt(f,   "lightlevel",   160),
                    SectorSpecial = GetInt(f,   "special",      0),
                    SectorTag     = GetInt(f,   "tag",          0)
                });
            }

            // Build vertex list
            var verts = new List<OGVertex2D>(rawVerts.Count);
            foreach (var f in rawVerts)
                verts.Add(new OGVertex2D { X = GetFloat(f, "x", 0), Y = GetFloat(f, "y", 0) });

            // Process linedefs → assign to sectors via sidefront
            foreach (var f in rawLines)
            {
                int v1idx = GetInt(f, "v1", -1);
                int v2idx = GetInt(f, "v2", -1);
                int sfIdx = GetInt(f, "sidefront", -1);
                int sbIdx = GetInt(f, "sideback",  -1);

                var ld = new OGLinedef
                {
                    Flags   = GetInt(f, "flags",   0),
                    Special = GetInt(f, "special", 0),
                    Tag     = GetInt(f, "tag",     0)
                };

                if (v1idx >= 0 && v1idx < verts.Count) ld.Start = verts[v1idx];
                if (v2idx >= 0 && v2idx < verts.Count) ld.End   = verts[v2idx];

                // Front sidedef
                if (sfIdx >= 0 && sfIdx < rawSides.Count)
                {
                    var sf = rawSides[sfIdx];
                    ld.UpperTexture  = EmptyToNull(GetStr(sf, "texturetop",    "-"));
                    ld.MiddleTexture = EmptyToNull(GetStr(sf, "texturemiddle", "-"));
                    ld.LowerTexture  = EmptyToNull(GetStr(sf, "texturebottom", "-"));
                    ld.OffsetX       = GetFloat(sf, "offsetx", 0);
                    ld.OffsetY       = GetFloat(sf, "offsety", 0);

                    int secRef = GetInt(sf, "sector", -1);
                    if (secRef >= 0 && secRef < sectors.Count)
                        sectors[secRef].Linedefs.Add(ld);
                }

                // Back sidedef
                if (sbIdx >= 0 && sbIdx < rawSides.Count)
                {
                    var sb = rawSides[sbIdx];
                    ld.BackUpperTexture  = EmptyToNull(GetStr(sb, "texturetop",    "-"));
                    ld.BackMiddleTexture = EmptyToNull(GetStr(sb, "texturemiddle", "-"));
                    ld.BackLowerTexture  = EmptyToNull(GetStr(sb, "texturebottom", "-"));
                }
            }

            // Things → point entities
            var pointEnts = new List<OGPointEntity>(rawThings.Count);
            foreach (var f in rawThings)
            {
                int type = GetInt(f, "type", 0);
                var pe   = new OGPointEntity
                {
                    Classname = $"udmf_thing_{type:D4}",
                    Origin    = new OGVector3(GetFloat(f, "x", 0), GetFloat(f, "y", 0),
                                             GetFloat(f, "height", 0)),
                    Angle     = GetFloat(f, "angle", 0)
                };
                pe.Keys["type"] = type.ToString();
                pointEnts.Add(pe);
            }

            var ir = new OGMapIR
            {
                MapName      = Path.GetFileNameWithoutExtension(filePath),
                SourceFormat = FormatId,
                Metadata     = new OGMapMetadata()
            };
            ir.PointEntities.AddRange(pointEnts);
            foreach (var sec in sectors) ir.WorldGeometry.Add(sec);
            return ir;
        }

        // ── Write ─────────────────────────────────────────────────────────────

        public void Write(OGMapIR map, string outputPath)
        {
            var sectors = new List<OGSector>();
            foreach (var p in map.WorldGeometry)
                if (p is OGSector s) sectors.Add(s);

            // Build global vertex list (deduped)
            var vertMap  = new Dictionary<long, int>();
            var vertList = new List<OGVertex2D>();
            int GetOrAddVert(OGVertex2D v)
            {
                long key = ((long)(short)v.X & 0xFFFF) | (((long)(short)v.Y & 0xFFFF) << 16);
                if (vertMap.TryGetValue(key, out int idx)) return idx;
                idx = vertList.Count;
                vertMap[key] = idx;
                vertList.Add(v);
                return idx;
            }

            // Collect unique linedefs with front sector index
            var ldList     = new List<OGLinedef>();
            var ldFrontSec = new Dictionary<OGLinedef, int>(RefEqComparer<OGLinedef>.Instance);
            for (int si = 0; si < sectors.Count; si++)
                foreach (var ld in sectors[si].Linedefs)
                    if (!ldFrontSec.ContainsKey(ld))
                    { ldFrontSec[ld] = si; ldList.Add(ld); }

            // Edge→sector for back-sector resolution
            var edgeSec = new Dictionary<long, int>();
            for (int si = 0; si < sectors.Count; si++)
                foreach (var ld in sectors[si].Linedefs)
                    if (ld.Start != null && ld.End != null)
                        edgeSec[EdgeKey((short)ld.Start.X, (short)ld.Start.Y,
                                        (short)ld.End.X,   (short)ld.End.Y)] = si;

            // Pre-touch vertices so indices are assigned
            foreach (var ld in ldList)
            {
                if (ld.Start != null) GetOrAddVert(ld.Start);
                if (ld.End   != null) GetOrAddVert(ld.End);
            }

            var sb = new StringBuilder();
            sb.AppendLine("namespace = \"doom\";");
            sb.AppendLine();

            // Vertices
            foreach (var v in vertList)
                sb.AppendLine($"vertex {{ x = {F(v.X)}; y = {F(v.Y)}; }}");
            sb.AppendLine();

            // Sectors
            foreach (var sec in sectors)
            {
                sb.AppendLine("sector {");
                sb.AppendLine($"\theightfloor = {(int)sec.FloorHeight};");
                sb.AppendLine($"\theightceiling = {(int)sec.CeilingHeight};");
                sb.AppendLine($"\ttexturefloor = \"{sec.FloorTexture ?? "FLOOR4_8"}\";");
                sb.AppendLine($"\ttextureceiling = \"{sec.CeilingTexture ?? "CEIL3_5"}\";");
                sb.AppendLine($"\tlightlevel = {sec.LightLevel};");
                sb.AppendLine($"\tspecial = {sec.SectorSpecial};");
                sb.AppendLine($"\ttag = {sec.SectorTag};");
                sb.AppendLine("}");
            }
            sb.AppendLine();

            // Sidedefs + linedefs together
            int sideIdx = 0;
            foreach (var ld in ldList)
            {
                int v1 = ld.Start != null ? GetOrAddVert(ld.Start) : 0;
                int v2 = ld.End   != null ? GetOrAddVert(ld.End)   : 0;
                int fSec = ldFrontSec.TryGetValue(ld, out int fi) ? fi : 0;

                int sfIdx = sideIdx++;
                sb.AppendLine("sidedef {");
                sb.AppendLine($"\tsector = {fSec};");
                sb.AppendLine($"\ttexturetop = \"{NullToMinus(ld.UpperTexture)}\";");
                sb.AppendLine($"\ttexturemiddle = \"{NullToMinus(ld.MiddleTexture)}\";");
                sb.AppendLine($"\ttexturebottom = \"{NullToMinus(ld.LowerTexture)}\";");
                sb.AppendLine($"\toffsetx = {(int)ld.OffsetX};");
                sb.AppendLine($"\toffsety = {(int)ld.OffsetY};");
                sb.AppendLine("}");

                int sbIdx2 = -1;
                if (ld.TwoSided)
                {
                    int backSec = 0;
                    if (ld.Start != null && ld.End != null)
                    {
                        long rk = EdgeKey((short)ld.End.X, (short)ld.End.Y,
                                          (short)ld.Start.X, (short)ld.Start.Y);
                        edgeSec.TryGetValue(rk, out backSec);
                    }
                    sbIdx2 = sideIdx++;
                    sb.AppendLine("sidedef {");
                    sb.AppendLine($"\tsector = {backSec};");
                    sb.AppendLine($"\ttexturetop = \"{NullToMinus(ld.BackUpperTexture)}\";");
                    sb.AppendLine($"\ttexturemiddle = \"{NullToMinus(ld.BackMiddleTexture)}\";");
                    sb.AppendLine($"\ttexturebottom = \"{NullToMinus(ld.BackLowerTexture)}\";");
                    sb.AppendLine("\toffsetx = 0;");
                    sb.AppendLine("\toffsety = 0;");
                    sb.AppendLine("}");
                }

                sb.AppendLine("linedef {");
                sb.AppendLine($"\tv1 = {v1};");
                sb.AppendLine($"\tv2 = {v2};");
                sb.AppendLine($"\tsidefront = {sfIdx};");
                if (sbIdx2 >= 0)
                {
                    sb.AppendLine($"\tsideback = {sbIdx2};");
                    sb.AppendLine("\ttwosided = true;");
                }
                else
                {
                    sb.AppendLine("\ttwosided = false;");
                }
                sb.AppendLine($"\tflags = {ld.Flags};");
                sb.AppendLine($"\tspecial = {ld.Special};");
                sb.AppendLine($"\ttag = {ld.Tag};");
                sb.AppendLine("}");
            }
            sb.AppendLine();

            // Things
            foreach (var pe in map.PointEntities)
            {
                string cn = pe.Classname ?? "";
                int type = 0;
                if (cn.StartsWith("udmf_thing_", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(cn.Substring(11), out type);
                else if (cn.StartsWith("doom_thing_", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(cn.Substring(11), out type);
                else
                    int.TryParse(pe.GetKey("type", "0"), out type);

                sb.AppendLine("thing {");
                sb.AppendLine($"\tx = {F(pe.Origin.X)};");
                sb.AppendLine($"\ty = {F(pe.Origin.Y)};");
                sb.AppendLine($"\theight = {F(pe.Origin.Z)};");
                sb.AppendLine($"\tangle = {(int)pe.Angle};");
                sb.AppendLine($"\ttype = {type};");
                sb.AppendLine("}");
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        // ── ValidateForWrite ──────────────────────────────────────────────────

        public IEnumerable<OGConversionDiagnostic> ValidateForWrite(OGMapIR map)
        {
            bool hasBrush = false;
            foreach (var p in map.WorldGeometry)
                if (p is OGBrush) { hasBrush = true; break; }
            if (!hasBrush)
                foreach (var be in map.BrushEntities)
                    foreach (var p in be.Geometry)
                        if (p is OGBrush) { hasBrush = true; break; }
            if (hasBrush)
                yield return new OGConversionDiagnostic(DiagnosticSeverity.Warning,
                    "OGBrush geometry (Brush3D family) is not supported by UDMF and will be ignored.");
        }

        public float ConversionFidelity(GeometryFamily src)
        {
            switch (src)
            {
                case GeometryFamily.Sector2D: return 1.0f;
                case GeometryFamily.Build2D:  return 0.70f;
                case GeometryFamily.Brush3D:  return 0.55f;
                case GeometryFamily.Tile2D:   return 0.20f;
                default:                      return 0.50f;
            }
        }

        public string RemapTexture(string sourceTexture, string sourceFormatId) => null;

        // ── Tokenizer ─────────────────────────────────────────────────────────

        private static List<string> Tokenize(string text, string filePath)
        {
            var tokens = new List<string>();
            int i = 0, len = text.Length;

            while (i < len)
            {
                char c = text[i];

                // Whitespace
                if (char.IsWhiteSpace(c)) { i++; continue; }

                // Line comment
                if (c == '/' && i + 1 < len && text[i + 1] == '/')
                {
                    while (i < len && text[i] != '\n') i++;
                    continue;
                }

                // Block comment
                if (c == '/' && i + 1 < len && text[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < len && !(text[i] == '*' && text[i + 1] == '/')) i++;
                    i += 2;
                    continue;
                }

                // Quoted string
                if (c == '"')
                {
                    i++;
                    var sb = new StringBuilder();
                    while (i < len && text[i] != '"') sb.Append(text[i++]);
                    if (i < len) i++; // closing "
                    tokens.Add(sb.ToString());
                    continue;
                }

                // Single-character punctuation
                if (c == '{' || c == '}' || c == '=' || c == ';')
                {
                    tokens.Add(c.ToString());
                    i++;
                    continue;
                }

                // Identifier or number
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '+' || c == '.')
                {
                    var sb = new StringBuilder();
                    while (i < len && (char.IsLetterOrDigit(text[i]) || text[i] == '_'
                           || text[i] == '-' || text[i] == '+' || text[i] == '.'))
                        sb.Append(text[i++]);
                    tokens.Add(sb.ToString());
                    continue;
                }

                i++; // skip unknown character
            }
            return tokens;
        }

        private static Dictionary<string, string> ParseBlock(List<string> tokens, ref int pos)
        {
            // pos is right after '{'
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (pos < tokens.Count)
            {
                string t = tokens[pos];
                if (t == "}") { pos++; break; }
                if (t == "{") { pos++; continue; } // nested block — skip

                // key = value ;
                string key = t; pos++;
                if (pos < tokens.Count && tokens[pos] == "=") pos++; // skip '='
                string val = pos < tokens.Count ? tokens[pos++] : "";
                if (pos < tokens.Count && tokens[pos] == ";") pos++; // skip ';'
                fields[key] = val;
            }
            return fields;
        }

        // ── Field helpers ─────────────────────────────────────────────────────

        private static string GetStr(Dictionary<string, string> f, string k, string def) =>
            f.TryGetValue(k, out string v) ? v : def;

        private static int GetInt(Dictionary<string, string> f, string k, int def)
        {
            if (!f.TryGetValue(k, out string v)) return def;
            if (int.TryParse(v, out int r)) return r;
            if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float rf)) return (int)rf;
            return def;
        }

        private static float GetFloat(Dictionary<string, string> f, string k, float def)
        {
            if (!f.TryGetValue(k, out string v)) return def;
            if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float r)) return r;
            return def;
        }

        private static string EmptyToNull(string s) =>
            string.IsNullOrEmpty(s) || s == "-" ? null : s;

        private static string NullToMinus(string s) =>
            string.IsNullOrEmpty(s) ? "-" : s;

        private static string F(float v) =>
            v.ToString("0.000", CultureInfo.InvariantCulture);

        private static long EdgeKey(short x1, short y1, short x2, short y2) =>
            ((long)(ushort)x1)
            | ((long)(ushort)y1 << 16)
            | ((long)(ushort)x2 << 32)
            | ((long)(ushort)y2 << 48);

        private sealed class RefEqComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly RefEqComparer<T> Instance = new RefEqComparer<T>();
            public bool Equals(T x, T y)   => ReferenceEquals(x, y);
            public int  GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
