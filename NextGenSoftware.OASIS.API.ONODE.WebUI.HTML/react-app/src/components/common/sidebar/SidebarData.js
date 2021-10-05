import { Link } from "react-router-dom";

const SidebarData = [
  {
    id: 1,
    name: "the oasis",
    show: false,
    subMenu: [
      {
        id: 1,
        name: "about",
      },
      {
        id: 2,
        name: "documentation",
      },
      {
        id: 3,
        name: "our world",
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
        name: "view avatar",
      },
      {
        id: 2,
        name: <Link to="/avatar/upload">edit avatar</Link>,
      },
      {
        id: 3,
        name: "search avatars",
      },
    ],
  },
  {
    id: 3,
    name: "karma",
    subMenu: [
      {
        id: 1,
        name: "View Curent Karma Weightings",
      },
      {
        id: 2,
        name: "Vote For Karma Weightings",
      },
      {
        id: 3,
        name: "View Avatar Karma",
      },
      {
        id: 4,
        name: "View/Search Karma Akashic Records",
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
      },
      {
        id: 2,
        name: "Send Data",
        popupName: "sendData",
      },
      {
        id: 3,
        name: "Manage Data",
        popupName: "manageData",
      },
      {
        id: 4,
        name: "Cross-Chain Management",
      },
      {
        id: 5,
        name: "Off-Chain Management",
      },
      {
        id: 6,
        name: "Search Data",
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
      },
      {
        id: 2,
        name: "Donate SEEDS",
      },
      {
        id: 3,
        name: "Reward SEEDS",
      },
      {
        id: 4,
        name: "Send Invite To Join SEEDS",
      },
      {
        id: 5,
        name: "Accept Invite to join seeds",
      },
      {
        id: 6,
        name: "View SEEDS",
      },
      {
        id: 7,
        name: "View Organisations",
      },
      {
        id: 8,
        name: "Manage SEEDS",
      },
      {
        id: 9,
        name: "Search Seeds",
      },
    ],
  },
  {
    id: 6,
    name: "provider",
    subMenu: [
      {
        id: 0,
        name: <Link to="/provider/key-management"> key Managment</Link>,
      },
      {
        id: 1,
        name: <Link to="/provider/provider"> View Providers</Link>,
      },
      {
        id: 2,
        name: "Manage Providers",
      },

      {
        id: 3,
        name: "Manage Auto-Replication",
      },
      {
        id: 4,
        name: "Manage Auto-Fail-Over",
      },
      {
        id: 5,
        name: "Manage Load Balancing",
      },
      {
        id: 6,
        name: "View Provider Stats",
      },
      {
        id: 7,
        name: "Compare Provider Speeds",
      },
      {
        id: 8,
        name: "Search Providers",
      },
      {
        id: 9,
        name: "Holochain",
      },
      {
        id: 10,
        name: "SEEDS",
      },
      {
        id: 11,
        name: "EOSIO",
      },
      {
        id: 12,
        name: "Ethereum",
      },
      {
        id: 13,
        name: "IPFS",
      },
      {
        id: 14,
        name: "ThreeFold",
      },
      {
        id: 15,
        name: "SOLID",
      },
      {
        id: 16,
        name: "Activity Pub",
      },
      {
        id: 17,
        name: "Mongo DB",
      },
      {
        id: 18,
        name: "SQLLite",
      },
      {
        id: 19,
        name: "Neo4j",
      },
    ],
  },
  {
    id: 7,
    name: "nft",
    subMenu: [],
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
    subMenu: [],
  },
];

export default SidebarData;
