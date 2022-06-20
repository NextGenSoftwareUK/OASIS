const SidebarData = [
    {
        id: 1,
        name: "the oasis",
        show: false,
        subMenu: [
            {
                id: 1,
                name: "about",
                path: 'https://drive.google.com/file/d/1nnhGpXcprr6kota1Y85HDDKsBfJHN6sn/view?usp=sharing',
                externalLink: true
            },
            {
                id: 2,
                name: "documentation",
                path: 'https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK',
                externalLink: true
            },
            {
                id: 3,
                name: "our world",
                path: 'http://www.ourworldthegame.com/',
                externalLink: true
            },
        ],
    },
    {
        id: 2,
        name: "avatar",
        show: false,
        subMenu: [
            {
                id: 1,
                name: "View Avatar",
                popupName: "viewAvatar",
                loginRequired: true
            },
            {
                id: 2,
                name: "Edit Avatar",
                popupName: "editAvatar",
                loginRequired: true
            },
            {
                id: 3,
                name: "Search Avatars",
                popupName: "searchAvatar",
                loginRequired: true
            },
            {
                id: 4,
                name: "Avatar Wallet",
                popupName: "avatarWallet",
                loginRequired: true
            }
        ],
    },
    {
        id: 3,
        name: "karma",
        subMenu: [
            {
                id: 1,
                name: "View Curent Karma Weightings",
                popupName: "viewKarma",
                loginRequired: true
            },
            {
                id: 2,
                name: "Vote For Karma Weightings",
                popupName: 'voteKarma',
                loginRequired: true
            },
            {
                id: 3,
                name: "View Avatar Karma",
                popupName: 'viewAvatarKarma',
                loginRequired: true
            },
            {
                id: 4,
                name: "View/Search Karma Akashic Records",
                popupName: 'searchKarma',
                loginRequired: true
            },
        ],
    },
    {
        id: 4,
        name: "data",
        subMenu: [
            {
                id: 1,

                name: "Load Data",
                popupName: "loadData",
                loginRequired: true
            },
            {
                id: 2,
                name: "Send Data",
                popupName: "sendData",
                loginRequired: true
            },
            {
                id: 3,
                name: "Manage Data",
                popupName: "manageData",
                loginRequired: true
            },
            {
                id: 4,
                name: "Cross-Chain Management",
                popupName: 'crossChainManagement',
                loginRequired: true
            },
            {
                id: 5,
                name: "Off-Chain Management",
                popupName: 'offChainManagement',
                loginRequired: true
            },
            {
                id: 6,
                name: "Search Data",
                popupName: 'searchData',
                loginRequired: true
            },
        ],
    },
    {
        id: 5,
        name: "seeds",
        subMenu: [
            {
                id: 1,
                name: "Pay With SEEDS",
                popupName: "payWithSeeds",
                loginRequired: true
            },
            {
                id: 2,
                name: "Donate SEEDS",
                popupName: "donateSeeds",
                loginRequired: true
            },
            {
                id: 3,
                name: "Reward SEEDS",
                popupName: "rewardSeeds",
                disabled: false
            },
            {
                id: 4,
                name: "Send Invite To Join SEEDS",
                popupName: "sendInvite",
                disabled: false
            },
            {
                id: 5,
                name: "View SEEDS",
                popupName: "viewSeeds",
                disabled: false
            },
            {
                id: 6,
                name: "View Organisations",
                popupName: "viewOrganizations",
                disabled: false
            },
            {
                id: 7,
                name: "Manage SEEDS",
                popupName: "manageSeeds",
                disabled: false
            },
            {
                id: 8,
                name: "Search Seeds",
                popupName: "searchSeeds",
                disabled: false
            },
        ],
    },
    {
        id: 6,
        name: "provider",
        subMenu: [
            {
                id: 1,
                name: "View Providers",
                popupName: "viewProviders",
                disabled: false
            },
            {
                id: 2,
                name: "Manage Providers",
                popupName: 'manageProviders',
                disabled: false
            },

            {
                id: 3,
                name: "Manage Auto-Replication",
                popupName: "manageAutoReplicaton",
                disabled: false
            },
            {
                id: 4,
                name: "Manage Auto-Fail-Over",
                popupName: "manageAutoFailOver",
                disabled: false
            },
            {
                id: 5,
                name: "Manage Load Balancing",
                popupName: "manageLoadBalancing",
                disabled: false
            },
            {
                id: 6,
                name: "View Provider Stats",
                popupName: "viewProviderStats",
                disabled: false
            },
            {
                id: 7,
                name: "Compare Provider Speeds",
                popupName: "compareProviderSpeeds",
                disabled: false
            },
            {
                id: 8,
                name: "Search Providers",
                popupName: "searchProviders",
                disabled: false
            },
            {
                id: 9,
                name: "Holochain",
                popupName: "holochain",
                disabled: false
            },
            {
                id: 10,
                name: "SEEDS",
                popupName: "seeds",
                disabled: false
            },
            {
                id: 11,
                name: "EOSIO",
                popupName: "eosio",
                disabled: false
            },
            {
                id: 12,
                name: "Ethereum",
                popupName: "ethereum",
                disabled: false
            },
            {
                id: 13,
                name: "IPFS",
                popupName: "ipfs",
                disabled: false
            },
            {
                id: 14,
                name: "ThreeFold",
                popupName: "threeFold",
                disabled: false
            },
            {
                id: 15,
                name: "SOLID",
                popupName: "solid",
                disabled: false
            },
            {
                id: 16,
                name: "Activity Pub",
                popupName: "activityPub",
                disabled: false
            },
            {
                id: 17,
                name: "Mongo DB",
                popupName: "mongoDb",
                disabled: false
            },
            {
                id: 18,
                name: "SQLLite",
                popupName: "sqlLite",
                disabled: false
            },
            {
                id: 19,
                name: "Neo4j",
                popupName: "neo4j",
                disabled: false
            },
        ],
    },
    {
        id: 7,
        name: "nft",
        subMenu: [
            {
                id: 1,
                name: "View OASIS NFTs",
                popupName: 'viewOasisNft',
                disabled: false
            },
            {
                id: 2,
                name: "Manage OASIS NFTs",
                popupName: 'manageOasisNft',
                disabled: false
            },
            {
                id: 3,
                name: "Search OASIS NFTs",
                popupName: 'searchOasisNft',
                disabled: false
            },
            {
                id: 4,
                name: "Purchase OASIS NFT",
                popupName: 'purchaseOasisNft',
                disabled: false
            },
            {
                id: 5,
                name: "Purchase OASIS Virtual Land NFT",
                popupName: 'PurchaseOasisVirtualLandNft',
                disabled: false
            },
            {
                id: 6,
                name: "Solana",
                popupName: 'solana',
                disabled: false
            },
            {
                id: 7,
                name: "Contact Popup",
                popupName: 'contactPopup',
                disabled: false
            },
        ],
    },
    {
        id: 8,
        name: "map",
        subMenu: [
            {
                id: 1,
                name: "View Global 3D Map",
                popupName: 'viewGlobal3dMap'
            },
            {
                id: 2,
                name: "Mange Map",
                popupName: 'manageMap'
            },
            {
                id: 3,
                name: "Add Quest To Map",
                popupName: 'addQuestToMap'
            },
            {
                id: 4,
                name: "Add 2D Object To Map",
                popupName: 'add2dObjectMap'
            },
            {
                id: 5,
                name: "Add 3D Object To Map",
                popupName: 'add3dObjectMap'
            },
            {
                id: 6,
                name: "Plot Route On Map",
                popupName: 'plotRouteOnMap'
            },
            {
                id: 7,
                name: "View OAPP's On Map",
                popupName: 'viewOappOnMap'
            },
            {
                id: 8,
                name: "View Holons On Map",
                popupName: 'viewHalonsOnMap'
            },
            {
                id: 9,
                name: "View Quests On Map",
                popupName: 'viewQuestOnMap'
            },
            {
                id: 10,
                name: "Search Map",
                popupName: 'searchMap'
            },
            {
                id: 11,
                name: "Download Our World",
                popupName: 'downloadOurWorld'
            },
        ],
    },
    {
        id: 9,
        name: "oapp",
        subMenu: [
            {
                id: 1,
                name: "Install OAPP's",
                popupName: 'installOAPP'
            },
            {
                id: 2,
                name: "Manage OAPP's",
                popupName: 'manageOAPP'
            },
            {
                id: 3,
                name: "Create OAPP",
                popupName: 'createOAPP'
            },
            {
                id: 4,
                name: "Deploy OAPP",
                popupName: 'deployOAPP'
            },
            {
                id: 5,
                name: "Edit OAPP",
                popupName: 'editOAPP'
            },
            {
                id: 6,
                name: "Launch OAPP",
                popupName: 'launchOAPP'
            },
            {
                id: 7,
                name: "Search OAPP",
                popupName: 'searchOAPP'
            },
            {
                id: 8,
                name: "Download Our World",
                popupName: 'downloadOurWorld'
            },
        ],
    },
    {
        id: 10,
        name: "quest",
        subMenu: [
            {
                id: 1,
                name: "View Quest",
                popupName: 'viewQuest'
            },
            {
                id: 2,
                name: "Manage Quest",
                popupName: 'manageQuest'
            },
            {
                id: 3,
                name: "Search Quest",
                popupName: 'searchQuest'
            },
        ],
    },
    {
        id: 11,
        name: "mission",
        subMenu: [
            {
                id: 1,
                name: "View Mission",
                popupName: 'viewMission'
            },
            {
                id: 2,
                name: "Manage Mission",
                popupName: 'manageMission'
            },
            {
                id: 3,
                name: "Search Mission",
                popupName: 'searchMission'
            },
        ],
    },
    {
        id: 12,
        name: "eggs",
        subMenu: [
            {
                id: 1,
                name: "View Eggs",
                popupName: 'viewEggs'
            },
            {
                id: 2,
                name: "Manage Eggs",
                popupName: 'manageEggs'
            },
            {
                id: 3,
                name: "Search Eggs",
                popupName: 'searchEggs'
            },
        ],
    },
    {
        id: 13,
        name: "game",
        subMenu: [
            {
                id: 1,
                name: "View StarCraft 2 Leagues",
                popupName: 'viewLeagues'
            },
            {
                id: 2,
                name: "View StarCraft 2 Tournaments",
                popupName: 'viewTournaments'
            },
            {
                id: 3,
                name: "View StarCraft 2 Achievements",
                popupName: 'viewAchievements'
            },
            {
                id: 4,
                name: "Search StarCraft 2 Profiles",
                popupName: 'searchProfiles'
            },
        ],
    },
    {
        id: 14,
        name: "developer",
        subMenu: [
            {
                name: "OASIS API LIVE",
                path: "https://api.oasisplatform.world/",
                externalLink: true
            },
            {
                name: "OASIS API STAGING",
                path: "https://staging.api.oasisplatform.world/",
                externalLink: true
            },
            {
                name: "OASIS API UI LIVE",
                path: "https://staging.api.oasisplatform.world/",
                externalLink: true
            },
            {
                name: "OASIS API UI STAGING",
                path: "https://staging.api.oasisplatform.world/",
                externalLink: true
            },
            {
                name: "STAR ODK",
                path: "https://oasisplatform.world/?#",
                externalLink: true
            },
            {
                name: "OASIS API Postman JSON File",
                path: "https://oasisplatform.world/postman/OASIS_API.postman_collection.json",
                externalLink: true
            },
            {
                name: "OASIS API DEV ENVIROMENT POSTMAN JSON FILE",
                path: "https://oasisplatform.world/postman/OASIS_API_DEV.postman_environment.json",
                externalLink: true
            },
            {
                name: "OASIS API STAGING ENVIROMENT POSTMAN JSON FILE",
                path: "https://oasisplatform.world/postman/OASIS_API_STAGING.postman_environment.json",
                externalLink: true
            },
            {
                name: "OASIS API LIVE ENVIROMENT POSTMAN JSON FILE",
                path: "https://oasisplatform.world/postman/OASIS_API_LIVE.postman_environment.json",
                externalLink: true
            },
            {
                name: "Code/Documentation",
                path: "https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK",
                externalLink: true
            },
            {
                name: "The Justice League Academy",
                path: "https://www.thejusticeleagueaccademy.icu/",
                externalLink: true
            },
        ],
    },
];

export default SidebarData;
