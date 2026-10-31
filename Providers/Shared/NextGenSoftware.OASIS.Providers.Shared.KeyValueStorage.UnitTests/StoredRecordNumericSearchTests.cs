using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StoredRecordNumericSearchTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach(var kind in System.Linq.Enumerable.Range(0,14))
            foreach(var operation in Enum.GetValues<SearchOperatorType>())yield return new object[]{kind,operation};
    }
    private static JsonElement Json(string text){using var parsed=JsonDocument.Parse(text);return parsed.RootElement.Clone();}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public void NumberPredicatesPreserveNumericTypesAndExactOrdering(int kind,SearchOperatorType operation)
    {
        object value=kind switch{
            0=>"1",1=>"1,000",2=>"1e3",3=>true,4=>123,5=>1.0m,
            6=>Json("1.0000000000000000000000000000000001"),
            7=>Json("0.9999999999999999999999999999999999"),
            8=>Json("1e1000"),9=>Json("1e-1000"),10=>Json("1e999999999999999999999999999999999999"),
            11=>Json("-1e999999999999999999999999999999999999"),12=>ulong.MaxValue,_=>null};
        var target=kind is 9 or 10 or 11?0:1;
        int? comparison=kind switch{<4 or 13=>null,5=>0,7 or 11=>-1,_=>1};
        var expected=comparison.HasValue&&operation switch{
            SearchOperatorType.LessThan=>comparison<0,SearchOperatorType.LessThanOrEqualTo=>comparison<=0,
            SearchOperatorType.EqualTo=>comparison==0,SearchOperatorType.NotEqualTo=>comparison!=0,
            SearchOperatorType.GreaterThan=>comparison>0,SearchOperatorType.GreaterThanOrEqualTo=>comparison>=0,
            _=>throw new ArgumentOutOfRangeException(nameof(operation))};
        var record=new Holon{Id=Guid.NewGuid(),MetaData=new(){{"score",value}}};
        var query=new SearchNumberGroup{SearchHolons=true,HolonType=HolonType.All,Number=target,NumberOperator=operation,
            HolonSearchParams=new SearchHolonParams{MetaData=true,MetaDataKey="score"}};
        Assert.AreEqual(expected,StoredRecordSearch.MatchesSearchFields(record,false,query));
    }
}
