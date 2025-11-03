using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("ProviderKey")]
    public class ProviderKeyModel
    {
        [Required, Key]
        public ProviderType ProviderId { get; set;}
        public string Value{ set; get; }
        public string OwnerId{ set; get; }

        public ProviderKeyModel(){}
        public ProviderKeyModel(ProviderType Id, String value){

            this.ProviderId = Id;
            this.Value = value;
        }
    }

}