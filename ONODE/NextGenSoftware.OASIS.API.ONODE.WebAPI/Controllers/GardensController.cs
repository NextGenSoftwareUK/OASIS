using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Gardens;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

[ApiController,Route("api/gardens"),Authorize]
public sealed class GardensController:OASISControllerBase
{
    private readonly GardenService gardens;
    public GardensController(GardenService gardens){this.gardens=gardens;}
    private static async Task<OASISResult<object>> Execute(Func<Task<object>> action)
    {try{return new(){Result=await action()};}catch(Exception ex){return new(){IsError=true,Message=ex.Message};}}
    [HttpGet("species")] public object Species()=>new OASISResult<object>{Result=GardenRules.Species};
    [HttpGet("wallet")] public Task<OASISResult<object>> Wallet(CancellationToken token)=>Execute(()=>gardens.Wallet(AvatarId,token));
    [HttpGet("nearby")] public Task<OASISResult<object>> Nearby(double latitude,double longitude,CancellationToken token)=>Execute(()=>gardens.Nearby(latitude,longitude,token));
    [HttpGet("locations")] public Task<OASISResult<object>> Locations(string category,double latitude,double longitude,CancellationToken token)=>Execute(()=>gardens.Locations(category,latitude,longitude,token));
    [HttpGet("history")] public Task<OASISResult<object>> History(CancellationToken token,int offset=0)=>Execute(()=>gardens.History(AvatarId,offset,token));
    [HttpPost("plant")] public Task<OASISResult<object>> Plant(PlantSeedRequest request,CancellationToken token)=>Execute(()=>gardens.Plant(AvatarId,request,token));
    [HttpPost("{id:guid}/visit")] public Task<OASISResult<object>> Visit(Guid id,GardenLocation request,CancellationToken token)=>Execute(()=>gardens.BeginVisit(AvatarId,id.ToString("D"),request,token));
    [HttpPost("{id:guid}/care")] public Task<OASISResult<object>> Care(Guid id,GardenActionRequest request,CancellationToken token)=>Execute(()=>gardens.CareFor(AvatarId,id.ToString("D"),request,token));
    [HttpPost("{id:guid}/leave")] public Task<OASISResult<object>> Leave(Guid id,CancellationToken token)=>Execute(async()=>{await gardens.CloseVisit(AvatarId,id.ToString("D"),token);return new{Message="Care visit ended. Come back tomorrow."};});
    [HttpPost("{id:guid}/thank-you")] public Task<OASISResult<object>> ThankYou(Guid id,GardenLocation request,CancellationToken token)=>Execute(()=>gardens.MatureReward(AvatarId,id.ToString("D"),request,token));
    [HttpPost("refill")] public Task<OASISResult<object>> Refill([FromQuery]string placeId,GardenLocation request,CancellationToken token)=>Execute(()=>gardens.Refill(AvatarId,placeId,request,token));
}
