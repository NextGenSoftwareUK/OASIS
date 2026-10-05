using System;
using NextGenSoftware.OASIS.API.Core.Apollo.Server;

namespace NextGenSoftware.OASIS.API.Core.Apollo.Server.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE OASIS API APOLLO SERVER TEST HARNESS V1.0");

            Console.WriteLine("");
            Console.WriteLine("Starting Server...");
            ApolloServer.StartServer();

            Console.WriteLine("Server Started.");
            Console.WriteLine("Press any key to shutdown the server...");

            Console.ReadKey();
            Console.WriteLine("Shutting Down The Server...");
            ApolloServer.ShutdownServer();
            Console.WriteLine("Server Shutdown.");
        }
    }
}
