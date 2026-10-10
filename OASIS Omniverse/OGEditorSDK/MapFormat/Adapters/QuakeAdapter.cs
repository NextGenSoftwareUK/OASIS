// QuakeAdapter.cs — Quake .map format adapter
// Parses and emits the classic Quake text .map format (standard UV projection, no Valve 220 axes).
// Quake2Adapter and Quake3Adapter extend this class.
//
// Format reference:
//   // entity N
//   {
//   "key" "value"
//   // brush N
//   {
//   ( x1 y1 z1 ) ( x2 y2 z2 ) ( x3 y3 z3 ) TEXTURE xoff yoff rotation xscale yscale
//   }
//   }

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using OGEditorSDK;
using OGEditorSDK.MapFormat;

namespace OGEditorSDK.MapFormat.Adapters
{
    public class QuakeAdapter : IOGMapFormatAdapter
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        public virtual string   FormatId       => "quake";
        public virtual string   DisplayName    => "Quake";
        public virtual string[] FileExtensions => new[] { ".map" };
        public GeometryFamily   Family         => GeometryFamily.Brush3D;

        // ── CanRead ───────────────────────────────────────────────────────────────

        public bool CanRead(string filePath)
            => filePath != null
            && File.Exists(filePath)
            && string.Equals(Path.GetExtension(filePath), ".map",
                             StringComparison.OrdinalIgnoreCase);

        // ── Read ──────────────────────────────────────────────────────────────────

        public OGMapIR Read(string filePath)
        {
            string[] rawLines;
            try   { rawLines = File.ReadAllLines(filePath, Encoding.UTF8); }
            catch (Exception ex) { throw new OGMapReadException(ex.Message, filePath); }

            var ir = new OGMapIR
            {
                SourceFormat = FormatId,
                MapName      = Path.GetFileNameWithoutExtension(filePath),
            };

            var entityBlocks = SplitIntoEntityBlocks(rawLines, filePath);
            for (int i = 0; i < entityBlocks.Count; i++)
                ParseEntityBlock(entityBlocks[i], i == 0, ir, filePath);

            return ir;
        }

        // Split the flat line stream into entity-level blocks.
        // Returns one list per entity; each list contains (1-based lineNo, strippedText)
        // for the INNER content of the entity (outer braces stripped).
        protected List<List<(int lineNo, string text)>> SplitIntoEntityBlocks(
            string[] rawLines, string filePath)
        {
            var blocks  = new List<List<(int, string)>>();
            List<(int, string)> cur = null;
            int depth = 0;

            for (int i = 0; i < rawLines.Length; i++)
            {
                string line = StripComment(rawLines[i]).Trim();
                if (line.Length == 0) continue;

                if (line == "{")
                {
                    depth++;
                    if (depth == 1) { cur = new List<(int, string)>(); blocks.Add(cur); }
                    else              cur?.Add((i + 1, line));
                }
                else if (line == "}")
                {
                    depth--;
                    if (depth > 0) cur?.Add((i + 1, line));
                    // depth == 0 closes the entity — cur is already in blocks
                }
                else
                {
                    cur?.Add((i + 1, line));
                }
            }

            return blocks;
        }

        // Parse one entity block. The outer braces have already been stripped.
        protected void ParseEntityBlock(
            List<(int lineNo, string text)> lines,
            bool isWorldspawn,
            OGMapIR ir,
            string filePath)
        {
            var keys        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var subBlocks   = new List<List<(int, string)>>();
            List<(int, string)> cur = null;
            int depth = 0;

            foreach (var (ln, txt) in lines)
            {
                if (txt == "{")
                {
                    depth++;
                    if (depth == 1)  cur = new List<(int, string)>();
                    else             cur?.Add((ln, txt));
                }
                else if (txt == "}")
                {
                    if (depth == 1) { if (cur != null) subBlocks.Add(cur); cur = null; }
                    else             cur?.Add((ln, txt));
                    depth--;
                }
                else if (depth == 0)
                {
                    // Key/value line outside any brush block
                    if (TryParseKeyValue(txt, out string k, out string v))
                        keys[k] = v;
                }
                else
                {
                    cur?.Add((ln, txt));
                }
            }

            string classname = keys.ContainsKey("classname") ? keys["classname"] : "unknown";

            if (isWorldspawn)
            {
                ApplyWorldspawnKeys(keys, ir);
                foreach (var block in subBlocks)
                {
                    var prim = ParseBrushOrPatch(block, filePath);
                    if (prim != null) ir.WorldGeometry.Add(prim);
                }
            }
            else if (subBlocks.Count > 0)
            {
                var be = new OGBrushEntity { Classname = classname };
                ApplyEntityKeys(keys, be);
                foreach (var block in subBlocks)
                {
                    var prim = ParseBrushOrPatch(block, filePath);
                    if (prim != null) be.Geometry.Add(prim);
                }
                ir.BrushEntities.Add(be);
            }
            else
            {
                var pe = new OGPointEntity { Classname = classname };
                ApplyEntityKeys(keys, pe);
                ir.PointEntities.Add(pe);
            }
        }

        // Quake3Adapter overrides to also detect patchDef2 blocks.
        protected virtual OGGeometryPrimitive ParseBrushOrPatch(
            List<(int lineNo, string text)> block, string filePath)
            => ParseBrush(block, filePath);

        protected OGBrush ParseBrush(List<(int lineNo, string text)> block, string filePath)
        {
            var brush = new OGBrush();
            foreach (var (ln, txt) in block)
            {
                if (!txt.StartsWith("(")) continue;
                var face = ParseFaceLine(txt, ln, filePath);
                if (face != null) brush.Faces.Add(face);
            }
            return brush;
        }

        // Quake2Adapter overrides to parse Valve 220 axes.
        protected virtual OGBrushFace ParseFaceLine(string line, int lineNo, string filePath)
        {
            // ( x1 y1 z1 ) ( x2 y2 z2 ) ( x3 y3 z3 ) TEXTURE xoff yoff rotation xscale yscale
            // Indices: 0=( 1=x 2=y 3=z 4=) 5=( 6=x 7=y 8=z 9=) 10=( 11=x 12=y 13=z 14=)
            //         15=TEXTURE 16=xoff 17=yoff 18=rot 19=xs 20=ys
            var t = Tokenize(line);
            if (t.Count < 21)
                throw new OGMapReadException(
                    $"Face line has {t.Count} tokens (need ≥21): {line}", filePath, lineNo);
            try
            {
                var p1 = new OGVector3(F(t[1]),  F(t[2]),  F(t[3]));
                var p2 = new OGVector3(F(t[6]),  F(t[7]),  F(t[8]));
                var p3 = new OGVector3(F(t[11]), F(t[12]), F(t[13]));
                return new OGBrushFace
                {
                    P1       = p1,
                    P2       = p2,
                    P3       = p3,
                    Plane    = OGPlane.FromPoints(p1, p2, p3),
                    Texture  = t[15],
                    Offset   = new OGVector2(F(t[16]), F(t[17])),
                    Rotation = F(t[18]),
                    Scale    = new OGVector2(F(t[19]), F(t[20])),
                    // UAxis / VAxis remain null (standard projection)
                };
            }
            catch (FormatException ex)
            {
                throw new OGMapReadException($"Cannot parse face tokens: {ex.Message}", filePath, lineNo);
            }
        }

        protected void ApplyWorldspawnKeys(Dictionary<string, string> keys, OGMapIR ir)
        {
            string v;
            if (keys.TryGetValue("sky",     out v)) ir.Metadata.SkyTexture  = v;
            if (keys.TryGetValue("skyname", out v)) ir.Metadata.SkyTexture  = v;
            if (keys.TryGetValue("music",   out v)) ir.Metadata.MusicTrack  = v;
            if (keys.TryGetValue("message", out v)) ir.Metadata.Description = v;
            // Preserve all keys for round-trip fidelity
            foreach (var kv in keys)
                if (!ir.Metadata.Extra.ContainsKey(kv.Key))
                    ir.Metadata.Extra[kv.Key] = kv.Value;
        }

        protected void ApplyEntityKeys(Dictionary<string, string> keys, OGEntity entity)
        {
            foreach (var kv in keys) entity.Keys[kv.Key] = kv.Value;

            string v;
            if (keys.TryGetValue("classname", out v)) entity.Classname = v;

            if (keys.TryGetValue("origin", out v))
            {
                var p = v.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length >= 3)
                    entity.Origin = new OGVector3(F(p[0]), F(p[1]), F(p[2]));
            }

            if (keys.TryGetValue("angle", out v))
            {
                float a;
                if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out a))
                    entity.Angle = a;
            }

            entity.OASISThingType = LookupThingType(entity.Classname);
        }

        // TODO: Replace with OGEntityMappings.ClassnameToDstThingType(classname) when that generic
        //       cross-game lookup method is added to OGEntityMappings.
        protected virtual int LookupThingType(string classname)
        {
            if (classname == null) return -1;
            int val;
            return OGEntityMappings.QuakeClassToDoom.TryGetValue(classname, out val) ? val : -1;
        }

        // ── Write ─────────────────────────────────────────────────────────────────

        public IEnumerable<OGConversionDiagnostic> ValidateForWrite(OGMapIR map)
            => ValidateCore(map);

        protected virtual List<OGConversionDiagnostic> ValidateCore(OGMapIR map)
        {
            var diags = new List<OGConversionDiagnostic>();
            WarnEmptyTextures(map.WorldGeometry, "worldspawn", diags);
            foreach (var be in map.BrushEntities)
                WarnEmptyTextures(be.Geometry, be.Classname ?? "brush_entity", diags);
            return diags;
        }

        protected static void WarnEmptyTextures(IEnumerable<OGGeometryPrimitive> prims,
            string entityClass, List<OGConversionDiagnostic> diags)
        {
            foreach (var prim in prims)
            {
                if (!(prim is OGBrush brush)) continue;
                foreach (var face in brush.Faces)
                {
                    if (string.IsNullOrEmpty(face.Texture))
                        diags.Add(new OGConversionDiagnostic(
                            DiagnosticSeverity.Warning,
                            $"Brush face in '{entityClass}' has a null or empty texture name.")
                        { EntityClass = entityClass });
                }
            }
        }

        public void Write(OGMapIR map, string outputPath)
        {
            try
            {
                using (var sw = new StreamWriter(outputPath, false, new UTF8Encoding(false)))
                    WriteMap(map, sw);
            }
            catch (OGMapWriteException) { throw; }
            catch (Exception ex)        { throw new OGMapWriteException(ex.Message, outputPath); }
        }

        protected void WriteMap(OGMapIR map, StreamWriter sw)
        {
            // Entity 0 — worldspawn
            sw.WriteLine("// entity 0");
            sw.WriteLine("{");
            sw.WriteLine("\"classname\" \"worldspawn\"");
            WriteWorldspawnKeys(map.Metadata, sw);
            int bi = 0;
            foreach (var prim in map.WorldGeometry) WritePrimitive(prim, ref bi, sw);
            sw.WriteLine("}");

            // Remaining entities: point entities then brush entities
            int ei = 1;
            foreach (var pe in map.PointEntities)
            {
                sw.WriteLine($"// entity {ei++}");
                sw.WriteLine("{");
                WriteEntityKeys(pe, sw);
                sw.WriteLine("}");
            }
            foreach (var be in map.BrushEntities)
            {
                sw.WriteLine($"// entity {ei++}");
                sw.WriteLine("{");
                WriteEntityKeys(be, sw);
                bi = 0;
                foreach (var prim in be.Geometry) WritePrimitive(prim, ref bi, sw);
                sw.WriteLine("}");
            }
        }

        // Quake3Adapter overrides to also emit patchDef2 blocks.
        protected virtual void WritePrimitive(OGGeometryPrimitive prim, ref int brushIdx, StreamWriter sw)
        {
            if (!(prim is OGBrush brush)) return;
            sw.WriteLine($"// brush {brushIdx++}");
            sw.WriteLine("{");
            foreach (var face in brush.Faces) WriteFaceLine(face, sw);
            sw.WriteLine("}");
        }

        // Quake2Adapter overrides for Valve 220 format.
        protected virtual void WriteFaceLine(OGBrushFace face, StreamWriter sw)
        {
            OGVector3 p1, p2, p3;
            if (!TryGetFacePoints(face, out p1, out p2, out p3)) return; // skip degenerate face

            string tex = face.Texture ?? "NULL";
            sw.WriteLine(
                $"( {V(p1)} ) ( {V(p2)} ) ( {V(p3)} ) {tex}" +
                $" {N(face.Offset.X)} {N(face.Offset.Y)}" +
                $" {N(face.Rotation)}" +
                $" {N(face.Scale.X)} {N(face.Scale.Y)}");
        }

        // Returns the three plane-defining points for a face.
        // Uses P1/P2/P3 when non-zero; otherwise derives from Plane.
        // Returns false (skip face) when P1/P2/P3 are all zero AND the Plane is degenerate.
        protected static bool TryGetFacePoints(OGBrushFace face,
            out OGVector3 p1, out OGVector3 p2, out OGVector3 p3)
        {
            p1 = face.P1; p2 = face.P2; p3 = face.P3;
            if (!IsZero(p1) || !IsZero(p2) || !IsZero(p3)) return true;

            // All three points are zero — derive approximate points from the plane instead
            var n = face.Plane.Normal;
            if (n.Length() < 1e-6f) { p1 = p2 = p3 = default(OGVector3); return false; }

            var pt  = n * face.Plane.Distance;
            var ax  = Math.Abs(n.X) < 0.9f ? new OGVector3(1f, 0f, 0f) : new OGVector3(0f, 1f, 0f);
            var t2  = n.Cross(ax).Normalized();
            var t1  = t2.Cross(n).Normalized();
            p1 = pt;
            p2 = pt + t1 * 16f;
            p3 = pt + t2 * 16f;
            return true;
        }

        protected void WriteWorldspawnKeys(OGMapMetadata meta, StreamWriter sw)
        {
            if (!string.IsNullOrEmpty(meta.SkyTexture))
                sw.WriteLine($"\"sky\" \"{Esc(meta.SkyTexture)}\"");
            if (!string.IsNullOrEmpty(meta.MusicTrack))
                sw.WriteLine($"\"music\" \"{Esc(meta.MusicTrack)}\"");
            if (!string.IsNullOrEmpty(meta.Description))
                sw.WriteLine($"\"message\" \"{Esc(meta.Description)}\"");
            // Round-trip any extra keys that were not promoted to metadata fields
            foreach (var kv in meta.Extra)
            {
                string lk = kv.Key.ToLowerInvariant();
                if (lk == "classname" || lk == "sky" || lk == "skyname"
                    || lk == "music"  || lk == "message") continue;
                sw.WriteLine($"\"{Esc(kv.Key)}\" \"{Esc(kv.Value)}\"");
            }
        }

        protected static void WriteEntityKeys(OGEntity entity, StreamWriter sw)
        {
            // classname is always written first
            sw.WriteLine($"\"classname\" \"{Esc(entity.Classname ?? "info_notnull")}\"");
            foreach (var kv in entity.Keys)
            {
                if (string.Equals(kv.Key, "classname", StringComparison.OrdinalIgnoreCase)) continue;
                sw.WriteLine($"\"{Esc(kv.Key)}\" \"{Esc(kv.Value)}\"");
            }
        }

        // ── Fidelity ──────────────────────────────────────────────────────────────

        public float ConversionFidelity(GeometryFamily sourceFamily)
        {
            switch (sourceFamily)
            {
                case GeometryFamily.Brush3D:  return 1.00f;
                case GeometryFamily.Sector2D: return 0.55f;
                case GeometryFamily.Build2D:  return 0.45f;
                case GeometryFamily.Tile2D:   return 0.20f;
                default:                      return 0.50f;
            }
        }

        public string RemapTexture(string sourceTexture, string sourceFormatId) => null;

        // ── Tokenizer / parse helpers ─────────────────────────────────────────────

        /// <summary>
        /// Strip // line comments. Respects quoted strings so // inside a value is preserved.
        /// </summary>
        protected static string StripComment(string line)
        {
            bool inQ = false;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"') inQ = !inQ;
                if (!inQ && i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/')
                    return line.Substring(0, i);
            }
            return line;
        }

        /// <summary>
        /// Whitespace tokenizer. The .map format uses spaces between all tokens
        /// (including parentheses and brackets), so simple split suffices.
        /// </summary>
        protected static List<string> Tokenize(string line)
        {
            var tokens = new List<string>(32);
            int i = 0, len = line.Length;
            while (i < len)
            {
                while (i < len && char.IsWhiteSpace(line[i])) i++;
                if (i >= len) break;
                int start = i;
                while (i < len && !char.IsWhiteSpace(line[i])) i++;
                tokens.Add(line.Substring(start, i - start));
            }
            return tokens;
        }

        /// <summary>
        /// Parse a "key" "value" line. Handles \" escape sequences inside quoted strings.
        /// Leading/trailing whitespace on the line is tolerated.
        /// </summary>
        protected static bool TryParseKeyValue(string line, out string key, out string value)
        {
            key = value = null;
            int i = 0;
            string k = ReadQuoted(line, ref i);
            if (k == null) return false;
            string v = ReadQuoted(line, ref i);
            if (v == null) return false;
            key = k; value = v;
            return true;
        }

        /// <summary>
        /// Read one double-quoted string starting at position i; advance i past the closing quote.
        /// Handles \" escape; returns null on unterminated string.
        /// </summary>
        protected static string ReadQuoted(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            if (i >= s.Length || s[i] != '"') return null;
            i++; // skip opening "
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                if      (s[i] == '\\' && i + 1 < s.Length && s[i + 1] == '"') { sb.Append('"'); i += 2; }
                else if (s[i] == '"')                                            { i++; return sb.ToString(); }
                else                                                              sb.Append(s[i++]);
            }
            return null; // unterminated string
        }

        // ── Formatting helpers ────────────────────────────────────────────────────

        // Parse a float token (invariant culture)
        protected static float F(string s)
            => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

        // Format a float: integers without decimal point, others with G7
        protected static string N(float v)
        {
            if (!float.IsNaN(v) && !float.IsInfinity(v)
                && v == (float)Math.Truncate((double)v)
                && Math.Abs(v) <= 1073741824.0f)   // 2^30 — safe for long cast
                return ((long)v).ToString(CultureInfo.InvariantCulture);
            return v.ToString("G7", CultureInfo.InvariantCulture);
        }

        // Format a vector as "x y z" using N() for each component
        protected static string V(OGVector3 v) => $"{N(v.X)} {N(v.Y)} {N(v.Z)}";

        // Test whether an OGVector3 is the zero vector
        protected static bool IsZero(OGVector3 v) => v.X == 0f && v.Y == 0f && v.Z == 0f;

        // Escape a string for .map output ("\" → "\\" and '"' → '\"')
        protected static string Esc(string s)
            => s == null ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
