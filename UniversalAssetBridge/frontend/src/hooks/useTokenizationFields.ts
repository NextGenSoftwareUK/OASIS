import {
  tokenizeBaseSchema,
  tokenizeBaseSchemaDefaultValues,
  tokenizeBaseSchemaFields,
} from "@/schemas/rwa/tokenizeBase.schema";
import { getFieldsByAssetType } from "@/utils/getFieldsByAssetType.util";
import { useMemo } from "react";

export const useTokenizationFields = (assetType?: string) => {
  const selectedAssetType = useMemo(() => {
    return assetType || "";
  }, [assetType]);

  const tokenizeSchema = tokenizeBaseSchema.merge(
    getFieldsByAssetType(selectedAssetType).schema
  );

  const tokenizeFields = [
    ...tokenizeBaseSchemaFields,
    ...getFieldsByAssetType(selectedAssetType).fields,
  ];

  const defaultTokenizeValues = {
    ...tokenizeBaseSchemaDefaultValues,
    ...getFieldsByAssetType(selectedAssetType).defaultValues,
  };

  return {
    tokenizeSchema,
    tokenizeFields,
    defaultTokenizeValues,
  };
};
