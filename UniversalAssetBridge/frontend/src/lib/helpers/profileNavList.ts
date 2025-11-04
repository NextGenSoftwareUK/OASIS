import {
  Database,
  History,
  Shield,
  UserRound,
  WalletCards,
} from "lucide-react";

export const profileNavList = [
  {
    name: "Profile",
    href: "/profile",
    icon: UserRound,
  },
  {
    name: "Security",
    href: "/profile/security",
    icon: Shield,
  },
  {
    name: "My RWAs",
    href: "/profile/my-rwas",
    icon: Database,
  },
  {
    name: "My Wallets",
    href: "/profile/wallets",
    icon: WalletCards,
  },
  {
    name: "History",
    href: "/profile/history",
    icon: History,
  },
];
