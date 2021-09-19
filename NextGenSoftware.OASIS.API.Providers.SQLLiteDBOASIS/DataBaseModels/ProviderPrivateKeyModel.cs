using System;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    [Table("ProviderPrivateKey")]
    public class ProviderPrivateKeyModel : ProviderKeyAbstract
    {
        public ProviderPrivateKeyModel():base(){}
        public ProviderPrivateKeyModel(ProviderType Id, String value) : base(Id,value){}

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