// Quake2Adapter.cs — Quake II .map format adapter
// Identical to the Quake format except face lines use Valve 220 UV axes:
//   ( x1 y1 z1 ) ( x2 y2 z2 ) ( x3 y3 z3 ) TEXTURE [ ux uy uz uoff ] [ vx vy vz voff ] rot xs ys
//
// On write, Valve 220 is emitted when UAxis is populated; otherwise falls back to
// standard Quake format (accommodates faces that were converted from another format).

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OGEditorSDK;
using OGEditorSDK.MapFormat;

namespace OGEditorSDK.MapFormat.Adapters
{
    public class Quake2Adapter : QuakeAdapter
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        public override string   FormatId       => "quake2";
        public override string   DisplayName    => "Quake2";
        public override string[] FileExtensions => new[] { ".map" };

        // ── Face parsing — Valve 220 ──────────────────────────────────────────────

        protected override OGBrushFace ParseFaceLine(string line, int lineNo, string filePath)
        {
            // Valve 220 format:
            // ( x1 y1 z1 ) ( x2 y2 z2 ) ( x3 y3 z3 ) TEXTURE [ ux uy uz uoff ] [ vx vy vz voff ] rot xs ys
            //
            // Token indices:
            //  0=( 1=x 2=y 3=z 4=)  5=( 6=x 7=y 8=z 9=)  10=( 11=x 12=y 13=z 14=)
            //  15=TEXTURE
            //  16=[  17=ux 18=uy 19=uz 20=uoff  21=]
            //  22=[  23=vx 24=vy 25=vz 26=voff  27=]
            //  28=rot  29=xs  30=ys

            var t = Tokenize(line);

            // If token 16 is not '[' this is a standard-format face (some tools write Quake2
            // maps without Valve 220 axes). Delegate to the base Quake parser in that case.
            if (t.Count >= 16 && t[15] != null && t.Count >= 17 && t[16] != "[")
                return base.ParseFaceLine(line, lineNo, filePath);

            if (t.Count < 31)
                throw new OGMapReadException(
                    $"Valve 220 face line has {t.Count} tokens (need ≥31): {line}", filePath, lineNo);

            try
            {
                var p1 = new OGVector3(F(t[1]),  F(t[2]),  F(t[3]));
                var p2 = new OGVector3(F(t[6]),  F(t[7]),  F(t[8]));
                var p3 = new OGVector3(F(t[11]), F(t[12]), F(t[13]));

                var uAxis = new OGVector3(F(t[17]), F(t[18]), F(t[19]));
                float uOff = F(t[20]);
                var vAxis = new OGVector3(F(t[23]), F(t[24]), F(t[25]));
                float vOff = F(t[26]);

                return new OGBrushFace
                {
                    P1       = p1,
                    P2       = p2,
                    P3       = p3,
                    Plane    = OGPlane.FromPoints(p1, p2, p3),
                    Texture  = t[15],
                    Rotation = F(t[28]),
                    Scale    = new OGVector2(F(t[29]), F(t[30])),
                    // Offset stores the UV shifts for round-trip when UAxis is present
                    Offset   = new OGVector2(uOff, vOff),
                    UAxis    = uAxis,
                    UShift   = uOff,
                    VAxis    = vAxis,
                    VShift   = vOff,
                };
            }
            catch (FormatException ex)
            {
                throw new OGMapReadException(
                    $"Cannot parse Valve 220 face tokens: {ex.Message}", filePath, lineNo);
            }
        }

        // ── Face writing — Valve 220 (with standard fallback) ─────────────────────

        protected override void WriteFaceLine(OGBrushFace face, StreamWriter sw)
        {
            OGVector3 p1, p2, p3;
            if (!TryGetFacePoints(face, out p1, out p2, out p3)) return;

            string tex = face.Texture ?? "NULL";

            if (face.UAxis.HasValue && face.VAxis.HasValue)
            {
                // Emit Valve 220 format
                var ua = face.UAxis.Value;
                var va = face.VAxis.Value;
                sw.WriteLine(
                    $"( {V(p1)} ) ( {V(p2)} ) ( {V(p3)} ) {tex}" +
                    $" [ {V(ua)} {N(face.UShift)} ]" +
                    $" [ {V(va)} {N(face.VShift)} ]" +
                    $" {N(face.Rotation)} {N(face.Scale.X)} {N(face.Scale.Y)}");
            }
            else
            {
                // Fall back to standard Quake format (no UV axes)
                sw.WriteLine(
                    $"( {V(p1)} ) ( {V(p2)} ) ( {V(p3)} ) {tex}" +
                    $" {N(face.Offset.X)} {N(face.Offset.Y)}" +
                    $" {N(face.Rotation)} {N(face.Scale.X)} {N(face.Scale.Y)}");
            }
        }

        // ── Entity lookup — Quake II table ────────────────────────────────────────

        // TODO: Replace with OGEntityMappings.ClassnameToDstThingType(classname) when that
        //       generic cross-game lookup method is added to OGEntityMappings.
        protected override int LookupThingType(string classname)
        {
            if (classname == null) return -1;
            int val;
            return OGEntityMappings.Quake2ClassToDoom.TryGetValue(classname, out val) ? val : -1;
        }
    }
}
