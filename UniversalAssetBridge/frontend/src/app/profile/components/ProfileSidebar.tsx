"use client";

import { useUserStore } from "@/store/useUserStore";
import { usePathname } from "next/navigation";
import { useRouter } from "nextjs-toploader/app";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { profileNavList } from "@/lib/helpers/profileNavList";
import { ChevronRight, LogOut } from "lucide-react";
import Link from "next/link";
import { useWalletStore } from "@/store/useWalletStore";

export default function ProfileSidebar() {
  const { user, logout } = useUserStore();
  const { disconnectWallet } = useWalletStore();

  const router = useRouter();
  const pathname = usePathname();

  if (!user) return null;

  return (
    <aside className="">
      <Link
        href="/profile"
        className="flex justify-between items-center cursor-pointer group relative"
      >
        <div className="flex gap-4 items-center">
          <Avatar className="w-10 h-10 flex justify-center items-center border-2 border-secondary">
            <AvatarImage src="/profile.svg" className="invert" />
            <AvatarFallback className="uppercase">
              {user.UserName.slice(0, 2)}
            </AvatarFallback>
          </Avatar>

          <div>
            <p className="p group-hover:underline">{user.UserName}</p>
            <p className="p-sm text-secondary -mt-1">{user.Email}</p>
          </div>
        </div>

        <ChevronRight
          className="absolute right-0 top-1/2 -translate-y-1/2 transition-all group-hover:-right-2"
          size={16}
        />
      </Link>

      <ul className="mt-7.5 space-y-0.5">
        {profileNavList.map((item, i) => {
          return (
            <Link
              href={item.href}
              key={i}
              className={`flex gap-4 px-5 items-center py-[14px] rounded-md transition-all relative group hover:bg-primary ${item.href === pathname && "bg-primary"}`}
            >
              {item.icon && <item.icon size={20} />}
              <span
                className={`transition-all group-hover:pl-2 ${item.href === pathname && "pl-2"}`}
              >
                {item.name}
              </span>
            </Link>
          );
        })}
      </ul>

      <hr className="border-muted/30 my-4" />

      <div
        onClick={() => {
          logout();
          disconnectWallet();
          localStorage.removeItem("user");
          router.push("/");
        }}
        className="flex gap-4 items-center px-5 py-[14px] rounded-md transition-all group hover:bg-primary cursor-pointer opacity-70"
      >
        <LogOut size={20} />
        <p className="p transition-all group-hover:pl-2">Logout</p>
      </div>
    </aside>
  );
}
