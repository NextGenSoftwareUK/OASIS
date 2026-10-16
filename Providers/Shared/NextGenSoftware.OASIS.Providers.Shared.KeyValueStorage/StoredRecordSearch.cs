using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage
{
    public static class StoredRecordSearch
    {
        public static void Validate(ISearchParams searchParams,int version,int maxChildDepth)
            => Validate(searchParams,version,maxChildDepth,supportsHistory:false);

        public static void Validate(ISearchParams searchParams,int version,int maxChildDepth,bool supportsHistory)
        {
                ArgumentNullException.ThrowIfNull(searchParams);
                if(searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId == Guid.Empty)
                    throw new ArgumentException("Current-avatar search requires AvatarId.");
                if(searchParams.MetaKeyValuePairMatchMode != MetaKeyValuePairMatchMode.All && searchParams.MetaKeyValuePairMatchMode != MetaKeyValuePairMatchMode.Any)
                    throw new ArgumentOutOfRangeException(nameof(searchParams.MetaKeyValuePairMatchMode));
                foreach(var group in searchParams.SearchGroups ?? new List<ISearchGroupBase>())
                {
                    if(group == null || group is not ISearchTextGroup && group is not ISearchNumberGroup && group is not ISearchDateGroup)
                        throw new NotSupportedException("Unknown Stored-record search group type.");
                    if(!Enum.IsDefined(typeof(SearchParamGroupOperator),group.PreviousSearchGroupOperator))
                        throw new ArgumentException("Unknown search group operator.");
                    if(group is ISearchTextGroup text && string.IsNullOrWhiteSpace(text.SearchQuery))
                        throw new ArgumentException("Text search query must be nonempty.");
                    if(group is ISearchDateGroup date && !Enum.IsDefined(typeof(SearchOperatorType),date.DateOperator)
                        || group is ISearchNumberGroup number && !Enum.IsDefined(typeof(SearchOperatorType),number.NumberOperator))
                        throw new ArgumentException("Unknown search comparison operator.");
                    if(group.AvatarSearchParams?.SearchAvatarWalletParams != null)
                        throw new NotSupportedException("Stored-record wallet-field search is not implemented.");
                }
                if(version != 0 && !supportsHistory) throw new NotSupportedException("Stored-record search history is not implemented; only current version 0 is supported.");
                if(supportsHistory && version<0) throw new ArgumentOutOfRangeException(nameof(version));
                if(maxChildDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
        }

        public static bool Matches(IHolonBase record,bool avatar,ISearchParams search,IEnumerable<IHolon> holons)
        {
            if(search.SearchOnlyForCurrentAvatar && record.CreatedByAvatarId != search.AvatarId) return false;
            if(search.ParentId != Guid.Empty)
            {
                if(record is not ISemanticHolon semantic) return false;
                var parent=semantic.ParentHolonId;
                var visited=new HashSet<Guid>();
                var parents=holons.ToDictionary(row=>row.Id,row=>row.ParentHolonId);
                while(parent != search.ParentId && parent != Guid.Empty && search.Recursive)
                {
                    if(!visited.Add(parent)) throw new InvalidOperationException("Stored-record search parent graph contains a cycle.");
                    parent=parents.TryGetValue(parent,out var next) ? next : Guid.Empty;
                }
                if(parent != search.ParentId) return false;
            }
            if(search.FilterByMetaData?.Count > 0)
            {
                bool Has(KeyValuePair<string,string> pair) => record.MetaData != null && record.MetaData.TryGetValue(pair.Key,out var value)
                    && value != null && string.Equals(Convert.ToString(value,CultureInfo.InvariantCulture),pair.Value,StringComparison.Ordinal);
                if(search.MetaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All ? !search.FilterByMetaData.All(Has) : !search.FilterByMetaData.Any(Has)) return false;
            }
            bool? combined=null;
            foreach(var group in search.SearchGroups ?? new List<ISearchGroupBase>())
            {
                bool targeted=avatar ? group.SearchAvatars || !group.SearchHolons : group.SearchHolons || !group.SearchAvatars;
                bool match=targeted && (group.HolonType == HolonType.All || group.HolonType == record.HolonType);
                if(match)
                {
                    object selection=avatar ? group.AvatarSearchParams : group.HolonSearchParams;
                    var json=JObject.FromObject(record);
                    RemoveSearchCredentials(json);
                    var flags=selection?.GetType().GetProperties().Where(p=>p.PropertyType == typeof(bool) && (bool)p.GetValue(selection)).Select(p=>p.Name).ToList();
                    if(flags?.Contains("SearchAllFields") == true)
                        flags=json.Properties().Select(p=>p.Name).ToList();
                    if(flags == null || flags.Count == 0)
                        flags=group is ISearchDateGroup ? new List<string> {"CreatedDate","ModifiedDate"}
                            : group is ISearchNumberGroup ? new List<string> {"Version"}
                            : avatar ? new List<string> {"Username","Email","FirstName","LastName"} : new List<string> {"Name","Description"};
                    // Credentials are never searchable even under an all-fields request.
                    flags.RemoveAll(name=>name is "Password" or "JwtToken" or "RefreshToken" or "ResetToken" or "VerificationToken");
                    flags=flags.Select(name=>name == "VerifiedDate" ? "Verified" : name == "AvatarId" ? "Id" : name).ToList();
                    if(!avatar && group.HolonSearchParams?.MetaDataKey is string metadataKey && !string.IsNullOrEmpty(metadataKey))
                        json["MetaData"]=json["MetaData"]?[metadataKey];
                    if(group is ISearchTextGroup text)
                    {
                        if(text.SearchProviderKeys) flags.Add("ProviderUniqueStorageKey");
                        match=flags.SelectMany(name=>SearchValues(json[name])).Any(value=>value.ToString().Contains(text.SearchQuery,StringComparison.OrdinalIgnoreCase));
                        if(text.SearchIds)
                            match |= MatchesTextIds(json,text.SearchQuery);
                    }
                    else if(group is ISearchNumberGroup number)
                        match=flags.SelectMany(name=>SearchValues(json[name])).Any(token=>decimal.TryParse(token.ToString(),NumberStyles.Number,CultureInfo.InvariantCulture,out var value) && CompareSearch(value.CompareTo(number.Number),number.NumberOperator));
                    else if(group is ISearchDateGroup date)
                        match=flags.SelectMany(name=>SearchValues(json[name])).Any(token=>token.Type == JTokenType.Date
                            && CompareSearch(token.Value<DateTime>().CompareTo(date.Date),date.DateOperator));
                    else throw new NotSupportedException("Unknown Stored-record search group type.");
                }
                combined=combined == null ? match : group.PreviousSearchGroupOperator == SearchParamGroupOperator.And ? combined.Value && match : combined.Value || match;
            }
            return combined ?? true;
        }

        public static bool MatchesTextIds(IHolonBase record,string query)
            => MatchesTextIdentifiers(record,query,searchIds:true,searchProviderKeys:false);

        public static bool MatchesTextFields(IHolonBase record,bool avatar,ISearchTextGroup group)
            => MatchesSearchFields(record,avatar,group);

        public static bool MatchesSearchFields(IHolonBase record,bool avatar,ISearchGroupBase group)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(group);
            var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase> {group}};
            Validate(query,0,0);
            return Matches(record,avatar,query,Enumerable.Empty<IHolon>());
        }

        public static bool MatchesTextIdentifiers(IHolonBase record,string query,bool searchIds,bool searchProviderKeys)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentException.ThrowIfNullOrWhiteSpace(query);
            var json=JObject.FromObject(record);
            RemoveSearchCredentials(json);
            return searchIds && MatchesTextIds(json,query)
                || searchProviderKeys && SearchValues(json["ProviderUniqueStorageKey"]).Any(value=>value.ToString().Contains(query,StringComparison.OrdinalIgnoreCase));
        }

        private static bool MatchesTextIds(JObject json,string query)
            => json.Descendants().OfType<JValue>().Any(value=>Guid.TryParse(value.ToString(),out _)
                && value.ToString().Contains(query,StringComparison.OrdinalIgnoreCase));

        private static IEnumerable<JValue> SearchValues(JToken token)
            => token is JValue value ? new[] {value} : token is JContainer container ? container.Descendants().OfType<JValue>() : Enumerable.Empty<JValue>();

        private static void RemoveSearchCredentials(JToken token)
        {
            if(token is JObject record)
                foreach(var property in record.Properties().ToList())
                {
                    if(new[] {"Password","JwtToken","RefreshToken","RefreshTokens","ResetToken","VerificationToken",
                        "PrivateKey","PrivateKeys","ProviderPrivateKeys","SecretKey","SigningKey","Mnemonic","SeedPhrase"}
                        .Contains(property.Name,StringComparer.OrdinalIgnoreCase)) property.Remove();
                    else RemoveSearchCredentials(property.Value);
                }
            else if(token is JArray array)
                foreach(var child in array) RemoveSearchCredentials(child);
        }

        private static bool CompareSearch(int comparison,SearchOperatorType operation) => operation switch
        {
            SearchOperatorType.LessThan=>comparison<0, SearchOperatorType.LessThanOrEqualTo=>comparison<=0,
            SearchOperatorType.EqualTo=>comparison==0, SearchOperatorType.NotEqualTo=>comparison!=0,
            SearchOperatorType.GreaterThan=>comparison>0, SearchOperatorType.GreaterThanOrEqualTo=>comparison>=0,
            _=>throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }
}
