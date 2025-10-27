using System;
using System.ComponentModel.DataAnnotations;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    public abstract class StarModelBase : CelestialBodyAbstract{

        [Required, Key]
        public String StarId{ set; get; }

        public int Luminosity { get; set; }
        public StarType StarType { get; set; }
        public StarClassification StarClassification { get; set; }
        public StarBinaryType StarBinaryType { get; set; }

        protected StarModelBase():base(){}
    }
}