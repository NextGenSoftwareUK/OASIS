using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search.Avatar;
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
                    var wallet=group.AvatarSearchParams?.SearchAvatarWalletParams;
                    if(wallet!=null && !SelectedFields(wallet,typeof(ISearchAvatarWalletParams)).Any()
                        && !SelectedFields(wallet.SearchAvatarWalletTransactionParams,typeof(ISearchAvatarWalletTransactionParams)).Any())
                        throw new ArgumentException("An explicit wallet search mask requires at least one selected wallet or transaction field.");
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
                var parents=new Dictionary<Guid,Guid>();
                foreach(var row in holons)
                    if(row==null || row.Id==Guid.Empty || !parents.TryAdd(row.Id,row.ParentHolonId))
                        throw new System.IO.InvalidDataException("Stored-record search parent graph contains an invalid or duplicate identity.");
                while(parent != search.ParentId && parent != Guid.Empty && search.Recursive)
                {
                    if(!visited.Add(parent)) throw new System.IO.InvalidDataException("Stored-record search parent graph contains a cycle.");
                    parent=parents.TryGetValue(parent,out var next) ? next : Guid.Empty;
                }
                if(parent != search.ParentId) return false;
            }
            if(!MatchesMetadata(record.MetaData,search.FilterByMetaData,search.MetaKeyValuePairMatchMode)) return false;
            bool? combined=null;
            foreach(var group in search.SearchGroups ?? new List<ISearchGroupBase>())
            {
                bool targeted=avatar ? group.SearchAvatars || !group.SearchHolons : group.SearchHolons || !group.SearchAvatars;
                bool match=targeted && (group.HolonType == HolonType.All || group.HolonType == record.HolonType);
                if(match)
                {
                    object selection=avatar ? group.AvatarSearchParams : group.HolonSearchParams;
                    var walletSelection=avatar ? group.AvatarSearchParams?.SearchAvatarWalletParams : null;
                    var json=SearchDocument(record);
                    RemoveSearchCredentials(json);
                    var flags=selection?.GetType().GetProperties().Where(p=>p.PropertyType == typeof(bool) && (bool)p.GetValue(selection)).Select(p=>p.Name).ToList();
                    if(flags?.Contains("SearchAllFields") == true)
                        flags=json.Properties().Select(p=>p.Name).ToList();
                    if((flags == null || flags.Count == 0) && walletSelection==null)
                        flags=group is ISearchDateGroup ? new List<string> {"CreatedDate","ModifiedDate"}
                            : group is ISearchNumberGroup ? new List<string> {"Version"}
                            : avatar ? new List<string> {"Username","Email","FirstName","LastName"} : new List<string> {"Name","Description"};
                    flags ??=new List<string>();
                    // Credentials are never searchable even under an all-fields request.
                    flags.RemoveAll(name=>name is "Password" or "JwtToken" or "RefreshToken" or "ResetToken" or "VerificationToken");
                    flags=flags.Select(name=>name == "VerifiedDate" ? "Verified" : name == "AvatarId" ? "Id" : name).ToList();
                    if(!avatar && group.HolonSearchParams?.MetaDataKey is string metadataKey && !string.IsNullOrEmpty(metadataKey))
                        json["MetaData"]=json["MetaData"]?[metadataKey];
                    if(group is ISearchTextGroup text)
                    {
                        if(text.SearchProviderKeys) flags.Add("ProviderUniqueStorageKey");
                        match=flags.SelectMany(name=>SearchValues(json[name])).Concat(WalletValues(json,walletSelection))
                            .Any(value=>value.ToString().Contains(text.SearchQuery,StringComparison.OrdinalIgnoreCase));
                        if(text.SearchIds)
                            match |= MatchesTextIds(json,text.SearchQuery);
                    }
                    else if(group is ISearchNumberGroup number)
                        match=flags.SelectMany(name=>SearchValues(json[name])).Concat(WalletValues(json,walletSelection))
                            .Any(token=>decimal.TryParse(token.ToString(),NumberStyles.Number,CultureInfo.InvariantCulture,out var value) && CompareSearch(value.CompareTo(number.Number),number.NumberOperator));
                    else if(group is ISearchDateGroup date)
                        match=flags.SelectMany(name=>SearchValues(json[name])).Concat(WalletValues(json,walletSelection)).Any(token=>token.Type == JTokenType.Date
                            && CompareSearch(token.Value<DateTime>().CompareTo(date.Date),date.DateOperator));
                    else throw new NotSupportedException("Unknown Stored-record search group type.");
                }
                combined=combined == null ? match : group.PreviousSearchGroupOperator == SearchParamGroupOperator.And ? combined.Value && match : combined.Value || match;
            }
            return combined ?? true;
        }

        public static bool MatchesTextIds(IHolonBase record,string query)
            => MatchesTextIdentifiers(record,query,searchIds:true,searchProviderKeys:false);

        public static string MetadataValueText(object value)
            => value==null ? null:Convert.ToString(value,CultureInfo.InvariantCulture);

        public static bool MatchesMetadata(IDictionary<string,object> metadata,IDictionary<string,string> filter,MetaKeyValuePairMatchMode mode)
        {
            if(mode!=MetaKeyValuePairMatchMode.All && mode!=MetaKeyValuePairMatchMode.Any) throw new ArgumentOutOfRangeException(nameof(mode));
            if(filter==null || filter.Count==0) return true;
            bool Has(KeyValuePair<string,string> pair)=>metadata!=null && metadata.TryGetValue(pair.Key,out var value)
                && value!=null && string.Equals(MetadataValueText(value),pair.Value,StringComparison.Ordinal);
            return mode==MetaKeyValuePairMatchMode.All ? filter.All(Has):filter.Any(Has);
        }

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

        // Field-only adapter: the caller owns target/type/group combination and scope.
        // Bind the single-group envelope to this record without changing its searchable values.
        public static bool MatchesSelectedFields(IHolonBase record,bool avatar,ISearchGroupBase group)
        {
            ArgumentNullException.ThrowIfNull(record);ArgumentNullException.ThrowIfNull(group);
            ISearchGroupBase selection=group switch {
                ISearchTextGroup text=>new SearchTextGroup {SearchQuery=text.SearchQuery,SearchIds=text.SearchIds,SearchProviderKeys=text.SearchProviderKeys},
                ISearchNumberGroup number=>new SearchNumberGroup {Number=number.Number,NumberOperator=number.NumberOperator},
                ISearchDateGroup date=>new SearchDateGroup {Date=date.Date,DateOperator=date.DateOperator},
                _=>throw new NotSupportedException("Unknown stored-record search group type.")};
            selection.HolonType=record.HolonType;selection.SearchAvatars=avatar;selection.SearchHolons=!avatar;
            selection.HolonSearchParams=group.HolonSearchParams;selection.AvatarSearchParams=group.AvatarSearchParams;
            return MatchesSearchFields(record,avatar,selection);
        }

        public static bool MatchesTextIdentifiers(IHolonBase record,string query,bool searchIds,bool searchProviderKeys)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentException.ThrowIfNullOrWhiteSpace(query);
            var json=SearchDocument(record);
            RemoveSearchCredentials(json);
            return searchIds && MatchesTextIds(json,query)
                || searchProviderKeys && SearchValues(json["ProviderUniqueStorageKey"]).Any(value=>value.ToString().Contains(query,StringComparison.OrdinalIgnoreCase));
        }

        private static bool MatchesTextIds(JObject json,string query)
            => json.Descendants().OfType<JValue>().Any(value=>Guid.TryParse(value.ToString(),out _)
                && value.ToString().Contains(query,StringComparison.OrdinalIgnoreCase));

        private static JObject SearchDocument(IHolonBase record)
            => JObject.FromObject(record,Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings {
                Converters={new SearchJsonElementConverter()}}));

        // Project JSON-backed metadata as JSON values, not JsonElement.ValueKind descriptors.
        // Do not infer DateTime from strings: only actual typed CLR dates are date candidates.
        private sealed class SearchJsonElementConverter : Newtonsoft.Json.JsonConverter
        {
            public override bool CanConvert(Type type)=>type==typeof(System.Text.Json.JsonElement);
            public override bool CanRead=>false;
            public override void WriteJson(Newtonsoft.Json.JsonWriter writer,object value,Newtonsoft.Json.JsonSerializer serializer)
            {
                using var reader=new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(((System.Text.Json.JsonElement)value).GetRawText())) {
                    DateParseHandling=Newtonsoft.Json.DateParseHandling.None};
                writer.WriteToken(reader);
            }
            public override object ReadJson(Newtonsoft.Json.JsonReader reader,Type type,object existing,Newtonsoft.Json.JsonSerializer serializer)
                =>throw new NotSupportedException("Search JSON projection is write-only.");
        }

        private static IEnumerable<JValue> SearchValues(JToken token)
            => token is JValue value ? new[] {value} : token is JContainer container ? container.Descendants().OfType<JValue>() : Enumerable.Empty<JValue>();

        // Select only fields declared by the public mask interfaces, never arbitrary runtime properties.
        private static IEnumerable<string> SelectedFields(object selection,Type contract)
            => selection==null ? Enumerable.Empty<string>() : contract.GetProperties()
                .Where(property=>property.PropertyType==typeof(bool) && (bool)property.GetValue(selection)).Select(property=>property.Name);

        private static IEnumerable<JValue> WalletValues(JObject document,ISearchAvatarWalletParams selection)
        {
            if(selection==null || document["ProviderWallets"] is not JObject providers)yield break;
            var fields=SelectedFields(selection,typeof(ISearchAvatarWalletParams)).ToArray();
            var transactionFields=SelectedFields(selection.SearchAvatarWalletTransactionParams,typeof(ISearchAvatarWalletTransactionParams)).ToArray();
            foreach(var provider in providers.Properties())
                if(provider.Value is JArray wallets)
                    foreach(var wallet in wallets.OfType<JObject>()){
                        foreach(var field in fields)foreach(var value in SearchValues(wallet[field]))yield return value;
                        if(wallet["Transactions"] is JArray transactions)
                            foreach(var transaction in transactions.OfType<JObject>())
                                foreach(var field in transactionFields)foreach(var value in SearchValues(transaction[field]))yield return value;
                    }
        }

        private static void RemoveSearchCredentials(JToken token)
        {
            if(token is JObject record)
                foreach(var property in record.Properties().ToList())
                {
                    if(new[] {"Password","JwtToken","RefreshToken","RefreshTokens","ResetToken","VerificationToken",
                        "PrivateKey","PrivateKeys","ProviderPrivateKeys","SecretKey","SigningKey","Mnemonic","SeedPhrase","SecretRecoveryPhrase"}
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
