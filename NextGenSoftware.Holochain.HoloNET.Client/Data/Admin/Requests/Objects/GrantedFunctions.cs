
using MessagePack;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class GrantedFunctions
    {
        [Key("functions")]
        public Dictionary<GrantedFunctionsType, List<(string, string)>> Functions { get; set; }

        public GrantedFunctions()
        {
            Functions = new Dictionary<GrantedFunctionsType, List<(string, string)>>();
        }
    }


    //public class GrantedFunctions
    //{
    //    //public Dictionary<GrantedFunctionsType, object> Functions { get; set; }
    //    //public Dictionary<GrantedFunctionsType, List<Tuple<string, string>>> Functions { get; set; }
    //    public Dictionary<GrantedFunctionsType, List<(string, string)>> Functions { get; set; }

    //    public GrantedFunctions()
    //    {
    //        Functions = new Dictionary<GrantedFunctionsType, List<(string, string)>>();
    //    }
    //}

}

//public class Program
//{
//    public static void Main()
//    {
//        GrantedFunctions grantedFunctions = new GrantedFunctions();

//        // Example 1: All functions granted
//        grantedFunctions.Functions.Add(GrantedFunctionsType.All, null);

//        // Example 2: Listed functions granted
//        List<Tuple<ZomeName, FunctionName>> listedFunctions = new List<Tuple<ZomeName, FunctionName>>();
//        listedFunctions.Add(new Tuple<ZomeName, FunctionName>(new ZomeName { Name = "Zome1" }, new FunctionName { Name = "Function1" }));
//        listedFunctions.Add(new Tuple<ZomeName, FunctionName>(new ZomeName { Name = "Zome2" }, new FunctionName { Name = "Function2" }));
//        grantedFunctions.Functions.Add(GrantedFunctionsType.Listed, listedFunctions);

//        // Accessing the granted functions
//        if (grantedFunctions.Functions.ContainsKey(GrantedFunctionsType.All))
//        {
//            Console.WriteLine("All functions granted");
//        }
//        else if (grantedFunctions.Functions.ContainsKey(GrantedFunctionsType.Listed))
//        {
//            List<Tuple<ZomeName, FunctionName>> listed = (List<Tuple<ZomeName, FunctionName>>)grantedFunctions.Functions[GrantedFunctionsType.Listed];
//            Console.WriteLine("Listed functions granted:");
//            foreach (var tuple in listed)
//            {
//                Console.WriteLine($"Zome: {tuple.Item1.Name}, Function: {tuple.Item2.Name}");
//            }
//        }
//    }
//}