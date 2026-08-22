# OGMapFormat SDK

Specification for the modular, community-extensible map format conversion system.
The SDK defines a neutral **Intermediate Representation (IR)** that all format adapters
read into and write from, so any two supported formats can be converted via
`read → IR → write` without N² custom converters.

---

## 1. Design Principles

- **One IR, many adapters.** Every format adapter speaks to `OGMapIR`. Adding a new
  game format never requires touching existing adapters.
- **Honest fidelity.** Cross-family conversions (3D brushes ↔ 2D sectors) are lossy.
  Adapters declare a `ConversionFidelity` score so users know what to expect before
  committing to a conversion.
- **Community-extensible.** A third-party adapter is a single DLL dropped into the
  OASIS adapters folder — no recompilation of OGEditorSDK required.
- **OASIS-aware.** The IR carries the full `OGMapSidecar` (portal pairs, OASIS thing
  types, map registration metadata) so OASIS data survives round-trips through any
  converter.

---

## 2. Geometry Families

Map formats divide into four geometry families. Conversion within a family is
high-fidelity; conversion across families is approximate and requires manual cleanup.

| Family | Formats | Geometry primitive |
|--------|---------|--------------------|
| `Brush3D` | Quake, Quake2, Quake3, Half-Life, Quake4, Doom3 | Convex polyhedra defined by planes |
| `Sector2D` | Doom, Doom2, Heretic, Hexen, Strife, UDMF | 2D linedefs → sectors with floor/ceiling heights |
| `Tile2D` | Wolfenstein 3D | Fixed-size grid cells |
| `Build2D` | Duke Nukem 3D, Blood, Shadow Warrior | BUILD-engine sectors with slopes |

```csharp
public enum GeometryFamily
{
    Brush3D,
    Sector2D,
    Build2D,
    Tile2D
}
```

Cross-family fidelity guide:

| From → To | Brush3D | Sector2D | Build2D | Tile2D |
|-----------|---------|----------|---------|--------|
| Brush3D   | 1.0     | 0.55†    | 0.40    | 0.15   |
| Sector2D  | 0.75‡   | 1.0      | 0.70    | 0.10   |
| Build2D   | 0.65    | 0.65     | 1.0     | 0.10   |
| Tile2D    | 0.80§   | 0.70     | 0.65    | 1.0    |

† Brush3D → Sector2D: brushes projected onto XY plane; vertical complexity lost.
‡ Sector2D → Brush3D: floor/ceiling extruded into box brushes; good structural result
  but detail work (slopes, specials) needs manual cleanup.
§ Tile2D → Brush3D: each tile becomes a box brush; clean grid result, easily editable.

---

## 3. OGMapIR — Intermediate Representation

### Top-level

```csharp
namespace OGEditorSDK.MapFormat
{
    public class OGMapIR
    {
        public string              MapName       { get; set; }
        public string              SourceFormat  { get; set; }   // e.g. "quake2"
        public OGMapMetadata       Metadata      { get; set; }
        public List<OGPointEntity> PointEntities { get; set; } = new();
        public List<OGBrushEntity> BrushEntities { get; set; } = new();

        // World geometry not owned by a brush entity (worldspawn brushes, sectors, tiles)
        public List<OGGeometryPrimitive> WorldGeometry { get; set; } = new();

        // OASIS portal topology and registration metadata (preserved through conversion)
        public OGMapSidecar        OASISSidecar  { get; set; }
    }
}
```

### Metadata

```csharp
public class OGMapMetadata
{
    public string    SkyTexture    { get; set; }
    public string    MusicTrack    { get; set; }
    public string    Author        { get; set; }
    public string    Description   { get; set; }
    public OGVector3 WorldMin      { get; set; }
    public OGVector3 WorldMax      { get; set; }
    public OGColor   AmbientLight  { get; set; }
    public OGFog     Fog           { get; set; }
    // Format-specific extras that don't map to any IR field but should round-trip
    public Dictionary<string, string> Extra { get; set; } = new();
}
```

### Entities

```csharp
public class OGEntity
{
    public string                     Classname      { get; set; }
    public int                        OASISThingType { get; set; } = -1;  // -1 = not OASIS
    public Dictionary<string, string> Keys           { get; set; } = new();
    public OGVector3                  Origin         { get; set; }
    public float                      Angle          { get; set; }
}

public class OGPointEntity : OGEntity { }

public class OGBrushEntity : OGEntity
{
    public List<OGGeometryPrimitive> Geometry { get; set; } = new();
}
```

### Geometry primitives

```csharp
public abstract class OGGeometryPrimitive
{
    public abstract GeometryFamily Family { get; }
}

// ── Brush3D ───────────────────────────────────────────────────────────────

public class OGBrush : OGGeometryPrimitive
{
    public override GeometryFamily    Family { get; } = GeometryFamily.Brush3D;
    public List<OGBrushFace>         Faces  { get; set; } = new();
}

public class OGBrushFace
{
    public OGPlane      Plane       { get; set; }
    public string       Texture     { get; set; }
    public OGVector2    Offset      { get; set; }
    public OGVector2    Scale       { get; set; }
    public float        Rotation    { get; set; }
    // Valve 220 UV axes (if present; otherwise null → use plane projection)
    public OGVector3?   UAxis       { get; set; }
    public OGVector3?   VAxis       { get; set; }
}

// ── Sector2D ──────────────────────────────────────────────────────────────

public class OGSector : OGGeometryPrimitive
{
    public override GeometryFamily  Family         { get; } = GeometryFamily.Sector2D;
    public List<OGLinedef>          Linedefs       { get; set; } = new();
    public float                    FloorHeight    { get; set; }
    public float                    CeilingHeight  { get; set; }
    public string                   FloorTexture   { get; set; }
    public string                   CeilingTexture { get; set; }
    public int                      LightLevel     { get; set; }
    public int                      SectorSpecial  { get; set; }
    public int                      SectorTag      { get; set; }
}

public class OGLinedef
{
    public OGVector2    Start       { get; set; }
    public OGVector2    End         { get; set; }
    public string       UpperTex    { get; set; }
    public string       MiddleTex   { get; set; }
    public string       LowerTex    { get; set; }
    public int          Flags       { get; set; }
    public int          Special     { get; set; }
    public int          Tag         { get; set; }
    public bool         TwoSided    { get; set; }
}

// ── Build2D ───────────────────────────────────────────────────────────────

public class OGBuildSector : OGGeometryPrimitive
{
    public override GeometryFamily  Family        { get; } = GeometryFamily.Build2D;
    public List<OGVector2>          Walls         { get; set; } = new();
    public short                    FloorZ        { get; set; }
    public short                    CeilingZ      { get; set; }
    public short                    FloorSlope    { get; set; }
    public short                    CeilingSlope  { get; set; }
    public short                    FloorPicnum   { get; set; }
    public short                    CeilingPicnum { get; set; }
    public short                    Visibility    { get; set; }
}

// ── Tile2D ────────────────────────────────────────────────────────────────

public class OGTile : OGGeometryPrimitive
{
    public override GeometryFamily Family   { get; } = GeometryFamily.Tile2D;
    public int                     GridX    { get; set; }
    public int                     GridY    { get; set; }
    public int                     TileType { get; set; }   // wall=1, door=2, ...
    public string                  Texture  { get; set; }
}

// ── Patch3D (Quake3 Bezier curves) ───────────────────────────────────────

public class OGPatch : OGGeometryPrimitive
{
    public override GeometryFamily Family         { get; } = GeometryFamily.Brush3D;
    public OGVector3[,]            ControlPoints  { get; set; }
    public string                  Texture        { get; set; }
}
```

### Value types

```csharp
public record OGVector2(float X, float Y);
public record OGVector3(float X, float Y, float Z);
public record OGPlane(OGVector3 Normal, float Distance);
public record OGColor(byte R, byte G, byte B, byte A = 255);
public record OGFog(OGColor Color, float Density);
```

---

## 4. IOGMapFormatAdapter — The Adapter Interface

Every built-in and community adapter implements this interface. The SDK discovers
and loads adapters at startup; no registration code in the core is needed.

```csharp
namespace OGEditorSDK.MapFormat
{
    /// <summary>
    /// Implement this interface and ship as a DLL in the OASIS adapters folder
    /// to add support for a new map format. The SDK discovers it automatically.
    /// </summary>
    public interface IOGMapFormatAdapter
    {
        // ── Identity ──────────────────────────────────────────────────────

        /// <summary>Short, stable identifier — "quake2", "doom", "goldsrc", etc.</summary>
        string FormatId { get; }

        /// <summary>Human-readable name shown in the UI.</summary>
        string DisplayName { get; }

        /// <summary>File extensions this adapter handles, e.g. [".map", ".bsp"].</summary>
        string[] FileExtensions { get; }

        /// <summary>Geometry family — determines cross-family fidelity.</summary>
        GeometryFamily Family { get; }

        // ── Read ──────────────────────────────────────────────────────────

        /// <summary>Returns true if this adapter can read the given file.</summary>
        bool CanRead(string filePath);

        /// <summary>
        /// Read the map file and return a populated OGMapIR.
        /// Throw OGMapReadException on unrecoverable parse errors.
        /// </summary>
        OGMapIR Read(string filePath);

        // ── Write ─────────────────────────────────────────────────────────

        /// <summary>
        /// Write the OGMapIR to the destination format.
        /// Call ValidateForWrite first; this method may throw on irrecoverable problems.
        /// </summary>
        void Write(OGMapIR map, string outputPath);

        /// <summary>
        /// Validate an OGMapIR before writing. Returns warnings and errors.
        /// Callers should show these to the user before proceeding.
        /// </summary>
        IEnumerable<OGConversionDiagnostic> ValidateForWrite(OGMapIR map);

        // ── Fidelity ──────────────────────────────────────────────────────

        /// <summary>
        /// Estimated fidelity of converting FROM the given source family TO this format.
        /// 1.0 = lossless, 0.5 = significant manual cleanup required, 0.0 = not viable.
        /// </summary>
        float ConversionFidelity(GeometryFamily sourceFamily);

        // ── Texture mapping ───────────────────────────────────────────────

        /// <summary>
        /// Optional: map a texture name from the source format to this format's equivalent.
        /// Return null to keep the original name. Used by cross-game texture remapping.
        /// </summary>
        string? RemapTexture(string sourceTexture, string sourceFormatId) => null;
    }
}
```

### Diagnostics

```csharp
public class OGConversionDiagnostic
{
    public DiagnosticSeverity Severity    { get; set; }  // Info, Warning, Error
    public string             Message     { get; set; }
    public string?            EntityClass { get; set; }  // which entity caused it
    public OGVector3?         Location    { get; set; }  // where in the map
}

public enum DiagnosticSeverity { Info, Warning, Error }
```

---

## 5. The Conversion Pipeline

A conversion is always: **Read → IR → Validate → (optionally: geometry approximate) → Write**

```csharp
public class OGMapConverter
{
    private readonly IReadOnlyList<IOGMapFormatAdapter> _adapters;

    public OGMapConversionResult Convert(
        string srcPath, string srcFormatId,
        string dstPath, string dstFormatId)
    {
        var src = _adapters.First(a => a.FormatId == srcFormatId);
        var dst = _adapters.First(a => a.FormatId == dstFormatId);

        // 1. Read
        OGMapIR ir = src.Read(srcPath);

        // 2. Remap OASIS entity classnames (src classnames → OASIS thing types → dst classnames)
        ir = OGEntityRemapper.Remap(ir, srcFormatId, dstFormatId);

        // 3. If geometry families differ, apply approximate geometry conversion
        if (src.Family != dst.Family)
            ir = OGGeometryApproximator.Approximate(ir, src.Family, dst.Family);

        // 4. Remap textures
        ir = OGTextureRemapper.Remap(ir, src, dst);

        // 5. Validate for destination format
        var diagnostics = dst.ValidateForWrite(ir).ToList();

        // 6. Write (unless hard errors)
        if (diagnostics.All(d => d.Severity != DiagnosticSeverity.Error))
            dst.Write(ir, dstPath);

        return new OGMapConversionResult
        {
            SourceFormat     = srcFormatId,
            DestinationFormat= dstFormatId,
            Fidelity         = dst.ConversionFidelity(src.Family),
            Diagnostics      = diagnostics,
            OutputPath       = dstPath
        };
    }
}
```

The `OGGeometryApproximator` handles the lossy cross-family cases:

| Conversion | Strategy |
|------------|----------|
| Sector2D → Brush3D | Extrude each sector linedef into a wall brush using floor/ceiling heights. Floor and ceiling become flat brushes. |
| Brush3D → Sector2D | Project brush outlines onto the XY plane; build linedefs from the silhouette. Vertical brushes become ceiling-height sectors. Overlapping brushes are merged. |
| Tile2D → Brush3D | Each solid tile becomes a 64×64×64 box brush. Door tiles become brush entities with `func_door`. |
| Build2D → Brush3D | Build sector walls extruded into brushes using floor/ceiling heights; slopes approximated. |
| Brush3D → Build2D | Similar to Brush3D → Sector2D but using BUILD sector primitives. |
| Tile2D → Sector2D | Grid cells projected to floor sectors. |

---

## 6. Built-in Adapters (shipped with OGEditorSDK)

| FormatId | DisplayName | Extensions | Family | Games |
|----------|------------|------------|--------|-------|
| `quake` | Quake | `.map`, `.bsp`† | Brush3D | OQuake |
| `quake2` | Quake2 | `.map`, `.bsp`† | Brush3D | OQuake2, OQuake2-RTX |
| `quake3` | Quake3 | `.map` | Brush3D | OQuake3 |
| `doom` | Doom / Doom2 | `.wad` | Sector2D | ODOOM |
| `udmf` | UDMF | `.udmf` | Sector2D | ODOOM (modern maps) |
| `doom3` | Doom3 | `.map` | Brush3D | ODOOM3, ODOOM3-BFG |
| `duke3d` | Duke Nukem 3D | `.map` | Build2D | ODuke3D, ODuke3D-RT |
| `wolf3d` | Wolfenstein 3D | `.WL6`, `.SOD` | Tile2D | OWolf3D |

† `.bsp` read-only (compiled format; write requires a game's map compiler tool).

---

## 7. Community Adapter Guide

### Creating an adapter (C#)

1. Create a .NET Standard 2.0 class library.
2. Reference `OGEditorSDK.MapFormat` NuGet package.
3. Implement `IOGMapFormatAdapter`.
4. Ship the DLL to the adapters folder — the SDK finds it automatically.

Minimal example for Half-Life / Goldsrc (Brush3D, high fidelity — same family as Quake):

```csharp
using OGEditorSDK.MapFormat;

[assembly: OGMapFormatAdapter]   // discovery attribute

public class GoldsrcAdapter : IOGMapFormatAdapter
{
    public string        FormatId      => "goldsrc";
    public string        DisplayName   => "Half-Life / Goldsrc";
    public string[]      FileExtensions => [".map", ".rmf"];
    public GeometryFamily Family       => GeometryFamily.Brush3D;

    public bool CanRead(string path) =>
        File.Exists(path) && path.EndsWith(".map", StringComparison.OrdinalIgnoreCase);

    public OGMapIR Read(string path)
    {
        // Parse Goldsrc .map format (Quake-format .map with extra keys)
        // Goldsrc uses Valve 220 UV format — populate OGBrushFace.UAxis/VAxis
        var ir = new OGMapIR { MapName = Path.GetFileNameWithoutExtension(path),
                               SourceFormat = FormatId };
        // ... parsing logic ...
        return ir;
    }

    public void Write(OGMapIR map, string outputPath)
    {
        // Serialize to Goldsrc .map format
        // OASIS entities: write as info_target with oasis_thing_type key
    }

    public IEnumerable<OGConversionDiagnostic> ValidateForWrite(OGMapIR map)
    {
        // Warn if any brush has > 32 faces (Goldsrc limit)
        // Warn if map exceeds Goldsrc coordinate bounds
        yield break;
    }

    public float ConversionFidelity(GeometryFamily src) =>
        src == GeometryFamily.Brush3D ? 0.95f : 0.55f;

    public string? RemapTexture(string tex, string srcFormat) =>
        // Map common Quake textures to Goldsrc equivalents where known
        _textureMap.TryGetValue(tex, out var mapped) ? mapped : null;

    private static readonly Dictionary<string, string> _textureMap = new()
    {
        ["*water1"]  = "!water",
        ["sky"]      = "sky",
        // ...
    };
}
```

### Creating an adapter (C++ / native)

For editors that can't use .NET, `OGEditorClient.dll` exposes a C ABI registration
interface. A native adapter is a DLL that exports `ogeditor_adapter_register()`:

```c
// In your adapter DLL — OGEditorClient.h defines the vtable

#include "OGEditorClient.h"

static OGMapIR* my_read(const char* path, OGMapReadError* err) {
    // ... parse the format, build and return OGMapIR* ...
}

static void my_write(const OGMapIR* ir, const char* path, OGMapWriteError* err) {
    // ... serialize ir to file ...
}

static float my_fidelity(OGGeometryFamily src_family) {
    return src_family == OG_FAMILY_BRUSH3D ? 0.95f : 0.40f;
}

// Called by OGEditorClient.dll when your DLL is loaded
void ogeditor_adapter_register(OGAdapterRegistry* reg) {
    OGFormatAdapterVTable vtable = {
        .format_id    = "goldsrc",
        .display_name = "Half-Life / Goldsrc",
        .extensions   = (const char*[]) { ".map", ".rmf", NULL },
        .family       = OG_FAMILY_BRUSH3D,
        .can_read     = my_can_read,
        .read         = my_read,
        .write        = my_write,
        .validate     = my_validate,
        .fidelity     = my_fidelity,
    };
    ogeditor_register_adapter(reg, &vtable);
}
```

### Adapter discovery

The SDK scans for adapters in this order:

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\OASIS\format-adapters\*.dll` |
| Linux | `~/.oasis/format-adapters/*.so` |
| macOS | `~/Library/Application Support/OASIS/format-adapters/*.dylib` |
| Any | Same directory as `OGEditorClient.dll` |
| Any | Paths listed in `editor_config.json → "adapter_paths"` |

.NET adapters are identified by the `[assembly: OGMapFormatAdapter]` attribute.
Native adapters are identified by the presence of an exported `ogeditor_adapter_register` symbol.

---

## 8. OGEditorClient.dll — C ABI for Conversion

Satellite editors (TrenchBroom, NetRadiant, DarkRadiant) call conversion through
`OGEditorClient.dll` rather than using the .NET types directly:

```c
// List available adapters
int ogeditor_list_adapters(OGAdapterInfo* out_buf, int buf_size);

// Query fidelity before committing to a conversion
float ogeditor_conversion_fidelity(const char* src_format, const char* dst_format);

// Convert a map file
// Returns 0 on success. out_result is populated with diagnostics and fidelity score.
int ogeditor_convert_map(
    const char*          src_path,
    const char*          src_format,
    const char*          dst_path,
    const char*          dst_format,
    OGConversionResult*  out_result);

// Free a result returned by ogeditor_convert_map
void ogeditor_free_conversion_result(OGConversionResult* result);

typedef struct {
    char   format_id[32];
    char   display_name[64];
    char   extensions[8][16];  // null-terminated list
    int    family;             // OGGeometryFamily enum value
} OGAdapterInfo;

typedef struct {
    float  fidelity;           // 0.0–1.0
    int    diagnostic_count;
    char** diagnostic_messages;
    int*   diagnostic_severities;  // 0=info, 1=warning, 2=error
    char   output_path[512];
} OGConversionResult;
```

---

## 9. Entity Remapping Through Conversion

OASIS entity classnames remap via the thing type system during conversion,
so OASIS assets survive cross-game conversions:

```
oasis_portal_enter  →  IR (thing type 5900)  →  oasis_portal_enter  (all formats)
item_key_blue_key   →  IR (thing type 6001)  →  item_key_blue_key   (quake2 → quake2)
                                              →  item_key_silver     (quake2 → quake)
                                                 [nearest equivalent, with warning]
weapon_rocketlauncher → IR (thing type 6017) →  weapon_rocketlauncher (quake2 → quake2)
                                              →  weapon_rocketlauncher (quake2 → quake, exact match)
```

Non-OASIS entities that have no equivalent in the destination format are:
- Preserved as `info_oasis_unknown` point entities carrying all original key/values
- Listed in the conversion diagnostics as warnings
- Restorable if the map is converted back to the original format

---

## 10. Known Community Adapter Candidates

These formats are not in the built-in set but are close matches to existing families:

| Format | Family | Fidelity from Quake | Notes |
|--------|--------|---------------------|-------|
| Half-Life / Goldsrc | Brush3D | ~0.95 | Near-identical to Quake .map; Valve 220 UV |
| Quake4 / Prey | Brush3D | ~0.90 | idTech4 .map; same family as Doom3 adapter |
| Heretic / Hexen | Sector2D | ~1.0 | Doom WAD variant; minimal changes |
| Blood (BUILD) | Build2D | ~0.95 | BUILD engine; same family as Duke3D adapter |
| Shadow Warrior | Build2D | ~0.95 | BUILD engine |
| Unreal (.t3d) | Brush3D | ~0.70 | BSP brushes; different UV convention |
| Source (.vmf) | Brush3D | ~0.85 | Valve's idTech2 derivative; similar to Goldsrc |
| Trenchbroom .tbx | Brush3D | ~1.0 | TrenchBroom's own enhanced format |

---

## 11. Related Documents

| Document | Contents |
|----------|---------|
| `OGEDITOR_INTEGRATION_ROADMAP.md` | Overall OGEditor roadmap; Phase D covers this SDK |
| `OGEDITOR_PLUGIN_GUIDE.md` | Per-editor implementation — how to call OGEditorClient.dll |
| `OGEDITOR_ASSET_CATALOG.md` | Asset catalog JSON — entity classname and thing type reference |
| `OGEDITOR_PORTAL_SYSTEM.md` | Portal entity spec and OGMapSidecar format |
