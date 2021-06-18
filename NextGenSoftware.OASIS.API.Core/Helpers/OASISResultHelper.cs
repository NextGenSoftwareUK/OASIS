using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class OASISResultHolonToHolonHelper<T1, T2> 
        where T1 : IHolon
        where T2 : IHolon //, new()
    {
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            //toResult.Result = Mapper<T1, T2>.MapBaseHolonProperties(fromResult.Result);
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult)
        {
            return CopyResult(fromResult, ref toResult);
        }
    }

    public static class OASISResultCollectionToCollectionHelper<T1, T2>
       where T1 : IEnumerable<IHolon>
       where T2 : IEnumerable<IHolon> //, new()
    {
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            //toResult.Result = MapperForCollections<T1, T2>.MapBaseHolonProperties(fromResult.Result); //TODO: Be ideal to get this working! :)


            /*
            for (int i = 0; i < fromResult.Result.Count(); i++)
            {
               // toResult.Result.Add(new T2())
               //TODO: Will only work if toResult has an existing collection of same size.

                toResult.Result[i].Id = fromResult.Result[i].Id;
                toResult.Result[i].ProviderKey = fromResult.Result[i].ProviderKey;
                toResult.Result[i].Name = fromResult.Result[i].Name;
                toResult.Result[i].Description = fromResult.Result[i].Description;
                toResult.Result[i].ParentGreatGrandSuperStar = fromResult.Result[i].ParentGreatGrandSuperStar;
                toResult.Result[i].ParentGreatGrandSuperStarId = fromResult.Result[i].ParentGreatGrandSuperStarId;
                toResult.Result[i].ParentGrandSuperStar = fromResult.Result[i].ParentGrandSuperStar;
                toResult.Result[i].ParentGrandSuperStarId = fromResult.Result[i].ParentGrandSuperStarId;
                toResult.Result[i].ParentSuperStar = fromResult.Result[i].ParentSuperStar;
                toResult.Result[i].ParentSuperStarId = fromResult.Result[i].ParentSuperStarId;
                toResult.Result[i].ParentStar = fromResult.Result[i].ParentStar;
                toResult.Result[i].ParentStarId = fromResult.Result[i].ParentStarId;
                toResult.Result[i].ParentPlanet = fromResult.Result[i].ParentPlanet;
                toResult.Result[i].ParentPlanetId = fromResult.Result[i].ParentPlanetId;
                toResult.Result[i].ParentMoon = fromResult.Result[i].ParentMoon;
                toResult.Result[i].ParentMoonId = fromResult.Result[i].ParentMoonId;
                toResult.Result[i].ParentZome = fromResult.Result[i].ParentZome;
                toResult.Result[i].ParentZomeId = fromResult.Result[i].ParentZomeId;
                toResult.Result[i].ParentHolon = fromResult.Result[i].ParentHolon;
                toResult.Result[i].ParentHolonId = fromResult.Result[i].ParentHolonId;
                targetHolons[i].ParentOmiverse = fromResult.Result[i].ParentOmiverse;
                targetHolons[i].ParentOmiverseId = fromResult.Result[i].ParentOmiverseId;
                targetHolons[i].ParentUniverse = fromResult.Result[i].ParentUniverse;
                targetHolons[i].ParentUniverseId = fromResult.Result[i].ParentUniverseId;
                targetHolons[i].ParentGalaxy = fromResult.Result[i].ParentGalaxy;
                targetHolons[i].ParentGalaxyId = fromResult.Result[i].ParentGalaxyId;
                targetHolons[i].ParentSolarSystem = fromResult.Result[i].ParentSolarSystem;
                targetHolons[i].ParentSolarSystemId = fromResult.Result[i].ParentSolarSystemId;
                targetHolons[i].Children = fromResult.Result[i].Children;
                targetHolons[i].Nodes = fromResult.Result[i].Nodes;
                //targetHolon.CelestialBodyCore.Id = sourceHolon.Id; //TODO: Dont think need targetHolon now?
                //targetHolon.CelestialBodyCore.ProviderKey = sourceHolon.ProviderKey; //TODO: Dont think need targetHolon now?
                targetHolons[i].CreatedByAvatar = fromResult.Result[i].CreatedByAvatar;
                targetHolons[i].CreatedByAvatarId = fromResult.Result[i].CreatedByAvatarId;
                targetHolons[i].CreatedDate = fromResult.Result[i].CreatedDate;
                targetHolons[i].ModifiedByAvatar = fromResult.Result[i].ModifiedByAvatar;
                targetHolons[i].ModifiedByAvatarId = fromResult.Result[i].ModifiedByAvatarId;
                targetHolons[i].ModifiedDate = fromResult.Result[i].ModifiedDate;
                targetHolons[i].DeletedByAvatar = fromResult.Result[i].DeletedByAvatar;
                targetHolons[i].DeletedByAvatarId = fromResult.Result[i].DeletedByAvatarId;
                targetHolons[i].DeletedDate = fromResult.Result[i].DeletedDate;
                targetHolons[i].Version = fromResult.Result[i].Version;
                targetHolons[i].IsActive = fromResult.Result[i].IsActive;
                targetHolons[i].IsChanged = fromResult.Result[i].IsChanged;
                targetHolons[i].IsNewHolon = fromResult.Result[i].IsNewHolon;
                targetHolons[i].MetaData = fromResult.Result[i].MetaData;
                targetHolons[i].ProviderMetaData = fromResult.Result[i].ProviderMetaData;
                targetHolons[i].Original = fromResult.Result[i].Original;
            }
            */

            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult)
        {
            return CopyResult(fromResult, ref toResult);
        }
    }

    public static class OASISResultCollectionToHolonHelper<T1, T2>
       where T1 : IEnumerable<IHolon>
       where T2 : IHolon //, new()
    {
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            //toResult.Result = Mapper<T1, T2>.MapBaseHolonProperties(fromResult.Result); //TODO: Be ideal to get this working! :)
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult)
        {
            return CopyResult(fromResult, ref toResult);
        }
    }

    public static class OASISResultHolonToCollectionHelper<T1, T2>
       where T1 : IHolon
       where T2 : IEnumerable<IHolon> //, new()
    {
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            //toResult.Result = Mapper<T1, T2>.MapBaseHolonProperties(fromResult.Result); //TODO: Be ideal to get this working! :)
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult)
        {
            return CopyResult(fromResult, ref toResult);
        }
    }
}