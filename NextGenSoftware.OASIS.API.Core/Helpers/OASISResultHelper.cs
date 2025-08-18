using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class OASISResultHelper
    {
        public static string BuildInnerMessageError(List<string> innerMessages, string seperator = "\n\n", bool addAmpersandAtEnd = false)
        {
            string result = "";
            for (int i = 0; i < innerMessages.Count; i++)
            {
                if (i == innerMessages.Count - 1 && addAmpersandAtEnd && innerMessages.Count > 1)
                    result = string.Concat(result, " & ");

                result = string.Concat(result, innerMessages[i]);

                if (i < innerMessages.Count - 2 && addAmpersandAtEnd)
                    result = string.Concat(result, seperator);

                else if (!addAmpersandAtEnd)
                    result = string.Concat(result, seperator);
            }

            if (addAmpersandAtEnd && (result.Length - seperator.Length) > 0)
                result = result.Substring(0, result.Length - seperator.Length);

            return result;
        }

        public static (OASISResult<T1>, T2) UnWrapOASISResult<T1, T2>(OASISResult<T1> parentResult, OASISResult<T2> result, string errorMessage)
        {
            if (!result.IsError && result.Result != null)
                return (parentResult, result.Result);
            else
            {
                //OASISErrorHandling.HandleError(ref parentResult, string.Format(errorMessage, result.Message));
                OASISErrorHandling.HandleError(ref parentResult, string.Concat(errorMessage, result.Message));

                return (parentResult, default(T2));
            }
        }

        public static (OASISResult<T1>, T2) UnWrapOASISResultWithDefaultErrorMessage<T1, T2>(OASISResult<T1> parentResult, OASISResult<T2> result, string methodName)
        {
            //return UnWrapOASISResult(parentResult, result, $"Error occured in {methodName}. Reason:{0}");
            return UnWrapOASISResult(parentResult, result, $"Error occured in {methodName}. Reason: ");
        }

        public static OASISResult<T2> CopyOASISResultOnlyWithNoInnerResult<T1, T2>(OASISResult<T1> fromResult, OASISResult<T2> toResult, bool copyMessage = true)
        {
            if (toResult == null)
                toResult = new OASISResult<T2>();

            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsWarning = fromResult.IsWarning;
            toResult.DetailedMessage = fromResult.DetailedMessage;
            toResult.WarningCount = fromResult.WarningCount;
            toResult.ErrorCount = fromResult.ErrorCount;
            toResult.HasAnyHolonsChanged = fromResult.HasAnyHolonsChanged;
            toResult.InnerMessages = fromResult.InnerMessages;
            toResult.LoadedCount = fromResult.LoadedCount;
            toResult.SavedCount = fromResult.SavedCount;
            toResult.MetaData = fromResult.MetaData;

            if (copyMessage)
                toResult.Message = fromResult.Message;

            return toResult;
        }

        public static OASISResult<T2> CopyOASISResultOnlyWithNoInnerResult<T1, T2>(OASISResult<T1> fromResult, bool copyMessage = true) 
        {
            return CopyOASISResultOnlyWithNoInnerResult(fromResult, new OASISResult<T2>(), copyMessage);
        }

        public static OASISResult<T2> CopyResultAndCreateToResultObjectIfNull<T1, T2>(OASISResult<T1> fromResult, OASISResult<T2> toResult, bool copyMessage = true, bool copyInnerResult = true) where T1 : IHolon where T2 : IHolon, new()
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
                toResult.Result = Mapper.MapBaseHolonPropertiesAndCreateT2IfNull(fromResult.Result, toResult.Result);

            return toResult;
        }

        public static OASISResult<T2> CopyResultAndCreateToResultObjectIfNull<T1, T2>(OASISResult<T1> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T1 : IHolon where T2 : IHolon, new()
        {
            return CopyResultAndCreateToResultObjectIfNull(fromResult, new OASISResult<T2>(), copyMessage, copyInnerResult);
        }

        public static OASISResult<IEnumerable<T2>> CopyResultAndCreateToResultObjectIfNull<T1, T2>(OASISResult<IEnumerable<T1>> fromResult, OASISResult<IEnumerable<T2>> toResult, bool copyMessage = true, bool copyInnerResult = true) where T1 : IHolon where T2 : IHolon, new()
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
                toResult.Result = Mapper.MapBaseHolonPropertiesAndCreateT2IfNull(fromResult.Result, toResult.Result);

            return toResult;
        }

        public static OASISResult<IEnumerable<T2>> CopyResultAndCreateToResultObjectIfNull<T1, T2>(OASISResult<IEnumerable<T1>> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T1 : IHolon where T2 : IHolon, new()
        {
            return CopyResultAndCreateToResultObjectIfNull(fromResult, new OASISResult<IEnumerable<T2>>(), copyMessage, copyInnerResult);
        }



        public static OASISResult<T2> CopyResult<T1, T2>(OASISResult<T1> fromResult, OASISResult<T2> toResult, bool copyMessage = true, bool copyInnerResult = true) where T1 : IHolon where T2 : IHolon
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
                //toResult.Result = (T2)fromResult.Result;
                //toResult.Result = Mapper.Convert<T1, T2>(fromResult.Result);
                toResult.Result = (T2)Mapper.MapBaseHolonProperties(fromResult.Result, toResult.Result);

            return toResult;
        }

        //We can no longer use this method because there is no way to create an instance of the target result object. If this is needed then either create it externally and pass it in with the method below or use CopyResultAndCreateToResultObject.
        //public static OASISResult<T2> CopyResult<T1, T2>(OASISResult<T1> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T1 : IHolon where T2 : IHolon
        //{
        //    return CopyResult(fromResult, new OASISResult<T2>(), copyMessage, copyInnerResult);
        //}

        public static OASISResult<IEnumerable<T2>> CopyResult<T1, T2>(OASISResult<IEnumerable<T1>> fromResult, OASISResult<IEnumerable<T2>> toResult, bool copyMessage = true, bool copyInnerResult = true) where T1 : IHolon where T2 : IHolon
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
                toResult.Result = Mapper.Convert<T1, T2>(fromResult.Result);

            return toResult;
        }

        //We can no longer use this method because there is no way to create an instance of the target result object. If this is needed then either create it externally and pass it in with the method below or use CopyResultAndCreateToResultObject.
        //public static OASISResult<IEnumerable<T2>> CopyResult<T1, T2>(OASISResult<IEnumerable<T1>> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T1 : IHolon where T2 : IHolon
        //{
        //    return CopyResult(fromResult, new OASISResult<IEnumerable<T2>>(), copyMessage, copyInnerResult);
        //}



        public static OASISResult<IHolon> CopyResult<T>(OASISResult<T> fromResult, OASISResult<IHolon> toResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult)
                toResult.Result = (IHolon)fromResult.Result;

            return toResult;
        }

        public static OASISResult<IHolon> CopyResult<T>(OASISResult<T> fromResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            return CopyResult(fromResult, new OASISResult<IHolon>(), copyMessage, copyInnerResult);
        }

        public static OASISResult<IEnumerable<IHolon>> CopyResult<T>(OASISResult<IEnumerable<T>> fromResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            List<IHolon> holons = new List<IHolon>();

            foreach (T holon in fromResult.Result)
            {
                OASISResult<IHolon> holonResult = CopyResult(new OASISResult<T>(holon), new OASISResult<IHolon>(), copyMessage, copyInnerResult);

                if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    holons.Add(holonResult.Result);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISResultHelper.CopyResultsToIHolon. Reason: {holonResult.Message}");
            }

            result.Result = holons;
            return result;
        }




        //public static OASISResult<dynamic> CopyResult(OASISResult<dynamic> fromResult, OASISResult<dynamic> toResult, bool copyMessage = true, bool copyInnerResult = true)
        //{
        //    toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

        //    if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
        //        toResult.Result = Mapper.MapBaseHolonProperties(fromResult.Result);

        //    return toResult;
        //}

        //public static OASISResult<dynamic> CopyResult(OASISResult<dynamic> fromResult, bool copyMessage = true, bool copyInnerResult = true)
        //{
        //    return CopyResult(fromResult, new OASISResult<dynamic>(), copyMessage, copyInnerResult);
        //}

        //public static OASISResult<IEnumerable<dynamic>> CopyResult(OASISResult<IEnumerable<dynamic>> fromResult, OASISResult<IEnumerable<dynamic>> toResult, bool copyMessage = true, bool copyInnerResult = true)
        //{
        //    toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

        //    if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
        //        toResult.Result = Mapper.MapBaseHolonProperties(fromResult.Result);

        //    return toResult;
        //}

        //public static OASISResult<IEnumerable<dynamic>> CopyResult(OASISResult<IEnumerable<dynamic>> fromResult, bool copyMessage = true, bool copyInnerResult = true)
        //{
        //    return CopyResult(fromResult, new OASISResult<IEnumerable<dynamic>>(), copyMessage, copyInnerResult);
        //}


        //public static OASISResult<IHolon> CopyResult(OASISResult<Holon> fromResult, OASISResult<IHolon> toResult, bool copyMessage = true, bool copyInnerResult = true)
        //{
        //    toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

        //    //if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
        //    //    toResult.Result = Mapper.MapBaseHolonProperties(fromResult.Result);

        //    if (copyInnerResult)
        //        toResult.Result = fromResult.Result;

        //    return toResult;
        //}

        //public static OASISResult<IHolon> CopyResult(OASISResult<Holon> fromResult, bool copyMessage = true, bool copyInnerResult = true)
        //{
        //    return CopyResult(fromResult, new OASISResult<IHolon>(), copyMessage, copyInnerResult);
        //}

        //public static OASISResult<IEnumerable<IHolon>> CopyResult(OASISResult<IEnumerable<Holon>> fromResult, OASISResult<IEnumerable<IHolon>> toResult, bool copyMessage = true, bool copyInnerResult = true)
        //{
        //    toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

        //    if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
        //        toResult.Result = Mapper.MapBaseHolonProperties(fromResult.Result, toResult.Result);

        //    return toResult;
        //}

        //public static OASISResult<IEnumerable<IHolon>> CopyResult(OASISResult<IEnumerable<Holon>> fromResult, bool copyMessage = true, bool copyInnerResult = true)
        //{
        //    return CopyResult(fromResult, new OASISResult<IEnumerable<IHolon>>(), copyMessage, copyInnerResult);
        //}

        //public static OASISResult<ICelestialBody> CopyResultToICelestialBody(OASISResult<IHolon> fromResult, OASISResult<ICelestialBody> toResult, bool copyMessage = true, bool copyInnerResult = true)
        //{
        //    toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

        //    if (copyInnerResult)
        //        toResult.Result = Mapper.ConvertIHolonToICelestialBody(fromResult.Result);

        //    return toResult;
        //}



        public static OASISResult<IHolon> CopyResultToIHolon<T>(OASISResult<T> fromResult, OASISResult<IHolon> toResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult)
                toResult.Result = Mapper.ConvertIHolonToICelestialBody(fromResult.Result);

            return toResult;
        }

        public static OASISResult<IHolon> CopyResultToIHolon<T>(OASISResult<T> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            return CopyResultToIHolon(fromResult, new OASISResult<IHolon>(), copyMessage, copyInnerResult);
        }


        public static OASISResult<IEnumerable<IHolon>> CopyResultToIHolon<T>(OASISResult<IEnumerable<T>> fromResult, OASISResult<IEnumerable<IHolon>> toResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
                toResult.Result = Mapper.ConvertIHolonsToICelestialBodies(fromResult.Result);

            return toResult;
        }

        public static OASISResult<IEnumerable<IHolon>> CopyResultToIHolon<T>(OASISResult<IEnumerable<T>> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            return CopyResultToIHolon(fromResult, new OASISResult<IEnumerable<IHolon>>(), copyMessage, copyInnerResult);
        }


        public static OASISResult<ICelestialBody> CopyResultToICelestialBody(OASISResult<IHolon> fromResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            return CopyResultToICelestialBody(fromResult, new OASISResult<ICelestialBody>(), copyMessage, copyInnerResult);
        }

        public static OASISResult<IEnumerable<ICelestialBody>> CopyResultToICelestialBody(OASISResult<IEnumerable<IHolon>> fromResult, OASISResult<IEnumerable<ICelestialBody>> toResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
                toResult.Result = Mapper.ConvertIHolonsToICelestialBodies(fromResult.Result);

            return toResult;
        }

        public static OASISResult<IEnumerable<ICelestialBody>> CopyResultToICelestialBody(OASISResult<IEnumerable<IHolon>> fromResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            return CopyResultToICelestialBody(fromResult, new OASISResult<IEnumerable<ICelestialBody>>(), copyMessage, copyInnerResult);
        }

        public static OASISResult<ICelestialBody> CopyResultToICelestialBody<T>(OASISResult<T> fromResult, OASISResult<ICelestialBody> toResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult)
                toResult.Result = Mapper.ConvertIHolonToICelestialBody(fromResult.Result);

            return toResult;
        }

        public static OASISResult<ICelestialBody> CopyResultToICelestialBody<T>(OASISResult<T> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            return CopyResultToICelestialBody(fromResult, new OASISResult<ICelestialBody>(), copyMessage, copyInnerResult);
        }


        public static OASISResult<IEnumerable<ICelestialBody>> CopyResultToICelestialBody<T>(OASISResult<IEnumerable<T>> fromResult, OASISResult<IEnumerable<ICelestialBody>> toResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
                toResult.Result = Mapper.ConvertIHolonsToICelestialBodies(fromResult.Result);

            return toResult;
        }

        public static OASISResult<IEnumerable<ICelestialBody>> CopyResultToICelestialBody<T>(OASISResult<IEnumerable<T>> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            return CopyResultToICelestialBody(fromResult, new OASISResult<IEnumerable<ICelestialBody>>(), copyMessage, copyInnerResult);
        }

        public static OASISResult<ICelestialSpace> CopyResultToICelestialSpace<T>(OASISResult<T> fromResult, OASISResult<ICelestialSpace> toResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult)
                toResult.Result = Mapper.ConvertIHolonToICelestialSpace(fromResult.Result);

            return toResult;
        }

        public static OASISResult<ICelestialSpace> CopyResultToICelestialSpace<T>(OASISResult<T> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            return CopyResultToICelestialSpace(fromResult, new OASISResult<ICelestialSpace>(), copyMessage, copyInnerResult);
        }


        public static OASISResult<IEnumerable<ICelestialSpace>> CopyResultToICelestialSpace<T>(OASISResult<IEnumerable<T>> fromResult, OASISResult<IEnumerable<ICelestialSpace>> toResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
                toResult.Result = Mapper.ConvertIHolonsToICelestialSpaces(fromResult.Result);

            return toResult;
        }

        public static OASISResult<IEnumerable<ICelestialSpace>> CopyResultToICelestialSpace<T>(OASISResult<IEnumerable<T>> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            return CopyResultToICelestialSpace(fromResult, new OASISResult<IEnumerable<ICelestialSpace>>(), copyMessage, copyInnerResult);
        }

        public static OASISResult<IZome> CopyResultToIZome<T>(OASISResult<T> fromResult, OASISResult<IZome> toResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult)
                //toResult.Result = Mapper.MapBaseHolonProperties<IHolon, IZome>(fromResult.Result, toResult.Result, copyMessage);
                toResult.Result = (IZome)fromResult.Result;

            return toResult;
        }

        public static OASISResult<IZome> CopyResultToIZome<T>(OASISResult<T> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            return CopyResultToIZome(fromResult, new OASISResult<IZome>(), copyMessage, copyInnerResult);
        }

        public static OASISResult<IEnumerable<IZome>> CopyResultToIZome<T>(OASISResult<IEnumerable<T>> fromResult, OASISResult<IEnumerable<IZome>> toResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            List<IZome> zomes = new List<IZome>();
            toResult = CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult)
            {
                //Mapper<T, IZome>.MapBaseHolonProperties(fromResult.Result, zomes);

                foreach (T holon in fromResult.Result)
                    zomes.Add((IZome)holon);
                    //zomes.Add(Mapper.MapBaseHolonProperties(holon, new Zome());
            }

            toResult.Result = zomes;
            return toResult;
        }

        public static OASISResult<IEnumerable<IZome>> CopyResultToIZome<T>(OASISResult<IEnumerable<T>> fromResult, bool copyMessage = true, bool copyInnerResult = true) where T : IHolon
        {
            return CopyResultToIZome(fromResult, new OASISResult<IEnumerable<IZome>>(), copyMessage, copyInnerResult);
        }
    }

    //TODO: Check if this is still needed?
    public static class OASISResultHelper<T1, T2>
        where T1 : IHolon
        where T2 : IHolon, new()
    {
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            toResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult)
                toResult.Result = Mapper<T1, T2>.MapBaseHolonProperties(fromResult.Result, toResult.Result);

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            return CopyResult(fromResult, new OASISResult<T2>(), copyMessage, copyInnerResult);
        }

        public static OASISResult<IEnumerable<T2>> CopyResult(OASISResult<IEnumerable<T1>> fromResult, OASISResult<IEnumerable<T2>> toResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            toResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

            if (copyInnerResult)
                toResult.Result = Mapper<T1, T2>.MapBaseHolonProperties(fromResult.Result, toResult.Result);

            return toResult;
        }

        public static OASISResult<IEnumerable<T2>> CopyResult(OASISResult<IEnumerable<T1>> fromResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            return CopyResult(fromResult, new OASISResult<IEnumerable<T2>>(), copyMessage, copyInnerResult);
        }
    }

    //public static class OASISResultHelper<T1, T2>
    //    where T1 : IHolon
    //    where T2 : IHolon
    //{
    //    public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult, bool copyMessage = true, bool copyInnerResult = true)
    //    {
    //        toResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

    //        if (copyInnerResult)
    //            toResult.Result = Mapper.MapBaseHolonProperties(fromResult.Result, toResult.Result);

    //        return toResult;
    //    }

    //    public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, bool copyMessage = true, bool copyInnerResult = true)
    //    {
    //        return CopyResult(fromResult, new OASISResult<T2>(), copyMessage, copyInnerResult);
    //    }

    //    public static OASISResult<IEnumerable<T2>> CopyResult2(OASISResult<IEnumerable<T1>> fromResult, OASISResult<IEnumerable<T2>> toResult, bool copyMessage = true, bool copyInnerResult = true)
    //    {
    //        toResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

    //        if (copyInnerResult)
    //            toResult.Result = Mapper.MapBaseHolonProperties(fromResult.Result, toResult.Result);

    //        return toResult;
    //    }

    //    public static OASISResult<IEnumerable<T2>> CopyResult3(OASISResult<IEnumerable<T1>> fromResult, bool copyMessage = true, bool copyInnerResult = true)
    //    {
    //        return CopyResult2(fromResult, new OASISResult<IEnumerable<T2>>(), copyMessage, copyInnerResult);
    //    }
    //}


    //public static class OASISResultHelper<T1, T2>
    //{
    //    public static (OASISResult<T1>, T2) UnWrapOASISResult(ref OASISResult<T1> parentResult, OASISResult<T2> result, string errorMessage)
    //    {
    //        if (!result.IsError && result.Result != null)
    //            return (parentResult, result.Result);
    //        else
    //        {
    //            OASISErrorHandling.HandleError(ref parentResult, string.Format(errorMessage, result.Message));
    //            return (parentResult, default(T2));
    //        }
    //    }

    //    public static (OASISResult<T1>, T2) UnWrapOASISResultWithDefaultErrorMessage(ref OASISResult<T1> parentResult, OASISResult<T2> result, string methodName)
    //    {
    //        return UnWrapOASISResult(ref parentResult, result, $"Error occured in {methodName}. Reason:{0}");
    //    }
    //}

    //public static class OASISResultHelperForHolons<T1, T2>
    //    where T1 : IHolon
    //    where T2 : IHolon, new()
    //{
    //    //TODO: Eventually we need to switch all results that use IHolon (most) to use this version so it automatically copies/maps the inner result also (currently it is done seperatley so is more code).
    //    public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult, bool copyMessage = true, bool copyInnerResult = true)
    //    {
    //        toResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

    //        if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
    //            toResult.Result = Mapper<T1, T2>.MapBaseHolonProperties(fromResult.Result);

    //        return toResult;
    //    }

    //    public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, bool copyMessage = true, bool copyInnerResult = true)
    //    {
    //        return CopyResult(fromResult, new OASISResult<T2>(), copyMessage, copyInnerResult);
    //    }

    //    //public static OASISResult<IEnumerable<T2>> CopyResult(OASISResult<IEnumerable<T1>> fromResult, OASISResult<IEnumerable<T2>> toResult, bool copyMessage = true, bool copyInnerResult = true)
    //    public static OASISResult<IEnumerable<T2>> CopyResultForCollections(OASISResult<IEnumerable<T1>> fromResult, OASISResult<IEnumerable<T2>> toResult, bool copyMessage = true, bool copyInnerResult = true)
    //    {
    //        toResult = OASISResultHelper<T1, T2>.CopyOASISResultOnlyWithNoInnerResult(fromResult, toResult, copyMessage);

    //        if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
    //            toResult.Result = Mapper<T1, T2>.MapBaseHolonProperties(fromResult.Result);

    //        return toResult;
    //    }

    //    public static OASISResult<IEnumerable<T2>> CopyResultForCollections(OASISResult<IEnumerable<T1>> fromResult, bool copyMessage = true, bool copyInnerResult = true)
    //    {
    //        return CopyResultForCollections(fromResult, new OASISResult<IEnumerable<T2>>(), copyMessage, copyInnerResult);
    //    }
    //}

    //public static class OASISResultHelperForHolons
    //{

    //}


    /*
    public static class OASISResultHelperForHolonCollections<T1, T2>
        where T1 : IEnumerable<IHolon>
        where T2 : IEnumerable<IHolon>, new()
    {
        //TODO: Eventually we need to switch all results that use IHolon (most) to use this version so it automatically copies/maps the inner result also (currently it is done seperatley so is more code).
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsWarning = fromResult.IsWarning;

            //TODO: Implement for all other properties ASAP.
            if (copyMessage)
                toResult.Message = fromResult.Message;

            toResult.DetailedMessage = fromResult.DetailedMessage;
            toResult.WarningCount = fromResult.WarningCount;
            toResult.ErrorCount = fromResult.ErrorCount;
            toResult.HasAnyHolonsChanged = fromResult.HasAnyHolonsChanged;
            toResult.InnerMessages = fromResult.InnerMessages;
            toResult.LoadedCount = fromResult.LoadedCount;
            toResult.SavedCount = fromResult.SavedCount;
            toResult.MetaData = fromResult.MetaData;

            if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
                toResult.Result = (T2)Mapper.MapBaseHolonProperties(fromResult.Result);

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult, bool copyMessage = true, bool copyInnerResult = true)
        {
            return CopyResult(fromResult, ref toResult, copyMessage, copyInnerResult);
        }
    }*/

    //public static class OASISResultHelperForHolonCollections2<T1, T2, T3, T4>
    //    where T1 : IEnumerable<T3>
    //    where T2 : IEnumerable<T4>, new()
    //{
    //    //TODO: Eventually we need to switch all results that use IHolon (most) to use this version so it automatically copies/maps the inner result also (currently it is done seperatley so is more code).
    //    public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult, bool copyMessage = true, bool copyInnerResult = true)
    //    {
    //        toResult.Exception = fromResult.Exception;
    //        toResult.IsError = fromResult.IsError;
    //        toResult.IsSaved = fromResult.IsSaved;
    //        toResult.IsWarning = fromResult.IsWarning;

    //        //TODO: Implement for all other properties ASAP.
    //        if (copyMessage)
    //            toResult.Message = fromResult.Message;

    //        toResult.DetailedMessage = fromResult.DetailedMessage;
    //        toResult.WarningCount = fromResult.WarningCount;
    //        toResult.ErrorCount = fromResult.ErrorCount;
    //        toResult.HasAnyHolonsChanged = fromResult.HasAnyHolonsChanged;
    //        toResult.InnerMessages = fromResult.InnerMessages;
    //        toResult.LoadedCount = fromResult.LoadedCount;
    //        toResult.SavedCount = fromResult.SavedCount;
    //        toResult.MetaData = fromResult.MetaData;

    //        if (copyInnerResult && !fromResult.IsError && fromResult.Result != null)
    //            toResult.Result = (T2)Mapper.MapBaseHolonProperties(fromResult.Result);

    //        return toResult;
    //    }

    //    public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult, bool copyMessage = true, bool copyInnerResult = true)
    //    {
    //        return CopyResult(fromResult, ref toResult, copyMessage, copyInnerResult);
    //    }
    //}

    //public static class OASISResultHelper<T>
    //{
    //    public static OASISResult<T> CopyResult(OASISResult<T> fromResult, ref OASISResult<T> toResult)
    //    {
    //        toResult.Exception = fromResult.Exception;
    //        toResult.IsError = fromResult.IsError;
    //        toResult.IsSaved = fromResult.IsSaved;
    //        toResult.IsWarning = fromResult.IsWarning;
    //        toResult.Message = fromResult.Message;
    //        toResult.MetaData = fromResult.MetaData;

    //        return toResult;
    //    }

    //    public static OASISResult<T> CopyResult(OASISResult<T> fromResult, OASISResult<T> toResult)
    //    {
    //        return CopyResult(fromResult, ref toResult);
    //    }
    //}

    //TODO: REMOVE ASAP!
    /*
    public static class OASISResultHelper<T1, T2> 
        //where T1 : IHolonBase //TODO: Ideally would like this code back in but this way it is more generic so can be used anywhere...
        //where T2 : IHolonBase //, new()
    {
        [ObsoleteAttribute("This is obsolete, please use OASISResultHelper.CopyResult instead.")]
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult, bool copyMessage = true)
        {
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsWarning = fromResult.IsWarning;
            
            //TODO: Implement for all other properties ASAP.
            if (copyMessage)
                toResult.Message = fromResult.Message;

            toResult.DetailedMessage = fromResult.DetailedMessage;
            toResult.WarningCount = fromResult.WarningCount;
            toResult.ErrorCount = fromResult.ErrorCount;
            toResult.HasAnyHolonsChanged = fromResult.HasAnyHolonsChanged;
            toResult.InnerMessages = fromResult.InnerMessages;
            toResult.LoadedCount = fromResult.LoadedCount;
            toResult.SavedCount = fromResult.SavedCount;
            toResult.MetaData = fromResult.MetaData;
           // toResult.Result = fromResult.Result;

            return toResult;
        }

        [ObsoleteAttribute("This is obsolete, please use OASISResultHelper.CopyResult instead.")]
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult, bool copyMessage = true)
        {
            return CopyResult(fromResult, ref toResult, copyMessage);
        }
    }

    //TODO: REMOVE ASAP!
    public static class OASISResultHelper<T1, T2>
       //where T1 : IEnumerable<IHolonBase>
       //where T2 : IEnumerable<IHolonBase> 
    {
        [ObsoleteAttribute("This is obsolete, please use OASISResultHelper.CopyResult instead.")]
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult, bool copyMessage = true)
        {
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsWarning = fromResult.IsWarning;

            //TODO: Implement for all other properties ASAP.
            if (copyMessage)
                toResult.Message = fromResult.Message;

            toResult.DetailedMessage = fromResult.DetailedMessage;
            toResult.WarningCount = fromResult.WarningCount;
            toResult.ErrorCount = fromResult.ErrorCount;
            toResult.HasAnyHolonsChanged = fromResult.HasAnyHolonsChanged;
            toResult.InnerMessages = fromResult.InnerMessages;
            toResult.LoadedCount = fromResult.LoadedCount;
            toResult.SavedCount = fromResult.SavedCount;
            toResult.MetaData = fromResult.MetaData;

            return toResult;
        }

        [ObsoleteAttribute("This is obsolete, please use OASISResultHelper.CopyResult instead.")]
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult)
        {
            return CopyResult(fromResult, ref toResult);
        }
    }*/

    /*
    public static class OASISResultCollectionToHolonHelper<T1, T2>
       where T1 : IEnumerable<IHolonBase>
       where T2 : IHolonBase
    {
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult, bool copyMessage = true)
        {
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsWarning = fromResult.IsWarning;

            //TODO: Implement for all other properties ASAP.
            if (copyMessage)
                toResult.Message = fromResult.Message;

            toResult.DetailedMessage = fromResult.DetailedMessage;
            toResult.WarningCount = fromResult.WarningCount;
            toResult.ErrorCount = fromResult.ErrorCount;
            toResult.HasAnyHolonsChanged = fromResult.HasAnyHolonsChanged;
            toResult.InnerMessages = fromResult.InnerMessages;
            toResult.LoadedCount = fromResult.LoadedCount;
            toResult.SavedCount = fromResult.SavedCount;
            toResult.MetaData = fromResult.MetaData;

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult)
        {
            return CopyResult(fromResult, ref toResult);
        }
    }

    public static class OASISResultHolonToCollectionHelper<T1, T2>
       where T1 : IHolonBase
       where T2 : IEnumerable<IHolonBase> 
    {
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult, bool copyMessage = true)
        {
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsWarning = fromResult.IsWarning;

            //TODO: Implement for all other properties ASAP.
            if (copyMessage)
                toResult.Message = fromResult.Message;

            toResult.DetailedMessage = fromResult.DetailedMessage;
            toResult.WarningCount = fromResult.WarningCount;
            toResult.ErrorCount = fromResult.ErrorCount;
            toResult.HasAnyHolonsChanged = fromResult.HasAnyHolonsChanged;
            toResult.InnerMessages = fromResult.InnerMessages;
            toResult.LoadedCount = fromResult.LoadedCount;
            toResult.SavedCount = fromResult.SavedCount;
            toResult.MetaData = fromResult.MetaData;

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult)
        {
            return CopyResult(fromResult, ref toResult);
        }
    }*/
}