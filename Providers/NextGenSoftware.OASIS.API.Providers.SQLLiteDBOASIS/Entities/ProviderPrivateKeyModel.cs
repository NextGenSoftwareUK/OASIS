using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("ProviderPrivateKey")]
    public class ProviderPrivateKeyModel
    {
        [Required, Key]
        public ProviderType ProviderId { get; set;}
        public string Value{ set; get; }
        public string OwnerId{ set; get; }

        public ProviderPrivateKeyModel(){}
        public ProviderPrivateKeyModel(ProviderType Id, String value){
            this.ProviderId = Id;
            this.Value = value;
        }
    }

}