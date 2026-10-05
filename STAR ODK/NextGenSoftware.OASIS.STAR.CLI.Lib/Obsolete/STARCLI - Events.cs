//using NextGenSoftware.CLI.Engine;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Events;
//using NextGenSoftware.OASIS.API.Core.Interfaces;

//namespace NextGenSoftware.OASIS.STAR.CLI.Lib
//{
//    public static partial class STARCLI
//    {
//        public static void HandleBooleansResponse(bool isSuccess, string successMessage, string errorMessage)
//        {
//            if (isSuccess)
//                CLIEngine.ShowSuccessMessage(successMessage);
//            else
//                CLIEngine.ShowErrorMessage(errorMessage);
//        }

//        public static void HandleOASISResponse<T>(OASISResult<T> result, string successMessage, string errorMessage)
//        {
//            if (!result.IsError && result.Result != null)
//                CLIEngine.ShowSuccessMessage(successMessage);
//            else
//                CLIEngine.ShowErrorMessage($"{errorMessage}Reason: {result.Message}");
//        }

//        public static void HandleHolonsOASISResponse(OASISResult<IEnumerable<IHolon>> result)
//        {
//            if (!result.IsError && result.Result != null)
//            {
//                CLIEngine.ShowSuccessMessage($"{result.Result.Count()} Holon(s) Loaded:");
//                ShowHolons(result.Result, false);
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Error Loading Holons. Reason: {result.Message}");
//        }

//        private static void CelestialBody_OnZomeError(object sender, ZomeErrorEventArgs e)
//        {
//            CLIEngine.ShowErrorMessage($"Celestial Body Zome Error: {e.Result.Message}");
//        }

//        private static void CelestialBody_OnHolonSaved(object sender, HolonSavedEventArgs e)
//        {
//            CLIEngine.ShowSuccessMessage($"Celestial Body Holon Saved: {e.Result.Message} Name: {e.Result.Result.Name}, Id: {e.Result.Result.Id}, Type: {Enum.GetName(typeof(HolonType), e.Result.Result.HolonType)}");
//        }

//        private static void CelestialBody_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
//        {
//            CLIEngine.ShowSuccessMessage($"Celestial Body Holon Loaded: {e.Result.Message}. Name: {e.Result.Result.Name}, Id: {e.Result.Result.Id}, Type: {Enum.GetName(typeof(HolonType), e.Result.Result.HolonType)}");
//        }
//    }
//}