using EOSNewYork.EOSCore.Lib;
using NextGenSoftware.OASIS.API.Core;
using System;

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

        public Profile ToProfile()
        {
            var profile = new Profile()
            {
                UserId = Guid.Parse(this.userid),
                Username = this.username,
                Password = this.password,
                Email = this.email,
                Title = this.title,
                FirstName = this.firstname,
                LastName = this.lastname,
                DOB = this.dob,
                PlayerAddress = this.playeraddr,
                Karma = this.karma
            };

            return profile;
        }
    }
}
