using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Direction
{
    public class Intersection
    {
        [JsonProperty("out")]
        public int Out { get; set; }

        [JsonProperty("entry")]
        public List<bool> Entry { get; set; }

        [JsonProperty("bearings")]
        public List<int> Bearings { get; set; }

        [JsonProperty("location")]
        public List<double> Location { get; set; }

        [JsonProperty("in")]
        public int? In { get; set; }
    }

    public class Maneuver
    {
        [JsonProperty("bearing_after")]
        public int BearingAfter { get; set; }

        [JsonProperty("bearing_before")]
        public int BearingBefore { get; set; }

        [JsonProperty("location")]
        public List<double> Location { get; set; }

        [JsonProperty("modifier")]
        public string Modifier { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("instruction")]
        public string Instruction { get; set; }
    }

    public class VoiceInstruction
    {
        [JsonProperty("distanceAlongGeometry")]
        public double DistanceAlongGeometry { get; set; }

        [JsonProperty("announcement")]
        public string Announcement { get; set; }

        [JsonProperty("ssmlAnnouncement")]
        public string SsmlAnnouncement { get; set; }
    }

    public class Component
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Primary
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("components")]
        public List<Component> Components { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("modifier")]
        public string Modifier { get; set; }
    }

    public class Secondary
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("components")]
        public List<Component> Components { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("modifier")]
        public string Modifier { get; set; }
    }

    public class BannerInstruction
    {
        [JsonProperty("distanceAlongGeometry")]
        public double DistanceAlongGeometry { get; set; }

        [JsonProperty("primary")]
        public Primary Primary { get; set; }

        [JsonProperty("secondary")]
        public Secondary Secondary { get; set; }

        [JsonProperty("then")]
        public object Then { get; set; }
    }

    public class Step
    {
        [JsonProperty("intersections")]
        public List<Intersection> Intersections { get; set; }

        [JsonProperty("driving_side")]
        public string DrivingSide { get; set; }

        [JsonProperty("geometry")]
        public string Geometry { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("maneuver")]
        public Maneuver Maneuver { get; set; }

        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("duration")]
        public double Duration { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("distance")]
        public double Distance { get; set; }

        [JsonProperty("voiceInstructions")]
        public List<VoiceInstruction> VoiceInstructions { get; set; }

        [JsonProperty("bannerInstructions")]
        public List<BannerInstruction> BannerInstructions { get; set; }
    }

    public class Leg
    {
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("duration")]
        public double Duration { get; set; }

        [JsonProperty("steps")]
        public List<Step> Steps { get; set; }

        [JsonProperty("distance")]
        public double Distance { get; set; }
    }

    public class Route
    {
        [JsonProperty("geometry")]
        public string Geometry { get; set; }

        [JsonProperty("legs")]
        public List<Leg> Legs { get; set; }

        [JsonProperty("weight_name")]
        public string WeightName { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("duration")]
        public double Duration { get; set; }

        [JsonProperty("distance")]
        public double Distance { get; set; }
    }

    public class Waypoint
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public List<double> Location { get; set; }
    }

    public class Direction
    {
        [JsonProperty("routes")]
        public List<Route> Routes { get; set; }

        [JsonProperty("waypoints")]
        public List<Waypoint> Waypoints { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }
    }
}