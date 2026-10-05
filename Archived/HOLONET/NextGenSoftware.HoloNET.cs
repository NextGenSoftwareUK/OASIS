using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Security.Authentication;
using WebSocket4Net;
using Microsoft.CSharp;
using JsonRpc;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using StreamJsonRpc;
using System.Net;
using System.Net.WebSockets;
//using EdgeJs;

namespace NextGenSoftware.HoloNET
{
    public class HoloNET
    {
        struct MyEntry
        {
            public string content;
        }

        //private WebSocket _connection = null;

        //public WebSocket Connection { get; }

        public HoloNET()
        {

        }

        //public async Task CallZomeFunction(JsonRpcHttpClient client)
        //{
        //    return await client.Invoke<string>("info/instances").Result;
        //}

        public async Task CallZomeFunction(string holoChainServerURI)
        {
            try
            {
                HttpListener httpListener = new HttpListener();
                httpListener.Prefixes.Add("http://localhost:8888/");
                httpListener.Start();

                HttpListenerContext context = await httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                    System.Net.WebSockets.WebSocket webSocket = webSocketContext.WebSocket;

                    StreamJsonRpc.JsonRpc client = new StreamJsonRpc.JsonRpc(new WebSocketMessageHandler(webSocket));


                    client.StartListening();
                    string result = await client.InvokeAsync<string>("info/instances");


                    await client.Completion;

                    //while (webSocket.State == WebSocketState.Open)
                    //{
                    //    await webSocket.SendAsync()
                    //}
                }
            }
            catch (Exception ex)
            {

            }
        }

        public bool Connect(string holoChainServerURI)
        {
            /*
            var services = new ServiceCollection();
            services.AddNodeServices(options => {
                // Set any properties that you want on 'options' here
            });
            */

            //CallZomeFunction(holoChainServerURI);

            //var func = Edge.Func(@"return require('./../myfunc.js')");
            //var func = Edge.Func(File.ReadAllText("myfunc.js"));

            /*
            JsonRpcHttpClient client = new JsonRpcHttpClient(new Uri(holoChainServerURI));
            

            client.Invoke<string>("info/instances").ContinueWith((antecedent) =>
            {
                object obj = antecedent.Result;

                if (antecedent.Status == TaskStatus.RanToCompletion)
                {
                    Console.WriteLine("completed");
                }
            });
            */


            




            //var socket = await this.HttpContext.WebSockets.AcceptWebSocketAsync();
            //var jsonRpc = new JsonRpc(new WebSocketMessageHandler(socket), new JsonRpcServer());
            //jsonRpc.StartListening();
            //await jsonRpc.Completion;

           // return true;

            /*
            client.Invoke<string>("info/instances", new ).ContinueWith((antecedent) =>
            {
                object obj = antecedent.Result;

                if (antecedent.Status == TaskStatus.RanToCompletion)
                {
                    Console.WriteLine("completed");
                }
            });*/


            /*
            var websocket = new JsonWebSocket(holoChainServerURI);

            websocket.Opened += (sender, e) =>
            {
                websocket.Send("info/instances",
                    JsonConvert.SerializeObject(
                        new
                        {
                            method = "info/instances",
                            @params = new { },
                            id = 123,
                        }
                    )
                );

                websocket.Send("test-instance/our_world/create_my_entry",
                    JsonConvert.SerializeObject(
                        new
                        {
                            method = "test-instance/our_world/create_my_entry",
                            @params = new { entry = new MyEntry { content = "blah" } },
                            id = 124,
                        }
                    )
                );



            };


            Action<string> s = Console.WriteLine;

            websocket.On("info/instances", s);

            */






            
            
            // note: reconnection handling needed.
            //var websocket = new WebSocket(holoChainServerURI, sslProtocols: SslProtocols.Tls12);
            //var websocket = new WebSocket(holoChainServerURI, sslProtocols: SslProtocols.Default);
            var websocket = new WebSocket4Net.WebSocket(holoChainServerURI);
           
            websocket.Opened += (sender, e) =>
            {
                websocket.Send(
                    JsonConvert.SerializeObject(
                        new
                        {
                            method = "info/instances",
                            //@params = new { channel = channelName },
                            id = 123,
                        }
                    )
                );

                websocket.Send(
                    JsonConvert.SerializeObject(
                        new
                        {
                            method = "test-instance/our_world_core/create_my_entry",
                            @params = new { entry = new MyEntry { content = "blah" } },
                            id = 124,
                        }
                    )
                );


                
            };
            websocket.MessageReceived += (sender, e) =>
            {
                Console.WriteLine("Message Received: " + e.Message);

                dynamic data = JObject.Parse(e.Message);
                if (data.id== 123)
                {
                    Console.WriteLine("Instance Name Received");
                }
                if (data.@params != null)
                {
                    Console.WriteLine(data.@params.channel + " " + data.@params.message);
                }

                //JObject data = JObject.Parse(e.Message);
                //if (data["id"].v == 123)
                //{
                //    Console.WriteLine("Instance Name Received");
                //}
                //if (data.@params != null)
                //{
                //    Console.WriteLine(data.@params.channel + " " + data.@params.message);
                //}
            };
            

            websocket.Open();

            websocket.Close();
            
            
            return true;


            //this.Connection = WebSocket.CreateClientBuffer(1024,1024)

          
        }
    }
}
