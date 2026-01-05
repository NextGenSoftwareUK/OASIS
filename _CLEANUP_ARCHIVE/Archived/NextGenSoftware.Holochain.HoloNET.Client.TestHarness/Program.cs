using System;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.Holochain.HoloNET.Client.Desktop;
using NextGenSoftware.WebSocket;

namespace NextGenSoftware.Holochain.HoloNET.Client.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.Holochain.HoloNET.Client Test Harness v1.3");
            Console.WriteLine("");
            await TestHoloNETClient();
            Console.ReadKey();
        }

        private static async Task TestHoloNETClient()
        {

            HoloNETClient holoNETClient = new HoloNETClient("ws://localhost:8888", HolochainVersion.RSM);
            //HoloNETClient holoNETClient = new HoloNETClient("ws://127.0.0.1:8889", HolochainVersion.RSM);
            //HoloNETClient holoNETClient = new HoloNETClient("ws://172.24.159.255:8889", HolochainVersion.RSM);
            //HoloNETClient holoNETClient = new HoloNETClient("ws://::1:8889", HolochainVersion.RSM);
            //HoloNETClient holoNETClient = new HoloNETClient("ws://172.24.159.255:8889", HolochainVersion.RSM);
            //HoloNETClient holoNETClient = new HoloNETClient("ws://172.24.144.1:8889", HolochainVersion.RSM);
           // HoloNETClient holoNETClient = new HoloNETClient("ws://172.25.240.1:8889", HolochainVersion.RSM);

            




            // holoNETClient.HolochainVersion = HolochainVersion.RSM;
            holoNETClient.WebSocket.Config.NeverTimeOut = true;
            //holoNETClient.Config.ErrorHandlingBehaviour = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent
            holoNETClient.Config.AutoStartConductor = false;
            holoNETClient.Config.AutoShutdownConductor = false;
            holoNETClient.Config.FullPathToHolochainAppDNA = @"D:\Dropbox\Our World\OASIS API\NextGenSoftware.Holochain.hApp.OurWorld\our_world\dist\our_world.dna.json";

            holoNETClient.OnConnected += HoloNETClient_OnConnected;
            holoNETClient.OnDataReceived += HoloNETClient_OnDataReceived;
            holoNETClient.OnZomeFunctionCallBack += HoloNETClient_OnZomeFunctionCallBack;
            holoNETClient.OnGetInstancesCallBack += HoloNETClient_OnGetInstancesCallBack;
            holoNETClient.OnSignalsCallBack += HoloNETClient_OnSignalsCallBack;
            holoNETClient.OnDisconnected += HoloNETClient_OnDisconnected;
            holoNETClient.OnError += HoloNETClient_OnError;
            holoNETClient.OnConductorDebugCallBack += HoloNETClient_OnConductorDebugCallBack;

            await holoNETClient.Connect();

            if (holoNETClient.State == System.Net.WebSockets.WebSocketState.Open)
            //if (holoNETClient.State2 == WebSocketState2.Open)
            {
                // await holoNETClient.GetHolochainInstancesAsync();
                //await holoNETClient.CallZomeFunctionAsync("1", "test-instance", "our_world_core", "test", ZomeCallback, null);

                await holoNETClient.CallZomeFunctionAsync("1", "test-instance", "whoami", "whoami", ZomeCallback, null);

              //  await holoNETClient.CallZomeFunctionAsync("1", "test-instance", "numbers", "add_ten", ZomeCallback, new { number = 10 });

                 //await holoNETClient.CallZomeFunctionAsync("1", "test-instance", "our_world_core", "test", ZomeCallback, new { message = new { content = "blah!" } });
                //await holoNETClient.CallZomeFunctionAsync("2", "test-instance", "our_world_core", "test2", ZomeCallback, new { _message = "blah!" });

                // await holoNETClient.CallZomeFunctionAsync("2", "test-instance", "our_world_core", "save_Avatar", ZomeCallback, new { address = "" });
                //await holoNETClient.CallZomeFunctionAsync("2", "test-instance", "our_world_core", "load_Avatar", ZomeCallback, new { address = "" });

                // Load testing
                //   for (int i = 0; i < 100; i++)
                //     await holoNETClient.CallZomeFunctionAsync(i.ToString(), "test-instance", "our_world_core", "test", ZomeCallback, new { message = new { content = "blah!" } });

                //  await holoNETClient.Disconnect();
            }

            //  for (int i = 100; i < 200; i++)
            //     holoNETClient.CallZomeFunctionAsync(i.ToString(), "test-instance", "our_world_core", "test", ZomeCallback, new { message = new { content = "blah!" } });

            //holoNET.Connect("ws://DESKTOP-CSNBOT9:8888");
            //holoNET.Connect("ws://localhost.fiddler:8888");
            //holoNET.Connect("ws://localhost.:8888");
            //holoNET.Connect("ws://ipv4.fiddler.:8888");
            //holoNET.Connect("ws://lvh.me:8888");
            //holoNET.Connect("ws://127.0.0.1:8888");
            //holoNET.Connect("ws://127.0.0.1.:8888");
        }

        private static void HoloNETClient_OnConductorDebugCallBack(object sender, ConductorDebugCallBackEventArgs e)
        {
          //  Console.WriteLine(string.Concat("OnConductorDebugCallBack: EndPoint: ", e.EndPoint, ", Data: ", e.RawJSONData, ", NumberDelayedValidations: ", e.NumberDelayedValidations, ", NumberHeldAspects: ", e.NumberHeldAspects, ", NumberHeldEntries: ", e.NumberHeldEntries, ", NumberPendingValidations: ", e.NumberPendingValidations, ", NumberRunningZomeCalls: ", e.NumberRunningZomeCalls, ", Offline: ", e.Offline, ", Type: ", e.Type));
          //  Console.WriteLine("");
        }

        private static void HoloNETClient_OnSignalsCallBack(object sender, SignalsCallBackEventArgs e)
        {
            Console.WriteLine(string.Concat("OnSignalsCallBack: EndPoint: ", e.EndPoint, ", Id: ", e.Id , ", Data: ", e.RawJSONData, "Name: ", e.Name, "SignalType: ", Enum.GetName(typeof(SignalsCallBackEventArgs.SignalTypes), e.SignalType), "Arguments: ", e.Arguments));
            Console.WriteLine("");
        }

        private static void HoloNETClient_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
        {
            Console.WriteLine(string.Concat("OnGetInstancesCallBack: EndPoint: ", e.EndPoint, ", Id: ", e.Id, ", Instances: ", string.Join(",", e.Instances), ", DNA: ", e.DNA, ", Agent: ", e.Agent, ", Data: ", e.RawJSONData));
            Console.WriteLine("");
        }

        private static void HoloNETClient_OnError(object sender, HoloNETErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("Error Occured. Resason: ", e.Reason,  ", EndPoint: ", e.EndPoint, ", Details: ", e.ErrorDetails.ToString()));
            Console.WriteLine("");
        }

        private static void ZomeCallback(object sender, ZomeFunctionCallBackEventArgs e)
        {
            Console.WriteLine(string.Concat("ZomeCallbackDelegate: Id: ", e.Id, ", Instance: ", e.Instance, ", Zome: ", e.Zome, ", ZomeFunction: ", e.ZomeFunction, ", Data: ", e.ZomeReturnData, ", Raw Zome Return Data: ", e.RawZomeReturnData, ", Raw JSON Data: ", e.RawJSONData, ", IsCallSuccessful: ", e.IsCallSuccessful ? "true" : "false"));
            Console.WriteLine("");
        }

        private static void HoloNETClient_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            Console.WriteLine(string.Concat("Disconnected from ", e.EndPoint, ". Resason: ", e.Reason));
            Console.WriteLine("");
        }

        private static void HoloNETClient_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            Console.WriteLine(string.Concat("ZomeFunction CallBack: EndPoint: ", e.EndPoint, ", Id: ", e.Id, ", Instance: ", e.Instance, ", Zome: ", e.Zome, ", ZomeFunction: ", e.ZomeFunction, ", Data: ",  e.ZomeReturnData, ", Raw Zome Return Data: ", e.RawZomeReturnData, ", Raw JSON Data: ", e.RawJSONData, ", IsCallSuccessful: ", e.IsCallSuccessful? "true" : "false"));
            Console.WriteLine("");
        }

        private static void HoloNETClient_OnDataReceived(object sender, HoloNETDataReceivedEventArgs e)
        {
            if (!e.IsConductorDebugInfo)
            {
                Console.WriteLine(string.Concat("Data Received: EndPoint: ", e.EndPoint, "RawJSONData: ", e.RawJSONData));
                Console.WriteLine("");
            }
        }

        private static void HoloNETClient_OnConnected(object sender, ConnectedEventArgs e)
        {
            Console.WriteLine(string.Concat("Connected to ", e.EndPoint));
            Console.WriteLine("");
        }
    }
}
