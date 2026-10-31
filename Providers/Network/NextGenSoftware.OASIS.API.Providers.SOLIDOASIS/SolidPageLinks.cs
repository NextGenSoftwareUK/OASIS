using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

// One strict HTTP Link parser for collection traversal; commas/semicolons inside
// URI references or quoted parameter values are not field delimiters.
internal static class SolidPageLinks
{
    internal sealed record Link(Uri Target, string[] Relations, string ETag);

    internal static IReadOnlyList<Link> Parse(IEnumerable<string> values, Uri context)
    {
        var result=new List<Link>();
        foreach(var value in values)
        foreach(var field in Split(value,','))
        {
            var text=field.Trim();
            if(!text.StartsWith('<'))throw new InvalidDataException("Malformed SOLID Link target.");
            var end=text.IndexOf('>');
            if(end<1||!Uri.TryCreate(context,text.Substring(1,end-1),out var target))throw new InvalidDataException("Invalid SOLID Link URI.");
            var parameters=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            var tail=text.Substring(end+1).Trim();
            if(tail.Length>0&&!tail.StartsWith(';'))throw new InvalidDataException("Malformed SOLID Link parameters.");
            foreach(var part in Split(tail.TrimStart(';'),';').Where(part=>!string.IsNullOrWhiteSpace(part)))
            {
                NameValueHeaderValue parsed;
                try{parsed=NameValueHeaderValue.Parse(part.Trim());}
                catch(FormatException error){throw new InvalidDataException("Malformed SOLID Link parameter.",error);}
                var parameter=parsed.Value;
                if(parameter!=null&&parameter.StartsWith('"'))
                {
                    var decoded=new System.Text.StringBuilder();
                    for(var i=1;i<parameter.Length-1;i++)
                    {
                        if(parameter[i]=='\\')i++;
                        decoded.Append(parameter[i]);
                    }
                    parameter=decoded.ToString();
                }
                if(!parameters.TryAdd(parsed.Name,parameter))throw new InvalidDataException("Ambiguous duplicate SOLID Link parameter.");
            }
            if(parameters.ContainsKey("anchor"))throw new InvalidDataException("Alternate Link contexts are not valid collection traversal links.");
            parameters.TryGetValue("rel",out var rel);parameters.TryGetValue("etag",out var etag);
            result.Add(new Link(target,(rel??"").Split(' ',StringSplitOptions.RemoveEmptyEntries),etag));
        }
        return result;
    }

    private static IEnumerable<string> Split(string value,char separator)
    {
        var start=0;var quoted=false;var angle=false;var escaped=false;
        for(var i=0;i<value.Length;i++)
        {
            var c=value[i];
            if(escaped){escaped=false;continue;}
            if(quoted&&c=='\\'){escaped=true;continue;}
            if(!angle&&c=='"'){quoted=!quoted;continue;}
            if(!quoted&&c=='<'){if(angle)throw new InvalidDataException("Nested Link target.");angle=true;continue;}
            if(!quoted&&c=='>'){if(!angle)throw new InvalidDataException("Unexpected Link target terminator.");angle=false;continue;}
            if(!quoted&&!angle&&c==separator){yield return value.Substring(start,i-start);start=i+1;}
        }
        if(quoted||angle||escaped)throw new InvalidDataException("Unterminated SOLID Link field.");
        yield return value.Substring(start);
    }
}
