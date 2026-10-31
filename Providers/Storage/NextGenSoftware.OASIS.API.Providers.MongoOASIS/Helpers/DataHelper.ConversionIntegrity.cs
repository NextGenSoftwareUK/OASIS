using System;
using System.Collections.Generic;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NativeBase=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.HolonBase;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;

public static partial class DataHelper
{
    private static OASISResult<T> ConversionFailure<T>(OASISResult<T> result,Exception cause)
    {
        result.Result=default;result.IsLoaded=false;result.LoadedCount=0;
        result.IsError=true;result.ErrorCount++;
        result.Exception=result.Exception==null?cause:new AggregateException("Native read and projection failed.",result.Exception,cause);
        result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        result.Message="Stored Mongo record conversion failed; no partial projection was published.";
        // IsSaved/SavedCount retain any already-acknowledged native write facts.
        return result;
    }

    private static OASISResult<TTarget> ConvertNativeRecord<TSource,TTarget>(OASISResult<TSource> source,Action<TSource,OASISResult<TTarget>> project)
        where TSource:NativeBase where TTarget:class
    {
        var result=new OASISResult<TTarget>();
        if(source==null)return ConversionFailure(result,new InvalidDataException("Native record result envelope is absent."));
        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(source,result);
        if(source.IsError || source.Result==null)return result;
        try
        {
            var audit=(IAuditBase)source.Result;
            _=audit.CreatedByAvatarId;_=audit.ModifiedByAvatarId;_=audit.DeletedByAvatarId;
            project(source.Result,result);
            if(result.Result==null)throw new InvalidDataException("Native record projection returned no record.");
            return result;
        }
        catch(Exception cause){return ConversionFailure(result,cause);}
    }

    private static OASISResult<IEnumerable<TTarget>> ConvertNativeCollection<TSource,TTarget>(OASISResult<IEnumerable<TSource>> source,Func<TSource,OASISResult<TTarget>> project)
        where TSource:NativeBase where TTarget:class
    {
        var result=new OASISResult<IEnumerable<TTarget>>();
        if(source==null)return ConversionFailure(result,new InvalidDataException("Native collection result envelope is absent."));
        OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(source,result);
        if(source.IsError)return result;
        try
        {
            if(source.Result==null)throw new InvalidDataException("Native collection content is absent.");
            var rows=new List<TTarget>();
            foreach(var row in source.Result)
            {
                if(row==null)throw new InvalidDataException("Native collection contains a null record.");
                var converted=project(row);
                if(converted==null || converted.IsError || converted.Result==null)
                    return ConversionFailure(result,converted?.Exception ?? new InvalidDataException("Native row conversion did not produce a record."));
                rows.Add(converted.Result);
            }
            result.Result=rows;result.IsLoaded=true;result.LoadedCount=rows.Count;
            return result;
        }
        catch(Exception cause){return ConversionFailure(result,cause);}
    }

    private static IEnumerable<T> RequireConvertedCollection<T>(OASISResult<IEnumerable<T>> result)
    {
        if(result.IsError)throw new InvalidDataException(result.Message,result.Exception);
        return result.Result ?? throw new InvalidDataException("Native collection conversion returned no collection.");
    }
}
