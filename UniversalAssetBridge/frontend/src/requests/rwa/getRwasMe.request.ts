import axiosInstance from "@/lib/axiosInstance";
import { useQuery } from "@tanstack/react-query";
import { RwasReq } from "@/types/rwa/rwa.type";

const getRwasMe = async (reqParams: RwasReq) => {
  const res = await axiosInstance.get("/rwa/me", {
    params: {
      PageSize: reqParams.pageSize,
      PageNumber: reqParams.pageNumber,
    },
  });
  return res.data;
};

export const useGetRwasMe = (reqParams: RwasReq, token: string) => {
  return useQuery({
    queryKey: ["rwas", "me", reqParams, token],
    queryFn: () => getRwasMe(reqParams),
  });
};
