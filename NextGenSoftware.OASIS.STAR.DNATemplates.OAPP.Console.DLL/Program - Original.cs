
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.STAR.TestHarness.Genesis;

//Console.WriteLine("NextGen Software STAR DNA Template Console");

//Console.WriteLine("Saving Test Holon...");
//SuperWorld superWorld = new SuperWorld();
//OASISResult<SuperTest> holonResult = await superWorld.SaveSuperTestAsync(new SuperTest() { Name = "Test Holon" });

//if (!holonResult.IsError && holonResult.Result != null)
//{
//    Console.WriteLine($"Test Holon Saved. Id: {holonResult.Result.Id}, Created Date: {holonResult.Result.CreatedDate}");
//    SuperTest testHolon = holonResult.Result;

//    Console.WriteLine("Loading Test Holon...");
//    OASISResult<SuperTest> superTestResult = await superWorld.LoadSuperTestAsync(testHolon.Id);

//    if (!superTestResult.IsError && superTestResult.Result != null)
//    {
//        testHolon = superTestResult.Result;
//        Console.WriteLine($"Test Holon Loaded. Id: {testHolon.Id}, Created Date: {testHolon.CreatedDate}");
//    }
//}