namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge
{
    /// <summary>
    /// The instructions a GO Map client can be asked to carry out. Every provider
    /// call that would touch the Unity scene emits one of these.
    /// </summary>
    public enum GOMapCommandType
    {
        Draw3DObjectOnMap,
        Draw2DSpriteOnMap,
        Draw2DSpriteOnHUD,
        DrawRoute,
        Pan,
        Zoom,
        ZoomToLocation,
        DropPin,
        RemovePin,
        PlaceEntity,
        SelectHolon,
        HighlightBuilding,
        SetOrbit
    }
}
