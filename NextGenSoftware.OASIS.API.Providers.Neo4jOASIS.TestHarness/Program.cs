using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE Neo4JOASIS TEST HARNESS V1.O");
            Console.WriteLine("");

            Console.WriteLine("Connecting To Graph DB...");
            Neo4jOASIS neo = new Neo4jOASIS();
            
            await neo.Connect("bolt://localhost:7687", "neo4j", "neo4j");
            neo.GraphClient.OperationCompleted += GraphClient_OperationCompleted;

            if (neo.GraphClient.IsConnected)
            {
                Console.WriteLine("Connected To Graph DB.");
                Console.WriteLine("Loading Avatar...");
                //Avatar avatar = (Avatar)neo.LoadAvatar("david");
                Avatar avatar = (Avatar)neo.LoadAvatar("david", "password");
                Console.WriteLine("Avatar Loaded.");

                Console.WriteLine("Updating Avatar...");
                avatar.FirstName = "bob";
                neo.SaveAvatar(avatar);
                Console.WriteLine("Avatar Saved.");

                Console.WriteLine("Deleting Avatar...");
                neo.DeleteAvatar(avatar.Id);
                Console.WriteLine("Avatar Deleted.");
            }
        }

        private static void GraphClient_OperationCompleted(object sender, Neo4jClient.OperationCompletedEventArgs e)
        {
            Console.WriteLine("Graph Client Operation Completed: QueryText:" + e.QueryText + ", Results: " + e.ResourcesReturned, ",Error: " + e.HasException);
        }
    }
}
