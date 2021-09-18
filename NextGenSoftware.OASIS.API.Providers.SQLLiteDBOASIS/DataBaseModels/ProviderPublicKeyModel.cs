using System;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    [Table("ProviderPublicKey")]
    public class ProviderPublicKeyModel : ProviderKeyAbstract
    {
        public ProviderPublicKeyModel():base(){}
        public ProviderPublicKeyModel(ProviderType Id, String value) : base(Id,value){}

        public override ProviderKeyAbstract GetProviderKey()
        {
            ProviderKeyAbstract item=new ProviderPrivateKeyModel();

            item.ProviderId=this.ProviderId;
            item.Value=this.Value;
            item.ParentId = this.ParentId;

            return(item);
        }
    }

}