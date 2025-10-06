using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Apollo.Server
{
    public static class ApolloServer
    {
        static Process apolloProcess = null;

        public static void StartServer()
        {
            //Make sure the server is not already running
            if (!Process.GetProcesses().Any(x => x.ProcessName == "npm"))
            {
                apolloProcess = new Process();
                apolloProcess.StartInfo.WorkingDirectory = string.Concat(Directory.GetCurrentDirectory(), "\\server");
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

        public static void ShutdownServer()
        {
            apolloProcess.Kill();
            apolloProcess.Close();

            // Kill all npm processes (Apollo server processes)
            foreach (Process process in Process.GetProcessesByName("npm"))
                process.Kill();
        }
    }
}
