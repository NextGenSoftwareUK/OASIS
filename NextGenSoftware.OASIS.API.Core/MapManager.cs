
namespace NextGenSoftware.OASIS.API.Core
{
    public class MapManager
    {
        public bool Draw2DSpriteOnMap(float x, float y)
        {
            return true;
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

        public bool SelectBuildingOnMap(float x, float y)
        {
            return true;
        }

        public bool HighlightBuildingOnMap(float x, float y)
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

        public void ZoomMapOut()
        {

        }

        public void ZoomMapIn()
        {

        }

        public void PanMapLeft()
        {

        }

        public void PanMapRight()
        {

        }

        public void PanMapUp()
        {

        }

        public void PanMapDown()
        {

        }

        public void SelectHolonOnMap(IHolon holon)
        {

        }

        public void ZoomToHolonOnMap(IHolon holon)
        {

        }

        public void CreateRouteBetweenHolons(IHolon fromHolon, IHolon toHolon)
        {

        }
    }
}
