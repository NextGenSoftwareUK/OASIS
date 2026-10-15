using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private static bool SearchRecordMatches(IHolonBase record,bool avatar,ISearchParams search,IEnumerable<IHolon> holons)
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
                    if(!visited.Add(parent)) throw new InvalidOperationException("Ethereum search parent graph contains a cycle.");
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
                    flags=flags.Select(name=>name == "VerifiedDate" ? "Verified" : name).ToList();
                    if(group.HolonSearchParams?.MetaDataKey is string metadataKey && !string.IsNullOrEmpty(metadataKey))
                        json["MetaData"]=json["MetaData"]?[metadataKey];
                    if(group is ISearchTextGroup text)
                    {
                        if(text.SearchProviderKeys) flags.Add("ProviderUniqueStorageKey");
                        match=flags.SelectMany(name=>SearchValues(json[name])).Any(value=>value.ToString().Contains(text.SearchQuery,StringComparison.OrdinalIgnoreCase));
                        if(text.SearchIds)
                            match |= json.Descendants().OfType<JValue>().Any(value=>Guid.TryParse(value.ToString(),out _)
                                && value.ToString().Contains(text.SearchQuery,StringComparison.OrdinalIgnoreCase));
                    }
                    else if(group is ISearchNumberGroup number)
                        match=flags.SelectMany(name=>SearchValues(json[name])).Any(token=>decimal.TryParse(token.ToString(),NumberStyles.Number,CultureInfo.InvariantCulture,out var value) && CompareSearch(value.CompareTo(number.Number),number.NumberOperator));
                    else if(group is ISearchDateGroup date)
                        match=flags.SelectMany(name=>SearchValues(json[name])).Any(token=>token.Type == JTokenType.Date
                            && CompareSearch(token.Value<DateTime>().CompareTo(date.Date),date.DateOperator));
                    else throw new NotSupportedException("Unknown Ethereum search group type.");
                }
                combined=combined == null ? match : group.PreviousSearchGroupOperator == SearchParamGroupOperator.And ? combined.Value && match : combined.Value || match;
            }
            return combined ?? true;
        }

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
