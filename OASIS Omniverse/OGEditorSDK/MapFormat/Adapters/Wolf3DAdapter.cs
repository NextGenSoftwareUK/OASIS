using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OGEditorSDK.MapFormat.Adapters
{
    /// <summary>
    /// Wolfenstein 3D map adapter. Reads GAMEMAPS.WL6 / MAPTEMP.WL6 (or .WL1/.SOD variants).
    /// Wolf3D maps are 64x64 grids of 16-bit tile values stored as RLEW-compressed planes.
    /// Also accepts a flat binary .wlf format (raw 64x64 uint16 plane dump) for easy round-trips.
    /// </summary>
    public class Wolf3DAdapter : IOGMapFormatAdapter
    {
        public string         FormatId       => "wolf3d";
        public string         DisplayName    => "Wolfenstein 3D";
        public string[]       FileExtensions => new[] { ".wl6", ".wl1", ".sod", ".wlf" };
        public GeometryFamily Family         => GeometryFamily.Tile2D;

        private const int MAP_SIZE = 64;
        private const ushort RLEW_TAG = 0xABCD;

        // Tile type classification (Wolf3D plane 0)
        private const ushort TILE_OPEN     = 0;
        private const ushort TILE_WALL_MAX = 63;    // 1–63 are wall tiles
        private const ushort TILE_DOOR_H   = 90;    // horizontal door
        private const ushort TILE_DOOR_V   = 91;    // vertical door
        private const ushort TILE_ELEVATOR = 92;    // elevator door
        private const ushort TILE_PUSH_MIN = 98;    // pushable walls
        private const ushort TILE_PUSH_MAX = 103;

        public bool CanRead(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".wl6" || ext == ".wl1" || ext == ".sod" || ext == ".wlf";
        }

        public OGMapIR Read(string filePath)
        {
            var ir = new OGMapIR
            {
                MapName      = Path.GetFileNameWithoutExtension(filePath),
                SourceFormat = FormatId,
                Metadata     = new OGMapMetadata()
            };

            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            ushort[] plane0, plane1;
            if (ext == ".wlf")
            {
                // Simple flat binary: 64*64*2 bytes for plane0 then plane1
                var raw = File.ReadAllBytes(filePath);
                plane0 = ReadPlane(raw, 0,          MAP_SIZE * MAP_SIZE);
                plane1 = ReadPlane(raw, MAP_SIZE * MAP_SIZE * 2, MAP_SIZE * MAP_SIZE);
            }
            else
            {
                // GAMEMAPS.WL6 — look for MAPHEAD.WL6 in same directory
                var dir  = Path.GetDirectoryName(filePath) ?? ".";
                var head = FindMapHead(dir, ext);
                if (head == null)
                    throw new OGMapReadException(
                        $"Cannot find MAPHEAD{ext} in {dir}. Place MAPHEAD and GAMEMAPS in the same folder.",
                        filePath);

                (plane0, plane1) = ReadFirstMap(head, filePath);
            }

            ReadTiles(plane0, plane1, ir);
            return ir;
        }

        public void Write(OGMapIR map, string outputPath)
        {
            // Write flat .wlf format (64x64 uint16 plane0 + plane1)
            var plane0 = new ushort[MAP_SIZE * MAP_SIZE];
            var plane1 = new ushort[MAP_SIZE * MAP_SIZE];

            foreach (var prim in map.WorldGeometry)
            {
                if (!(prim is OGTile t)) continue;
                int idx = t.GridY * MAP_SIZE + t.GridX;
                if (idx < 0 || idx >= plane0.Length) continue;

                switch (t.TileType)
                {
                    case 0: plane0[idx] = TILE_OPEN;   break;
                    case 2: plane0[idx] = TILE_DOOR_H; break;
                    case 3: plane0[idx] = TILE_PUSH_MIN; break;
                    default:
                        int tileId = t.TileId > 0 ? Math.Min(t.TileId, TILE_WALL_MAX) : 1;
                        plane0[idx] = (ushort)tileId;
                        break;
                }
            }

            foreach (var e in map.PointEntities)
            {
                int gx = (int)(e.Origin.X / 64f);
                int gy = (int)(e.Origin.Y / 64f);
                int idx = gy * MAP_SIZE + gx;
                if (idx >= 0 && idx < plane1.Length
                    && int.TryParse(e.GetKey("wolf3d_sprite"), out int sprId))
                    plane1[idx] = (ushort)sprId;
            }

            // Write the output path with .wlf extension if not already
            var outPath = Path.ChangeExtension(outputPath, ".wlf");
            using var fs = File.Create(outPath);
            using var bw = new BinaryWriter(fs);
            foreach (var v in plane0) bw.Write(v);
            foreach (var v in plane1) bw.Write(v);
        }

        public IEnumerable<OGConversionDiagnostic> ValidateForWrite(OGMapIR map)
        {
            foreach (var prim in map.WorldGeometry)
            {
                if (!(prim is OGTile))
                    yield return new OGConversionDiagnostic(DiagnosticSeverity.Warning,
                        $"Non-tile geometry ({prim.GetType().Name}) will be skipped in Wolf3D output.");
            }
        }

        public float ConversionFidelity(GeometryFamily src)
        {
            switch (src)
            {
                case GeometryFamily.Tile2D:   return 1.0f;
                case GeometryFamily.Sector2D: return 0.65f;
                case GeometryFamily.Build2D:  return 0.60f;
                case GeometryFamily.Brush3D:  return 0.30f;
                default:                      return 0.30f;
            }
        }

        public string RemapTexture(string sourceTexture, string sourceFormatId) => null;

        // ── Internal helpers ──────────────────────────────────────────────────

        private void ReadTiles(ushort[] plane0, ushort[] plane1, OGMapIR ir)
        {
            for (int y = 0; y < MAP_SIZE; y++)
            {
                for (int x = 0; x < MAP_SIZE; x++)
                {
                    int idx  = y * MAP_SIZE + x;
                    ushort p0 = plane0[idx];
                    ushort p1 = plane1[idx];

                    int tileType = ClassifyTile(p0);
                    ir.WorldGeometry.Add(new OGTile
                    {
                        GridX    = x,
                        GridY    = y,
                        TileType = tileType,
                        TileId   = p0,
                        Texture  = tileType > 0 ? $"wall{p0:D3}" : null
                    });

                    // Plane 1: sprites/objects → point entities
                    if (p1 > 0)
                    {
                        var ent = new OGPointEntity
                        {
                            Classname = $"wolf3d_sprite_{p1}",
                            Origin    = new OGVector3(x * 64f + 32f, y * 64f + 32f, 0)
                        };
                        ent.Keys["wolf3d_sprite"] = p1.ToString();
                        ir.PointEntities.Add(ent);
                    }
                }
            }
        }

        private static int ClassifyTile(ushort v)
        {
            if (v == TILE_OPEN) return 0;
            if (v >= 1 && v <= TILE_WALL_MAX) return 1;          // wall
            if (v == TILE_DOOR_H || v == TILE_DOOR_V
                || v == TILE_ELEVATOR) return 2;                   // door
            if (v >= TILE_PUSH_MIN && v <= TILE_PUSH_MAX) return 3; // pushwall
            return 1;  // treat unknown solids as walls
        }

        private static ushort[] ReadPlane(byte[] raw, int byteOffset, int count)
        {
            var result = new ushort[count];
            for (int i = 0; i < count && byteOffset + 1 < raw.Length; i++, byteOffset += 2)
                result[i] = (ushort)(raw[byteOffset] | (raw[byteOffset + 1] << 8));
            return result;
        }

        private static string FindMapHead(string dir, string ext)
        {
            var candidate = Path.Combine(dir, "MAPHEAD" + ext.ToUpperInvariant());
            if (File.Exists(candidate)) return candidate;
            candidate = Path.Combine(dir, "MAPHEAD" + ext.ToLowerInvariant());
            if (File.Exists(candidate)) return candidate;
            return null;
        }

        private (ushort[] plane0, ushort[] plane1) ReadFirstMap(string mapHeadPath, string gameMapsPath)
        {
            // Read MAPHEAD: magic + 100 int32 offsets
            var headBytes = File.ReadAllBytes(mapHeadPath);
            if (headBytes.Length < 202)
                throw new OGMapReadException("MAPHEAD file too small.");
            ushort magic = (ushort)(headBytes[0] | (headBytes[1] << 8));
            // RLEW tag is stored as magic in MAPHEAD

            // Find first non-zero offset
            int mapOffset = -1;
            for (int i = 0; i < 100; i++)
            {
                int off = BitConverter.ToInt32(headBytes, 2 + i * 4);
                if (off > 0) { mapOffset = off; break; }
            }
            if (mapOffset < 0)
                throw new OGMapReadException("No maps found in MAPHEAD.");

            var gameBytes = File.ReadAllBytes(gameMapsPath);

            // Read map header at mapOffset (38 bytes)
            // int32 plane0off, plane1off, plane2off
            // short plane0len, plane1len, plane2len
            // short width, height
            // char[16] name
            // char[4] sig
            if (mapOffset + 38 > gameBytes.Length)
                throw new OGMapReadException("Map offset out of bounds in GAMEMAPS.");

            int p0off = BitConverter.ToInt32(gameBytes, mapOffset);
            int p1off = BitConverter.ToInt32(gameBytes, mapOffset + 4);
            int p0len = BitConverter.ToInt16(gameBytes, mapOffset + 12);
            int p1len = BitConverter.ToInt16(gameBytes, mapOffset + 14);

            var plane0 = DecompressRLEW(gameBytes, p0off, p0len, magic, MAP_SIZE * MAP_SIZE);
            var plane1 = DecompressRLEW(gameBytes, p1off, p1len, magic, MAP_SIZE * MAP_SIZE);
            return (plane0, plane1);
        }

        private static ushort[] DecompressRLEW(byte[] src, int offset, int compLen,
            ushort rlewTag, int outputWords)
        {
            var result = new ushort[outputWords];
            int outIdx = 0;

            // Skip the first 2 bytes (uncompressed size word)
            int i = offset + 2;
            int end = offset + compLen;

            while (i < end && outIdx < outputWords)
            {
                ushort val = (ushort)(src[i] | (src[i + 1] << 8));
                i += 2;
                if (val == rlewTag)
                {
                    ushort count = (ushort)(src[i] | (src[i + 1] << 8)); i += 2;
                    ushort data  = (ushort)(src[i] | (src[i + 1] << 8)); i += 2;
                    for (int r = 0; r < count && outIdx < outputWords; r++)
                        result[outIdx++] = data;
                }
                else
                {
                    result[outIdx++] = val;
                }
            }
            return result;
        }
    }
}
