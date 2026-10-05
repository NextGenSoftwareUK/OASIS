import Cookies from "js-cookie";
import { parseJwt } from "@/utils/parseJwt.util";
import { User } from "@/types";

export const saveUser = (data: User, setUser: (user: User | null) => void) => {
  // const { token, expiresAt, startTime } = data;
  // const { Id, UserName, Email } = parseJwt(token);
  const { token, Id, UserName, Email, expiresAt, startTime } = data;

  setUser({ token, expiresAt, startTime, Id, UserName, Email });
  Cookies.set("oasisToken", token, { expires: new Date(expiresAt) });
};
