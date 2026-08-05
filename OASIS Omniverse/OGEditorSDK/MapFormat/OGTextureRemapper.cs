namespace OGEditorSDK.MapFormat
{
    /// <summary>
    /// Applies texture name remapping across format adapters.
    /// Each adapter's RemapTexture() is called; if it returns null the name is unchanged.
    /// </summary>
    public static class OGTextureRemapper
    {
        public static OGMapIR Remap(OGMapIR map, IOGMapFormatAdapter src, IOGMapFormatAdapter dst)
        {
            if (src.FormatId == dst.FormatId) return map;

            foreach (var prim in map.WorldGeometry)
                RemapPrimitive(prim, src.FormatId, dst);

            foreach (var e in map.BrushEntities)
                foreach (var prim in e.Geometry)
                    RemapPrimitive(prim, src.FormatId, dst);

            return map;
        }

        private static void RemapPrimitive(OGGeometryPrimitive prim, string srcFormat,
            IOGMapFormatAdapter dst)
        {
            switch (prim)
            {
                case OGBrush brush:
                    foreach (var face in brush.Faces)
                        face.Texture = Remap(face.Texture, srcFormat, dst);
                    break;
                case OGPatch patch:
                    patch.Texture = Remap(patch.Texture, srcFormat, dst);
                    break;
                case OGSector sector:
                    sector.FloorTexture   = Remap(sector.FloorTexture,   srcFormat, dst);
                    sector.CeilingTexture = Remap(sector.CeilingTexture, srcFormat, dst);
                    foreach (var ld in sector.Linedefs)
                    {
                        ld.UpperTexture      = Remap(ld.UpperTexture,      srcFormat, dst);
                        ld.MiddleTexture     = Remap(ld.MiddleTexture,     srcFormat, dst);
                        ld.LowerTexture      = Remap(ld.LowerTexture,      srcFormat, dst);
                        ld.BackUpperTexture  = Remap(ld.BackUpperTexture,  srcFormat, dst);
                        ld.BackMiddleTexture = Remap(ld.BackMiddleTexture, srcFormat, dst);
                        ld.BackLowerTexture  = Remap(ld.BackLowerTexture,  srcFormat, dst);
                    }
                    break;
                case OGBuildSector bs:
                    bs.FloorTexture   = Remap(bs.FloorTexture,   srcFormat, dst);
                    bs.CeilingTexture = Remap(bs.CeilingTexture, srcFormat, dst);
                    foreach (var w in bs.Walls)
                        w.Texture = Remap(w.Texture, srcFormat, dst);
                    break;
                case OGTile tile:
                    tile.Texture = Remap(tile.Texture, srcFormat, dst);
                    break;
            }
        }

        private static string Remap(string name, string srcFormat, IOGMapFormatAdapter dst)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return dst.RemapTexture(name, srcFormat) ?? name;
        }
    }
}
