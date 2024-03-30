using System;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR;
using {OAPPNAMESPACE};

Console.WriteLine("Welcome To The {OAPPNAME} Console");

CLIEngine.ShowWorkingMessage("BOOTING OASIS...");

//OASISResult<bool> bootResult = await OASISAPI.BootOASISAsync();
OASISResult<bool> bootResult = await STAR.OASISAPI.BootOASISAsync();

if (bootResult != null && !bootResult.IsError)
{
    CLIEngine.ShowWorkingMessage("Saving Test Holon...", true, 1, true);

    {HOLON} holon = new {HOLON}() { Name = "Test Holon" };
    holon.{STRINGPROPERTY} = "test custom property value";

    //CelestialBodyOnly:{CELESTIALBODY} {CELESTIALBODYVAR} = new {CELESTIALBODY}();
    //CelestialBodyOnly:OASISResult<{HOLON}> saveHolonResult = await {CELESTIALBODYVAR}.Save{HOLON}Async(holon);
    //CelestialBodyOnly://OASISResult<{HOLON}> saveHolonResult = await holon.SaveAsync<{HOLON}>(); // Alternatively you can save holons by calling Save(Async) on them directly.
    //ZomesAndHolonsOnly:OASISResult<{HOLON}> saveHolonResult = await holon.SaveAsync<{HOLON}>();

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        CLIEngine.ShowMessage($"Test Holon Saved. Id: {saveHolonResult.Result.Id}, Created Date: {saveHolonResult.Result.CreatedDate}, {STRINGPROPERTY} = {saveHolonResult.Result.{STRINGPROPERTY}}");
        CLIEngine.ShowMessage($"Test Holon Saved. Id: {holon.Id}, Created Date: {holon.CreatedDate}, {STRINGPROPERTY} = {holon.{STRINGPROPERTY}}");

        //Create a new instance of the holon to empty out all properties so it is a fair load test...
        holon = new {HOLON}() { Id = holon.Id };

        CLIEngine.ShowWorkingMessage("Loading Test Holon...");
        //CelestialBodyOnly:OASISResult<{HOLON}> loadHolonResult = await {CELESTIALBODYVAR}.Load{HOLON}Async(testHolon.Id);
        //CelestialBodyOnly://OASISResult<{HOLON}> loadHolonResult = await holon.LoadAsync<{HOLON}>(); // Alternatively you can load holons by calling Load(Async) on them directly.
        //ZomesAndHolonsOnly:OASISResult<{HOLON}> loadHolonResult = await holon.LoadAsync<{HOLON}>();

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            CLIEngine.ShowMessage($"Test Holon Loaded. Id: {saveHolonResult.Result.Id}, Created Date: {saveHolonResult.Result.CreatedDate}, {STRINGPROPERTY} = {saveHolonResult.Result.{STRINGPROPERTY}}");
            CLIEngine.ShowMessage($"Test Holon Loaded. Id: {holon.Id}, Created Date: {holon.CreatedDate}, {STRINGPROPERTY} = {holon.{STRINGPROPERTY}}");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon. Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon. Reason: {saveHolonResult.Message}");

    //Alternatively you can save/load holons/data using the Data API/HolonManager on the OASIS API.
    CLIEngine.ShowWorkingMessage("Saving Test Holon...");

    holon = new SuperTest2() { Name = "Test Holon" };
    holon.{STRINGPROPERTY} = "test custom property value!";

    //saveHolonResult = await OASISAPI.Data.SaveHolonAsync<{HOLON}>(holon);
    saveHolonResult = await STAR.OASISAPI.Data.SaveHolonAsync<SuperTest2>(holon);

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        CLIEngine.ShowMessage($"Test Holon Saved. Id: {saveHolonResult.Result.Id}, Created Date: {saveHolonResult.Result.CreatedDate}, {STRINGPROPERTY}: {saveHolonResult.Result.{STRINGPROPERTY}}");
        CLIEngine.ShowMessage($"Test Holon Saved. Id: {holon.Id}, Created Date: {holon.CreatedDate}, {STRINGPROPERTY} = {holon.{STRINGPROPERTY}}");

        CLIEngine.ShowWorkingMessage("Loading Test Holon...");
        //OASISResult<{HOLON}> loadHolonResult = await OASISAPI.Data.LoadHolonAsync<{HOLON}>(testHolon.Id);
        OASISResult<{HOLON}> loadHolonResult = await STAR.OASISAPI.Data.LoadHolonAsync<{HOLON}>(holon.Id);

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            holon = loadHolonResult.Result;
            CLIEngine.ShowMessage($"Test Holon Loaded. Id: {holon.Id}, Created Date: {holon.CreatedDate}, {STRINGPROPERTY} = {holon.{STRINGPROPERTY}}");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon. Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon. Reason: {saveHolonResult.Message}");
}
else
    CLIEngine.ShowErrorMessage($"Error Booting OASIS: Reason: {bootResult.Message}");