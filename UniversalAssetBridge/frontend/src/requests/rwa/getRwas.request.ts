import axiosInstance from "@/lib/axiosInstance";
import { useQuery } from "@tanstack/react-query";
import { RwasReq } from "@/types/rwa/rwa.type";

const getRwas = async (reqParams: RwasReq) => {
  const res = await axiosInstance.get("/rwa", {
    params: {
      AssetType: reqParams.assetType,
      PriceMin: reqParams.priceMin,
      PriceMax: reqParams.priceMax,
      SortBy: reqParams.sortBy || "CreatedAt",
      SortOrder: reqParams.sortOrder,
      PageSize: reqParams.pageSize,
      PageNumber: reqParams.pageNumber,
    },
  });
  return res.data;
};

export const useGetRwas = (reqParams: RwasReq) => {
  return useQuery({
    queryKey: ["rwas", reqParams],
    queryFn: () => getRwas(reqParams),
  });
};
