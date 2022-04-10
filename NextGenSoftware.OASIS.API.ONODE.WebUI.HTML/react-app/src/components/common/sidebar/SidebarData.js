const SidebarData = [
    {
        id: 1,
        name: "the oasis",
        show: false,
        subMenu: [
            {
                id: 1,
                name: "about",
                disabled: false
            },
            {
                id: 2,
                name: "documentation",
                disabled: false
            },
            {
                id: 3,
                name: "our world",
                disabled: false
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
                disabled: true
            },
            {
                id: 2,
                name: "Edit Avatar",
                disabled: true
            },
            {
                id: 3,
                name: "Search Avatars",
                popupName: "searchAvatar",
                disabled: false
            },
            {
                id: 4,
                name: "Avatar Wallet",
                popupName: "avatarWallet",
                disabled: false
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
                disabled: false
            },
            {
                id: 2,
                name: "Vote For Karma Weightings",
                disabled: false
            },
            {
                id: 3,
                name: "View Avatar Karma",
                disabled: false
            },
            {
                id: 4,
                name: "View/Search Karma Akashic Records",
                disabled: false
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
                disabled: false
            },
            {
                id: 2,
                name: "Send Data",
                popupName: "sendData",
                disabled: false
            },
            {
                id: 3,
                name: "Manage Data",
                popupName: "manageData",
                disabled: false
            },
            {
                id: 4,
                name: "Cross-Chain Management",
                popupName: 'crossChainManagement',
                disabled: false
            },
            {
                id: 5,
                name: "Off-Chain Management",
                popupName: 'offChainManagement',
                disabled: false
            },
            {
                id: 6,
                name: "Search Data",
                disabled: false
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
                popupName: "payWithSeeds"
            },
            {
                id: 2,
                name: "Donate SEEDS",
                popupName: "donateSeeds"
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
                popupName: "viewseeds",
                disabled: false
            },
            {
                id: 6,
                name: "View Organisations",
                disabled: false
            },
            {
                id: 7,
                name: "Manage SEEDS",
                disabled: false
            },
            {
                id: 8,
                name: "Search Seeds",
                disabled: false
            },
        ],
    },
    {
        id: 6,
        name: "provider",
        subMenu: [
            {
                id: 2,
                name: "Manage Providers",
                disabled: false
            },

            {
                id: 3,
                name: "Manage Auto-Replication",
                disabled: false
            },
            {
                id: 4,
                name: "Manage Auto-Fail-Over",
                disabled: false
            },
            {
                id: 5,
                name: "Manage Load Balancing",
                disabled: false
            },
            {
                id: 6,
                name: "View Provider Stats",
                disabled: false
            },
            {
                id: 7,
                name: "Compare Provider Speeds",
                disabled: false
            },
            {
                id: 8,
                name: "Search Providers",
                disabled: false
            },
            {
                id: 9,
                name: "Holochain",
                disabled: false
            },
            {
                id: 10,
                name: "SEEDS",
                disabled: false
            },
            {
                id: 11,
                name: "EOSIO",
                disabled: false
            },
            {
                id: 12,
                name: "Ethereum",
                disabled: false
            },
            {
                id: 13,
                name: "IPFS",
                disabled: false
            },
            {
                id: 14,
                name: "ThreeFold",
                disabled: false
            },
            {
                id: 15,
                name: "SOLID",
                disabled: false
            },
            {
                id: 16,
                name: "Activity Pub",
                disabled: false
            },
            {
                id: 17,
                name: "Mongo DB",
                disabled: false
            },
            {
                id: 18,
                name: "SQLLite",
                disabled: false
            },
            {
                id: 19,
                name: "Neo4j",
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
                name: "Solana",
                popupName: 'solana',
                disabled: false
            },
            {
                id: 2,
                name: "Contact Popup",
                popupName: 'contactPopup',
                disabled: false
            }
        ],
    },
    {
        id: 8,
        name: "map",
        subMenu: [],
    },
    {
        id: 9,
        name: "oapp",
        subMenu: [],
    },
    {
        id: 10,
        name: "quest",
        subMenu: [],
    },
    {
        id: 11,
        name: "mission",
        subMenu: [],
    },
    {
        id: 12,
        name: "egg",
        subMenu: [],
    },
    {
        id: 13,
        name: "game",
        subMenu: [
            {
                id: 1,
                name: "View StarCraft 2 Leagues",
            },
            {
                id: 2,
                name: "View StarCraft 2 Tournaments",
            },
            {
                id: 3,
                name: "View StarCraft 2 Achievements",
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
            },
            {
                name: "OASIS API STAGING",
                path: "https://staging.api.oasisplatform.world/",
            },
            {
                name: "OASIS API UI LIVE",
                path: "https://staging.api.oasisplatform.world/",
            },
            {
                name: "OASIS API UI STAGING",
                path: "https://staging.api.oasisplatform.world/",
            },
            {
                name: "STAR ODK",
                path: "https://oasisplatform.world/?#",
            },
            {
                name: "OASIS API Postman JSON File",
                path: "https://oasisplatform.world/postman/OASIS_API.postman_collection.json",
            },
            {
                name: "OASIS API DEV ENVIROMENT POSTMAN JSON FILE",
                path: "https://oasisplatform.world/postman/OASIS_API_DEV.postman_environment.json",
            },
            {
                name: "OASIS API STAGING ENVIROMENT POSTMAN JSON FILE",
                path: "https://oasisplatform.world/postman/OASIS_API_STAGING.postman_environment.json",
            },
            {
                name: "OASIS API LIVE ENVIROMENT POSTMAN JSON FILE",
                path: "https://oasisplatform.world/postman/OASIS_API_LIVE.postman_environment.json",
            },
            {
                name: "Code/Documentation",
                path: "https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK",
            },
            {
                name: "The Justice League Academy",
                path: "https://www.thejusticeleagueaccademy.icu/",
            },
        ],
    },
];

export default SidebarData;
