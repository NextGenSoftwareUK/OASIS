using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Omiverse : CelestialSpace, IOmiverse
    {
        private IOmniverseDimensions _dimensions = new OmniverseDimensions();
        private List<IMultiverse> _multiverses = new List<IMultiverse>();

        //let's you jump between Multiverses/Dimensions.
        //public IGreatGrandSuperStar GreatGrandSuperStar { get; set; } = new GreatGrandSuperStar() 
        //{ 
        //    CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI),
        //    Name = "GreatGrandSuperStar",
        //    Description = "GreatGrandSuperStar at the centre of the Omniverse (The OASIS). Can create Multiverses, Universes, Galaxies, SolarSystems, Stars, Planets (Super OAPPS) and moons (OAPPS)",
        //    ParentOmiverse = this,
        //    ParentOmiverseId = this.Id

        //}; 

        public IGreatGrandSuperStar GreatGrandSuperStar { get; set; }
       
        public IOmniverseDimensions Dimensions
        {
            get
            {
                return _dimensions;
            }
            set
            {
                _dimensions = value;
                RegisterAllCelestialSpaces();
            }
        }

        public List<IMultiverse> Multiverses
        {
            get
            {
                return _multiverses;
            }
            set
            {
                _multiverses = value;
                RegisterAllCelestialSpaces();
            }
        }

        public Omiverse() : base(HolonType.Omiverse) 
        {
            Init();
        }

        public Omiverse(Guid id) : base(id, HolonType.Omiverse) 
        {
            Init();
        }

        public Omiverse(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Omiverse) 
        {
            Init();
        }

        private void Init()
        {
            this.Name = "The OASIS Omniverse";
            this.Description = "The OASIS Omniverse that contains everything else.";
            CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
            
            GreatGrandSuperStar = new GreatGrandSuperStar()
            {
                CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI),
                Name = "GreatGrandSuperStar",
                Description = "GreatGrandSuperStar at the centre of the Omniverse (The OASIS). Can create Multiverses, Universes, Galaxies, SolarSystems, Stars, Planets (Super OAPPS) and moons (OAPPS)",
                ParentOmiverse = this,
                ParentOmiverseId = this.Id
            };

            ParentGreatGrandSuperStar = GreatGrandSuperStar;
            ParentGreatGrandSuperStarId = GreatGrandSuperStar.Id;

            //TODO: Not sure if to add the seed multiverse and everything in it here?
            Multiverses.Add(new Multiverse());
        }

        private void RegisterAllCelestialSpaces()
        {
            base.UnregisterAllCelestialSpaces();
            base.RegisterCelestialSpaces(new List<ICelestialSpace>() { Dimensions.EighthDimension, Dimensions.NinthDimension, Dimensions.TenthDimension, Dimensions.EleventhDimension, Dimensions.TwelfthDimension });
            base.RegisterCelestialSpaces(this.Multiverses);
        }
    }
}