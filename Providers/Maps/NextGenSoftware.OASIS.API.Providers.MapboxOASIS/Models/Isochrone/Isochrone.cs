using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Isochrone
{
    public class Properties
    {
        [JsonProperty("contour")]
        public int Contour { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("opacity")]
        public double Opacity { get; set; }

        [JsonProperty("fill")]
        public string Fill { get; set; }

        [JsonProperty("fill-opacity")]
        public double FillOpacity { get; set; }

        [JsonProperty("fillColor")]
        public string FillColor { get; set; }

        [JsonProperty("fillOpacity")]
        public double FillOpacity1 { get; set; }
    }

    public class Geometry
    {
        [JsonProperty("coordinates")]
        public List<List<double>> Coordinates { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class Feature
    {
        [JsonProperty("properties")]
        public Properties Properties { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("geometry")]
        public Geometry Geometry { get; set; }
    }
    
    public class Isochrone
    {
        [JsonProperty("features")]
        public List<Feature> Features { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}