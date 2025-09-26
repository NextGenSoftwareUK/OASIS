using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("ProviderMetaData")]
    public class ProviderMetaData
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get; }

        public ProviderType ProviderId{ set; get; }
        public string Property{ set; get; }
        public string Value{ set; get; }
        
        public string OwnerId{ set; get; }

        public ProviderMetaData(){}
        public ProviderMetaData(ProviderType provider,String prop, String value){
            this.ProviderId = provider;
            this.Property=prop;
            this.Value=value;
        }
    }

}