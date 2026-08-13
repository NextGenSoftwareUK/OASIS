using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract partial class CelestialBodyCore<T>
    {

        private void SetParentsAndAddZome(IZome zome)
        {
            zome.ParentHolonId = this.Id;
            zome.ParentCelestialBodyId = this.Id;

            switch (this.HolonType)
            {
                case HolonType.Moon:
                    zome.ParentMoonId = this.Id;
                    break;

                case HolonType.Planet:
                    zome.ParentPlanetId = this.Id;
                    break;

                case HolonType.Star:
                    zome.ParentStarId = this.Id;
                    break;

                case HolonType.SuperStar:
                    zome.ParentSuperStarId = this.Id;
                    break;

                case HolonType.GrandSuperStar:
                    zome.ParentGrandSuperStarId = this.Id;
                    break;

                case HolonType.GreatGrandSuperStar:
                    zome.ParentGrandSuperStarId = this.Id;
                    break;
            }

            this.Zomes.Add(zome);
        }


        private void BlankParentsAndRemoveZome(IZome zome)
        {
            zome.ParentHolonId = Guid.Empty;
            zome.ParentCelestialBodyId = Guid.Empty;

            switch (this.HolonType)
            {
                case HolonType.Moon:
                    zome.ParentMoonId = Guid.Empty;
                    break;

                case HolonType.Planet:
                    zome.ParentPlanetId = Guid.Empty;
                    break;

                case HolonType.Star:
                    zome.ParentStarId = Guid.Empty;
                    break;

                case HolonType.SuperStar:
                    zome.ParentSuperStarId = Guid.Empty;
                    break;

                case HolonType.GrandSuperStar:
                    zome.ParentGrandSuperStarId = Guid.Empty;
                    break;

                case HolonType.GreatGrandSuperStar:
                    zome.ParentGrandSuperStarId = Guid.Empty;
                    break;
            }

            this.Zomes.Remove(zome);
        }

    }
}
