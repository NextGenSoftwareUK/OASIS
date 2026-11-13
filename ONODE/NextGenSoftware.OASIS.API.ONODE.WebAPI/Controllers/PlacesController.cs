using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Places;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

[ApiController, Route("api/places"), Authorize]
public sealed class PlacesController : OASISControllerBase
{
    private readonly WorldscapePlaceCatalogue catalogue;
    public PlacesController(WorldscapePlaceCatalogue catalogue) { this.catalogue=catalogue; }
    [HttpGet("nearby")]
    public async Task<OASISResult<WorldscapePlace[]>> Nearby([FromQuery] string category,[FromQuery] double latitude,[FromQuery] double longitude,CancellationToken token)
    {
        try { return new() { Result=await catalogue.Search(category,latitude,longitude,token) }; }
        catch(Exception ex){return new(){IsError=true,Message="Nearby places could not be loaded: "+ex.Message};}
    }
    [HttpGet("presence")]
    public Task<OASISResult<List<PlaceVisitRecord>>> Presence() => KarmaManager.Instance.GetActivePlaceVisitsAsync(AvatarId);
    [HttpGet("visits")]
    public async Task<OASISResult<object>> Visits([FromQuery]int offset=0,[FromQuery]int limit=50)
    {
        if(offset<0||limit<1||limit>100)return new(){IsError=true,Message="Invalid visit page."};
        var history=await KarmaManager.Instance.GetPlaceVisitsAsync(AvatarId);
        if(history.IsError)return new(){IsError=true,Message=history.Message};
        return new(){Result=new{Items=history.Result.Skip(offset).Take(limit),Total=history.Result.Count,NextOffset=offset+limit<history.Result.Count?(int?)(offset+limit):null}};
    }
    public sealed class Observation
    {
        public Guid Id {get;set;}
        public string PlaceId {get;set;}
        public double Latitude {get;set;}
        public double Longitude {get;set;}
        public DateTime ObservedUtc {get;set;}
    }
    [HttpPost("observe")]
    public async Task<OASISResult<PlaceVisitRecord>> Observe([FromBody]Observation observation,CancellationToken token)
    {
        try
        {
            if(observation==null||observation.Id==Guid.Empty||observation.ObservedUtc.Kind!=DateTimeKind.Utc||
                Math.Abs((DateTime.UtcNow-observation.ObservedUtc).TotalMinutes)>2)
                return new(){IsError=true,Message="A fresh location observation is required."};
            WorldscapePlaceCatalogue.ValidateCoordinates(observation.Latitude,observation.Longitude);
            var active = await KarmaManager.Instance.GetActivePlaceVisitsAsync(AvatarId);
            if(active.IsError)return new(){IsError=true,Message=active.Message};
            var prior=active.Result.SingleOrDefault(p=>p.PlaceId==observation.PlaceId);
            var place=await catalogue.Resolve(observation.PlaceId,
                prior?.Latitude??observation.Latitude,prior?.Longitude??observation.Longitude,token);
            double distance=WorldscapePlaceCatalogue.DistanceToPlace(place,observation.Latitude,observation.Longitude);
            if(distance>50&&distance<100)return new(){Message="Between arrival and departure boundaries."};
            return await KarmaManager.Instance.ObservePlaceVisitAsync(AvatarId,new PlaceVisitRecord {PlaceId=place.Id,Name=place.Name,Category=place.Category,
                Latitude=place.Latitude,Longitude=place.Longitude},distance<=50,observation.Id);
        }
        catch(Exception ex){return new(){IsError=true,Message="Visit could not be recorded: "+ex.Message};}
    }
}
