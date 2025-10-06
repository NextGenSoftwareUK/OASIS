using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("Universe")]
    public class UniverseModel : CelestialSpaceAbstract{

        [Required, Key]
        public String UniverseId{ set; get; }

        public List<GalaxyClusterModel> GalaxyClusters { get; set; }
        public List<SolarSystemModel> SolarSystems { get; set; }
        public List<NebulaModel> Nebulas { get; set; }
        public List<StarModel> Stars { get; set; }
        public List<PlanetModel> Planets { get; set; }
        public List<AsteroidModel> Asteroids { get; set; }
        public List<CometModel> Comets { get; set; }
        public List<MeteroidModel> Meteroids { get; set; }

        public UniverseModel():base(){}
        public UniverseModel(IUniverse source):base(){

            if(source.Id == Guid.Empty){
                source.Id = Guid.NewGuid();
            }

            this.UniverseId = source.Id.ToString();
            this.HolonId = source.ParentHolonId.ToString();

            // if(source.SolarSystems != null){
            //
            //     this.SolarSystems = new List<SolarSystemModel>();
            //     foreach(SolarSystem item in source.SolarSystems){
            //
            //         this.SolarSystems.Add(new SolarSystemModel(item));
            //     }
            //
            // }
            //
            // if(source.Nebulas != null){
            //
            //     this.Nebulas = new List<NebulaModel>();
            //     foreach(Nebula nebula in source.Nebulas){
            //
            //         this.Nebulas.Add(new NebulaModel(nebula));
            //     }
            //
            // }
            //
            // if(source.GalaxyClusters != null){
            //
            //     this.GalaxyClusters = new List<GalaxyClusterModel>();
            //     foreach(GalaxyCluster galaxy in source.GalaxyClusters){
            //
            //         this.GalaxyClusters.Add(new GalaxyClusterModel(galaxy));
            //     }
            //
            // }
            //
            // if(source.Stars != null){
            //
            //     this.Stars = new List<StarModel>();
            //     foreach(Star star in source.Stars){
            //
            //         this.Stars.Add(new StarModel(star));
            //     }
            //
            // }
            //
            // if(source.Planets != null){
            //
            //     this.Planets = new List<PlanetModel>();
            //     foreach(Planet planet in source.Planets){
            //
            //         this.Planets.Add(new PlanetModel(planet));
            //     }
            //
            // }
            //
            // if(source.Asteroids != null){
            //
            //     this.Asteroids = new List<AsteroidModel>();
            //     foreach(Asteroid asteroid in source.Asteroids){
            //
            //         this.Asteroids.Add(new AsteroidModel(asteroid));
            //     }
            //
            // }
            //
            // if(source.Comets != null){
            //
            //     this.Comets = new List<CometModel>();
            //     foreach(Comet comet in source.Comets){
            //
            //         this.Comets.Add(new CometModel(comet));
            //     }
            //
            // }
            //
            // if(source.Meteroids != null){
            //
            //     this.Meteroids = new List<MeteroidModel>();
            //     foreach(Meteroid meteroid in source.Meteroids){
            //
            //         this.Meteroids.Add(new MeteroidModel(meteroid));
            //     }
            //
            // }
        }

        // public IUniverse GetUniverse(){
        //     Universe item=new Universe();
        //
        //     item.Id = Guid.Parse(this.UniverseId);
        //     item.ParentHolonId = Guid.Parse(this.HolonId);
        //
        //     if(this.GalaxyClusters != null){
        //
        //         item.GalaxyClusters = new List<IGalaxyCluster>();
        //         foreach (GalaxyClusterModel galaxy in this.GalaxyClusters)
        //         {
        //             item.GalaxyClusters.Add(galaxy.GetGalaxyCluster());
        //         }
        //     }
        //
        //     if(this.SolarSystems != null){
        //
        //         item.SolarSystems = new List<ISolarSystem>();
        //         foreach (SolarSystemModel solar in this.SolarSystems)
        //         {
        //             item.SolarSystems.Add(solar.GetSolarSystem());
        //         }
        //     }
        //
        //     if(this.Nebulas != null){
        //
        //         item.Nebulas = new List<INebula>();
        //         foreach (NebulaModel nebula in this.Nebulas)
        //         {
        //             item.Nebulas.Add(nebula.GetNebula());
        //         }
        //     }
        //
        //     if(this.Stars != null){
        //
        //         item.Stars = new List<IStar>();
        //         foreach (StarModel star in this.Stars)
        //         {
        //             item.Stars.Add(star.GetStar());
        //         }
        //     }
        //
        //     if(this.Planets != null){
        //
        //         item.Planets = new List<IPlanet>();
        //         foreach (PlanetModel planet in this.Planets)
        //         {
        //             item.Planets.Add(planet.GetPlanet());
        //         }
        //     }
        //
        //     if(this.Asteroids != null){
        //
        //         item.Asteroids = new List<IAsteroid>();
        //         foreach (AsteroidModel asteroid in this.Asteroids)
        //         {
        //             item.Asteroids.Add(asteroid.GetAsteroid());
        //         }
        //     }
        //
        //     if(this.Comets != null){
        //
        //         item.Comets = new List<IComet>();
        //         foreach (CometModel comet in this.Comets)
        //         {
        //             item.Comets.Add(comet.GetComet());
        //         }
        //     }
        //
        //     if(this.Meteroids != null){
        //
        //         item.Meteroids = new List<IMeteroid>();
        //         foreach (MeteroidModel meteroid in this.Meteroids)
        //         {
        //             item.Meteroids.Add(meteroid.GetMeteroid());
        //         }
        //     }
        //
        //     return(item);
        // }
    }
}