using NextGenSoftware.OASIS.API.Core.ARC.Membrane;
using System;

namespace NextGenSoftware.OASIS.API.Core.ARC.Membrane.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            PsyberManager psyberManager = new PsyberManager();

            Console.WriteLine($"DrawCircle: {psyberManager.DrawCircle(10, 10, 5, 1, "#FF0000", 255)}");
            Console.WriteLine($"DrawRect: {psyberManager.DrawRect(0, 0, 10, 0, 10, 10, 0, 10, 1, "#00FF00", 255)}");
            Console.WriteLine($"DrawPoints: {psyberManager.DrawPoints(new float[] { 0, 5, 10 }, new float[] { 0, 10, 0 }, new float[] { 0, 0, 0 }, 1, "#0000FF", 255)}");
            Console.WriteLine($"RenderText: {psyberManager.RenderText(1, 1, 0, "OASIS", 1, "#FFFFFF", 255)}");
        }
    }
}
