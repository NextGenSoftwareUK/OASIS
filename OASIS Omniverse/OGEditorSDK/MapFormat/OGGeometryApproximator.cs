using System;
using System.Collections.Generic;

namespace OGEditorSDK.MapFormat
{
    /// <summary>
    /// Converts geometry between incompatible families (Brush3D, Sector2D, Build2D, Tile2D).
    /// All cross-family conversions are approximate — results require manual cleanup.
    /// </summary>
    public static class OGGeometryApproximator
    {
        private const float GRID = 64f;  // standard game grid unit

        public static OGMapIR Approximate(OGMapIR map, GeometryFamily src, GeometryFamily dst)
        {
            if (src == dst) return map;

            map.WorldGeometry = ConvertList(map.WorldGeometry, src, dst);
            foreach (var e in map.BrushEntities)
                e.Geometry = ConvertList(e.Geometry, src, dst);

            return map;
        }

        private static List<OGGeometryPrimitive> ConvertList(
            List<OGGeometryPrimitive> prims, GeometryFamily src, GeometryFamily dst)
        {
            var result = new List<OGGeometryPrimitive>(prims.Count);
            foreach (var p in prims)
                result.AddRange(ConvertPrimitive(p, src, dst));
            return result;
        }

        private static IEnumerable<OGGeometryPrimitive> ConvertPrimitive(
            OGGeometryPrimitive prim, GeometryFamily src, GeometryFamily dst)
        {
            if (src == GeometryFamily.Sector2D && dst == GeometryFamily.Brush3D)
                return Sector2DToBrush3D(prim as OGSector);
            if (src == GeometryFamily.Brush3D && dst == GeometryFamily.Sector2D)
                return Brush3DToSector2D(prim as OGBrush);
            if (src == GeometryFamily.Tile2D && dst == GeometryFamily.Brush3D)
                return Tile2DToBrush3D(prim as OGTile);
            if (src == GeometryFamily.Tile2D && dst == GeometryFamily.Sector2D)
                return Tile2DToSector2D(prim as OGTile);
            if (src == GeometryFamily.Build2D && dst == GeometryFamily.Brush3D)
                return Build2DToBrush3D(prim as OGBuildSector);
            if (src == GeometryFamily.Build2D && dst == GeometryFamily.Sector2D)
                return Build2DToSector2D(prim as OGBuildSector);
            if (src == GeometryFamily.Brush3D && dst == GeometryFamily.Tile2D)
                return Brush3DToTile2D(prim as OGBrush);

            return new[] { prim };  // same family or unhandled — pass through
        }

        // ── Sector2D → Brush3D ────────────────────────────────────────────────
        // Extrude each sector's floor and ceiling into box brushes using the linedef
        // walls. Produces clean structural brushes; slope specials are dropped.

        private static IEnumerable<OGGeometryPrimitive> Sector2DToBrush3D(OGSector sector)
        {
            if (sector == null) yield break;
            float floor   = sector.FloorHeight;
            float ceiling = sector.CeilingHeight;

            // Wall brushes — one per linedef
            foreach (var ld in sector.Linedefs)
            {
                yield return MakeWallBrush(
                    ld.Start.X, ld.Start.Y,
                    ld.End.X,   ld.End.Y,
                    floor, ceiling,
                    ld.MiddleTexture ?? "NULL");
            }

            // Floor brush (flat 8-unit slab)
            yield return MakeFloorBrush(sector.Linedefs, floor, sector.FloorTexture ?? "NULL");
            // Ceiling brush (flat 8-unit slab)
            yield return MakeCeilingBrush(sector.Linedefs, ceiling, sector.CeilingTexture ?? "NULL");
        }

        private static OGBrush MakeWallBrush(float x1, float y1, float x2, float y2,
            float zFloor, float zCeiling, string tex)
        {
            // Wall direction
            float dx = x2 - x1, dy = y2 - y1;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) return new OGBrush();
            float nx = -dy / len, ny = dx / len;  // outward normal

            const float thick = 8f;
            var brush = new OGBrush();

            // 6 faces: front, back, left cap, right cap, bottom, top
            OGVector3 p00 = new OGVector3(x1, y1, zFloor);
            OGVector3 p10 = new OGVector3(x2, y2, zFloor);
            OGVector3 p01 = new OGVector3(x1, y1, zCeiling);
            OGVector3 p11 = new OGVector3(x2, y2, zCeiling);
            OGVector3 p00b = new OGVector3(x1 + nx * thick, y1 + ny * thick, zFloor);
            OGVector3 p10b = new OGVector3(x2 + nx * thick, y2 + ny * thick, zFloor);
            OGVector3 p01b = new OGVector3(x1 + nx * thick, y1 + ny * thick, zCeiling);

            brush.Faces.Add(MakeFace(p00, p10, p01, tex));
            brush.Faces.Add(MakeFace(p00b, p01b, p10b, tex));
            brush.Faces.Add(MakeFace(p00, p01, p00b, tex));
            brush.Faces.Add(MakeFace(p10, p10b, p11, tex));
            brush.Faces.Add(MakeFace(p00, p00b, p10b, "NULL"));
            brush.Faces.Add(MakeFace(p01, p11, p01b, "NULL"));

            return brush;
        }

        private static OGBrush MakeFloorBrush(List<OGLinedef> linedefs, float z, string tex)
        {
            // Simple bounding-box slab
            float minX = float.MaxValue, minY = float.MaxValue,
                  maxX = float.MinValue, maxY = float.MinValue;
            foreach (var ld in linedefs)
            {
                Expand(ref minX, ref minY, ref maxX, ref maxY, ld.Start.X, ld.Start.Y);
                Expand(ref minX, ref minY, ref maxX, ref maxY, ld.End.X,   ld.End.Y);
            }
            return MakeBoxBrush(minX, minY, z - 8f, maxX, maxY, z, tex);
        }

        private static OGBrush MakeCeilingBrush(List<OGLinedef> linedefs, float z, string tex)
        {
            float minX = float.MaxValue, minY = float.MaxValue,
                  maxX = float.MinValue, maxY = float.MinValue;
            foreach (var ld in linedefs)
            {
                Expand(ref minX, ref minY, ref maxX, ref maxY, ld.Start.X, ld.Start.Y);
                Expand(ref minX, ref minY, ref maxX, ref maxY, ld.End.X,   ld.End.Y);
            }
            return MakeBoxBrush(minX, minY, z, maxX, maxY, z + 8f, tex);
        }

        // ── Brush3D → Sector2D ────────────────────────────────────────────────
        // Project each brush onto the XY plane; build a 4-sided sector from its
        // 2D bounding box. Vertical complexity is lost.

        private static IEnumerable<OGGeometryPrimitive> Brush3DToSector2D(OGBrush brush)
        {
            if (brush == null) yield break;

            float minX = float.MaxValue, minY = float.MaxValue,
                  maxX = float.MinValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            string floorTex = "FLOOR4_8", ceilTex = "CEIL3_5";

            foreach (var face in brush.Faces)
            {
                Update2D(ref minX, ref minY, ref maxX, ref maxY, face.P1.X, face.P1.Y);
                Update2D(ref minX, ref minY, ref maxX, ref maxY, face.P2.X, face.P2.Y);
                Update2D(ref minX, ref minY, ref maxX, ref maxY, face.P3.X, face.P3.Y);
                if (face.P1.Z < minZ) { minZ = face.P1.Z; floorTex = face.Texture ?? floorTex; }
                if (face.P1.Z > maxZ) { maxZ = face.P1.Z; ceilTex  = face.Texture ?? ceilTex;  }
            }

            yield return MakeBoxSector(minX, minY, maxX, maxY,
                (int)Math.Round(minZ), (int)Math.Round(maxZ),
                floorTex, ceilTex);
        }

        // ── Tile2D → Brush3D ──────────────────────────────────────────────────
        // Each solid tile becomes a 64×64×64 unit box brush.

        private static IEnumerable<OGGeometryPrimitive> Tile2DToBrush3D(OGTile tile)
        {
            if (tile == null) yield break;
            if (tile.TileType == 0) yield break;  // open space

            float x0 = tile.GridX * GRID, y0 = tile.GridY * GRID;
            yield return MakeBoxBrush(x0, y0, 0, x0 + GRID, y0 + GRID, GRID,
                tile.Texture ?? "NULL");
        }

        // ── Tile2D → Sector2D ─────────────────────────────────────────────────

        private static IEnumerable<OGGeometryPrimitive> Tile2DToSector2D(OGTile tile)
        {
            if (tile == null || tile.TileType == 0) yield break;
            float x0 = tile.GridX * GRID, y0 = tile.GridY * GRID;
            yield return MakeBoxSector(x0, y0, x0 + GRID, y0 + GRID, 0, (int)GRID,
                tile.Texture ?? "FLOOR4_8", "CEIL3_5");
        }

        // ── Build2D → Brush3D ─────────────────────────────────────────────────
        // Extrude BUILD sector walls into brushes using floor/ceiling Z values.

        private static IEnumerable<OGGeometryPrimitive> Build2DToBrush3D(OGBuildSector bs)
        {
            if (bs == null || bs.Walls.Count < 3) yield break;

            float floor   = bs.FloorZ   / 16f;
            float ceiling = bs.CeilingZ / 16f;

            for (int i = 0; i < bs.Walls.Count; i++)
            {
                var w1 = bs.Walls[i];
                var w2 = bs.Walls[(i + 1) % bs.Walls.Count];
                yield return MakeWallBrush(w1.Point.X, w1.Point.Y, w2.Point.X, w2.Point.Y,
                    floor, ceiling, w1.Texture ?? "NULL");
            }
            // Floor and ceiling slabs from bounding box
            float minX = float.MaxValue, minY = float.MaxValue,
                  maxX = float.MinValue, maxY = float.MinValue;
            foreach (var w in bs.Walls)
                Expand(ref minX, ref minY, ref maxX, ref maxY, w.Point.X, w.Point.Y);
            yield return MakeBoxBrush(minX, minY, floor - 8, maxX, maxY, floor,
                bs.FloorTexture ?? "NULL");
            yield return MakeBoxBrush(minX, minY, ceiling, maxX, maxY, ceiling + 8,
                bs.CeilingTexture ?? "NULL");
        }

        // ── Build2D → Sector2D ────────────────────────────────────────────────

        private static IEnumerable<OGGeometryPrimitive> Build2DToSector2D(OGBuildSector bs)
        {
            if (bs == null || bs.Walls.Count < 3) yield break;

            var sector = new OGSector
            {
                FloorHeight   = bs.FloorZ   / 16f,
                CeilingHeight = bs.CeilingZ / 16f,
                FloorTexture  = bs.FloorTexture   ?? "FLOOR4_8",
                CeilingTexture= bs.CeilingTexture ?? "CEIL3_5",
                LightLevel    = Math.Max(0, Math.Min(255, 160 - bs.Visibility))
            };

            for (int i = 0; i < bs.Walls.Count; i++)
            {
                var w1 = bs.Walls[i];
                var w2 = bs.Walls[(i + 1) % bs.Walls.Count];
                sector.Linedefs.Add(new OGLinedef
                {
                    Start        = new OGVertex2D { X = w1.Point.X, Y = w1.Point.Y },
                    End          = new OGVertex2D { X = w2.Point.X, Y = w2.Point.Y },
                    MiddleTexture= w1.Texture ?? "-",
                    Flags        = 1
                });
            }
            yield return sector;
        }

        // ── Brush3D → Tile2D ──────────────────────────────────────────────────
        // Project each brush onto the tile grid. Coarse approximation.

        private static IEnumerable<OGGeometryPrimitive> Brush3DToTile2D(OGBrush brush)
        {
            if (brush == null) yield break;

            float minX = float.MaxValue, minY = float.MaxValue,
                  maxX = float.MinValue, maxY = float.MinValue;
            string tex = "NULL";
            foreach (var face in brush.Faces)
            {
                Update2D(ref minX, ref minY, ref maxX, ref maxY, face.P1.X, face.P1.Y);
                Update2D(ref minX, ref minY, ref maxX, ref maxY, face.P2.X, face.P2.Y);
                Update2D(ref minX, ref minY, ref maxX, ref maxY, face.P3.X, face.P3.Y);
                tex = face.Texture ?? tex;
            }

            int gx0 = (int)Math.Floor(minX / GRID);
            int gy0 = (int)Math.Floor(minY / GRID);
            int gx1 = (int)Math.Ceiling(maxX / GRID);
            int gy1 = (int)Math.Ceiling(maxY / GRID);

            for (int gx = gx0; gx < gx1; gx++)
                for (int gy = gy0; gy < gy1; gy++)
                    yield return new OGTile { GridX = gx, GridY = gy, TileType = 1, Texture = tex };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static OGBrushFace MakeFace(OGVector3 a, OGVector3 b, OGVector3 c, string tex)
        {
            return new OGBrushFace
            {
                P1      = a, P2 = b, P3 = c,
                Plane   = OGPlane.FromPoints(a, b, c),
                Texture = tex,
                Scale   = new OGVector2(1, 1)
            };
        }

        private static OGBrush MakeBoxBrush(float x0, float y0, float z0,
                                             float x1, float y1, float z1, string tex)
        {
            var b = new OGBrush();
            // 6 axis-aligned faces
            b.Faces.Add(MakeFace(new OGVector3(x0,y0,z0), new OGVector3(x0,y1,z0), new OGVector3(x0,y0,z1), tex));
            b.Faces.Add(MakeFace(new OGVector3(x1,y0,z0), new OGVector3(x1,y0,z1), new OGVector3(x1,y1,z0), tex));
            b.Faces.Add(MakeFace(new OGVector3(x0,y0,z0), new OGVector3(x1,y0,z0), new OGVector3(x0,y1,z0), tex));
            b.Faces.Add(MakeFace(new OGVector3(x0,y1,z0), new OGVector3(x0,y1,z1), new OGVector3(x1,y1,z0), tex));
            b.Faces.Add(MakeFace(new OGVector3(x0,y0,z0), new OGVector3(x0,y0,z1), new OGVector3(x1,y0,z0), "NULL"));
            b.Faces.Add(MakeFace(new OGVector3(x0,y0,z1), new OGVector3(x0,y1,z1), new OGVector3(x1,y0,z1), "NULL"));
            return b;
        }

        private static OGSector MakeBoxSector(float x0, float y0, float x1, float y1,
            int floor, int ceiling, string floorTex, string ceilTex)
        {
            var s = new OGSector
            {
                FloorHeight    = floor,
                CeilingHeight  = ceiling,
                FloorTexture   = floorTex,
                CeilingTexture = ceilTex,
                LightLevel     = 160
            };
            // 4 linedefs forming the bounding box (clockwise)
            s.Linedefs.Add(Ld(x0, y0, x1, y0));
            s.Linedefs.Add(Ld(x1, y0, x1, y1));
            s.Linedefs.Add(Ld(x1, y1, x0, y1));
            s.Linedefs.Add(Ld(x0, y1, x0, y0));
            return s;
        }

        private static OGLinedef Ld(float x1, float y1, float x2, float y2) =>
            new OGLinedef
            {
                Start        = new OGVertex2D { X = x1, Y = y1 },
                End          = new OGVertex2D { X = x2, Y = y2 },
                MiddleTexture= "STARTAN2",
                Flags        = 1
            };

        private static void Expand(ref float minX, ref float minY,
                                    ref float maxX, ref float maxY, float x, float y)
        {
            if (x < minX) minX = x; if (x > maxX) maxX = x;
            if (y < minY) minY = y; if (y > maxY) maxY = y;
        }

        private static void Update2D(ref float minX, ref float minY,
                                      ref float maxX, ref float maxY, float x, float y)
            => Expand(ref minX, ref minY, ref maxX, ref maxY, x, y);
    }
}
