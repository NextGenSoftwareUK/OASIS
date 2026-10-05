import {
  tokenizeRealEstateSchema,
  tokenizeRealEstateSchemaDefaultValues,
  tokenizeRealEstateSchemaFields,
} from "@/schemas/rwa/tokenizeRealEstate.schema";
import { FieldsByAssetType } from "@/types/util/useTokenizationFieldsByAssetType.type";
import { z } from "zod";

export const getFieldsByAssetType = (type: string): FieldsByAssetType => {
  switch (type) {
    case "RealEstate":
      return {
        schema: tokenizeRealEstateSchema,
        fields: tokenizeRealEstateSchemaFields,
        defaultValues: tokenizeRealEstateSchemaDefaultValues,
      };
    default:
      return {
        schema: z.object({}),
        fields: [],
        defaultValues: {},
      };
  }
};
