using System;
using EOSNewYork.EOSCore.Lib;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.EOSIOClasses
{
    public class EOSIOAccountTableRow: IEOSTable
    {
        public string userid { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string title { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string dob { get; set; }
        public string playeraddr { get; set; }
        public int karma { get; set; }
        public int level { get; set; }

        public EOSTableMetadata GetMetaData()
        {
            var meta = new EOSTableMetadata
            {
                primaryKey = "userid",
                contract = "oasis",
                scope = "oasis",
                table = "accounts",
                key_type = "string"
            };
            return meta;
        }

        public Avatar ToAvatar()
        {
            var avatar = new Avatar()
            {
                Id = Guid.Parse(this.userid),
                Username = this.username,
                Password = this.password,
                Email = this.email,
                Title = this.title,
                FirstName = this.firstname,
               // DOB = Convert.ToDateTime(this.dob),
              //  Address = this.playeraddr,
                //Karma = this.karma,
                LastName = this.lastname
            };

            //avatar.SetKarmaForDataObject(this.karma);
            return avatar;
        }
    }
}
