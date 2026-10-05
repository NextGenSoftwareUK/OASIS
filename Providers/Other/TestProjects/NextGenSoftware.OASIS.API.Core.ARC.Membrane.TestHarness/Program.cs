using NextGenSoftware.OASIS.ARC.Core;
using System;

namespace NextGenSoftware.OASIS.API.Core.ARC.Membrane.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            DeviceManager deviceManager = new DeviceManager();
            
            string result = deviceManager.CallNodeAddNumbers().Result;
            Console.WriteLine(string.Concat("Result: ", result));

            //result = deviceAPIManager.CallNodeAddNumbersExternal().Result;
            //Console.WriteLine(string.Concat("Result From External: ", result));
        }
    }
}
