using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("ProviderPublicKey")]
    public class ProviderPublicKeyModel
    {
        [Required, Key]
        public ProviderType ProviderId { get; set;}
        public string Value{ set; get; }
        public string OwnerId{ set; get; }

        public ProviderPublicKeyModel(){}
        public ProviderPublicKeyModel(ProviderType Id, String value){
            this.ProviderId = Id;
            this.Value = value;
        }
    }

}