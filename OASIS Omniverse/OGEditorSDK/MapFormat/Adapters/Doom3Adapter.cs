using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace OGEditorSDK.MapFormat.Adapters
{
    /// <summary>
    /// Adapter for the Doom 3 / idTech4 text .map format (Brush3D family).
    /// Supports brushDef3 and patchDef2 primitives. Reads/writes Version 2 files.
    /// </summary>
    public class Doom3Adapter : IOGMapFormatAdapter
    {
        public string         FormatId       => "doom3";
        public string         DisplayName    => "Doom 3 / idTech4";
        public string[]       FileExtensions => new[] { ".map" };
        public GeometryFamily Family         => GeometryFamily.Brush3D;

        // ── CanRead ───────────────────────────────────────────────────────────

        public bool CanRead(string filePath)
        {
            if (!File.Exists(filePath) || !filePath.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
                return false;
            try
            {
                using var sr = new StreamReader(filePath, Encoding.UTF8);
                string first = sr.ReadLine()?.Trim() ?? "";
                return first == "Version 2" || first == "Version 3";
            }
            catch { return false; }
        }

        // ── Read ──────────────────────────────────────────────────────────────

        public OGMapIR Read(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            int pos = 0;

            // Skip "Version N" header
            if (pos < lines.Length && lines[pos].TrimStart().StartsWith("Version")) pos++;

            var ir = new OGMapIR
            {
                MapName      = Path.GetFileNameWithoutExtension(filePath),
                SourceFormat = FormatId,
                Metadata     = new OGMapMetadata()
            };

            int entityIndex = 0;
            while (pos < lines.Length)
            {
                SkipBlankAndComments(lines, ref pos);
                if (pos >= lines.Length) break;
                if (lines[pos].Trim() != "{") { pos++; continue; }
                pos++; // skip opening '{'

                ParseEntity(lines, ref pos, entityIndex, ir, filePath);
                entityIndex++;
            }

            return ir;
        }

        // ── Write ─────────────────────────────────────────────────────────────

        public void Write(OGMapIR map, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Version 2");

            // Entity 0: worldspawn
            sb.AppendLine("// entity 0");
            sb.AppendLine("{");
            sb.AppendLine("\"classname\" \"worldspawn\"");

            int primIdx = 0;
            foreach (var prim in map.WorldGeometry)
            {
                WritePrimitive(sb, prim, ref primIdx);
            }
            sb.AppendLine("}");

            // Brush entities
            int entIdx = 1;
            foreach (var be in map.BrushEntities)
            {
                sb.AppendLine($"// entity {entIdx++}");
                sb.AppendLine("{");
                WriteEntityKeys(sb, be);
                primIdx = 0;
                foreach (var prim in be.Geometry)
                    WritePrimitive(sb, prim, ref primIdx);
                sb.AppendLine("}");
            }

            // Point entities
            foreach (var pe in map.PointEntities)
            {
                sb.AppendLine($"// entity {entIdx++}");
                sb.AppendLine("{");
                sb.AppendLine($"\"classname\" \"{EscapeQuoted(pe.Classname ?? "info_player_start")}\"");
                sb.AppendLine($"\"origin\" \"{V3(pe.Origin)}\"");
                sb.AppendLine($"\"angle\" \"{(int)pe.Angle}\"");
                foreach (var kv in pe.Keys)
                    sb.AppendLine($"\"{EscapeQuoted(kv.Key)}\" \"{EscapeQuoted(kv.Value)}\"");
                sb.AppendLine("}");
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        // ── ValidateForWrite ──────────────────────────────────────────────────

        public IEnumerable<OGConversionDiagnostic> ValidateForWrite(OGMapIR map)
        {
            foreach (var p in map.WorldGeometry)
                if (p is OGSector || p is OGBuildSector || p is OGTile)
                    yield return new OGConversionDiagnostic(DiagnosticSeverity.Warning,
                        $"{p.GetType().Name} (non-Brush3D geometry) will be skipped in Doom3 output.");
        }

        public float ConversionFidelity(GeometryFamily src)
        {
            switch (src)
            {
                case GeometryFamily.Brush3D:  return 0.90f;
                case GeometryFamily.Sector2D: return 0.55f;
                case GeometryFamily.Build2D:  return 0.45f;
                case GeometryFamily.Tile2D:   return 0.20f;
                default:                      return 0.40f;
            }
        }

        public string RemapTexture(string sourceTexture, string sourceFormatId) => null;

        // ── Parse helpers ─────────────────────────────────────────────────────

        private static void SkipBlankAndComments(string[] lines, ref int pos)
        {
            while (pos < lines.Length)
            {
                string t = lines[pos].Trim();
                if (t.Length == 0 || t.StartsWith("//")) pos++;
                else break;
            }
        }

        private void ParseEntity(string[] lines, ref int pos, int entityIndex,
                                 OGMapIR ir, string filePath)
        {
            var keys    = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var brushes = new List<OGBrush>();
            var patches = new List<OGPatch>();

            while (pos < lines.Length)
            {
                string line = lines[pos].Trim();

                if (line == "}")
                {
                    pos++;
                    break;
                }

                if (line.StartsWith("//") || line.Length == 0)
                {
                    pos++;
                    continue;
                }

                // Primitive block: begins with '{'
                if (line == "{")
                {
                    pos++;
                    ParsePrimitive(lines, ref pos, brushes, patches, filePath);
                    continue;
                }

                // Key-value pair: "key" "value"
                if (line.StartsWith("\""))
                {
                    ParseKeyValue(line, keys);
                    pos++;
                    continue;
                }

                pos++;
            }

            // Route geometry to IR
            string classname = keys.TryGetValue("classname", out string cn) ? cn : "";

            if (entityIndex == 0 || string.Equals(classname, "worldspawn", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var b in brushes) ir.WorldGeometry.Add(b);
                foreach (var p in patches) ir.WorldGeometry.Add(p);
            }
            else if (brushes.Count > 0 || patches.Count > 0)
            {
                var be = new OGBrushEntity { Classname = classname };
                CopyKeys(keys, be);
                foreach (var b in brushes) be.Geometry.Add(b);
                foreach (var p in patches) be.Geometry.Add(p);
                ir.BrushEntities.Add(be);
            }
            else
            {
                var pe = new OGPointEntity { Classname = classname };
                CopyKeys(keys, pe);
                if (keys.TryGetValue("origin", out string originStr))
                    pe.Origin = ParseVec3(originStr);
                if (keys.TryGetValue("angle", out string angleStr)
                    && float.TryParse(angleStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float ang))
                    pe.Angle = ang;
                ir.PointEntities.Add(pe);
            }
        }

        private void ParsePrimitive(string[] lines, ref int pos,
                                    List<OGBrush> brushes, List<OGPatch> patches,
                                    string filePath)
        {
            // We are after the outer '{'; look for "brushDef3" or "patchDef2"
            while (pos < lines.Length)
            {
                string line = lines[pos].Trim();
                if (line == "}") { pos++; return; } // empty / done

                if (line == "brushDef3")
                {
                    pos++;
                    brushes.Add(ParseBrushDef3(lines, ref pos, filePath));
                    return;
                }
                if (line == "patchDef2")
                {
                    pos++;
                    var patch = ParsePatchDef2(lines, ref pos, filePath);
                    if (patch != null) patches.Add(patch);
                    return;
                }
                pos++;
            }
        }

        private OGBrush ParseBrushDef3(string[] lines, ref int pos, string filePath)
        {
            var brush = new OGBrush();

            // Expect '{'
            SkipBlankAndComments(lines, ref pos);
            if (pos < lines.Length && lines[pos].Trim() == "{") pos++;

            while (pos < lines.Length)
            {
                string line = lines[pos].Trim();
                if (line == "}") { pos++; break; }
                if (line.Length == 0 || line.StartsWith("//")) { pos++; continue; }

                var face = ParseBrushDef3Face(line);
                if (face != null) brush.Faces.Add(face);
                pos++;
            }

            // Close the outer primitive block '}'
            SkipBlankAndComments(lines, ref pos);
            if (pos < lines.Length && lines[pos].Trim() == "}") pos++;

            return brush;
        }

        /// <summary>
        /// Parses a brushDef3 face line:
        /// ( nx ny nz dist ) ( ( su sv sshift ) ( tu tv tshift ) ) "texture" contents flags value
        /// </summary>
        private static OGBrushFace ParseBrushDef3Face(string line)
        {
            try
            {
                var toks = SplitTokens(line);
                int i = 0;

                // ( nx ny nz dist )
                if (!Expect(toks, ref i, "(")) return null;
                float nx = Float(toks, ref i); float ny = Float(toks, ref i);
                float nz = Float(toks, ref i); float dist= Float(toks, ref i);
                if (!Expect(toks, ref i, ")")) return null;

                // ( ( su sv sshift ) ( tu tv tshift ) )
                if (!Expect(toks, ref i, "(")) return null;
                if (!Expect(toks, ref i, "(")) return null;
                float su = Float(toks, ref i); float sv  = Float(toks, ref i); float ss = Float(toks, ref i);
                if (!Expect(toks, ref i, ")")) return null;
                if (!Expect(toks, ref i, "(")) return null;
                float tu = Float(toks, ref i); float tv  = Float(toks, ref i); float ts = Float(toks, ref i);
                if (!Expect(toks, ref i, ")")) return null;
                if (!Expect(toks, ref i, ")")) return null;

                // "texture"
                string texture = i < toks.Count ? toks[i++] : "textures/common/caulk";
                // Remove surrounding quotes if present
                if (texture.StartsWith("\"") && texture.EndsWith("\"") && texture.Length >= 2)
                    texture = texture.Substring(1, texture.Length - 2);

                var normal = new OGVector3(nx, ny, nz).Normalized();
                var plane  = new OGPlane(normal, dist);

                // Derive three points from the plane for P1/P2/P3
                var (p1, p2, p3) = ThreePointsFromPlane(normal, dist);

                return new OGBrushFace
                {
                    P1      = p1,
                    P2      = p2,
                    P3      = p3,
                    Plane   = plane,
                    Texture = texture,
                    UAxis   = new OGVector3(su, sv, 0),
                    UShift  = ss,
                    VAxis   = new OGVector3(tu, tv, 0),
                    VShift  = ts,
                    Scale   = new OGVector2(1, 1)
                };
            }
            catch { return null; }
        }

        private static OGPatch ParsePatchDef2(string[] lines, ref int pos, string filePath)
        {
            SkipBlankAndComments(lines, ref pos);
            if (pos < lines.Length && lines[pos].Trim() == "{") pos++;

            // Texture line
            string texture = "";
            SkipBlankAndComments(lines, ref pos);
            if (pos < lines.Length)
            {
                texture = lines[pos].Trim().Trim('"');
                pos++;
            }

            // ( rows cols ... ) header
            int rows = 0, cols = 0;
            SkipBlankAndComments(lines, ref pos);
            if (pos < lines.Length)
            {
                var toks = SplitTokens(lines[pos]);
                int ti = 0;
                Expect(toks, ref ti, "(");
                rows = (int)Float(toks, ref ti);
                cols = (int)Float(toks, ref ti);
                pos++;
            }

            if (rows <= 0 || cols <= 0)
            {
                // Skip to closing '}'
                while (pos < lines.Length && lines[pos].Trim() != "}") pos++;
                if (pos < lines.Length) pos++;
                SkipBlankAndComments(lines, ref pos);
                if (pos < lines.Length && lines[pos].Trim() == "}") pos++;
                return null;
            }

            var cp  = new OGVector3[rows, cols];
            var uvs = new OGVector2[rows, cols];

            // Opening '(' for control-point grid
            SkipBlankAndComments(lines, ref pos);
            if (pos < lines.Length && lines[pos].Trim() == "(") pos++;

            for (int r = 0; r < rows; r++)
            {
                SkipBlankAndComments(lines, ref pos);
                if (pos >= lines.Length) break;

                // Row: ( (x y z u v) (x y z u v) ... )
                var toks = SplitTokens(lines[pos]); pos++;
                int ti = 0;
                Expect(toks, ref ti, "("); // outer row paren

                for (int c = 0; c < cols; c++)
                {
                    Expect(toks, ref ti, "(");
                    float x  = Float(toks, ref ti); float y  = Float(toks, ref ti);
                    float z  = Float(toks, ref ti); float u  = Float(toks, ref ti);
                    float v  = Float(toks, ref ti);
                    Expect(toks, ref ti, ")");
                    cp[r, c]  = new OGVector3(x, y, z);
                    uvs[r, c] = new OGVector2(u, v);
                }
            }

            // Close grid ')'
            SkipBlankAndComments(lines, ref pos);
            if (pos < lines.Length && lines[pos].Trim() == ")") pos++;

            // Close patchDef2 '}'
            SkipBlankAndComments(lines, ref pos);
            if (pos < lines.Length && lines[pos].Trim() == "}") pos++;

            // Close outer primitive '}'
            SkipBlankAndComments(lines, ref pos);
            if (pos < lines.Length && lines[pos].Trim() == "}") pos++;

            return new OGPatch
            {
                Texture       = texture,
                Rows          = rows,
                Cols          = cols,
                ControlPoints = cp,
                TexCoords     = uvs
            };
        }

        // ── Write helpers ─────────────────────────────────────────────────────

        private static void WritePrimitive(StringBuilder sb, OGGeometryPrimitive prim, ref int primIdx)
        {
            if (prim is OGBrush brush)
            {
                sb.AppendLine($"// primitive {primIdx++}");
                sb.AppendLine("{");
                sb.AppendLine("brushDef3");
                sb.AppendLine("{");
                foreach (var face in brush.Faces)
                    sb.AppendLine(FormatBrushDef3Face(face));
                sb.AppendLine("}");
                sb.AppendLine("}");
            }
            else if (prim is OGPatch patch)
            {
                sb.AppendLine($"// primitive {primIdx++}");
                sb.AppendLine("{");
                sb.AppendLine("patchDef2");
                sb.AppendLine("{");
                sb.AppendLine($"\"{EscapeQuoted(patch.Texture ?? "textures/common/caulk")}\"");
                int rows = patch.ControlPoints?.GetLength(0) ?? 0;
                int cols = patch.ControlPoints?.GetLength(1) ?? 0;
                sb.AppendLine($"( {rows} {cols} 0 0 0 )");
                sb.AppendLine("(");
                for (int r = 0; r < rows; r++)
                {
                    var row = new StringBuilder("( ");
                    for (int c = 0; c < cols; c++)
                    {
                        var pt = patch.ControlPoints[r, c];
                        var uv = patch.TexCoords != null ? patch.TexCoords[r, c] : new OGVector2(0, 0);
                        row.Append($"( {F(pt.X)} {F(pt.Y)} {F(pt.Z)} {F(uv.X)} {F(uv.Y)} ) ");
                    }
                    row.Append(")");
                    sb.AppendLine(row.ToString());
                }
                sb.AppendLine(")");
                sb.AppendLine("}");
                sb.AppendLine("}");
            }
        }

        private static string FormatBrushDef3Face(OGBrushFace face)
        {
            var n = face.Plane.Normal;
            float d = face.Plane.Distance;

            string uvPart;
            if (face.UAxis.HasValue && face.VAxis.HasValue)
            {
                var u = face.UAxis.Value;
                var v = face.VAxis.Value;
                uvPart = $"( ( {F(u.X)} {F(u.Y)} {F(face.UShift)} ) ( {F(v.X)} {F(v.Y)} {F(face.VShift)} ) )";
            }
            else
            {
                uvPart = $"( ( 0.015625 0 {F(face.UShift)} ) ( 0 0.015625 {F(face.VShift)} ) )";
            }

            string tex = EscapeQuoted(face.Texture ?? "textures/common/caulk");
            return $"( {F(n.X)} {F(n.Y)} {F(n.Z)} {F(d)} ) {uvPart} \"{tex}\" 0 0 0";
        }

        private static void WriteEntityKeys(StringBuilder sb, OGEntity entity)
        {
            sb.AppendLine($"\"classname\" \"{EscapeQuoted(entity.Classname ?? "func_static")}\"");
            sb.AppendLine($"\"origin\" \"{V3(entity.Origin)}\"");
            foreach (var kv in entity.Keys)
                sb.AppendLine($"\"{EscapeQuoted(kv.Key)}\" \"{EscapeQuoted(kv.Value)}\"");
        }

        // ── Token helpers ─────────────────────────────────────────────────────

        private static List<string> SplitTokens(string line)
        {
            var result = new List<string>();
            int i = 0, len = line.Length;
            while (i < len)
            {
                while (i < len && char.IsWhiteSpace(line[i])) i++;
                if (i >= len) break;
                char c = line[i];
                if (c == '"')
                {
                    i++;
                    var sb = new StringBuilder("\"");
                    while (i < len && line[i] != '"') sb.Append(line[i++]);
                    sb.Append('"');
                    if (i < len) i++;
                    result.Add(sb.ToString());
                }
                else if (c == '(' || c == ')')
                {
                    result.Add(c.ToString()); i++;
                }
                else
                {
                    var sb = new StringBuilder();
                    while (i < len && !char.IsWhiteSpace(line[i]) && line[i] != '(' && line[i] != ')')
                        sb.Append(line[i++]);
                    if (sb.Length > 0) result.Add(sb.ToString());
                }
            }
            return result;
        }

        private static bool Expect(List<string> toks, ref int i, string expected)
        {
            if (i < toks.Count && toks[i] == expected) { i++; return true; }
            return false;
        }

        private static float Float(List<string> toks, ref int i)
        {
            if (i >= toks.Count) return 0;
            float.TryParse(toks[i++], NumberStyles.Float, CultureInfo.InvariantCulture, out float v);
            return v;
        }

        private static void ParseKeyValue(string line, Dictionary<string, string> keys)
        {
            // "key" "value"
            int i = 0;
            string key = ReadQuoted(line, ref i);
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            string val = ReadQuoted(line, ref i);
            if (key != null) keys[key] = val ?? "";
        }

        private static string ReadQuoted(string line, ref int i)
        {
            while (i < line.Length && line[i] != '"') i++;
            if (i >= line.Length) return null;
            i++; // skip opening "
            var sb = new StringBuilder();
            while (i < line.Length && line[i] != '"') sb.Append(line[i++]);
            if (i < line.Length) i++; // skip closing "
            return sb.ToString();
        }

        private static void CopyKeys(Dictionary<string, string> keys, OGEntity entity)
        {
            foreach (var kv in keys)
            {
                if (string.Equals(kv.Key, "classname", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(kv.Key, "origin",    StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(kv.Key, "angle",     StringComparison.OrdinalIgnoreCase)) continue;
                entity.Keys[kv.Key] = kv.Value;
            }
        }

        private static OGVector3 ParseVec3(string s)
        {
            var parts = s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            float x = 0, y = 0, z = 0;
            if (parts.Length > 0) float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x);
            if (parts.Length > 1) float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
            if (parts.Length > 2) float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z);
            return new OGVector3(x, y, z);
        }

        /// <summary>Generates three coplanar points for P1/P2/P3 from a normal+distance plane.</summary>
        private static (OGVector3 p1, OGVector3 p2, OGVector3 p3) ThreePointsFromPlane(OGVector3 n, float d)
        {
            // Pick a point on the plane
            OGVector3 p0;
            if (Math.Abs(n.X) >= 0.57735f)
                p0 = new OGVector3(d / n.X, 0, 0);
            else if (Math.Abs(n.Y) >= 0.57735f)
                p0 = new OGVector3(0, d / n.Y, 0);
            else
                p0 = new OGVector3(0, 0, d / (Math.Abs(n.Z) > 1e-6f ? n.Z : 1e-6f));

            // Two tangent vectors
            OGVector3 up = (Math.Abs(n.Z) < 0.9f)
                ? new OGVector3(0, 0, 1)
                : new OGVector3(1, 0, 0);
            OGVector3 t1 = n.Cross(up).Normalized();
            OGVector3 t2 = n.Cross(t1).Normalized();

            return (p0, p0 + t1, p0 + t2);
        }

        private static string F(float v) =>
            v.ToString("G", CultureInfo.InvariantCulture);

        private static string V3(OGVector3 v) =>
            $"{F(v.X)} {F(v.Y)} {F(v.Z)}";

        private static string EscapeQuoted(string s) =>
            (s ?? "").Replace("\"", "\\\"");
    }
}
