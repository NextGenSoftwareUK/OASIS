using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OGEditorSDK.MapFormat.Adapters
{
    /// <summary>
    /// Adapter for the Doom/Doom2 binary WAD map format (Sector2D family).
    /// Reads/writes the first map found in IWAD or PWAD files.
    /// </summary>
    public class DoomAdapter : IOGMapFormatAdapter
    {
        public string         FormatId       => "doom";
        public string         DisplayName    => "Doom / Doom2";
        public string[]       FileExtensions => new[] { ".wad" };
        public GeometryFamily Family         => GeometryFamily.Sector2D;

        // ── CanRead ───────────────────────────────────────────────────────────

        public bool CanRead(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            try
            {
                using var fs = File.OpenRead(filePath);
                if (fs.Length < 4) return false;
                var sig = new byte[4];
                fs.Read(sig, 0, 4);
                string id = Encoding.ASCII.GetString(sig);
                return id == "IWAD" || id == "PWAD";
            }
            catch { return false; }
        }

        // ── Read ──────────────────────────────────────────────────────────────

        public OGMapIR Read(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            using var br = new BinaryReader(fs);

            // WAD header
            byte[] sigBytes = br.ReadBytes(4);
            string ident = Encoding.ASCII.GetString(sigBytes);
            if (ident != "IWAD" && ident != "PWAD")
                throw new OGMapReadException("Not a valid WAD file (missing IWAD/PWAD magic).", filePath);

            int numlumps    = br.ReadInt32();
            int infotableofs = br.ReadInt32();

            // Lump directory
            var lumps = new List<LumpEntry>(numlumps);
            fs.Seek(infotableofs, SeekOrigin.Begin);
            for (int i = 0; i < numlumps; i++)
            {
                int  filepos  = br.ReadInt32();
                int  size     = br.ReadInt32();
                byte[] nameB  = br.ReadBytes(8);
                lumps.Add(new LumpEntry(filepos, size, ReadFixedString(nameB, 8)));
            }

            // Find first map marker (size==0, name matches E#M# or MAP##)
            int markerIdx = -1;
            for (int i = 0; i < lumps.Count; i++)
            {
                if (lumps[i].Size == 0 && IsMapMarker(lumps[i].Name))
                { markerIdx = i; break; }
            }
            if (markerIdx < 0)
                throw new OGMapReadException("No map marker lump found in WAD.", filePath);

            // Collect named lumps following the marker (stop at next marker or end)
            var mapLumps = new Dictionary<string, LumpEntry>(StringComparer.OrdinalIgnoreCase);
            for (int i = markerIdx + 1; i < lumps.Count; i++)
            {
                if (IsMapMarker(lumps[i].Name)) break;
                mapLumps[lumps[i].Name] = lumps[i];
            }

            // ── VERTEXES ──────────────────────────────────────────────────────
            var verts = new List<OGVertex2D>();
            if (mapLumps.TryGetValue("VERTEXES", out var vtxL))
            {
                fs.Seek(vtxL.FilePos, SeekOrigin.Begin);
                int cnt = vtxL.Size / 4;
                for (int i = 0; i < cnt; i++)
                    verts.Add(new OGVertex2D { X = br.ReadInt16(), Y = br.ReadInt16() });
            }

            // ── SECTORS ───────────────────────────────────────────────────────
            var sectors = new List<OGSector>();
            if (mapLumps.TryGetValue("SECTORS", out var secL))
            {
                fs.Seek(secL.FilePos, SeekOrigin.Begin);
                int cnt = secL.Size / 26;
                for (int i = 0; i < cnt; i++)
                {
                    short floorH  = br.ReadInt16();
                    short ceilH   = br.ReadInt16();
                    byte[] floorP = br.ReadBytes(8);
                    byte[] ceilP  = br.ReadBytes(8);
                    short light   = br.ReadInt16();
                    short special = br.ReadInt16();
                    short tag     = br.ReadInt16();
                    sectors.Add(new OGSector
                    {
                        FloorHeight   = floorH,
                        CeilingHeight = ceilH,
                        FloorTexture  = ReadFixedString(floorP, 8),
                        CeilingTexture= ReadFixedString(ceilP,  8),
                        LightLevel    = light,
                        SectorSpecial = special,
                        SectorTag     = tag
                    });
                }
            }

            // ── SIDEDEFS ──────────────────────────────────────────────────────
            var sides = new List<RawSidedef>();
            if (mapLumps.TryGetValue("SIDEDEFS", out var sideL))
            {
                fs.Seek(sideL.FilePos, SeekOrigin.Begin);
                int cnt = sideL.Size / 30;
                for (int i = 0; i < cnt; i++)
                {
                    short xoff  = br.ReadInt16();
                    short yoff  = br.ReadInt16();
                    string upTx = ReadFixedString(br.ReadBytes(8), 8);
                    string loTx = ReadFixedString(br.ReadBytes(8), 8);
                    string midTx= ReadFixedString(br.ReadBytes(8), 8);
                    short secRef= br.ReadInt16();
                    sides.Add(new RawSidedef(xoff, yoff, upTx, loTx, midTx, secRef));
                }
            }

            // ── LINEDEFS ─────────────────────────────────────────────────────
            if (mapLumps.TryGetValue("LINEDEFS", out var lineL))
            {
                fs.Seek(lineL.FilePos, SeekOrigin.Begin);
                int cnt = lineL.Size / 14;
                for (int i = 0; i < cnt; i++)
                {
                    short v1      = br.ReadInt16();
                    short v2      = br.ReadInt16();
                    short flags   = br.ReadInt16();
                    short special = br.ReadInt16();
                    short tag     = br.ReadInt16();
                    short sd1     = br.ReadInt16();
                    short sd2     = br.ReadInt16();

                    var ld = new OGLinedef { Flags = flags, Special = special, Tag = tag };

                    if (v1 >= 0 && v1 < verts.Count) ld.Start = verts[v1];
                    if (v2 >= 0 && v2 < verts.Count) ld.End   = verts[v2];

                    // Front sidedef → textures + owner sector
                    if (sd1 >= 0 && sd1 < sides.Count)
                    {
                        var s = sides[sd1];
                        ld.UpperTexture  = EmptyToNull(s.Upper);
                        ld.MiddleTexture = EmptyToNull(s.Middle);
                        ld.LowerTexture  = EmptyToNull(s.Lower);
                        ld.OffsetX       = s.XOff;
                        ld.OffsetY       = s.YOff;
                        if (s.SectorRef >= 0 && s.SectorRef < sectors.Count)
                            sectors[s.SectorRef].Linedefs.Add(ld);
                    }

                    // Back sidedef (0xFFFF = none)
                    if ((ushort)sd2 != 0xFFFF && sd2 >= 0 && sd2 < sides.Count)
                    {
                        var s = sides[sd2];
                        ld.BackUpperTexture  = EmptyToNull(s.Upper);
                        ld.BackMiddleTexture = EmptyToNull(s.Middle);
                        ld.BackLowerTexture  = EmptyToNull(s.Lower);
                    }
                }
            }

            // ── THINGS ───────────────────────────────────────────────────────
            var pointEntities = new List<OGPointEntity>();
            if (mapLumps.TryGetValue("THINGS", out var thingL))
            {
                fs.Seek(thingL.FilePos, SeekOrigin.Begin);
                int cnt = thingL.Size / 10;
                for (int i = 0; i < cnt; i++)
                {
                    short tx   = br.ReadInt16();
                    short ty   = br.ReadInt16();
                    short ang  = br.ReadInt16();
                    short type = br.ReadInt16();
                    short opts = br.ReadInt16();
                    var pe = new OGPointEntity
                    {
                        Classname = $"doom_thing_{type:D4}",
                        Origin    = new OGVector3(tx, ty, 0),
                        Angle     = ang
                    };
                    pe.Keys["options"] = opts.ToString();
                    pointEntities.Add(pe);
                }
            }

            var ir = new OGMapIR
            {
                MapName      = lumps[markerIdx].Name,
                SourceFormat = FormatId,
                Metadata     = new OGMapMetadata()
            };
            ir.PointEntities.AddRange(pointEntities);
            foreach (var sec in sectors) ir.WorldGeometry.Add(sec);
            return ir;
        }

        // ── Write ─────────────────────────────────────────────────────────────

        public void Write(OGMapIR map, string outputPath)
        {
            // Collect OGSectors
            var sectors = new List<OGSector>();
            foreach (var p in map.WorldGeometry)
                if (p is OGSector s) sectors.Add(s);

            // Build global vertex list (deduped by position)
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

            // Collect unique linedefs; record which sector owns each (= front sector)
            var ldList       = new List<OGLinedef>();
            var ldFrontSec   = new Dictionary<OGLinedef, int>(ReferenceEqualityComparer<OGLinedef>.Instance);
            for (int si = 0; si < sectors.Count; si++)
            {
                foreach (var ld in sectors[si].Linedefs)
                {
                    if (!ldFrontSec.ContainsKey(ld))
                    {
                        ldFrontSec[ld] = si;
                        ldList.Add(ld);
                    }
                }
            }

            // Edge→sector lookup for back-sector resolution on two-sided linedefs
            var edgeSec = new Dictionary<long, int>();
            for (int si = 0; si < sectors.Count; si++)
            {
                foreach (var ld in sectors[si].Linedefs)
                {
                    if (ld.Start == null || ld.End == null) continue;
                    long k = EdgeKey((short)ld.Start.X, (short)ld.Start.Y,
                                     (short)ld.End.X,   (short)ld.End.Y);
                    edgeSec[k] = si;
                }
            }

            // Build sidedef and linedef tables
            var sideOut = new List<RawSidedef>();
            var lineOut = new List<(int v1, int v2, short flags, short special, short tag, short sd1, short sd2)>();

            foreach (var ld in ldList)
            {
                int v1 = ld.Start != null ? GetOrAddVert(ld.Start) : 0;
                int v2 = ld.End   != null ? GetOrAddVert(ld.End)   : 0;

                int frontSec = ldFrontSec.TryGetValue(ld, out int fs2) ? fs2 : 0;
                short sd1 = (short)sideOut.Count;
                sideOut.Add(new RawSidedef(
                    (short)ld.OffsetX, (short)ld.OffsetY,
                    NullToMinus(ld.UpperTexture), NullToMinus(ld.LowerTexture), NullToMinus(ld.MiddleTexture),
                    (short)frontSec));

                short sd2 = unchecked((short)0xFFFF);
                if (ld.TwoSided)
                {
                    int backSec = 0;
                    if (ld.Start != null && ld.End != null)
                    {
                        long rk = EdgeKey((short)ld.End.X, (short)ld.End.Y,
                                          (short)ld.Start.X, (short)ld.Start.Y);
                        edgeSec.TryGetValue(rk, out backSec);
                    }
                    sd2 = (short)sideOut.Count;
                    sideOut.Add(new RawSidedef(
                        0, 0,
                        NullToMinus(ld.BackUpperTexture), NullToMinus(ld.BackLowerTexture), NullToMinus(ld.BackMiddleTexture),
                        (short)backSec));
                }

                lineOut.Add((v1, v2, (short)ld.Flags, (short)ld.Special, (short)ld.Tag, sd1, sd2));
            }

            // Things from point entities
            var thingOut = new List<(short x, short y, short angle, short type, short opts)>();
            foreach (var pe in map.PointEntities)
            {
                string cn = pe.Classname ?? "";
                if (cn.StartsWith("doom_thing_", StringComparison.OrdinalIgnoreCase)
                    && short.TryParse(cn.Substring(11), out short thingType))
                {
                    pe.Keys.TryGetValue("options", out string optStr);
                    short opts = short.TryParse(optStr, out short parsedOpts) ? parsedOpts : (short)7;
                    thingOut.Add(((short)pe.Origin.X, (short)pe.Origin.Y,
                                   (short)pe.Angle, thingType, opts));
                }
            }

            string mapName = !string.IsNullOrEmpty(map.MapName) ? map.MapName : "MAP01";

            using var fs = File.Create(outputPath);
            using var bw = new BinaryWriter(fs);

            // Header placeholder (fixed up at end)
            bw.Write(Encoding.ASCII.GetBytes("PWAD"));
            bw.Write(0);  // numlumps
            bw.Write(0);  // infotableofs

            var dir = new List<(int pos, int size, string name)>();

            // Map marker (size=0, points to current offset)
            dir.Add(((int)fs.Position, 0, mapName));

            // THINGS
            int thingPos = (int)fs.Position;
            foreach (var t in thingOut)
            { bw.Write(t.x); bw.Write(t.y); bw.Write(t.angle); bw.Write(t.type); bw.Write(t.opts); }
            dir.Add((thingPos, thingOut.Count * 10, "THINGS"));

            // LINEDEFS
            int ldPos = (int)fs.Position;
            foreach (var (lv1, lv2, fl, sp, tg, s1, s2) in lineOut)
            { bw.Write((short)lv1); bw.Write((short)lv2); bw.Write(fl); bw.Write(sp); bw.Write(tg); bw.Write(s1); bw.Write(s2); }
            dir.Add((ldPos, lineOut.Count * 14, "LINEDEFS"));

            // SIDEDEFS
            int sdPos = (int)fs.Position;
            foreach (var sd in sideOut)
            {
                bw.Write(sd.XOff); bw.Write(sd.YOff);
                WriteFixed8(bw, sd.Upper); WriteFixed8(bw, sd.Lower); WriteFixed8(bw, sd.Middle);
                bw.Write(sd.SectorRef);
            }
            dir.Add((sdPos, sideOut.Count * 30, "SIDEDEFS"));

            // VERTEXES
            int vtxPos = (int)fs.Position;
            foreach (var v in vertList) { bw.Write((short)v.X); bw.Write((short)v.Y); }
            dir.Add((vtxPos, vertList.Count * 4, "VERTEXES"));

            // SECTORS
            int secPos = (int)fs.Position;
            foreach (var sec in sectors)
            {
                bw.Write((short)sec.FloorHeight);
                bw.Write((short)sec.CeilingHeight);
                WriteFixed8(bw, sec.FloorTexture   ?? "FLAT1");
                WriteFixed8(bw, sec.CeilingTexture ?? "CEIL1_1");
                bw.Write((short)sec.LightLevel);
                bw.Write((short)sec.SectorSpecial);
                bw.Write((short)sec.SectorTag);
            }
            dir.Add((secPos, sectors.Count * 26, "SECTORS"));

            // Lump directory
            int dirOfs = (int)fs.Position;
            foreach (var (dpos, dsz, dname) in dir)
            {
                bw.Write(dpos); bw.Write(dsz);
                WriteFixed8(bw, dname);
            }

            // Fix up header
            fs.Seek(4, SeekOrigin.Begin);
            bw.Write(dir.Count);
            bw.Write(dirOfs);
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
                    "OGBrush geometry (Brush3D family) is not supported by the Doom format and will be ignored.");
        }

        // ── ConversionFidelity ────────────────────────────────────────────────

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

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsMapMarker(string name)
        {
            if (name == null) return false;
            if (name.Length == 4
                && name[0] == 'E' && char.IsDigit(name[1])
                && name[2] == 'M' && char.IsDigit(name[3]))
                return true;
            if (name.Length == 5
                && name[0] == 'M' && name[1] == 'A' && name[2] == 'P'
                && char.IsDigit(name[3]) && char.IsDigit(name[4]))
                return true;
            return false;
        }

        private static string ReadFixedString(byte[] b, int max)
        {
            int end = 0;
            while (end < max && end < b.Length && b[end] != 0) end++;
            return Encoding.ASCII.GetString(b, 0, end);
        }

        private static void WriteFixed8(BinaryWriter bw, string s)
        {
            var buf = new byte[8];
            if (!string.IsNullOrEmpty(s))
            {
                byte[] enc = Encoding.ASCII.GetBytes(s);
                Array.Copy(enc, buf, Math.Min(enc.Length, 8));
            }
            bw.Write(buf);
        }

        private static string EmptyToNull(string s) =>
            string.IsNullOrEmpty(s) || s == "-" ? null : s;

        private static string NullToMinus(string s) =>
            string.IsNullOrEmpty(s) ? "-" : s;

        private static long EdgeKey(short x1, short y1, short x2, short y2) =>
            ((long)(ushort)x1)
            | ((long)(ushort)y1 << 16)
            | ((long)(ushort)x2 << 32)
            | ((long)(ushort)y2 << 48);

        // ── Internal types ────────────────────────────────────────────────────

        private struct LumpEntry
        {
            public int    FilePos;
            public int    Size;
            public string Name;
            public LumpEntry(int p, int s, string n) { FilePos = p; Size = s; Name = n; }
        }

        private struct RawSidedef
        {
            public short  XOff, YOff;
            public string Upper, Lower, Middle;
            public short  SectorRef;
            public RawSidedef(short x, short y, string up, string lo, string mid, short sec)
            { XOff = x; YOff = y; Upper = up; Lower = lo; Middle = mid; SectorRef = sec; }
        }

        // Minimal reference-equality comparer for OGLinedef dictionary keys
        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();
            public bool Equals(T x, T y)   => ReferenceEquals(x, y);
            public int  GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
