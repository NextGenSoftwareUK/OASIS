import axiosInstance from "@/lib/axiosInstance";
import { useQuery } from "@tanstack/react-query";

const getNetworks = async () => {
  const res = await axiosInstance.get("/networks");
  return res.data;
};

export const useGetNetworks = () => {
  return useQuery({
    queryKey: ["networks"],
    queryFn: () => getNetworks(),
  });
};
