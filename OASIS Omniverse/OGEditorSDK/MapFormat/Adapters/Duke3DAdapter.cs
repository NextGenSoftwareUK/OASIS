using System;
using System.Collections.Generic;
using System.IO;

namespace OGEditorSDK.MapFormat.Adapters
{
    /// <summary>
    /// BUILD engine .map format adapter for Duke Nukem 3D, Blood, and Shadow Warrior.
    /// Reads/writes the binary BUILD version-7 .map format.
    /// </summary>
    public class Duke3DAdapter : IOGMapFormatAdapter
    {
        public string          FormatId      => "duke3d";
        public string          DisplayName   => "Duke Nukem 3D (BUILD engine)";
        public string[]        FileExtensions => new[] { ".map" };
        public GeometryFamily  Family        => GeometryFamily.Build2D;

        // BUILD fixed-point: divide by (1 << 4) for world units
        private const float FP_SCALE = 1f / 16f;

        public bool CanRead(string filePath)
        {
            if (!File.Exists(filePath) || !filePath.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
                return false;
            try
            {
                using var fs = File.OpenRead(filePath);
                using var br = new BinaryReader(fs);
                int version = br.ReadInt32();
                return version == 7 || version == 8 || version == 9;
            }
            catch { return false; }
        }

        public OGMapIR Read(string filePath)
        {
            var ir = new OGMapIR
            {
                MapName      = Path.GetFileNameWithoutExtension(filePath),
                SourceFormat = FormatId,
                Metadata     = new OGMapMetadata()
            };

            using var fs  = File.OpenRead(filePath);
            using var br  = new BinaryReader(fs);

            int version = br.ReadInt32();
            if (version != 7 && version != 8 && version != 9)
                throw new OGMapReadException($"Unsupported BUILD map version {version}", filePath);

            // Player start
            int posX = br.ReadInt32();
            int posY = br.ReadInt32();
            int posZ = br.ReadInt32();
            short ang = br.ReadInt16();
            short startSector = br.ReadInt16();

            int numSectors = br.ReadInt16();
            var sectors = ReadSectors(br, numSectors);

            int numWalls = br.ReadInt16();
            var walls = ReadWalls(br, numWalls);

            int numSprites = br.ReadInt16();
            var sprites = ReadSprites(br, numSprites);

            // Build OGBuildSector objects
            for (int s = 0; s < numSectors; s++)
            {
                var sec = sectors[s];
                var buildSec = new OGBuildSector
                {
                    FloorZ        = (short)(sec.floorz >> 4),
                    CeilingZ      = (short)(sec.ceilingz >> 4),
                    FloorSlope    = sec.floorheinum,
                    CeilingSlope  = sec.ceilingheinum,
                    FloorPicNum   = sec.floorpicnum,
                    CeilingPicNum = sec.ceilingpicnum,
                    FloorTexture  = $"tile{sec.floorpicnum:D4}",
                    CeilingTexture= $"tile{sec.ceilingpicnum:D4}",
                    Visibility    = sec.visibility,
                    LotTag        = sec.lotag,
                    HiTag         = sec.hitag,
                    Extra         = sec.extra
                };

                for (int w = sec.wallptr; w < sec.wallptr + sec.wallnum; w++)
                {
                    var wall = walls[w];
                    buildSec.Walls.Add(new OGBuildWall
                    {
                        Point    = new OGVector2(wall.x, wall.y),
                        Texture  = $"tile{wall.picnum:D4}",
                        PicNum   = wall.picnum,
                        OverPicNum=wall.overpicnum,
                        Shade    = wall.shade,
                        XRepeat  = wall.xrepeat,
                        YRepeat  = wall.yrepeat,
                        XPanning = wall.xpanning,
                        YPanning = wall.ypanning,
                        Lotag    = wall.lotag,
                        Hitag    = wall.hitag,
                        Extra    = wall.extra
                    });
                }

                ir.WorldGeometry.Add(buildSec);
            }

            // Sprites → point entities
            foreach (var sp in sprites)
            {
                var ent = new OGPointEntity
                {
                    Classname = $"build_sprite_{sp.picnum}",
                    Origin    = new OGVector3(sp.x, sp.y, sp.z * FP_SCALE),
                    Angle     = sp.ang * 360f / 2048f
                };
                ent.Keys["picnum"]  = sp.picnum.ToString();
                ent.Keys["lotag"]   = sp.lotag.ToString();
                ent.Keys["hitag"]   = sp.hitag.ToString();
                ent.Keys["statnum"] = sp.statnum.ToString();
                ir.PointEntities.Add(ent);
            }

            return ir;
        }

        public void Write(OGMapIR map, string outputPath)
        {
            using var fs = File.Create(outputPath);
            using var bw = new BinaryWriter(fs);

            // Collect sectors and walls
            var sectors  = new List<RawSector>();
            var walls    = new List<RawWall>();
            var sprites  = new List<RawSprite>();

            foreach (var prim in map.WorldGeometry)
            {
                if (prim is OGBuildSector bs) AppendBuildSector(bs, sectors, walls);
            }
            foreach (var e in map.BrushEntities)
                foreach (var prim in e.Geometry)
                    if (prim is OGBuildSector bs) AppendBuildSector(bs, sectors, walls);

            foreach (var e in map.PointEntities)
            {
                if (!int.TryParse(e.GetKey("picnum", "0"), out int picnum)) picnum = 0;
                sprites.Add(new RawSprite
                {
                    x     = (int)e.Origin.X,
                    y     = (int)e.Origin.Y,
                    z     = (int)(e.Origin.Z / FP_SCALE),
                    ang   = (short)(e.Angle * 2048f / 360f),
                    picnum= (short)picnum
                });
            }

            // Header
            bw.Write((int)7);           // version
            bw.Write((int)0);           // posx
            bw.Write((int)0);           // posy
            bw.Write((int)0);           // posz
            bw.Write((short)0);         // ang
            bw.Write((short)0);         // cursectnum
            bw.Write((short)sectors.Count);

            foreach (var sec in sectors) WriteSector(bw, sec);
            bw.Write((short)walls.Count);
            foreach (var w in walls)     WriteWall(bw, w);
            bw.Write((short)sprites.Count);
            foreach (var sp in sprites)  WriteSprite(bw, sp);
        }

        public IEnumerable<OGConversionDiagnostic> ValidateForWrite(OGMapIR map)
        {
            foreach (var prim in map.WorldGeometry)
            {
                if (prim is OGBrush || prim is OGSector || prim is OGTile)
                    yield return new OGConversionDiagnostic(DiagnosticSeverity.Warning,
                        $"WorldGeometry contains {prim.GetType().Name} — expected OGBuildSector for BUILD format. Will be skipped.");
            }
        }

        public float ConversionFidelity(GeometryFamily src)
        {
            switch (src)
            {
                case GeometryFamily.Build2D:  return 1.0f;
                case GeometryFamily.Sector2D: return 0.70f;
                case GeometryFamily.Brush3D:  return 0.45f;
                case GeometryFamily.Tile2D:   return 0.30f;
                default:                      return 0.40f;
            }
        }

        public string RemapTexture(string sourceTexture, string sourceFormatId) => null;

        // ── Raw struct reading ────────────────────────────────────────────────

        private struct RawSector
        {
            public int   wallptr, wallnum, ceilingz, floorz;
            public short ceilingstat, floorstat, ceilingpicnum, ceilingheinum;
            public sbyte ceilingshade; public byte ceilingpal, ceilingxpanning, ceilingypanning;
            public short floorpicnum, floorheinum;
            public sbyte floorshade;   public byte floorpal, floorxpanning, floorypanning;
            public byte  visibility, filler;
            public short lotag, hitag, extra;
        }

        private struct RawWall
        {
            public int   x, y;
            public short point2, nextwall, nextsector, cstat, picnum, overpicnum;
            public sbyte shade; public byte pal, xrepeat, yrepeat, xpanning, ypanning;
            public short lotag, hitag, extra;
        }

        private struct RawSprite
        {
            public int   x, y, z;
            public short cstat, picnum;
            public sbyte shade; public byte pal, clipdist, filler, xrepeat, yrepeat;
            public sbyte xoffset, yoffset;
            public short sectnum, statnum, ang, owner, vel, xvel, yvel, zvel;
            public short lotag, hitag, extra;
        }

        private List<RawSector> ReadSectors(BinaryReader br, int count)
        {
            var list = new List<RawSector>(count);
            for (int i = 0; i < count; i++)
                list.Add(new RawSector
                {
                    wallptr        = br.ReadInt32(),
                    wallnum        = br.ReadInt32(),
                    ceilingz       = br.ReadInt32(),
                    floorz         = br.ReadInt32(),
                    ceilingstat    = br.ReadInt16(),
                    floorstat      = br.ReadInt16(),
                    ceilingpicnum  = br.ReadInt16(),
                    ceilingheinum  = br.ReadInt16(),
                    ceilingshade   = br.ReadSByte(),
                    ceilingpal     = br.ReadByte(),
                    ceilingxpanning= br.ReadByte(),
                    ceilingypanning= br.ReadByte(),
                    floorpicnum    = br.ReadInt16(),
                    floorheinum    = br.ReadInt16(),
                    floorshade     = br.ReadSByte(),
                    floorpal       = br.ReadByte(),
                    floorxpanning  = br.ReadByte(),
                    floorypanning  = br.ReadByte(),
                    visibility     = br.ReadByte(),
                    filler         = br.ReadByte(),
                    lotag          = br.ReadInt16(),
                    hitag          = br.ReadInt16(),
                    extra          = br.ReadInt16()
                });
            return list;
        }

        private List<RawWall> ReadWalls(BinaryReader br, int count)
        {
            var list = new List<RawWall>(count);
            for (int i = 0; i < count; i++)
                list.Add(new RawWall
                {
                    x          = br.ReadInt32(),
                    y          = br.ReadInt32(),
                    point2     = br.ReadInt16(),
                    nextwall   = br.ReadInt16(),
                    nextsector = br.ReadInt16(),
                    cstat      = br.ReadInt16(),
                    picnum     = br.ReadInt16(),
                    overpicnum = br.ReadInt16(),
                    shade      = br.ReadSByte(),
                    pal        = br.ReadByte(),
                    xrepeat    = br.ReadByte(),
                    yrepeat    = br.ReadByte(),
                    xpanning   = br.ReadByte(),
                    ypanning   = br.ReadByte(),
                    lotag      = br.ReadInt16(),
                    hitag      = br.ReadInt16(),
                    extra      = br.ReadInt16()
                });
            return list;
        }

        private List<RawSprite> ReadSprites(BinaryReader br, int count)
        {
            var list = new List<RawSprite>(count);
            for (int i = 0; i < count; i++)
                list.Add(new RawSprite
                {
                    x        = br.ReadInt32(),
                    y        = br.ReadInt32(),
                    z        = br.ReadInt32(),
                    cstat    = br.ReadInt16(),
                    picnum   = br.ReadInt16(),
                    shade    = br.ReadSByte(),
                    pal      = br.ReadByte(),
                    clipdist = br.ReadByte(),
                    filler   = br.ReadByte(),
                    xrepeat  = br.ReadByte(),
                    yrepeat  = br.ReadByte(),
                    xoffset  = br.ReadSByte(),
                    yoffset  = br.ReadSByte(),
                    sectnum  = br.ReadInt16(),
                    statnum  = br.ReadInt16(),
                    ang      = br.ReadInt16(),
                    owner    = br.ReadInt16(),
                    vel      = br.ReadInt16(),
                    xvel     = br.ReadInt16(),
                    yvel     = br.ReadInt16(),
                    zvel     = br.ReadInt16(),
                    lotag    = br.ReadInt16(),
                    hitag    = br.ReadInt16(),
                    extra    = br.ReadInt16()
                });
            return list;
        }

        private void AppendBuildSector(OGBuildSector bs,
            List<RawSector> sectors, List<RawWall> walls)
        {
            int wallStart = walls.Count;
            foreach (var w in bs.Walls)
            {
                walls.Add(new RawWall
                {
                    x        = (int)w.Point.X,
                    y        = (int)w.Point.Y,
                    point2   = (short)((walls.Count + 1 - wallStart < bs.Walls.Count)
                                       ? walls.Count + 1 : wallStart),
                    nextwall = -1, nextsector = -1,
                    cstat    = 0,
                    picnum   = (short)w.PicNum,
                    overpicnum=(short)w.OverPicNum,
                    shade    = (sbyte)Math.Max(-128, Math.Min(127, w.Shade)),
                    xrepeat  = (byte)Math.Max(1, w.XRepeat),
                    yrepeat  = (byte)Math.Max(1, w.YRepeat),
                    xpanning = (byte)w.XPanning,
                    ypanning = (byte)w.YPanning,
                    lotag    = (short)w.Lotag,
                    hitag    = (short)w.Hitag,
                    extra    = (short)w.Extra
                });
            }
            sectors.Add(new RawSector
            {
                wallptr  = wallStart,
                wallnum  = bs.Walls.Count,
                ceilingz = bs.CeilingZ << 4,
                floorz   = bs.FloorZ << 4,
                ceilingpicnum = (short)bs.CeilingPicNum,
                floorpicnum   = (short)bs.FloorPicNum,
                ceilingheinum = bs.CeilingSlope,
                floorheinum   = bs.FloorSlope,
                visibility    = (byte)bs.Visibility,
                lotag         = (short)bs.LotTag,
                hitag         = (short)bs.HiTag,
                extra         = (short)bs.Extra
            });
        }

        private void WriteSector(BinaryWriter bw, RawSector s)
        {
            bw.Write(s.wallptr);     bw.Write(s.wallnum);
            bw.Write(s.ceilingz);   bw.Write(s.floorz);
            bw.Write(s.ceilingstat); bw.Write(s.floorstat);
            bw.Write(s.ceilingpicnum); bw.Write(s.ceilingheinum);
            bw.Write(s.ceilingshade);  bw.Write(s.ceilingpal);
            bw.Write(s.ceilingxpanning); bw.Write(s.ceilingypanning);
            bw.Write(s.floorpicnum); bw.Write(s.floorheinum);
            bw.Write(s.floorshade);  bw.Write(s.floorpal);
            bw.Write(s.floorxpanning); bw.Write(s.floorypanning);
            bw.Write(s.visibility);  bw.Write(s.filler);
            bw.Write(s.lotag);       bw.Write(s.hitag); bw.Write(s.extra);
        }

        private void WriteWall(BinaryWriter bw, RawWall w)
        {
            bw.Write(w.x);       bw.Write(w.y);
            bw.Write(w.point2);  bw.Write(w.nextwall); bw.Write(w.nextsector);
            bw.Write(w.cstat);   bw.Write(w.picnum);   bw.Write(w.overpicnum);
            bw.Write(w.shade);   bw.Write(w.pal);
            bw.Write(w.xrepeat); bw.Write(w.yrepeat);
            bw.Write(w.xpanning);bw.Write(w.ypanning);
            bw.Write(w.lotag);   bw.Write(w.hitag);    bw.Write(w.extra);
        }

        private void WriteSprite(BinaryWriter bw, RawSprite sp)
        {
            bw.Write(sp.x);    bw.Write(sp.y);    bw.Write(sp.z);
            bw.Write(sp.cstat);bw.Write(sp.picnum);
            bw.Write(sp.shade);bw.Write(sp.pal);  bw.Write(sp.clipdist); bw.Write(sp.filler);
            bw.Write(sp.xrepeat); bw.Write(sp.yrepeat);
            bw.Write(sp.xoffset); bw.Write(sp.yoffset);
            bw.Write(sp.sectnum); bw.Write(sp.statnum); bw.Write(sp.ang);
            bw.Write(sp.owner);   bw.Write(sp.vel);
            bw.Write(sp.xvel);    bw.Write(sp.yvel);    bw.Write(sp.zvel);
            bw.Write(sp.lotag);   bw.Write(sp.hitag);   bw.Write(sp.extra);
        }
    }
}
