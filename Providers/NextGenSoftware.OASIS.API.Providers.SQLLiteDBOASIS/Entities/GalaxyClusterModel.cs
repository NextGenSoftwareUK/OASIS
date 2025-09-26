using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("GalaxyCluster")]
    public class GalaxyClusterModel : CelestialSpaceAbstract{

        [Required, Key]
        public String GalaxyClusterId{ set; get; }

        public String UniverseId{ set; get; }

        public List<GalaxyModel> Galaxies { get; set; }
        public List<SolarSystemModel> SolarSystems { get; set; }
        public List<StarModel> Stars { get; set; }
        public List<PlanetModel> Planets { get; set; }
        public List<AsteroidModel> Asteroids { get; set; }
        public List<CometModel> Comets { get; set; }
        public List<MeteroidModel> Meteroids { get; set; }
        public bool IsSuperCluster { get; }

        public GalaxyClusterModel():base(){}
        public GalaxyClusterModel(IGalaxyCluster source):base(){

            if(source.Id == Guid.Empty){
                source.Id = Guid.NewGuid();
            }

            this.GalaxyClusterId = source.Id.ToString();
            this.HolonId = source.ParentHolonId.ToString();

            // if(source.SoloarSystems != null){
            //
            //     this.SolarSystems = new List<SolarSystemModel>();
            //     foreach(SolarSystem item in source.SoloarSystems){
            //
            //         this.SolarSystems.Add(new SolarSystemModel(item));
            //     }
            //
            // }
            //
            // if(source.Galaxies != null){
            //
            //     this.Galaxies = new List<GalaxyModel>();
            //     foreach(Galaxy galaxy in source.Galaxies){
            //
            //         this.Galaxies.Add(new GalaxyModel(galaxy));
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

        // public IGalaxyCluster GetGalaxyCluster(){
        //     GalaxyCluster item=new GalaxyCluster();
        //
        //     item.Id = Guid.Parse(this.GalaxyClusterId);
        //     item.ParentHolonId = Guid.Parse(this.HolonId);
        //
        //     return(item);
        // }
    }
}