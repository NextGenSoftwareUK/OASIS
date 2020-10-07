
namespace NextGenSoftware.OASIS.API.Core
{
    public class MapManager : OASISManager
    {
        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public MapManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
        {

        }

        public bool Draw3DObjectOnMap(object obj, float x, float y)
        {
            return true;
        }

        public bool Draw2DSpriteOnMap(object sprite, float x, float y)
        {
            return true;
        }

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        {
            return true;
        }

        public bool SelectBuildingOnMap(Building building)
        {
            return true;
        }

        public bool HighlightBuildingOnMap(Building building)
        {
            return true;
        }

        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY)
        {
            return true;
        }

        public bool DrawRouteOnMap(float[] xPoints, float[] yPoints)
        {
            return true;
        }

        public bool ZoomMapOut()
        {
            return true;
        }

        public bool ZoomMapIn()
        {
            return true;
        }

        public bool PanMapLeft()
        {
            return true;
        }

        public bool PanMapRight()
        {
            return true;
        }

        public bool PanMapUp()
        {
            return true;
        }

        public bool PanMapDown()
        {
            return true;
        }

        public bool SelectHolonOnMap(IHolon holon)
        {
            return true;
        }

        public bool ZoomToHolonOnMap(IHolon holon)
        {
            return true;
        }

        public bool CreateRouteBetweenHolons(IHolon fromHolon, IHolon toHolon)
        {
            return true;
        }
    }
}
