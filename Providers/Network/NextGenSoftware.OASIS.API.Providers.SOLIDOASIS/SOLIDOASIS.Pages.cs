using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

public partial class SOLIDOASIS
{
    private async Task<Graph> ReadPrivateContainerPagesAsync(Uri container)
    {
        var aggregate=new Graph{BaseUri=container};
        var visited=new HashSet<string>(StringComparer.Ordinal);
        var current=container;string sequenceTag=null;var paged=false;
        try
        {
            while(current!=null)
            {
                var pod=new Uri(_podServerUrl+"/");
                if(!pod.IsBaseOf(current)||!string.IsNullOrEmpty(current.UserInfo)||!string.IsNullOrEmpty(current.Fragment))
                    throw new InvalidDataException("SOLID page target must remain within the configured Pod without credentials or fragment.");
                if(!visited.Add(current.AbsoluteUri)||visited.Count>1000)throw new InvalidDataException("SOLID page cycle or 1000-page bound exceeded; no partial collection returned.");
                using var request=new HttpRequestMessage(HttpMethod.Get,current);
                request.Headers.Accept.ParseAdd("text/turtle");
                request.Headers.TryAddWithoutValidation("Prefer","return=representation; max-member-count=1000");
                using var response=await _httpClient.SendAsync(request);
                if(response.StatusCode==HttpStatusCode.SeeOther)
                {
                    var location=response.Headers.Location??throw new InvalidDataException("SOLID paging redirect lacks Location.");
                    current=new Uri(current,location);paged=true;continue;
                }
                response.EnsureSuccessStatusCode();
                if(!string.Equals(response.Content.Headers.ContentType?.MediaType,"text/turtle",StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SOLID container page must be text/turtle.");
                var links=SolidPageLinks.Parse(response.Headers.TryGetValues("Link",out var values)?values:Array.Empty<string>(),current);
                var next=links.Where(link=>link.Relations.Contains("next",StringComparer.OrdinalIgnoreCase)).ToArray();
                var canonical=links.Where(link=>link.Relations.Contains("canonical",StringComparer.OrdinalIgnoreCase)).ToArray();
                if(next.Length>1||canonical.Length>1)throw new InvalidDataException("Ambiguous SOLID paging links.");
                using var page=new Graph{BaseUri=current};
                using var input=new StringReader(await response.Content.ReadAsStringAsync());new TurtleParser().Load(page,input);
                paged|=next.Length>0||links.Any(link=>link.Relations.Contains("type",StringComparer.OrdinalIgnoreCase)&&link.Target.AbsoluteUri=="http://www.w3.org/ns/ldp#Page")
                    ||page.Triples.Any(triple=>triple.Predicate is IUriNode predicate&&predicate.Uri.AbsoluteUri=="http://www.w3.org/1999/02/22-rdf-syntax-ns#type"
                        &&triple.Object is IUriNode type&&type.Uri.AbsoluteUri=="http://www.w3.org/ns/ldp#Page");
                if(paged)
                {
                    if(canonical.Length!=1||canonical[0].Target!=container||string.IsNullOrWhiteSpace(canonical[0].ETag))
                        throw new InvalidDataException("Paged SOLID collection requires its exact canonical resource and sequence ETag.");
                    sequenceTag??=canonical[0].ETag;
                    if(!string.Equals(sequenceTag,canonical[0].ETag,StringComparison.Ordinal))
                        throw new InvalidDataException("SOLID collection changed during page traversal; no partial result or restart.");
                }
                aggregate.Merge(page);
                if(aggregate.Triples.Count>100000)throw new InvalidDataException("SOLID collection exceeds the 100000-triple safety bound.");
                current=next.SingleOrDefault()?.Target;
            }
            return aggregate;
        }
        catch{aggregate.Dispose();throw;}
    }
}
