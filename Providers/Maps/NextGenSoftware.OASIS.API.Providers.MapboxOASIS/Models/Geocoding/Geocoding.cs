using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Geocoding
{
    public class Properties
    {
        [JsonProperty("wikidata")]
        public string Wikidata { get; set; }

        [JsonProperty("short_code")]
        public string ShortCode { get; set; }
    }

    public class Geometry
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("coordinates")]
        public List<double> Coordinates { get; set; }

        [JsonProperty("interpolated")]
        public bool Interpolated { get; set; }

        [JsonProperty("omitted")]
        public bool Omitted { get; set; }
    }

    public class Context
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("wikidata")]
        public string Wikidata { get; set; }

        [JsonProperty("short_code")]
        public string ShortCode { get; set; }
    }

    public class Feature
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("place_type")]
        public List<string> PlaceType { get; set; }

        [JsonProperty("relevance")]
        public double Relevance { get; set; }

        [JsonProperty("properties")]
        public Properties Properties { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("place_name")]
        public string PlaceName { get; set; }

        [JsonProperty("matching_text")]
        public string MatchingText { get; set; }

        [JsonProperty("matching_place_name")]
        public string MatchingPlaceName { get; set; }

        [JsonProperty("center")]
        public List<double> Center { get; set; }

        [JsonProperty("geometry")]
        public Geometry Geometry { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("context")]
        public List<Context> Context { get; set; }
    }

    public class Geocoding
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("query")]
        public List<string> Query { get; set; }

        [JsonProperty("features")]
        public List<Feature> Features { get; set; }

        [JsonProperty("attribution")]
        public string Attribution { get; set; }   
    }
}