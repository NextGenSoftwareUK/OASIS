using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Matrix
{
    public class Destination
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public List<double> Location { get; set; }

        [JsonProperty("distance")]
        public int Distance { get; set; }
    }

    public class Source
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public List<double> Location { get; set; }

        [JsonProperty("distance")]
        public int Distance { get; set; }
    }

    public class Matrix
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("durations")]
        public List<List<double>> Durations { get; set; }

        [JsonProperty("destinations")]
        public List<Destination> Destinations { get; set; }

        [JsonProperty("sources")]
        public List<Source> Sources { get; set; }
    }
} 