using Enums;

namespace Models.Requests
{
    public class GetIsochroneRequest
    {
        public RoutingProfile RoutingProfile { get; set; }
        public Coordinate Coordinate { get; set; }
        public string ContoursMeters { get; set; }
    }
}