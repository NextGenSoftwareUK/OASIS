using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

/// <summary>One subject's literal projection; no network dereferencing or private-record codec.</summary>
internal sealed class SolidRdfRecord
{
    internal Guid Id { get; private set; }
    private readonly Dictionary<string,string> _literals=new(StringComparer.Ordinal);
    internal string Get(string predicate)=>_literals.TryGetValue(predicate,out var value)?value:null;

    internal static IReadOnlyList<SolidRdfRecord> Read(string turtle)
    {
        using var graph=new Graph();
        using var reader=new StringReader(turtle??throw new ArgumentNullException(nameof(turtle)));
        new TurtleParser().Load(graph,reader);
        var idPredicate=graph.CreateUriNode(new Uri("https://oasisomniverse.one/ns/id"));
        var records=new List<SolidRdfRecord>();
        foreach(var subject in graph.GetTriplesWithPredicate(idPredicate).Select(triple=>triple.Subject).Distinct()){
            var record=new SolidRdfRecord();
            foreach(var group in graph.GetTriplesWithSubject(subject).GroupBy(triple=>((IUriNode)triple.Predicate).Uri.AbsoluteUri,StringComparer.Ordinal)){
                // RDF permits arbitrary multi-valued properties. Only the mapped scalar fields are constrained.
                if(!MappedPredicates.Contains(group.Key))continue;
                var values=group.Select(triple=>triple.Object).Distinct().ToArray();
                if(values.Length!=1||values[0] is not ILiteralNode literal)
                    throw new InvalidDataException("OASIS RDF scalar predicate requires one literal value: "+group.Key);
                record._literals.Add(group.Key,literal.Value);
            }
            if(!Guid.TryParse(record.Get(idPredicate.Uri.AbsoluteUri),out var id)||id==Guid.Empty)
                throw new InvalidDataException("OASIS RDF record requires a nonempty GUID identity.");
            record.Id=id;records.Add(record);
        }
        if(records.Select(record=>record.Id).Distinct().Count()!=records.Count)
            throw new InvalidDataException("Distinct RDF subjects cannot share one OASIS record identity.");
        return records;
    }

    private static readonly HashSet<string> MappedPredicates=new(StringComparer.Ordinal){
        "https://oasisomniverse.one/ns/id","http://www.w3.org/2000/01/rdf-schema#label",
        "http://xmlns.com/foaf/0.1/nick","http://xmlns.com/foaf/0.1/firstName","http://xmlns.com/foaf/0.1/familyName",
        "https://schema.org/name","https://schema.org/email","https://schema.org/givenName","https://schema.org/familyName","https://schema.org/description"};
}
