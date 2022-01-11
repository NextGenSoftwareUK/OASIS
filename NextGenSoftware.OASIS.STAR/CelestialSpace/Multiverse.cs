using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Multiverse : CelestialSpace, IMultiverse
    {
        private IMultiverseDimensions _dimensions;

        public IGrandSuperStar GrandSuperStar { get; set; } //Lets you jump between universes/dimensions within this multiverse.
        
        public IMultiverseDimensions Dimensions
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

        public Multiverse() : base(HolonType.Multiverse)
        {
            Init();
        }

        public Multiverse(IOmiverse omniverse = null) : base(HolonType.Multiverse) 
        {
            Init(omniverse);
        }
        
        public Multiverse(Guid id, IOmiverse omniverse = null) : base(id, HolonType.Multiverse) 
        {
            Init(omniverse);
        }

        public Multiverse(Dictionary<ProviderType, string> providerKey, IOmiverse omniverse = null) : base(providerKey, HolonType.Multiverse) 
        {
            Init(omniverse);
        }

        private void Init(IOmiverse omniverse = null)
        {
            this.Name = "Multiverse";
            this.Description = "A Multiverse that contains dimensions 1-7, each of which contain it's own Universe.";
            this.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);

            GrandSuperStar = new GrandSuperStar()
            {
                CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI),
                ParentMultiverse = this,
                ParentMultiverseId = this.Id,
                Name = "GrandSuperStar",
                Description = "The GrandSuperStar at the centre of this Multiverse/Universe. Can create Universes within it's parent Multiverse.",
            };

            GrandSuperStar.OnCelestialBodySaved += GrandSuperStar_OnCelestialBodySaved;
            base.RegisterCelestialBodies(new List<ICelestialBody>() { this.GrandSuperStar }, false);

            ParentGrandSuperStar = GrandSuperStar;
            ParentGrandSuperStarId = GrandSuperStar.Id;

            if (omniverse != null)
            {
                Mapper<IOmiverse, Multiverse>.MapParentCelestialBodyProperties(omniverse, this);
                Mapper<IOmiverse, GrandSuperStar>.MapParentCelestialBodyProperties(omniverse, (GrandSuperStar)GrandSuperStar);
                this.ParentOmniverse = omniverse;
                this.ParentOmniverseId = omniverse.Id;
                this.GrandSuperStar.ParentOmniverse = omniverse;
                this.GrandSuperStar.ParentOmniverseId = omniverse.Id;
            }

            Dimensions = new MultiverseDimensions(this);
        }

        private void GrandSuperStar_OnCelestialBodySaved(object sender, API.Core.Events.CelestialBodySavedEventArgs e)
        {
            STAR.ShowStatusMessage(e);
            GrandSuperStar.OnCelestialBodySaved -= GrandSuperStar_OnCelestialBodySaved;
        }

        private void RegisterAllCelestialSpaces()
        {
            base.UnregisterAllCelestialSpaces();
            base.RegisterCelestialSpaces(new List<ICelestialSpace>() { Dimensions.FirstDimension, Dimensions.SecondDimension, Dimensions.ThirdDimension, Dimensions.FourthDimension, Dimensions.FifthDimension, Dimensions.SixthDimension, Dimensions.SeventhDimension }, false);
            base.RegisterCelestialSpaces(Dimensions.CustomDimensions, false);
        }
    }
}