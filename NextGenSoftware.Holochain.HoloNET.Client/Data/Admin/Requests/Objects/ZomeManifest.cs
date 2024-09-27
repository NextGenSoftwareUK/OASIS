
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class ZomeManifest
    {
        [Key("name")]
        public string name { get; set; }

        [Key("hash")]
        public string hash { get; set; }

        [Key("dependencies")]
        public ZomeDependency[] dependencies { get; set; }

        //[Key("ZomeLocation")]
        //public ZomeLocation ZomeLocation { get; set; }

        [Key("bundled")]
        public string bundled { get; set; }

        [Key("path")]
        public string path { get; set; }

        [Key("url")]
        public string url { get; set; }
    }
}


//export type ZomeManifest = {
//  name: string;
//hash ?: string;
//dependencies ?: ZomeDependency[];
//} &ZomeLocation;


//export type ZomeLocation = Location;


//export type Location =
//  | {
//      /**
//       * Expect file to be part of this bundle
//       */
//      bundled: string;
//    }
//  | {
///**
// * Get file from local filesystem (not bundled)
// */
//path: string;
//}
//  | {
///**
// * Get file from URL
// */
//url: string;
//};