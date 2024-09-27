namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    public enum GrantedFunctionsType
    {
        All,
        Listed
    }
}

//TODO: We may need to convert the enum to a string as the js client does:
//export enum GrantedFunctionsType
//{
//    All = "All",
//    Listed = "Listed",
//}


//https://codepal.ai/language-translator/typescript-to-csharp


//export type GrantedFunctions =
//  | { [GrantedFunctionsType.All]: null }
//  | { [GrantedFunctionsType.Listed]: [ZomeName, FunctionName][] };


//=

/*
using System;
using System.Collections.Generic;

public enum GrantedFunctionsType
{
    All,
    Listed
}

public class GrantedFunctions
{
    public Dictionary<GrantedFunctionsType, object> Functions { get; set; }

    public GrantedFunctions()
    {
        Functions = new Dictionary<GrantedFunctionsType, object>();
    }
}

public class ZomeName
{
    public string Name { get; set; }
}

public class FunctionName
{
    public string Name { get; set; }
}

public class Program
{
    public static void Main()
    {
        GrantedFunctions grantedFunctions = new GrantedFunctions();

        // Example 1: All functions granted
        grantedFunctions.Functions.Add(GrantedFunctionsType.All, null);

        // Example 2: Listed functions granted
        List<Tuple<ZomeName, FunctionName>> listedFunctions = new List<Tuple<ZomeName, FunctionName>>();
        listedFunctions.Add(new Tuple<ZomeName, FunctionName>(new ZomeName { Name = "Zome1" }, new FunctionName { Name = "Function1" }));
        listedFunctions.Add(new Tuple<ZomeName, FunctionName>(new ZomeName { Name = "Zome2" }, new FunctionName { Name = "Function2" }));
        grantedFunctions.Functions.Add(GrantedFunctionsType.Listed, listedFunctions);

        // Accessing the granted functions
        if (grantedFunctions.Functions.ContainsKey(GrantedFunctionsType.All))
        {
            Console.WriteLine("All functions granted");
        }
        else if (grantedFunctions.Functions.ContainsKey(GrantedFunctionsType.Listed))
        {
            List<Tuple<ZomeName, FunctionName>> listed = (List<Tuple<ZomeName, FunctionName>>)grantedFunctions.Functions[GrantedFunctionsType.Listed];
            Console.WriteLine("Listed functions granted:");
            foreach (var tuple in listed)
            {
                Console.WriteLine($"Zome: {tuple.Item1.Name}, Function: {tuple.Item2.Name}");
            }
        }
    }
}*/