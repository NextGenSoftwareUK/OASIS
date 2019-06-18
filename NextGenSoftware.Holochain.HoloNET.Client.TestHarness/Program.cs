
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.Holochain.HoloNET.Client.Desktop;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.Client.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.Holochain.HoloNET.Client Test Harness v1.2");
            Console.WriteLine("");
            await TestHoloNETClient();
            Console.ReadKey();
        }

        private static async Task TestHoloNETClient()
        {
            HoloNETClient holoNETClient = new HoloNETClient("ws://localhost:8888");
            holoNETClient.Config.NeverTimeOut = true;
            //holoNETClient.Config.ErrorHandlingBehaviour = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent

            holoNETClient.OnConnected += HoloNETClient_OnConnected;
            holoNETClient.OnDataReceived += HoloNETClient_OnDataReceived;
            holoNETClient.OnZomeFunctionCallBack += HoloNETClient_OnZomeFunctionCallBack;
            holoNETClient.OnGetInstancesCallBack += HoloNETClient_OnGetInstancesCallBack;
            holoNETClient.OnSignalsCallBack += HoloNETClient_OnSignalsCallBack;
            holoNETClient.OnDisconnected += HoloNETClient_OnDisconnected;
            holoNETClient.OnError += HoloNETClient_OnError;

            await holoNETClient.Connect();

            if (holoNETClient.State == System.Net.WebSockets.WebSocketState.Open)
            {
                await holoNETClient.GetHolochainInstancesAsync();
                await holoNETClient.CallZomeFunctionAsync("1", "test-instance", "our_world_core", "test", ZomeCallback, new { message = new { content = "blah!" } });
                await holoNETClient.CallZomeFunctionAsync("2", "test-instance", "our_world_core", "test2", ZomeCallback, new { message = "blah!" });

                // Load testing
                for (int i = 0; i < 100; i++)
                    await holoNETClient.CallZomeFunctionAsync(i.ToString(), "test-instance", "our_world_core", "test", ZomeCallback, new { message = new { content = "blah!" } });
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

        private static void HoloNETClient_OnSignalsCallBack(object sender, SignalsCallBackEventArgs e)
        {
            Console.WriteLine("OnSignalsCallBack: Id: " + e.Id + ", Data: " + e.RawJSONData);
            Console.WriteLine("");
        }

        private static void HoloNETClient_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
        {
            Console.WriteLine(string.Concat("OnGetInstancesCallBack: Id: ", e.Id, ", Instances: ", string.Join(",", e.Instances), ", DNA: ", e.DNA, ", Agent: ", e.Agent, ", Data: ", e.RawJSONData));
            Console.WriteLine("");
        }

        private static void HoloNETClient_OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("Error Occured. Resason: " + e.Reason + ", EndPoint: " + e.EndPoint + ", Details: " + e.ErrorDetails.ToString());
            Console.WriteLine("");
        }

        private static void ZomeCallback(object sender, ZomeFunctionCallBackEventArgs e)
        {
            Console.WriteLine("ZomeCallbackDelegate: Id: " + e.Id + ", Instance: " + e.Instance + ", ZomeFunction: " + e.ZomeFunction + ", Data: " + e.ZomeReturnData);
            Console.WriteLine("");
        }

        private static void HoloNETClient_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            Console.WriteLine("Disconnected. Resason: " + e.Reason);
            Console.WriteLine("");
        }

        private static void HoloNETClient_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            Console.WriteLine("ZomeFunction CallBack: Id: " + e.Id + ", Instance: " + e.Instance + ", ZomeFunction: " + e.ZomeFunction + ", Data: "+ e.ZomeReturnData);
            Console.WriteLine("");
        }

        private static void HoloNETClient_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Data Received: " + e.RawJSONData);
            Console.WriteLine("");
        }

        private static void HoloNETClient_OnConnected(object sender, ConnectedEventArgs e)
        {
            Console.WriteLine("Connected");
            Console.WriteLine("");
        }
    }
}



/*
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Jayrock.Json;
using System.IO;
using Jayrock.Json.Conversion.Import;


namespace HttpServiceJSONCall
{
    class JSONService
    {
        string url;
        public JSONService(string url_)
        {
            url = url_;
        }


        object CallInternal(string method_, object[] params_)
        {
            JsonObject jsonrequest = new JsonObject();
            jsonrequest["id"] = 0;
            jsonrequest["method"] = method_;
            jsonrequest["params"] = params_;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "POST";
            TextWriter writer = new StreamWriter(webRequest.GetRequestStream());
            writer.Write(jsonrequest.ToString());
            writer.Close();
            WebResponse response = webRequest.GetResponse();
            ImportContext import = new ImportContext();
            JsonReader reader = new JsonTextReader(new StreamReader(response.GetResponseStream()));
            object jsonresponse_ = import.ImportAny(reader);
            if (!(jsonresponse_ is JsonObject))
                throw new Exception("Something weird happened to the request, check the foobar or something" );



            JsonObject jsonresponse = (JsonObject)jsonresponse_;


            if (jsonresponse["error"] != null)
                throw new Exception(jsonresponse["error"].ToString());


            return jsonresponse["result"];
        }


        public object Call(string method)
        {
            return CallInternal(method, null);
        }


        public object Call(string method, object p1)
        {
            return CallInternal(method, new object[] { p1 });
        }


        public object Call(string method, object p1, object p2)
        {
            return CallInternal(method, new object[] { p1, p2 });
        }


        public object Call(string method, object p1, object p2, object p3)
        {
            return CallInternal(method, new object[] { p1, p2, p3 });
        }


        public object Call(string method, object p1, object p2, object p3, object p4
)
        {
            return CallInternal(method, new object[] { p1, p2, p3, p4 });
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            JSONService service = new JSONService(
"http://localhost:2380/TSA/Services/Test.ashx");
            object res = service.Call("ReverseString", "test string");
            Console.WriteLine(res);
            Console.ReadLine();
        }
    }
}*/
