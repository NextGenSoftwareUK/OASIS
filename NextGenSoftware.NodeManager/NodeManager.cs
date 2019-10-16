using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NextGenSoftware.NodeManager
{
    public static class NodeManager
    {
        private static INodeServices _nodeServices;
        private static bool _isInit = false;

        //var func = Edge.Func(@"
        //    return function (data, callback) {
        //        callback(null, 'Node.js welcomes ' + data);
        //    }
        //");

        //Console.WriteLine(await func(".NET"));

        //public NodeManager()
        //{
        //    Init();
        //}

        private static void Init()
        {
            var services = new ServiceCollection();
            services.AddNodeServices(options => {
                // Set any properties that you want on 'options' here
            });

            var serviceProvider = services.BuildServiceProvider();
            _nodeServices = serviceProvider.GetRequiredService<INodeServices>();
            _isInit = true;
        }

        public static async Task<string> CallNodeMethod(string nodeModule, params object[] args)
        {
            if (!_isInit)
                Init();

            var result = await _nodeServices.InvokeAsync<string>(nodeModule, args);
            return result;
        }

        //public async Task<int> CallNodeMethod(string nodeModule, params object[] args)
        //{
        //    var result = await _nodeServices.InvokeAsync<int>(nodeModule, args);
        //    return result;
        //}

        //TODO: need to get this Generic method working, I have no idea why it doesn't recoginise T as a Generic method, it should work! :(
        //public async Task<T> CallNodeMethod(string nodeModule, params object[] args) 
        //{
        //    var result = await _nodeServices.InvokeAsync<T>(nodeModule, args);
        //    return result;
        //}

        //public async Task<int> CallNodeMethod()
        //{
        //    var result = await _nodeServices.InvokeAsync<int>("./addNumbers", 1, 2);
        //    return result;
        //}



    }
}
