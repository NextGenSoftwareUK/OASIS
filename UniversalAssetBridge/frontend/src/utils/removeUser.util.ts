import { AppRouterInstance } from "next/dist/shared/lib/app-router-context.shared-runtime";
import Cookies from "js-cookie";

export const removeUser = (
  setUser: (user: null) => void,
  router: AppRouterInstance
) => {
  setUser(null);
  Cookies.remove("oasisToken");
  router.push("/signin");
};
