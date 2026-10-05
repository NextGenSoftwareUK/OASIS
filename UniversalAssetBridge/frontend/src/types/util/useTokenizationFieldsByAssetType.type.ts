import { TokenizeRealEstateSchema } from "@/schemas/rwa/tokenizeRealEstate.schema";
import { FormFieldGroup } from "@/types/form/formField.type";
import { AnyZodObject, ZodObject } from "zod";

export type FieldsByAssetType = {
  schema: ZodObject<any> | AnyZodObject;
  fields: FormFieldGroup[] | [];
  defaultValues: TokenizeRealEstateSchema | object;
};
