﻿using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests
{
    public class GetMatrixRequest
    {
        public RoutingProfile RoutingProfile { get; set; }
        public List<Coordinate> Coordinates { get; set; }
    }
}