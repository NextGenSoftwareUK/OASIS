using System;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
class Program
{
 static void Main()
 {
  int checks=0; DateTime now=new DateTime(2026,9,17,12,0,0,DateTimeKind.Utc);
  foreach(bool permanent in new[]{false,true})
  foreach(bool shared in new[]{false,true})
  foreach(int globalLimit in new[]{-1,0,1,3})
  foreach(int playerLimit in new[]{-1,0,1,3})
  foreach(int count in new[]{0,1,3})
  foreach(bool other in new[]{false,true})
  foreach(int elapsed in new[]{0,59,60,61})
  {
   bool allowed= (shared || !other) && (permanent || (globalLimit==0 ? playerLimit==-1 || count<playerLimit : globalLimit==-1 || count+(other?1:0)<globalLimit)) && (count==0 || elapsed>=60);
   var result=GeoNFTCollectionPolicy.Evaluate(Guid.NewGuid(),permanent,shared,globalLimit,playerLimit,60,count+(other?1:0),count,other,count==0?null:now.AddSeconds(-elapsed),now);
   if(result.CanCollect!=allowed) throw new Exception($"Rule mismatch: permanent={permanent},shared={shared},global={globalLimit},player={playerLimit},count={count},other={other},elapsed={elapsed}");
   checks++;
  }
  if(GeoNFTCollectionPolicy.Evaluate(Guid.NewGuid(),true,true,-2,1,60,0,0,false,null,now).CanCollect)throw new Exception("Invalid limit accepted");
  int named = 0;
  void Check(string name, bool expected, bool permanent, bool shared, int global, int player,
      int seconds, long total, long mine, bool other, DateTime? last, DateTime? next = null)
  {
   var id=Guid.NewGuid();
   var actual=GeoNFTCollectionPolicy.Evaluate(id,permanent,shared,global,player,seconds,total,mine,other,last,now);
   if(actual.CanCollect!=expected || actual.NextCollectAtUtc!=next || actual.GeoNFTId!=id ||
      actual.PlayerCollectionCount!=mine || actual.GlobalCollectionCount!=total ||
      (!expected && string.IsNullOrEmpty(actual.Reason))) throw new Exception(name);
   named++;
  }
  Check("Anorak first collection",true,false,true,0,1,0,8,0,true,null);
  Check("Anorak second collection",false,false,true,0,1,0,9,1,true,now);
  Check("Global overrides zero player limit",true,false,true,3,0,0,2,0,true,null);
  Check("Global exhausted by other players",false,false,true,3,-1,0,3,0,true,null);
  Check("Infinite global overrides zero player limit",true,false,true,-1,0,0,100,0,true,null);
  Check("Zero per-player limit",false,false,true,0,0,0,0,0,false,null);
  Check("Permanent overrides finite caps",true,true,true,1,1,0,100,50,true,now);
  Check("Permanent retains exclusive claim",false,true,false,-1,-1,0,1,0,true,null);
  Check("Cooldown immediately before expiry",false,true,true,-1,-1,60,5,5,false,now.AddSeconds(-59),now.AddSeconds(1));
  Check("Cooldown exact expiry",true,true,true,-1,-1,60,5,5,false,now.AddSeconds(-60));
  Check("Zero cooldown",true,true,true,-1,-1,0,5,5,false,now);
  Check("Exhaustion never schedules respawn",false,false,true,0,1,60,1,1,false,now);
  Check("Invalid player limit",false,true,true,0,-2,0,0,0,false,null);
  Check("Invalid cooldown",false,true,true,0,1,-1,0,0,false,null);
  Console.WriteLine($"PASS {checks} collection policy combinations, {named} named boundary checks, plus invalid-limit rejection");
 }
}
