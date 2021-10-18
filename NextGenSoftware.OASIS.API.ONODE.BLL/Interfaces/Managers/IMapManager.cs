using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.BLL.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces
{
    public interface IMapManager : IOASISManager
    {
        bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon);
        bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points);
        bool Draw2DSpriteOnHUD(object sprite, float x, float y);
        bool Draw2DSpriteOnMap(object sprite, float x, float y);
        bool Draw3DObjectOnMap(object obj, float x, float y);
        bool DrawRouteOnMap(float startX, float startY, float endX, float endY);
        bool HighlightBuildingOnMap(Building building);
        bool PamMapDown(float value);
        bool PamMapLeft(float value);
        bool PamMapRight(float value);
        bool PamMapUp(float value);
        bool SelectBuildingOnMap(Building building);
        bool SelectHolonOnMap(IHolon holon);
        bool SelectQuestOnMap(IQuest quest);
        bool ZoomMapIn(float value);
        bool ZoomMapOut(float value);
        bool ZoomToHolonOnMap(IHolon holon);
        bool ZoomToQuestOnMap(IQuest quest);
    }
}