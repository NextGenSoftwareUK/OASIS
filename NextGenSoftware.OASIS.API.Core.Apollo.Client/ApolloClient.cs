using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Apollo.Client
{
    public static class ApolloClient
    {
        static Process apolloProcess = null;

        public static void StartClient()
        {
            //Make sure the server is not already running
            if (!Process.GetProcesses().Any(x => x.ProcessName == "npm"))
            {
                apolloProcess = new Process();
                apolloProcess.StartInfo.WorkingDirectory = string.Concat(Directory.GetCurrentDirectory(), "\\client");
                apolloProcess.StartInfo.FileName = "npm";
                apolloProcess.StartInfo.Arguments = "start";
                apolloProcess.StartInfo.UseShellExecute = true;
                apolloProcess.StartInfo.RedirectStandardOutput = false;
                apolloProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                apolloProcess.StartInfo.CreateNoWindow = false;
                apolloProcess.Start();

                //await Task.Delay(Config.SecondsToWaitForHolochainConductorToStart); // Give the conductor 5 seconds to start up...
            }
        }

        public async Task<string> ExecuteGraphQLQuery(string query)
        {
            return await NodeManager.NodeManager.CallNodeMethod("./CallApolloClient", query);
        }

        public static void ShutdownClient()
        {
            apolloProcess.Kill();
            apolloProcess.Close();
        }
    }
}
