
namespace NextGenSoftware.OASIS.API.Core.ARC.Membrane
{
    public class PsyberManager
    {
        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        {
            return true;
        }

        public bool Draw3DObjectOnHUD(object obj, float x, float y)
        {
            return true;
        }

        public bool DrawCircle(float x, float y, float radius, int thickness, string hexColour, int altha)
        {
            return true;
        }

        public bool DrawCircle(float x, float y, float radius, int thickness, int red, int green, int blue, int altha)
        {
            return true;
        }

        public bool DrawRect(float topLeftX, float topLeftY, float topRightX, float topRightY, float bottomRightX, float bottomRightY, float bottomLeftX, float bottomLeftY, int thickness, string hexColour, int altha)
        {
            return true;
        }

        public bool DrawRect(float topLeftX, float topLeftY, float topRightX, float topRightY, float bottomRightX, float bottomRightY, float bottomLeftX, float bottomLeftY, int thickness, int red, int green, int blue, int altha)
        {
            return true;
        }

        public bool DrawPoints(float[] xPoints, float[] yPoints, float[] zPoints, int thickness, string hexColour, int altha)
        {
            return true;
        }

        // Can be used to create any shape such as triangle, etc
        public bool DrawPoints(float[] xPoints, float[] yPoints, float[] zPoints, int thickness, int red, int green, int blue, int altha)
        {
            return true;
        }

        public bool RenderText(float x, float y, float z, string text, int thickness, string hexColour, int altha)
        {
            return true;
        }

        public bool RenderText(float x, float y, float z, string text, int thickness, int red, int green, int blue, int altha)
        {
            return true;
        }
    }
}
