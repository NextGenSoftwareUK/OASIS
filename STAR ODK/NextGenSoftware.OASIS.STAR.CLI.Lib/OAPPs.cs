using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class OAPPs : STARUIBase<OAPP, DownloadedOAPP, InstalledChapter>
    {
        public OAPPs(Guid avatarId) : base(new API.ONODE.Core.Managers.OAPPManager(avatarId),
            "Welcome to the OASIS Omniverse/MagicVerse Light Wizard!", new List<string> 
            {
                "This wizard will allow you create an OAPP (Moon, Planet, Star & More) which will appear in the MagicVerse within the OASIS Omniverse.",
                "The OAPP will also optionally appear within the AR geo-location Our World/AR World platform/game in your desired geo-location.",
                "The OAPP will also optionally appear within the One World (Open World MMORPG) game/platform. VR support is also provided.",
                "The OAPP can have as many interfaces/lenses (way to visualize/represent the data of your OAPP) as you like, for example you can also have a 2D web view as well as a 3D view, Metaverse/Omniverse view, etc.",
                "Each OAPP is composed of zomes (re-usable/composable modules containing collections of holons) & holons (generic/composable re-usable OASIS Data Objects). This means the zomes and holons can be shared and re-used with other OAPPs within the STARNET Library. Different zomes and holons can be plugged together to form unique combinations for new OAPPs saving lots of time!",
                "Each OAPP is built/generated on top of a powerful easy to use ORM called (WEB5) COSMIC (The Worlds ORM because it aggregrates all of the worlds data into a simple to use ORM) which allows very easy data management across all of web2 and web3 making data interoperability and interchange very simple and makes silos a thing of the past!",
                "COSMIC is built on top of the powerful WEB4 OASIS API so each OAPP also has easy to use API's for manging keys, wallets, data, nfts, geo-nfts, providers, avatars, karma & much more!",
                "The OAPP can be created from anything you like such as a website, javascript template, game, app, service, etc in any language, platform or OS.",
                "Data can be shared between OAPP's but you are always in full control of your data, you own your data and you can choose exactly who and how that data is shared. You have full data sovereignty.",
                "Due to your OAPP being built on the OASIS API you also benefit from many other advanced features such as auto-replication, auto-failover and auto-load balancing so if one node goes down in your local area it will automatically find the next fastest one in your area irrespective of network.",
                "The more users your OAPP has the larger that celestial body (moon, planet or star) will appear within The MagicVerse. The higher the karma score of the owner (can be a individual or company/organisation) of the OAPP becomes the closer that celestial bodies orbit will be to it's parent so if it's a moon it will get closer and closer to the planet and if it's a planet it will get closer and closer to it's star."
            },
            STAR.STARDNA.DefaultOAPPsSourcePath, "DefaultOAPPsSourcePath",
            STAR.STARDNA.DefaultOAPPsPublishedPath, "DefaultOAPPsPublishedPath",
            STAR.STARDNA.DefaultOAPPsDownloadedPath, "DefaultOAPPsDownloadedPath",
            STAR.STARDNA.DefaultOAPPsInstalledPath, "DefaultOAPPsInstalledPath")
        { }
    }
}