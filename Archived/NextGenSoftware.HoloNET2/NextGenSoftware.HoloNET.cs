

using Newtonsoft.Json;
using System;
using WebSocket4Net;
using UnityEngine;
//using EdgeJs;

namespace NextGenSoftware.HoloNET2
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
        /*
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
        }*/


        private void messageReceived(string message)
        {
            Console.WriteLine("Message Received:" + message);
            Debug.Log("Message Received:" + message);
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


            Debug.Log("Opening websocket on " + holoChainServerURI + "...");
            var websocket = new JsonWebSocket(holoChainServerURI);

            /*
            string GetInstancesMethod = JsonConvert.SerializeObject(
                        new
                        {
                            jsonrpc = "2.0",
                            id = 0,
                            method = "info/instances",
                            //@params = new { }
                        }
                    );*/

            //websocket.On<string>("info/instances", messageReceived);
           // websocket.On<string>(GetInstancesMethod, messageReceived);

            websocket.WebSocket.MessageReceived += WebSocket_MessageReceived;


            websocket.Error += this.Websocket_Error;

           

            websocket.Opened += (sender, e) =>
            {
                //websocket.Send("info/instances", null);

                //websocket.Send("info/instances", "@params = new {}");
                //websocket.Send("info/instances", "{}");
                //websocket.Send("info/instances", "{\"jsonrpc\": \"2.0\", \"method\": \"info/instances\", \"params\": [], \"id\": 1}");
                //websocket.Send("info/instances", "{\"jsonrpc\": \"2.0\", \"method\": \"info/instances\", \"params\": [] }");
                //websocket.Send("", "{\"jsonrpc\": \"2.0\", \"method\": \"info/instances\", \"params\": [] }");
                //websocket.Send("info/instances", "{\"jsonrpc\": \"2.0\", \"method\": \"info/instances\", \"params\": {} }");
                //websocket.Send("info/instances", "{\"jsonrpc\": \"2.0\", \"params\": {} }");
                //websocket.Send("info/instances", "{\"params\": {} }");
                //websocket.Send("info/instances", "{\"method\": \"info/instances\", \"params\": {} }");
                //websocket.Send("info/instances", "{\"method\": \"info/instances\", \"params\": {}, \"id\": 1 }");
                //websocket.Send("info/instances", "{\"jsonrpc\": \"2.0\", \"method\": \"info/instances\", \"params\": {}, \"id\": 1 }");
                //websocket.Send("info/instances", "{\"jsonrpc\": \"2.0\", \"id\": \"0\", \"method\": \"info/instances\"}");
                //websocket.Send("{\"jsonrpc\": \"2.0\", \"id\": \"0\", \"method\": \"info/instances\"}", null);

                //websocket.Send("{\"jsonrpc\": \"2.0\", \"id\": \"0\", \"method\": \"info/instances\"}", null);

                //websocket.Send("{\"jsonrpc\": \"2.0\", \"id\": \"0\", \"method\": \"test-instance/our_world/test\", \"params\": { \"item\":{ \"message\":\"hhhgghhg\"}}}", null);

                Debug.Log("Opening websocket on " + holoChainServerURI + "... Done!");
                Debug.Log("Sending info/instances request...");

                websocket.Send(
                   JsonConvert.SerializeObject(
                       new
                       {
                           jsonrpc = "2.0",
                           id = 0,
                           method = "info/instances",
                           //@params = new { }
                       }
                   ), null);

                /*
                websocket.Send(
                    JsonConvert.SerializeObject(
                        new
                        {
                            jsonrpc = "2.0",
                            id = 1,
                            method = "test-instance/our_world_core/test",
                            @params = new { message = "blah!" },
                        }
                    ), null);*/

                websocket.Send(
                    JsonConvert.SerializeObject(
                        new
                        {
                            jsonrpc = "2.0",
                            id = 1,
                            method = "call",
                            @params = new { instance_id = "test-instance", zome = "our_world_core", function = "test", @params = new { message = new { content = "blah!" } } }
                            //@params = new { instance_id = "test-instance", zome = "our_world_core", function = "test", args = new { content = "blah!" } }
                        }
                    ), null);

                websocket.Send(
                    JsonConvert.SerializeObject(
                        new
                        {
                            jsonrpc = "2.0",
                            id = 2,
                            method = "call",
                            @params = new { instance_id = "test-instance", zome = "our_world_core", function = "test2", @params = new { message = "blah!" } }
                        
                        }
                    ), null);

                /*
                websocket.Send(
                    JsonConvert.SerializeObject(
                        new
                        {
                            jsonrpc = "2.0",
                            id = 2,
                            method = "test-instance/our_world_core/test2"
                            //@params = new { message = "blah!" },
                        }
                    ), null);*/






                /*
                websocket.Send(
                    JsonConvert.SerializeObject(
                        new
                        {
                            jsonrpc = "2.0",
                            id = 1,
                            method = "test-instance/our_world/create_my_entry",
                            @params = new { entry = new MyEntry { content = "blah" } },
                        }
                    ), null);
                    */

                //websocket.Send("info/instances",
                //    JsonConvert.SerializeObject(
                //        new
                //        {
                //            //method = "info/instances",
                //            @params = new { },
                //            id = 123,
                //        }
                //    )
                //);

                //websocket.Send("test-instance/our_world/create_my_entry",
                //    JsonConvert.SerializeObject(
                //        new
                //        {
                //            //method = "test-instance/our_world/create_my_entry",
                //            @params = new { entry = new MyEntry { content = "blah" } },
                //            id = 124,
                //        }
                //    )
                //);



            };

            websocket.Open();
            websocket.Close();


            Debug.Log("Websocket closed.");










            /*
            // note: reconnection handling needed.
            //var websocket = new WebSocket(holoChainServerURI, sslProtocols: SslProtocols.Tls12);
            //var websocket = new WebSocket(holoChainServerURI, sslProtocols: SslProtocols.Default);
            var websocket = new WebSocket4Net.WebSocket(holoChainServerURI, "", WebSocket4Net.WebSocketVersion.Rfc6455);

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
                            method = "test-instance/our_world/create_my_entry",
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
                if (data.id == 123)
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
            */

            return true;


            //this.Connection = WebSocket.CreateClientBuffer(1024,1024)


        }

        private void WebSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("");
            Console.WriteLine("Message Received: " + e.Message);
            Debug.Log("Message Received:" + e.Message);
        }

        private void Websocket_Error(object sender, global::SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Console.WriteLine("Websocket_Error: " + e.ToString());
            Debug.Log("Websocket_Error: " + e.ToString() + " --- " + e.Exception.Message);
        }
    }
}
