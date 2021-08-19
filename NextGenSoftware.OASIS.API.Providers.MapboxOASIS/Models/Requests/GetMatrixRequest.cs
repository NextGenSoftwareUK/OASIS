using System.Collections.Generic;
using Enums;

namespace Models.Requests
{
    public class GetMatrixRequest
    {
        public RoutingProfile RoutingProfile { get; set; }
        public List<Coordinate> Coordinates { get; set; }
    }
}