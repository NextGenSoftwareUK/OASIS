using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS
{
    public class WRDLD3DOASIS : IOASISMapProvider
    {
        public MapProviderType MapProviderType { get; set; }
        public string MapProviderName { get; set; }
        public string MapProviderDescription { get; set; }

        public WRDLD3DOASIS()
        {
            MapProviderType = MapProviderType.WRLD3D;
            MapProviderName = "WRLD 3D";
            MapProviderDescription = "WRLD 3D OASIS Map Provider";
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(IHolon fromHolon, IHolon toHolon)
        {
            throw new NotImplementedException();
        }

        public bool CreateAndDrawRouteOnMapBeweenPoints(MapPoints points)
        {
            throw new NotImplementedException();
        }

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        {
            throw new NotImplementedException();
        }

        public bool Draw2DSpriteOnMap(object sprite, float x, float y)
        {
            throw new NotImplementedException();
        }

        public bool Draw3DObjectOnMap(object obj, float x, float y)
        {
            throw new NotImplementedException();
        }

        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY)
        {
            throw new NotImplementedException();
        }

        public bool HighlightBuildingOnMap(IBuilding building)
        {
            throw new NotImplementedException();
        }

        public bool PanMapDown(float value)
        {
            throw new NotImplementedException();
        }

        public bool PanMapLeft(float value)
        {
            throw new NotImplementedException();
        }

        public bool PanMapRight(float value)
        {
            throw new NotImplementedException();
        }

        public bool PanMapUp(float value)
        {
            throw new NotImplementedException();
        }

        public bool SelectBuildingOnMap(IBuilding building)
        {
            throw new NotImplementedException();
        }

        public bool SelectHolonOnMap(IHolon holon)
        {
            throw new NotImplementedException();
        }

        public bool SelectQuestOnMap(IQuest quest)
        {
            throw new NotImplementedException();
        }

        public void SetCurrentMapProvider(MapProviderType mapProviderType)
        {
            throw new NotImplementedException();
        }

        public bool ZoomMapIn(float value)
        {
            throw new NotImplementedException();
        }

        public bool ZoomMapOut(float value)
        {
            throw new NotImplementedException();
        }

        public bool ZoomToHolonOnMap(IHolon holon)
        {
            throw new NotImplementedException();
        }

        public bool ZoomToQuestOnMap(IQuest quest)
        {
            throw new NotImplementedException();
        }
    }
}