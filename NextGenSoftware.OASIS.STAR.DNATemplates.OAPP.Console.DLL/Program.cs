using System;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR;
using NextGenSoftware.OASIS.STAR.CLI.Lib;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using {OAPPNAMESPACE};

Console.WriteLine("Welcome To The {OAPPNAME} Console");

CLIEngine.ShowWorkingMessage("BOOTING OASIS...");

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
    OASISResult<IHolon> zomeResult = await zome.LoadAsync();

    if (!zomeResult.IsError && zomeResult.Result != null)
        CLIEngine.ShowErrorMessage($"Zome Loaded. Name: {zomeResult.Result.Name}, Name: {zome.Name}, Holons Count: {zome.Holons.Count}, Children Count: {zome.Children.Count()}");
    else
        CLIEngine.ShowErrorMessage($"Error Loading Zome. Reason: {zomeResult.Message}");


    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Zome)...");

    SuperTest2 holon = new SuperTest2() { Name = "Test Holon" };
    holon.TestString = "test custom property value";


    OASISResult<SuperTest2> saveHolonResult = zome.SaveSuperTest2(holon);

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        CLIEngine.ShowMessage($"Test Holon Saved (Using Zome). Id: {saveHolonResult.Result.Id}, Created Date: {saveHolonResult.Result.CreatedDate}, TestString = {saveHolonResult.Result.TestString}");
        CLIEngine.ShowMessage($"Test Holon Saved (Using Zome). Id: {holon.Id}, Created Date: {holon.CreatedDate}, TestString = {holon.TestString}");

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Zome)...");
        OASISResult<SuperTest2> loadHolonResult = await zome.LoadSuperTest2Async(holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            holon = loadHolonResult.Result;
            CLIEngine.ShowMessage($"Test Holon Loaded (Using Zome). Id: {holon.Id}, Created Date: {holon.CreatedDate}, TestString = {holon.TestString}");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Zome). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Zome). Reason: {saveHolonResult.Message}");


    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Holon)...", true, 1, true);

    {HOLON} holon = new {HOLON}() { Name = "Test Holon" };
    holon.{STRINGPROPERTY} = "test custom property value";

    //CelestialBodyOnly:{CELESTIALBODY} {CELESTIALBODYVAR} = new {CELESTIALBODY}();
    //CelestialBodyOnly:OASISResult<{HOLON}> saveHolonResult = await {CELESTIALBODYVAR}.Save{HOLON}Async(holon);
    //CelestialBodyOnly://OASISResult<{HOLON}> saveHolonResult = await holon.SaveAsync<{HOLON}>(); // Alternatively you can save holons by calling Save(Async) on them directly.
    //ZomesAndHolonsOnly:OASISResult<{HOLON}> saveHolonResult = await holon.SaveAsync<{HOLON}>();

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        CLIEngine.ShowMessage($"Test Holon Saved (Using Holon). Id: {saveHolonResult.Result.Id}, Created Date: {saveHolonResult.Result.CreatedDate}, {STRINGPROPERTY} = {saveHolonResult.Result.{STRINGPROPERTY}}");
        CLIEngine.ShowMessage($"Test Holon Saved (Using Holon). Id: {holon.Id}, Created Date: {holon.CreatedDate}, {STRINGPROPERTY} = {holon.{STRINGPROPERTY}}");

        //Create a new instance of the holon to empty out all properties so it is a fair load test...
        holon = new {HOLON}() { Id = holon.Id };

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Holon)...");
        //CelestialBodyOnly:OASISResult<{HOLON}> loadHolonResult = await {CELESTIALBODYVAR}.Load{HOLON}Async(testHolon.Id);
        //CelestialBodyOnly://OASISResult<{HOLON}> loadHolonResult = await holon.LoadAsync<{HOLON}>(); // Alternatively you can load holons by calling Load(Async) on them directly.
        //ZomesAndHolonsOnly:OASISResult<{HOLON}> loadHolonResult = await holon.LoadAsync<{HOLON}>();

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            CLIEngine.ShowMessage($"Test Holon Loaded (Using Holon). Id: {saveHolonResult.Result.Id}, Created Date: {saveHolonResult.Result.CreatedDate}, {STRINGPROPERTY} = {saveHolonResult.Result.{STRINGPROPERTY}}");
            CLIEngine.ShowMessage($"Test Holon Loaded (Using Holon). Id: {holon.Id}, Created Date: {holon.CreatedDate}, {STRINGPROPERTY} = {holon.{STRINGPROPERTY}}");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Holon). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Holon). Reason: {saveHolonResult.Message}");

    //Alternatively you can save/load holons/data using the Data API/HolonManager on the OASIS API.
    CLIEngine.ShowWorkingMessage("Saving Test Holon (Using Data API)...");

    holon = new SuperTest2() { Name = "Test Holon" };
    holon.{STRINGPROPERTY} = "test custom property value!";

    //saveHolonResult = await OASISAPI.Data.SaveHolonAsync<{HOLON}>(holon);
    saveHolonResult = await STAR.OASISAPI.Data.SaveHolonAsync<SuperTest2>(holon);

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        CLIEngine.ShowMessage($"Test Holon Saved (Using Data API). Id: {saveHolonResult.Result.Id}, Created Date: {saveHolonResult.Result.CreatedDate}, {STRINGPROPERTY}: {saveHolonResult.Result.{STRINGPROPERTY}}");
        CLIEngine.ShowMessage($"Test Holon Saved (Using Data API). Id: {holon.Id}, Created Date: {holon.CreatedDate}, {STRINGPROPERTY} = {holon.{STRINGPROPERTY}}");

        CLIEngine.ShowWorkingMessage("Loading Test Holon (Using Data API)...");
        //OASISResult<{HOLON}> loadHolonResult = await OASISAPI.Data.LoadHolonAsync<{HOLON}>(testHolon.Id);
        OASISResult<{HOLON}> loadHolonResult = await STAR.OASISAPI.Data.LoadHolonAsync<{HOLON}>(holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            holon = loadHolonResult.Result;
            CLIEngine.ShowMessage($"Test Holon Loaded (Using Data API). Id: {holon.Id}, Created Date: {holon.CreatedDate}, {STRINGPROPERTY} = {holon.{STRINGPROPERTY}}");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon (Using Data API). Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon (Using Data API). Reason: {saveHolonResult.Message}");
}
else
    CLIEngine.ShowErrorMessage($"Error Booting OASIS: Reason: {bootResult.Message}");