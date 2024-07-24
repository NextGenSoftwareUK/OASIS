using System;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR;
using NextGenSoftware.OASIS.STAR.CLI.Lib;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using {OAPPNAMESPACE};

Console.WriteLine("Welcome To The {OAPPNAME} Console");

//OASISResult<bool> bootResult = await OASISAPI.BootOASISAsync();
OASISResult<bool> bootResult = await STAR.OASISAPI.BootOASISAsync();

if (bootResult != null && !bootResult.IsError)
{
    SuperZome2 zome = new SuperZome2();

    CLIEngine.ShowWorkingMessage("Loading Child Holons...");
    OASISResult<IEnumerable<IHolon>> holonsReuslt = zome.LoadChildHolons();

    if (!holonsReuslt.IsError && holonsReuslt.Result != null)
    {
        CLIEngine.ShowErrorMessage($"{holonsReuslt.Result.Count()} Child Holons Loaded.");
        STARCLI.ShowHolons(holonsReuslt.Result);
    }
    else
        CLIEngine.ShowErrorMessage($"Error Loading Child Holons. Reason: {holonsReuslt.Message}");


    CLIEngine.ShowWorkingMessage("Loading Zome...");
    OASISResult<IZome> zomeResult = await zome.LoadAsync();

    if (!zomeResult.IsError && zomeResult.Result != null)
        CLIEngine.ShowErrorMessage($"Zome Loaded. Name: {zomeResult.Result.Name}, Name: {zome.Name}, Children Count: {zome.Children.Count()}");
    else
        CLIEngine.ShowErrorMessage($"Error Loading Zome. Reason: {zomeResult.Message}");


    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Zome)...");

    {HOLON} holon = new {HOLON}();

    //Example of how to set one of the generated properties/fields of our strongly typed type (holon). The same applies for any generated zomes or celestial bodies.
    holon.{STRINGPROPERTY} = "test custom property value (Using Zome).";

    //Name and Description are two built-in properties for all COSMIC objects.
    holon.Name = "test name (Using Zome).";
    holon.Description = "test desc (Using Zome).";

    //We can save any custom meta data like this (there is no limit to how much metadata you wish to save in the keyvalue pairs.
    holon.MetaData["CustomData"] = "test custom data (Using Zome).";

    OASISResult<SuperTest2> saveHolonResult = zome.SaveSuperTest2(holon);

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Saved (Using Zome).");
        ShowHolon(holon, "holon: Test Holon Saved (Using Zome).");

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Zome)...");
        OASISResult<SuperTest2> loadHolonResult = await zome.LoadSuperTest2Async(holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            holon = loadHolonResult.Result;
            ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Loaded (Using Zome).");
            ShowHolon(holon, "holon: Test Holon Loaded (Using Zome).");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Zome). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Zome). Reason: {saveHolonResult.Message}");


    //Alternatively you can use the generic functions in GlobalHolonData to save/load/delete holons. There are generic and standard versions of all load & save functions. If you use the generic versions then it works in the same way as above, however if you use the standard versions then they are not strongly typed to the generated holons/zomes, etc
    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Zome GlobalHolonData Generic SaveAsync)...");

    holon = new {HOLON}();
    holon.{STRINGPROPERTY} = "test custom property value (Using Zome GlobalHolonData Standard SaveAsync).";
    holon.Name = "test name (Using Zome GlobalHolonData Standard SaveAsync).";
    holon.Description = "test desc (Using Zome GlobalHolonData Standard SaveAsync).";
    holon.MetaData["CustomData"] = "test custom data (Using Zome GlobalHolonData Standard SaveAsync).";

    saveHolonResult = await zome.GlobalHolonData.SaveHolonAsync<{HOLON}> (holon);

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Saved (Using Zome GlobalHolonData Generic SaveAsync).");
        ShowHolon(holon, "holon: Test Holon Saved (Using Zome GlobalHolonData Generic SaveAsync).");

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Zome GlobalHolonData Generic LoadAsync)...");
        OASISResult<{HOLON}> loadHolonResult = await zome.GlobalHolonData.LoadHolonAsync<{HOLON}> (holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Loaded (Using Zome GlobalHolonData Generic SaveAsync).");
            ShowHolon(holon, "holon: Test Holon Loaded (Using Zome GlobalHolonData Generic SaveAsync).");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Zome GlobalHolonData Generic LoadAsync). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Zome GlobalHolonData Generic LoadAsync). Reason: {saveHolonResult.Message}");



    //Below is an example of using the standard versions of the GlobalHolonData functions.
    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Zome GlobalHolonData Standard SaveAsync)...");

    //Because this test uses Standard SaveAsync it means that any custom properties generated from the DNA metadata will still be saved in the MetaData property and to view after saving you will only be able to view via the MetaData property (see the ShowHolon overload at the bottom that takes IHolon as a param) rather than a strongly typed property (as the other tests use).
    holon = new {HOLON}();
    holon.{STRINGPROPERTY} = "test custom property value (Using Zome GlobalHolonData Standard SaveAsync).";
    holon.Name = "test name (Using Zome GlobalHolonData Standard SaveAsync).";
    holon.Description = "test desc (Using Zome GlobalHolonData Standard SaveAsync).";
    holon.MetaData["CustomData"] = "test custom data (Using Zome GlobalHolonData Standard SaveAsync).";

    OASISResult<IHolon> saveGlobalHolonResult = await zome.GlobalHolonData.SaveHolonAsync(holon);

    if (!saveGlobalHolonResult.IsError && saveGlobalHolonResult.Result != null)
    {
        ShowHolon(saveGlobalHolonResult.Result, "saveGlobalHolonResult: Test Holon Saved (Using Zome GlobalHolonData Standard SaveAsync).");
        ShowHolon(holon, "holon: Test Holon Saved (Using Zome GlobalHolonData Standard SaveAsync).");

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Zome GlobalHolonData Standard LoadAsync)...");
        OASISResult<IHolon> loadHolonResult = await zome.GlobalHolonData.LoadHolonAsync(holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            ShowHolon(saveGlobalHolonResult.Result, "saveGlobalHolonResult: Test Holon Loaded (Using Zome GlobalHolonData Standard SaveAsync).");
            ShowHolon(holon, "holon: Test Holon Loaded (Using Zome GlobalHolonData Standard SaveAsync).");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Zome GlobalHolonData Standard LoadAsync). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Zome GlobalHolonData Standard LoadAsync). Reason: {saveGlobalHolonResult.Message}");



    //CelestialBodyOnly:holon = new {HOLON}();
    //CelestialBodyOnly:holon.{STRINGPROPERTY} = "test custom property value (Using CelestialBody).";
    //CelestialBodyOnly:holon.Name = "test name (Using CelestialBody).";
    //CelestialBodyOnly:holon.Description = "test desc (Using CelestialBody).";
    //CelestialBodyOnly:holon.MetaData["CustomData"] = "test custom data (Using CelestialBody).";

    //CelestialBodyOnly:{CELESTIALBODY} {CELESTIALBODYVAR} = new {CELESTIALBODY}();
    //CelestialBodyOnly:saveHolonResult = await {CELESTIALBODYVAR}.Save{HOLON}Async(holon);
    //CelestialBodyOnly://saveHolonResult = await supermama.GlobalHolonData.SaveHolonAsync<{HOLON}>(holon); //Alternatively you can use the generic GlobalHolonData functions just like you could with the zome example above (GlobalHolonData is available on ALL COSMIC objects so includes CelestialBodies, CelesitalSpaces, Zomes & Holons).
    //CelestialBodyOnly://saveGlobalHolonResult = await supermama.GlobalHolonData.SaveHolonAsync(holon);

    //CelestialBodyOnly:if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    //CelestialBodyOnly:{
    //CelestialBodyOnly:ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Saved (Using CelestialBody).");
    //CelestialBodyOnly:ShowHolon(holon, "holon: Test Holon Saved (Using CelestialBody).");

    //CelestialBodyOnly:CLIEngine.ShowWorkingMessage("Loading Test Holon (Using CelestialBody)...");
    //CelestialBodyOnly:OASISResult<IHolon> loadHolonResult = await zome.GlobalHolonData.LoadHolonAsync(holon.Id);

    //CelestialBodyOnly:if (!loadHolonResult.IsError && loadHolonResult.Result != null)
    //CelestialBodyOnly:{
    //CelestialBodyOnly:ShowHolon(saveGlobalHolonResult.Result, "saveGlobalHolonResult: Test Holon Loaded (Using CelestialBody).");
    //CelestialBodyOnly:ShowHolon(holon, "holon: Test Holon Loaded (Using CelestialBody).");
    //CelestialBodyOnly:}
    //CelestialBodyOnly:else
    //CelestialBodyOnly:CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Zome GlobalHolonData Standard LoadAsync). Reason: {loadHolonResult.Message}");
    //CelestialBodyOnly:}
    //CelestialBodyOnly:else
    //CelestialBodyOnly:CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Zome GlobalHolonData Standard LoadAsync). Reason: {saveHolonResult.Message}");



    holon = new {HOLON}();
    holon.{STRINGPROPERTY} = "test custom property value (Using Holon).";
    holon.Name = "test name (Using Holon).";
    holon.Description = "test desc (Using Holon).";
    holon.MetaData["CustomData"] = "test custom data (Using Holon).";

    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Holon)...", true, 1, true);
    saveHolonResult = await holon.SaveAsync<{HOLON}>();

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Saved (Using Holon).");
        ShowHolon(holon, "holon: Test Holon Saved (Using Holon).");

        //Create a new instance of the holon to empty out all properties so it is a fair load test...
        holon = new {HOLON}() { Id = holon.Id };

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Holon)...");
        OASISResult<{HOLON}> loadHolonResult = await holon.LoadAsync<{HOLON}>();

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Loaded (Using Holon).");
            ShowHolon(holon, "holon: Test Holon Loaded (Using Holon).");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Holon). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Holon). Reason: {saveHolonResult.Message}");



    //Alternatively you can save/load holons/data using the Data API/HolonManager on the OASIS API (this is what is also used on the REST API).
    //This works in a similar way to the GlobalHolonData functions above.
    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Data API)...");

    holon = new {HOLON}();
    holon.{STRINGPROPERTY} = "test custom property value (Using Data API).";
    holon.Name = "test name (Using Data API).";
    holon.Description = "test desc (Using Data API).";
    holon.MetaData["CustomData"] = "test custom data (Using Data API).";

    saveHolonResult = await STAR.OASISAPI.Data.SaveHolonAsync<{HOLON}>(holon);
    //saveGlobalHolonResult = await OASISAPI.Data.SaveHolonAsync(holon); //Just like with the GlobalHolonData you can use the Generic of Standard versions of the functions.

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Saved (Using Data API).");
        ShowHolon(holon, "holon: Test Holon Saved (Using Data API).");

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Data API)...");
        //OASISResult<{HOLON}> loadHolonResult = await OASISAPI.Data.LoadHolonAsync<{HOLON}>(testHolon.Id);
        OASISResult<{HOLON}> loadHolonResult = await STAR.OASISAPI.Data.LoadHolonAsync<{HOLON}>(holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Loaded (Using Data API).");
            ShowHolon(holon, "holon: Test Holon Loaded (Using Data API).");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Data API). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Data API). Reason: {saveHolonResult.Message}");
}
else
    CLIEngine.ShowErrorMessage($"Error Booting OASIS: Reason: {bootResult.Message}");



public static partial class Program
{
    public static void ShowHolon(IHolon holon, string prefix = "")
    {
        CLIEngine.ShowMessage($"{prefix} Id: {holon.Id}, Created Date: {holon.CreatedDate}, Name: {holon.Name}, Description: {holon.Description}, {STRINGPROPERTY}: {holon.MetaData["{STRINGPROPERTY}"]}, CustomData: {holon.MetaData["CustomData"]}");
    }

    public static void ShowHolon({HOLON} holon, string prefix = "")
    {
        CLIEngine.ShowMessage($"{prefix} Id: {holon.Id}, Created Date: {holon.CreatedDate}, Name: {holon.Name}, Description: {holon.Description}, {STRINGPROPERTY}: {holon.{STRINGPROPERTY}}, CustomData: {holon.MetaData["CustomData"]}");
    }
}