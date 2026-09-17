// Quake3Adapter.cs — Quake III Arena .map format adapter
// Extends Quake2Adapter (Valve 220 faces) and adds support for patchDef2 Bezier patch blocks.
//
// patchDef2 block structure (appears in place of a brush block inside an entity):
//   {
//   patchDef2
//   {
//   texturename
//   ( rows cols 0 0 0 )
//   (
//   ( ( x y z u v ) ( x y z u v ) ... )   <- one row of control points
//   ...
//   )
//   }
//   }

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OGEditorSDK;
using OGEditorSDK.MapFormat;

namespace OGEditorSDK.MapFormat.Adapters
{
    public class Quake3Adapter : Quake2Adapter
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        public override string   FormatId       => "quake3";
        public override string   DisplayName    => "Quake3";
        public override string[] FileExtensions => new[] { ".map" };

        // ── ParseBrushOrPatch — detects patchDef2 ────────────────────────────────

        protected override OGGeometryPrimitive ParseBrushOrPatch(
            List<(int lineNo, string text)> block, string filePath)
        {
            // The first non-empty content line in a patch block is "patchDef2"
            foreach (var (_, txt) in block)
            {
                string trimmed = txt.Trim();
                if (trimmed.Length == 0) continue;
                if (string.Equals(trimmed, "patchDef2", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(trimmed, "patchDef3", StringComparison.OrdinalIgnoreCase))
                    return ParsePatch(block, filePath);
                break; // first non-empty token is something else — it's a brush
            }
            return ParseBrush(block, filePath);
        }

        // ── patchDef2 parser ──────────────────────────────────────────────────────

        private OGPatch ParsePatch(List<(int lineNo, string text)> block, string filePath)
        {
            // block content (outer entity-brace stripped by ParseEntityBlock):
            //   patchDef2
            //   {
            //   texturename
            //   ( rows cols 0 0 0 )
            //   (
            //   ( ( x y z u v ) ... )   <- rows × cols control points
            //   ...
            //   )
            //   }

            int idx = 0;

            // Skip to "patchDef2" keyword
            while (idx < block.Count
                   && !string.Equals(block[idx].text.Trim(), "patchDef2",
                                     StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(block[idx].text.Trim(), "patchDef3",
                                     StringComparison.OrdinalIgnoreCase))
                idx++;
            if (idx >= block.Count)
                throw new OGMapReadException("patchDef2 keyword not found in patch block", filePath);
            idx++;

            // Skip opening "{"
            while (idx < block.Count && block[idx].text.Trim() != "{") idx++;
            if (idx >= block.Count)
                throw new OGMapReadException("patchDef2: missing opening {", filePath);
            idx++;

            // Texture name — first non-blank line
            while (idx < block.Count && block[idx].text.Trim().Length == 0) idx++;
            if (idx >= block.Count)
                throw new OGMapReadException("patchDef2: missing texture name", filePath);
            string texture = block[idx].text.Trim();
            idx++;

            // Size line: ( rows cols 0 0 0 )
            while (idx < block.Count && block[idx].text.Trim().Length == 0) idx++;
            if (idx >= block.Count)
                throw new OGMapReadException("patchDef2: missing size line", filePath);
            var sizeToks = Tokenize(block[idx].text);
            // sizeToks: ( rows cols 0 0 0 )  → index 1=rows, 2=cols
            if (sizeToks.Count < 3)
                throw new OGMapReadException("patchDef2 size line malformed", filePath, block[idx].lineNo);
            int rows, cols;
            try
            {
                rows = int.Parse(sizeToks[1]);
                cols = int.Parse(sizeToks[2]);
            }
            catch (FormatException)
            {
                throw new OGMapReadException(
                    "patchDef2: cannot parse rows/cols", filePath, block[idx].lineNo);
            }
            idx++;

            if (rows <= 0 || cols <= 0)
                throw new OGMapReadException(
                    $"patchDef2: invalid grid size {rows}×{cols}", filePath);

            // Opening "(" of the control-point block
            while (idx < block.Count && block[idx].text.Trim() != "(") idx++;
            if (idx >= block.Count)
                throw new OGMapReadException("patchDef2: missing control-point block opener", filePath);
            idx++;

            var patch = new OGPatch
            {
                Texture       = texture,
                Rows          = rows,
                Cols          = cols,
                ControlPoints = new OGVector3[rows, cols],
                TexCoords     = new OGVector2[rows, cols],
            };

            // Parse rows of control points
            for (int r = 0; r < rows; r++)
            {
                // Skip blank lines
                while (idx < block.Count && block[idx].text.Trim().Length == 0) idx++;
                if (idx >= block.Count) break;

                string rowText   = block[idx].text.Trim();
                int    rowLineNo = block[idx].lineNo;
                idx++;

                // ")" closes the control-point block — stop reading rows
                if (rowText == ")") break;

                ParsePatchRow(rowText, r, cols, patch, filePath, rowLineNo);
            }

            return patch;
        }

        // Parse one row line: ( ( x y z u v ) ( x y z u v ) ... )
        private static void ParsePatchRow(string line, int row, int cols,
            OGPatch patch, string filePath, int lineNo)
        {
            var toks = Tokenize(line);
            int i = 0;

            // Skip the outer opening (
            if (i < toks.Count && toks[i] == "(") i++;

            for (int col = 0; col < cols; col++)
            {
                // Advance to the next ( that begins a control point
                while (i < toks.Count && toks[i] != "(") i++;
                if (i >= toks.Count)
                    throw new OGMapReadException(
                        $"patchDef2 row {row}: expected {cols} control points, found only {col}",
                        filePath, lineNo);
                i++; // skip (

                if (i + 5 > toks.Count)
                    throw new OGMapReadException(
                        $"patchDef2 row {row} col {col}: control point truncated",
                        filePath, lineNo);

                float x, y, z, u, v;
                try
                {
                    x = F(toks[i++]); y = F(toks[i++]); z = F(toks[i++]);
                    u = F(toks[i++]); v = F(toks[i++]);
                }
                catch (FormatException ex)
                {
                    throw new OGMapReadException(
                        $"patchDef2 row {row} col {col}: {ex.Message}", filePath, lineNo);
                }

                if (i < toks.Count && toks[i] == ")") i++; // skip closing )

                patch.ControlPoints[row, col] = new OGVector3(x, y, z);
                patch.TexCoords[row, col]     = new OGVector2(u, v);
            }
        }

        // ── Write — patches + brushes ─────────────────────────────────────────────

        protected override void WritePrimitive(OGGeometryPrimitive prim, ref int brushIdx, StreamWriter sw)
        {
            if (prim is OGPatch patch)
            {
                WritePatch(patch, brushIdx++, sw);
            }
            else if (prim is OGBrush brush)
            {
                sw.WriteLine($"// brush {brushIdx++}");
                sw.WriteLine("{");
                foreach (var face in brush.Faces) WriteFaceLine(face, sw);
                sw.WriteLine("}");
            }
            // Other geometry families silently skipped (ValidateCore will have warned)
        }

        private static void WritePatch(OGPatch patch, int brushIdx, StreamWriter sw)
        {
            sw.WriteLine($"// brush {brushIdx}");
            sw.WriteLine("{");
            sw.WriteLine("patchDef2");
            sw.WriteLine("{");
            sw.WriteLine(patch.Texture ?? "NULL");
            sw.WriteLine($"( {patch.Rows} {patch.Cols} 0 0 0 )");
            sw.WriteLine("(");

            bool hasTexCoords = patch.TexCoords != null;
            for (int r = 0; r < patch.Rows; r++)
            {
                var sb = new StringBuilder();
                sb.Append("( ");
                for (int c = 0; c < patch.Cols; c++)
                {
                    var cp = patch.ControlPoints != null
                        ? patch.ControlPoints[r, c]
                        : default(OGVector3);
                    float u = hasTexCoords ? patch.TexCoords[r, c].X : 0f;
                    float v = hasTexCoords ? patch.TexCoords[r, c].Y : 0f;
                    sb.Append($"( {N(cp.X)} {N(cp.Y)} {N(cp.Z)} {N(u)} {N(v)} ) ");
                }
                sb.Append(")");
                sw.WriteLine(sb.ToString());
            }

            sw.WriteLine(")");
            sw.WriteLine("}");
            sw.WriteLine("}");
        }

        // ── Validation — also warn on non-Brush3D geometry ───────────────────────

        protected override List<OGConversionDiagnostic> ValidateCore(OGMapIR map)
        {
            var diags = base.ValidateCore(map);

            // Warn about geometry that cannot be represented in Quake3 format
            foreach (var prim in map.WorldGeometry)
                WarnWrongFamily(prim, "worldspawn", diags);
            foreach (var be in map.BrushEntities)
                foreach (var prim in be.Geometry)
                    WarnWrongFamily(prim, be.Classname ?? "brush_entity", diags);

            return diags;
        }

        private static void WarnWrongFamily(OGGeometryPrimitive prim,
            string entityClass, List<OGConversionDiagnostic> diags)
        {
            if (prim.Family == GeometryFamily.Sector2D)
                diags.Add(new OGConversionDiagnostic(DiagnosticSeverity.Warning,
                    $"OGSector geometry in '{entityClass}' cannot be written as Quake3 brush/patch. "
                    + "Convert to brushes first.")
                { EntityClass = entityClass });

            else if (prim.Family == GeometryFamily.Tile2D)
                diags.Add(new OGConversionDiagnostic(DiagnosticSeverity.Warning,
                    $"OGTile geometry in '{entityClass}' cannot be written as Quake3 brush/patch. "
                    + "Convert to brushes first.")
                { EntityClass = entityClass });
        }

        // ── Entity lookup — Quake III table ───────────────────────────────────────

        // TODO: Replace with OGEntityMappings.ClassnameToDstThingType(classname) when that
        //       generic cross-game lookup method is added to OGEntityMappings.
        protected override int LookupThingType(string classname)
        {
            if (classname == null) return -1;
            int val;
            return OGEntityMappings.Quake3ClassToDoom.TryGetValue(classname, out val) ? val : -1;
        }
    }
}
