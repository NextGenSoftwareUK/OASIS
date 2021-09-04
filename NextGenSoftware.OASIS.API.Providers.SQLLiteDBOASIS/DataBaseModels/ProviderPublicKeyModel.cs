using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    [Table("ProviderPublicKey")]
    public class ProviderPublicKeyModel : ProviderKeyAbstract
    {
        public ProviderPublicKeyModel():base(){}
        public ProviderPublicKeyModel(KeyValuePair<ProviderType,string> key) : base(key){}

        public override ProviderKeyAbstract GetProviderKey()
        {
            ProviderKeyAbstract item=new ProviderPrivateKeyModel();

            item.KeyId=this.KeyId;
            item.Value=this.Value;

            return(item);
        }
    }

}