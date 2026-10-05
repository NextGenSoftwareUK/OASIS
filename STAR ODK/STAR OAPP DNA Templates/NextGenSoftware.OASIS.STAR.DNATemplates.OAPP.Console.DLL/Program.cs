using System;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR;
using NextGenSoftware.OASIS.STAR.CLI.Lib;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using {OAPPNAMESPACE};

CLIEngine.ShowDivider();
CLIEngine.ShowMessage("Welcome To The {OAPPNAME} Console");
CLIEngine.ShowMessage("I am a custom tag: [[MYCUSTOMTAG]]!");
CLIEngine.ShowDivider();
Console.WriteLine("");

//OASISResult<bool> bootResult = await OASISAPI.BootOASISAsync();
OASISResult<bool> bootResult = await STAR.OASISAPI.BootOASISAsync(OASISDNAPath: "DNA\\OASIS_DNA.json");

if (bootResult != null && !bootResult.IsError)
{
    //{INITCUSTOMTAGHOLONS}
    {ZOME1} zome = new {ZOME1}();

    CLIEngine.ShowDivider();
    CLIEngine.ShowWorkingMessage("Loading Zome...");
    OASISResult<IZome> zomeResult = await zome.LoadAsync();

    if (!zomeResult.IsError && zomeResult.Result != null)
        CLIEngine.ShowSuccessMessage($"Zome Loaded. Name: {zomeResult.Result.Name}, Name: {zome.Name}, Children Count: {zome.Children.Count()}");
    else
        CLIEngine.ShowErrorMessage($"Error Loading Zome. Reason: {zomeResult.Message}");

    {{MYCUSTOMHOLONTAG}} = "My test holon custom tag";
    CLIEngine.ShowMessage($"Custom Holon MetaTag: {{{MYCUSTOMHOLONTAG}}}");
    CLIEngine.ShowMessage(string.Concat("Custom Holon MetaTag: ", {{MYCUSTOMHOLONTAG}}));

    CLIEngine.ShowWorkingMessage("Loading Child Holons...");
    OASISResult<IEnumerable<IHolon>> holonsReuslt = zome.LoadChildHolons();

    if (!holonsReuslt.IsError && holonsReuslt.Result != null)
    {
        CLIEngine.ShowMessage("", false);
        STARCLI.Holons.ShowHolons(holonsReuslt.Result);
    }
    else
        CLIEngine.ShowErrorMessage($"Error Loading Child Holons. Reason: {holonsReuslt.Message}");


    CLIEngine.ShowDivider();
    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Zome)...");

    {HOLON1} holon = new {HOLON1}();

    //Example of how to set one of the generated properties/fields of our strongly typed type (holon). The same applies for any generated zomes or celestial bodies.
    holon.{HOLON1_STRINGPROPERTY1} = "test custom property value (Using Zome)";

    //Name and Description are two built-in properties for all COSMIC objects.
    holon.Name = "test name (Using Zome)";
    holon.Description = "test desc (Using Zome)";

    //We can save any custom meta data like this (there is no limit to how much metadata you wish to save in the keyvalue pairs.
    holon.MetaData["CustomData"] = "test custom data (Using Zome)";

    OASISResult<{HOLON1}> saveHolonResult = zome.Save{HOLON1}(holon);

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Saved (Using Zome)");
        ShowHolon(holon, "holon: Test Holon Saved (Using Zome)");

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Zome)...");
        OASISResult<{HOLON1}> loadHolonResult = await zome.Load{HOLON1}Async(holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
            ShowHolon(loadHolonResult.Result, "loadHolonResult: Test Holon Loaded (Using Zome)");
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Zome). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Zome). Reason: {saveHolonResult.Message}");


    //Alternatively you can use the generic functions in GlobalHolonData to save/load/delete holons. There are generic and standard versions of all load & save functions. If you use the generic versions then it works in the same way as above, however if you use the standard versions then they are not strongly typed to the generated holons/zomes, etc
    CLIEngine.ShowDivider();
    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Zome GlobalHolonData Generic SaveAsync)...");

    holon = new {HOLON1}();
    holon.{HOLON1_STRINGPROPERTY1} = "test custom property value (Using Zome GlobalHolonData Standard SaveAsync)";
    holon.Name = "test name (Using Zome GlobalHolonData Standard SaveAsync)";
    holon.Description = "test desc (Using Zome GlobalHolonData Standard SaveAsync)";
    holon.MetaData["CustomData"] = "test custom data (Using Zome GlobalHolonData Standard SaveAsync)";

    saveHolonResult = await zome.GlobalHolonData.SaveHolonAsync<{HOLON1}>(holon);

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Saved (Using Zome GlobalHolonData Generic SaveAsync).");
        ShowHolon(holon, "holon: Test Holon Saved (Using Zome GlobalHolonData Generic SaveAsync).");

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Zome GlobalHolonData Generic LoadAsync)...");
        OASISResult<{HOLON1}> loadHolonResult = await zome.GlobalHolonData.LoadHolonAsync<{HOLON1}> (holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
            ShowHolon(loadHolonResult.Result, "loadHolonResult: Test Holon Loaded (Using Zome GlobalHolonData Generic SaveAsync).");
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Zome GlobalHolonData Generic LoadAsync). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Zome GlobalHolonData Generic LoadAsync). Reason: {saveHolonResult.Message}");



    //Below is an example of using the standard versions of the GlobalHolonData functions.
    CLIEngine.ShowDivider();
    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Zome GlobalHolonData Standard SaveAsync)...");

    //Because this test uses Standard SaveAsync it means that any custom properties generated from the DNA metadata will still be saved in the MetaData property and to view after saving you will only be able to view via the MetaData property (see the ShowHolon overload at the bottom that takes IHolon as a param) rather than a strongly typed property (as the other tests use).
    holon = new {HOLON1}();
    holon.{HOLON1_STRINGPROPERTY1} = "test custom property value (Using Zome GlobalHolonData Standard SaveAsync)";
    holon.Name = "test name (Using Zome GlobalHolonData Standard SaveAsync)";
    holon.Description = "test desc (Using Zome GlobalHolonData Standard SaveAsync)";
    holon.MetaData["CustomData"] = "test custom data (Using Zome GlobalHolonData Standard SaveAsync)";

    OASISResult<IHolon> saveGlobalHolonResult = await zome.GlobalHolonData.SaveHolonAsync(holon);

    if (!saveGlobalHolonResult.IsError && saveGlobalHolonResult.Result != null)
    {
        ShowHolon(saveGlobalHolonResult.Result, "saveGlobalHolonResult: Test Holon Saved (Using Zome GlobalHolonData Standard SaveAsync).");
        ShowHolon(holon, "holon: Test Holon Saved (Using Zome GlobalHolonData Standard SaveAsync).");

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Zome GlobalHolonData Standard LoadAsync)...");
        OASISResult<IHolon> loadHolonResult = await zome.GlobalHolonData.LoadHolonAsync(holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
            ShowHolon(loadHolonResult.Result, "loadHolonResult: Test Holon Loaded (Using Zome GlobalHolonData Standard SaveAsync).");
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Zome GlobalHolonData Standard LoadAsync). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Zome GlobalHolonData Standard LoadAsync). Reason: {saveGlobalHolonResult.Message}");

    
    //CelestialBodyOnly:BEGIN
    CLIEngine.ShowDivider();
    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using CelestialBody)...");

    holon = new {HOLON1}();
    holon.{HOLON1_STRINGPROPERTY1} = "test custom property value (Using CelestialBody)";
    holon.Name = "test name (Using CelestialBody)";
    holon.Description = "test desc (Using CelestialBody)";
    holon.MetaData["CustomData"] = "test custom data (Using CelestialBody)";

    {CELESTIALBODY} {CELESTIALBODYVAR} = new {CELESTIALBODY}();
    saveHolonResult = await {CELESTIALBODYVAR}.Save{HOLON1}Async(holon);
    //saveHolonResult = await {CELESTIALBODY}.GlobalHolonData.SaveHolonAsync<{HOLON1}>(holon); //Alternatively you can use the generic GlobalHolonData functions just like you could with the zome example above (GlobalHolonData is available on ALL COSMIC objects so includes CelestialBodies, CelesitalSpaces, Zomes & Holons).
    //saveGlobalHolonResult = await {CELESTIALBODY}.GlobalHolonData.SaveHolonAsync(holon);

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Saved (Using CelestialBody).");
        ShowHolon(holon, "holon: Test Holon Saved (Using CelestialBody).");

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using CelestialBody)...");
        OASISResult<IHolon> loadHolonResult = await zome.GlobalHolonData.LoadHolonAsync(holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
            ShowHolon(loadHolonResult.Result, "loadHolonResult: Test Holon Loaded (Using CelestialBody).");
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Zome GlobalHolonData Standard LoadAsync). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Zome GlobalHolonData Standard LoadAsync). Reason: {saveHolonResult.Message}");
    //CelestialBodyOnly:END


    holon = new {HOLON1}();
    holon.{HOLON1_STRINGPROPERTY1} = "test custom property value (Using Holon)";
    holon.Name = "test name (Using Holon)";
    holon.Description = "test desc (Using Holon)";
    holon.MetaData["CustomData"] = "test custom data (Using Holon)";

    CLIEngine.ShowDivider();
    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Holon)...");
    saveHolonResult = await holon.SaveAsync<{HOLON1}>();

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Saved (Using Holon).");
        ShowHolon(holon, "holon: Test Holon Saved (Using Holon).");

        //Create a new instance of the holon to empty out all properties so it is a fair load test...
        holon = new {HOLON1}() { Id = holon.Id };

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Holon)...");
        OASISResult<{HOLON1}> loadHolonResult = await holon.LoadAsync<{HOLON1}>();

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            ShowHolon(loadHolonResult.Result, "loadHolonResult: Test Holon Loaded (Using Holon).");
            ShowHolon(holon, "holon: Test Holon Loaded (Using Holon).");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Holon). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Holon). Reason: {saveHolonResult.Message}");



    //Alternatively you can save/load holons/data using the Data API/HolonManager on the OASIS API (this is what is also used on the REST API).
    //This works in a similar way to the GlobalHolonData functions above.
    CLIEngine.ShowDivider();
    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Data API)...");

    holon = new {HOLON1}();
    holon.{HOLON1_STRINGPROPERTY1} = "test custom property value (Using Data API)";
    holon.Name = "test name (Using Data API)";
    holon.Description = "test desc (Using Data API)";
    holon.MetaData["CustomData"] = "test custom data (Using Data API)";

    saveHolonResult = await STAR.OASISAPI.Data.SaveHolonAsync<{HOLON1}>(holon);
    //saveGlobalHolonResult = await OASISAPI.Data.SaveHolonAsync(holon); //Just like with the GlobalHolonData you can use the Generic of Standard versions of the functions.

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        ShowHolon(saveHolonResult.Result, "saveHolonResult: Test Holon Saved (Using Data API).");
        ShowHolon(holon, "holon: Test Holon Saved (Using Data API).");

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Data API)...");
        //OASISResult<{HOLON1}> loadHolonResult = await OASISAPI.Data.LoadHolonAsync<{HOLON1}>(testHolon.Id);
        OASISResult<{HOLON1}> loadHolonResult = await STAR.OASISAPI.Data.LoadHolonAsync<{HOLON1}>(holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
            ShowHolon(loadHolonResult.Result, "loadHolonResult: Test Holon Loaded (Using Data API).");
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
        //CLIEngine.ShowMessage($"{prefix} Id: {holon.Id}, Created Date: {holon.CreatedDate}, Name: {holon.Name}, Description: {holon.Description}, {HOLON1_STRINGPROPERTY1}: {holon.MetaData["{HOLON1_STRINGPROPERTY1}"]}, CustomData: {holon.MetaData["CustomData"]}");
        CLIEngine.ShowMessage($"{prefix} Id: {holon.Id}, Created Date: {holon.CreatedDate}, Name: {holon.Name}, Description: {holon.Description}, CustomData: {holon.MetaData["CustomData"]}");
    }

    public static void ShowHolon({HOLON1} holon, string prefix = "")
    {
        CLIEngine.ShowMessage($"{prefix} Id: {holon.Id}, Created Date: {holon.CreatedDate}, Name: {holon.Name}, Description: {holon.Description}, {HOLON1_STRINGPROPERTY1}: {holon.{HOLON1_STRINGPROPERTY1}}, CustomData: {holon.MetaData["CustomData"]}");
    }
}