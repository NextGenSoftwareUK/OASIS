
using NextGenSoftware.OASIS.API.Core.Helpers;
using {OAPPNAMESPACE};

Console.WriteLine("Welcome To The {OAPPNAME} Console");

Console.WriteLine("Saving Test Holon...");
//CelestialBodyOnly:{CELESTIALBODY} {CELESTIALBODYVAR} = new {CELESTIALBODY}();
//CelestialBodyOnly:OASISResult<{HOLON}> saveHolonResult = await {CELESTIALBODYVAR}.Save{HOLON}Async(new {HOLON}() { Name = "Test Holon" });
//ZomesAndHolonsOnly:{HOLON} holon = new {HOLON}{ Name = "Test Holon" };
//ZomesAndHolonsOnly:OASISResult<{HOLON}> saveHolonResult = holon.SaveAsync();

if (!saveHolonResult.IsError && saveHolonResult.Result != null)
{
    Console.WriteLine($"Test Holon Saved. Id: {saveHolonResult.Result.Id}, Created Date: {saveHolonResult.Result.CreatedDate}");
    {HOLON} testHolon = saveHolonResult.Result;

    Console.WriteLine("Loading Test Holon...");
    OASISResult<{HOLON}> loadHolonResult = await {CELESTIALBODYVAR}.Load{HOLON}Async(testHolon.Id);

    if (!loadHolonResult.IsError && loadHolonResult.Result != null)
    {
        testHolon = loadHolonResult.Result;
        Console.WriteLine($"Test Holon Loaded. Id: {testHolon.Id}, Created Date: {testHolon.CreatedDate}");
    }
}