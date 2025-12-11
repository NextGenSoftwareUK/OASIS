import { useEffect, useMemo, useState } from "react";
import { useGetRwaMultiple } from "@/requests/rwa/getRwa.request";
import { useGetRwaChangesMultiple } from "@/requests/rwa/getRwaChanges.request";
import { useGetRwas } from "@/requests/rwa/getRwas.request";
import {
  CombinedRwa,
  Rwa,
  RwaChanges,
  RwaFiltersParams,
  RwaWithPagination,
} from "@/types/rwa/rwa.type";

export const useRwasData = (initialPage: number) => {
  const [tokenIds, setTokenIds] = useState<string[]>([]);
  const [reqParams, setReqParams] = useState<RwaFiltersParams>({
    assetType: null,
    priceMin: null,
    priceMax: null,
    sortBy: null,
    sortOrder: null,
    pageSize: 10,
    pageNumber: initialPage,
  });

  const rwas = useGetRwas(reqParams);
  const rwaMultiple = useGetRwaMultiple(tokenIds);
  const rwaChangesMultiple = useGetRwaChangesMultiple(tokenIds);

  const normalizeTokenData = (
    arr: (Rwa | RwaChanges | undefined)[]
  ): CombinedRwa[] =>
    arr
      .filter((item): item is Rwa | RwaChanges => !!item && !!item.tokenId)
      .map((item) => ({ ...item, tokenId: item.tokenId }) as CombinedRwa);

  const combinedRwas = useMemo(() => {
    const base = normalizeTokenData(rwaMultiple.data.map((rwa) => rwa?.data));
    const changes = normalizeTokenData(
      rwaChangesMultiple.data.map((rwa) => rwa?.data?.at(-1))
    );

    const combinedMap = new Map<string, CombinedRwa>();

    for (const item of [...base, ...changes]) {
      const existing = combinedMap.get(item.tokenId);
      combinedMap.set(item.tokenId, {
        ...existing,
        ...item,
      });
    }

    return Array.from(combinedMap.values());
  }, [rwaMultiple.data, rwaChangesMultiple.data]);

  const isSomeFetching = useMemo(() => {
    return (
      rwaMultiple.isFetching?.includes(true) ||
      rwaChangesMultiple.isFetching?.includes(true) ||
      rwas.isFetching
    );
  }, [rwaMultiple.isFetching, rwaChangesMultiple.isFetching, rwas.isFetching]);

  useEffect(() => {
    if (rwas?.data?.data?.data) {
      const ids = rwas.data.data.data.map(
        (rwa: RwaWithPagination) => rwa.tokenId
      );
      setTokenIds(ids);
    }
  }, [rwas.data]);

  useEffect(() => {
    setReqParams((prev) => ({
      ...prev,
      pageNumber: initialPage,
    }));
  }, [initialPage]);

  return {
    rwas,
    combinedRwas,
    isSomeFetching,
    reqParams,
    setReqParams,
  };
};
