using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("SolarSystem")]
    public class SolarSystemModel : CelestialSpaceAbstract{

        [Required, Key]
        public String SolarSystemId{ set; get; }
        
        public String GalaxyId{ set; get; }
        public String GalaxyClusterId{ set; get; }
        public String UniverseId{ set; get; }

        public StarModel Star { get; set; }
        public List<PlanetModel> Planets { get; set; }
        public List<AsteroidModel> Asteroids { get; set; }
        public List<CometModel> Comets { get; set; }
        public List<MeteroidModel> Meteroids { get; set; }

        public SolarSystemModel():base(){}
        public SolarSystemModel(ISolarSystem source):base(){

            if(source.Id == Guid.Empty){
                source.Id = Guid.NewGuid();
            }

            this.SolarSystemId = source.Id.ToString();
            this.HolonId = source.ParentHolonId.ToString();

            if(source.Star != null){
                this.Star = new StarModel(source.Star);
            }

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

        // public ISolarSystem GetSolarSystem(){
        //     SolarSystem item=new SolarSystem();
        //
        //     item.Id = Guid.Parse(this.SolarSystemId);
        //     item.ParentHolonId = Guid.Parse(this.HolonId);
        //
        //     if(this.Star != null){
        //         item.Star = this.Star.GetStar();
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