using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS
{
    public class MapBoxOASIS : IOASISMapProvider
    {
        public MapProviderType MapProviderType { get; set; }
        public string MapProviderName { get; set; }
        public string MapProviderDescription { get; set; }

        public MapBoxOASIS()
        {
            MapProviderType = MapProviderType.MapBox;
            MapProviderName = "MapBpx";
            MapProviderDescription = "MapBpx OASIS Map Provider";
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon)
        {
            return true;
        }

        public bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points)
        {
            throw new System.NotImplementedException();
        }

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        {
            throw new System.NotImplementedException();
        }

        public bool Draw2DSpriteOnMap(object sprite, float x, float y)
        {
            throw new System.NotImplementedException();
        }

        public bool Draw3DObjectOnMap(object obj, float x, float y)
        {
            throw new System.NotImplementedException();
        }

        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY)
        {
            throw new System.NotImplementedException();
        }

        public bool HighlightBuildingOnMap(IBuilding building)
        {
            throw new System.NotImplementedException();
        }

        public bool PanMapDown(float value)
        {
            throw new System.NotImplementedException();
        }

        public bool PanMapLeft(float value)
        {
            throw new System.NotImplementedException();
        }

        public bool PanMapRight(float value)
        {
            throw new System.NotImplementedException();
        }

        public bool PanMapUp(float value)
        {
            throw new System.NotImplementedException();
        }

        public bool SelectBuildingOnMap(IBuilding building)
        {
            throw new System.NotImplementedException();
        }

        public bool SelectHolonOnMap(IHolon holon)
        {
            throw new System.NotImplementedException();
        }

        public bool SelectQuestOnMap(IQuest quest)
        {
            throw new System.NotImplementedException();
        }

        public void SetCurrentMapProvider(MapProviderType mapProviderType)
        {
            throw new System.NotImplementedException();
        }

        public bool ZoomMapIn(float value)
        {
            throw new System.NotImplementedException();
        }

        public bool ZoomMapOut(float value)
        {
            throw new System.NotImplementedException();
        }

        public bool ZoomToHolonOnMap(IHolon holon)
        {
            throw new System.NotImplementedException();
        }

        public bool ZoomToQuestOnMap(IQuest quest)
        {
            throw new System.NotImplementedException();
        }
    }
}