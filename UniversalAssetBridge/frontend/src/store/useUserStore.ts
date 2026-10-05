import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";
import Cookies from "js-cookie";
import { User } from "@/types/user/user.type";

interface UserState {
  user: User | null;
  setUser: (user: User | null) => void;
  logout: () => void;
}

export const useUserStore = create<UserState>()(
  persist(
    (set) => {
      let logoutTimer: NodeJS.Timeout | null = null;

      const clearLogoutTimer = () => {
        if (logoutTimer) {
          clearTimeout(logoutTimer);
          logoutTimer = null;
        }
      };

      const scheduleLogout = (expiresAt: string) => {
        const expiresInMs: number = new Date(expiresAt).getTime() - Date.now();
        if (expiresInMs > 0) {
          logoutTimer = setTimeout(() => {
            set({ user: null });
            Cookies.remove("oasisToken");
          }, expiresInMs);
        } else {
          set({ user: null });
          Cookies.remove("oasisToken");
        }
      };

      return {
        user: null,

        setUser: (user) => {
          clearLogoutTimer();
          set({ user });
          console.log("User", user);

          if (user?.expiresAt && Cookies.get("oasisToken")) {
            scheduleLogout(user.expiresAt);
          }

          if (Cookies.get("oasisToken")) {
            set({ user: null });
            console.log("user null cookie");
          }
        },

        logout: () => {
          clearLogoutTimer();
          set({ user: null });
          Cookies.remove("oasisToken");
        },
      };
    },
    {
      name: "user-store",
      storage: createJSONStorage(() => localStorage),
      onRehydrateStorage: () => (state) => {
        if (state?.user && !Cookies.get("oasisToken")) {
          state.logout();
        }
      },
    }
  )
);
