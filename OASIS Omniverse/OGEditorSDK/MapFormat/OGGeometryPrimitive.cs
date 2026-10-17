using System.Collections.Generic;

namespace OGEditorSDK.MapFormat
{
    public abstract class OGGeometryPrimitive
    {
        public abstract GeometryFamily Family { get; }
    }

    // ── Brush3D (Quake / Quake2 / Quake3 / Half-Life / Doom3) ────────────────

    public class OGBrushFace
    {
        // Three points defining the plane (as written in .map source)
        public OGVector3 P1       { get; set; }
        public OGVector3 P2       { get; set; }
        public OGVector3 P3       { get; set; }
        public OGPlane   Plane    { get; set; }   // derived from P1/P2/P3
        public string    Texture  { get; set; }
        public OGVector2 Offset   { get; set; }
        public OGVector2 Scale    { get; set; }
        public float     Rotation { get; set; }
        // Valve 220 / idTech4 UV axes — null means use standard projection
        public OGVector3? UAxis   { get; set; }
        public float      UShift  { get; set; }
        public OGVector3? VAxis   { get; set; }
        public float      VShift  { get; set; }
    }

    public class OGBrush : OGGeometryPrimitive
    {
        public override GeometryFamily Family => GeometryFamily.Brush3D;
        public List<OGBrushFace> Faces { get; set; } = new List<OGBrushFace>();
    }

    // Quake3 Bezier patch (still Brush3D family — same editor, same geometry space)
    public class OGPatch : OGGeometryPrimitive
    {
        public override GeometryFamily Family => GeometryFamily.Brush3D;
        public string      Texture       { get; set; }
        public int         Rows          { get; set; }
        public int         Cols          { get; set; }
        public OGVector3[,] ControlPoints { get; set; }   // [row, col]
        public OGVector2[,] TexCoords     { get; set; }   // [row, col], may be null
    }

    // ── Sector2D (Doom / Doom2 / Heretic / Hexen / UDMF) ─────────────────────

    public class OGVertex2D
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class OGLinedef
    {
        public OGVertex2D Start      { get; set; }
        public OGVertex2D End        { get; set; }
        // Front sidedef
        public string  UpperTexture  { get; set; }
        public string  MiddleTexture { get; set; }
        public string  LowerTexture  { get; set; }
        public float   OffsetX       { get; set; }
        public float   OffsetY       { get; set; }
        // Back sidedef (null = one-sided)
        public string  BackUpperTexture  { get; set; }
        public string  BackMiddleTexture { get; set; }
        public string  BackLowerTexture  { get; set; }
        // Game flags / specials
        public int     Flags   { get; set; }
        public int     Special { get; set; }
        public int     Tag     { get; set; }
        public bool    TwoSided => BackUpperTexture != null || BackMiddleTexture != null || BackLowerTexture != null;
    }

    public class OGSector : OGGeometryPrimitive
    {
        public override GeometryFamily Family => GeometryFamily.Sector2D;
        public List<OGLinedef> Linedefs       { get; set; } = new List<OGLinedef>();
        public float  FloorHeight             { get; set; }
        public float  CeilingHeight           { get; set; }
        public string FloorTexture            { get; set; }
        public string CeilingTexture          { get; set; }
        public int    LightLevel              { get; set; } = 160;
        public int    SectorSpecial           { get; set; }
        public int    SectorTag               { get; set; }
    }

    // ── Build2D (Duke Nukem 3D / Blood / Shadow Warrior) ─────────────────────

    public class OGBuildWall
    {
        public OGVector2 Point    { get; set; }   // start vertex of this wall
        public string    Texture  { get; set; }
        public int       PicNum   { get; set; }
        public int       OverPicNum { get; set; }
        public int       Shade    { get; set; }
        public int       Pal      { get; set; }
        public int       XRepeat  { get; set; } = 8;
        public int       YRepeat  { get; set; } = 8;
        public int       XPanning { get; set; }
        public int       YPanning { get; set; }
        public int       Cstat    { get; set; }
        public int       Lotag    { get; set; }
        public int       Hitag    { get; set; }
        public int       Extra    { get; set; }
    }

    public class OGBuildSector : OGGeometryPrimitive
    {
        public override GeometryFamily Family => GeometryFamily.Build2D;
        public List<OGBuildWall> Walls         { get; set; } = new List<OGBuildWall>();
        public short  FloorZ                   { get; set; }
        public short  CeilingZ                 { get; set; }
        public short  FloorSlope               { get; set; }
        public short  CeilingSlope             { get; set; }
        public string FloorTexture             { get; set; }
        public string CeilingTexture           { get; set; }
        public int    FloorPicNum              { get; set; }
        public int    CeilingPicNum            { get; set; }
        public int    Visibility               { get; set; }
        public int    LotTag                   { get; set; }
        public int    HiTag                    { get; set; }
        public int    Extra                    { get; set; }
    }

    // ── Tile2D (Wolfenstein 3D) ───────────────────────────────────────────────

    public class OGTile : OGGeometryPrimitive
    {
        public override GeometryFamily Family => GeometryFamily.Tile2D;
        public int    GridX    { get; set; }
        public int    GridY    { get; set; }
        public int    TileType { get; set; }   // 0=open, 1=wall, 2=door, 3=pushwall
        public int    TileId   { get; set; }   // texture index
        public string Texture  { get; set; }
    }
}
