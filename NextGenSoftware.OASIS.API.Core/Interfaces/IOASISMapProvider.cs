using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public interface IOASISMapProvider
    {
        MapProviderType MapProviderType { get; set; }
        string MapProviderName { get; set; }
        string MapProviderDescription { get; set; }
        void SetCurrentMapProvider(MapProviderType mapProviderType);
        bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon);
        bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points);
        bool Draw2DSpriteOnHUD(object sprite, float x, float y);
        bool Draw2DSpriteOnMap(object sprite, float x, float y);
        bool Draw3DObjectOnMap(object obj, float x, float y);
        bool DrawRouteOnMap(float startX, float startY, float endX, float endY);
        bool HighlightBuildingOnMap(IBuilding building);
        bool PanMapDown(float value);
        bool PanMapLeft(float value);
        bool PanMapRight(float value);
        bool PanMapUp(float value);
        bool SelectBuildingOnMap(IBuilding building);
        bool SelectHolonOnMap(IHolon holon);
       // bool SelectQuestOnMap(IQuest quest); //TODO: Fix later
        bool ZoomMapIn(float value);
        bool ZoomMapOut(float value);
        bool ZoomToHolonOnMap(IHolon holon);
        // bool ZoomToQuestOnMap(IQuest quest); //TODO: Fix later
    }
}