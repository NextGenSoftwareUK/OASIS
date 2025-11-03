using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    //TODO: Finish moving these to OASISStorageBase... (or maybe somewhere better? Be nice to split this out into its own AvatarManager...)
    public static class ExtensionMethods
    {
        public static IEnumerable<Avatar> WithoutPasswords(this IEnumerable<Avatar> users) {
            return users.Select(x => x.WithoutPassword());
        }

        public static Avatar WithoutPassword(this Avatar user) {
            user.Password = null;
            return user;
        }
    }
}