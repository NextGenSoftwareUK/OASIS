using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.STAR;
using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.Native.EndPoint;
using { OAPPNAMESPACE};

Console.WriteLine("Welcome To The {OAPPNAME} Console");

CLIEngine.ShowWorkingMessage("BOOTING OASIS...", true, 1, true);
OASISResult<bool> bootResult = await STAR.OASISAPI.BootOASISAsync();
//OASISResult<bool> bootResult2 = await OASISAPI.BootOASISAsync();

if (bootResult != null && !bootResult.IsError)
{
    //CLIEngine.ShowSuccessMessage("DONE!");
    CLIEngine.ShowWorkingMessage("Saving Test Holon...");
    //CelestialBodyOnly:{CELESTIALBODY} {CELESTIALBODYVAR} = new {CELESTIALBODY}();
    //CelestialBodyOnly:OASISResult<{HOLON}> saveHolonResult = await {CELESTIALBODYVAR}.Save{HOLON}Async(new {HOLON}() { Name = "Test Holon" });
    //ZomesAndHolonsOnly:{HOLON} holon = new {HOLON}() { Name = "Test Holon" };
    //ZomesAndHolonsOnly:OASISResult<IHolon> saveHolonResult = await holon.SaveAsync();

    if (!saveHolonResult.IsError && saveHolonResult.Result != null)
    {
        CLIEngine.ShowMessage($"Test Holon Saved. Id: {saveHolonResult.Result.Id}, Created Date: {saveHolonResult.Result.CreatedDate}");
        //CelestialBodyOnly:{HOLON} testHolon = saveHolonResult.Result;
        //ZomesAndHolonsOnly:{HOLON} testHolon = ({HOLON})saveHolonResult.Result;

        CLIEngine.ShowWorkingMessage("Loading Test Holon...");
        //CelestialBodyOnly:OASISResult<{HOLON}> loadHolonResult = await {CELESTIALBODYVAR}.Load{HOLON}Async(testHolon.Id);
        //ZomesAndHolonsOnly:OASISResult<IHolon> loadHolonResult = await holon.LoadAsync();

        if (!loadHolonResult.IsError && loadHolonResult.Result != null)
        {
            //CelestialBodyOnly:testHolon = loadHolonResult.Result;
            //ZomesAndHolonsOnly:testHolon = ({HOLON})loadHolonResult.Result;
            CLIEngine.ShowMessage($"Test Holon Loaded. Id: {testHolon.Id}, Created Date: {testHolon.CreatedDate}");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon. Reason: {loadHolonResult.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon. Reason: {saveHolonResult.Message}");

    //Alternatively you can save/load holons/data using the Data API/HolonManager on the OASIS API.
    CLIEngine.ShowWorkingMessage("Saving Test Holon...");
    OASISResult<IHolon> saveHolonResult2 = await STAR.OASISAPI.Data.SaveHolonAsync(new {HOLON}() { Name = "Test Holon" });
    //OASISResult<IHolon> saveHolonResult2 = await OASISAPI.Data.SaveHolonAsync(new SuperTest2() { Name = "Test Holon" });

    if (!saveHolonResult2.IsError && saveHolonResult2.Result != null)
    {
        CLIEngine.ShowMessage($"Test Holon Saved. Id: {saveHolonResult2.Result.Id}, Created Date: {saveHolonResult2.Result.CreatedDate}");
        {HOLON} testHolon = ({HOLON})saveHolonResult2.Result; //If you use Data API above you will need to cast here.

        CLIEngine.ShowWorkingMessage("Loading Test Holon...");
        OASISResult<IHolon> loadHolonResult2 = await STAR.OASISAPI.Data.LoadHolonAsync(testHolon.Id);
       // OASISResult<IHolon> loadHolonResult2 = await OASISAPI.Data.LoadHolonAsync(testHolon.Id);

        if (!loadHolonResult2.IsError && loadHolonResult2.Result != null)
        {
            testHolon = ({HOLON})loadHolonResult2.Result; //If you use Data API above you will need to cast here.
            CLIEngine.ShowMessage($"Test Holon Loaded. Id: {testHolon.Id}, Created Date: {testHolon.CreatedDate}");
        }
        else
            CLIEngine.ShowErrorMessage($"Error Loading Holon. Reason: {loadHolonResult2.Message}");
    }
    else
        CLIEngine.ShowErrorMessage($"Error Saving Holon. Reason: {saveHolonResult2.Message}");
}
else
    CLIEngine.ShowErrorMessage($"Error Booting OASIS: Reason: {bootResult.Message}");