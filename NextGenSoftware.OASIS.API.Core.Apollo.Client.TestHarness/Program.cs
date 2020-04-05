using System;
using NextGenSoftware.OASIS.API.Core.Apollo.Client;

namespace NextGenSoftware.OASIS.API.Core.Apollo.Client.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE OASIS API APOLLO CLIENT TEST HARNESS V1.0");

            Console.WriteLine("");
            Console.WriteLine("Starting Client...");
            ApolloClient.StartClient();

            Console.WriteLine("Client Started.");

            ApolloClient.ExecuteGraphQLQuery("test query");

            Console.WriteLine("Press any key to shutdown the client...");

            Console.ReadKey();
            Console.WriteLine("Shutting Down The Client...");
            ApolloClient.ShutdownClient();
            Console.WriteLine("Client Shutdown.");
        }
    }
}
