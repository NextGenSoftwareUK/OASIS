"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form } from "@/components/ui/form";
import { Button } from "@/components/ui/button";
import { FormFieldRenderer } from "@/components/form/FormFieldRenderer";
import { tokenConfigurationSchema, TokenConfigurationSchema, tokenConfigurationSchemaFields } from "@/schemas/trust/tokenConfiguration.schema";
import { useEffect, useState } from "react";
import { PropertyDetailsSchema } from "@/schemas/trust/propertyDetails.schema";

interface TokenConfigurationFormProps {
  onComplete: (data: TokenConfigurationSchema) => void;
  onNext: () => void;
  onPrevious: () => void;
  initialData?: TokenConfigurationSchema | null;
  propertyData?: PropertyDetailsSchema | null;
}

export default function TokenConfigurationForm({ 
  onComplete, 
  onNext, 
  onPrevious, 
  initialData, 
  propertyData 
}: TokenConfigurationFormProps) {
  const [tokenAttributes, setTokenAttributes] = useState<Array<{trait_type: string, value: string | number}>>([]);

  const form = useForm<TokenConfigurationSchema>({
    resolver: zodResolver(tokenConfigurationSchema),
    defaultValues: initialData || {
      tokenName: "",
      tokenSymbol: "",
      tokenStandard: "ERC721",
      totalSupply: 0,
      tokenPrice: 0,
      minimumPurchase: 1,
      requiresNotarization: true,
      transferRestrictions: ["Wyoming notarization required", "Beneficiary verification required"],
      tokenMetadata: {
        name: "",
        description: "",
        image: "",
        attributes: [],
      },
      blockchain: "Kadena",
      contractVersion: "1.0.0",
    },
  });

  const onSubmit = (data: TokenConfigurationSchema) => {
    onComplete(data);
    onNext();
  };

  const handleNext = () => {
    form.handleSubmit(onSubmit)();
  };

  // Auto-populate from property data
  useEffect(() => {
    if (propertyData) {
      form.setValue("totalSupply", propertyData.tokenSupply || 0);
      form.setValue("tokenPrice", propertyData.tokenPrice || 0);
      
      // Auto-generate token name and symbol
      if (!form.getValues("tokenName")) {
        const tokenName = propertyData.tokenNaming || "PROPERTY-TOKEN";
        form.setValue("tokenName", tokenName);
      }
      
      if (!form.getValues("tokenSymbol")) {
        const symbol = "PROP";
        form.setValue("tokenSymbol", symbol);
      }

      // Auto-generate metadata
      if (!form.getValues("tokenMetadata.name")) {
        form.setValue("tokenMetadata.name", `Property Token - ${propertyData.propertyAddress}`);
      }
      
      if (!form.getValues("tokenMetadata.description")) {
        form.setValue("tokenMetadata.description", 
          `Fractional ownership token for ${propertyData.propertyAddress}. ` +
          `Each token represents 1 square foot of the property. ` +
          `Total value: $${propertyData.propertyValue?.toLocaleString()}. ` +
          `Total supply: ${propertyData.tokenSupply?.toLocaleString()} tokens.`
        );
      }
    }
  }, [propertyData, form]);

  // Auto-generate token attributes
  useEffect(() => {
    if (propertyData) {
      const attributes = [
        { trait_type: "Property Value", value: propertyData.propertyValue || 0 },
        { trait_type: "Square Footage", value: propertyData.totalSquareFootage || 0 },
        { trait_type: "Property Type", value: propertyData.propertyType || "Single Family Residential" },
        { trait_type: "Annual Rental Income", value: propertyData.annualRentalIncome || 0 },
        { trait_type: "Annual Expenses", value: propertyData.annualExpenses || 0 },
        { trait_type: "Net Income", value: propertyData.netIncome || 0 },
        { trait_type: "Token Price", value: propertyData.tokenPrice || 0 },
        { trait_type: "Trust Duration", value: "1000 years" },
        { trait_type: "Jurisdiction", value: "Wyoming" },
      ];
      
      setTokenAttributes(attributes);
      form.setValue("tokenMetadata.attributes", attributes);
    }
  }, [propertyData, form]);

  const addAttribute = () => {
    const newAttribute = { trait_type: "", value: "" };
    setTokenAttributes([...tokenAttributes, newAttribute]);
  };

  const updateAttribute = (index: number, field: 'trait_type' | 'value', value: string) => {
    const updated = [...tokenAttributes];
    updated[index] = { ...updated[index], [field]: value };
    setTokenAttributes(updated);
    form.setValue("tokenMetadata.attributes", updated);
  };

  const removeAttribute = (index: number) => {
    const updated = tokenAttributes.filter((_, i) => i !== index);
    setTokenAttributes(updated);
    form.setValue("tokenMetadata.attributes", updated);
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-8">
        {tokenConfigurationSchemaFields.map((group, groupIndex) => (
          <div key={groupIndex} className="space-y-6">
            <h3 className="text-xl font-semibold text-white border-b border-gray-600 pb-2">
              {group.title}
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {group.fields.map((field, fieldIndex) => (
                <div key={fieldIndex} className="space-y-2">
                  <FormFieldRenderer
                    fieldType={field.type}
                    form={form}
                    input={field}
                  />
                </div>
              ))}
            </div>
          </div>
        ))}

        {/* Token Attributes Editor */}
        <div className="space-y-6">
          <h3 className="text-xl font-semibold text-white border-b border-gray-600 pb-2">
            Token Attributes
          </h3>
          <div className="space-y-4">
            {tokenAttributes.map((attr, index) => (
              <div key={index} className="flex space-x-4 items-end">
                <div className="flex-1">
                  <label className="block text-sm font-medium text-gray-300 mb-1">
                    Trait Type
                  </label>
                  <input
                    type="text"
                    value={attr.trait_type}
                    onChange={(e) => updateAttribute(index, 'trait_type', e.target.value)}
                    className="w-full px-3 py-2 bg-gray-700 border border-gray-600 rounded-md text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="e.g., Property Value"
                  />
                </div>
                <div className="flex-1">
                  <label className="block text-sm font-medium text-gray-300 mb-1">
                    Value
                  </label>
                  <input
                    type="text"
                    value={attr.value}
                    onChange={(e) => updateAttribute(index, 'value', e.target.value)}
                    className="w-full px-3 py-2 bg-gray-700 border border-gray-600 rounded-md text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="e.g., $1,000,000"
                  />
                </div>
                <Button
                  type="button"
                  onClick={() => removeAttribute(index)}
                  variant="outline"
                  size="sm"
                  className="text-red-400 hover:text-red-300"
                >
                  Remove
                </Button>
              </div>
            ))}
            <Button
              type="button"
              onClick={addAttribute}
              variant="outline"
              className="w-full"
            >
              Add Attribute
            </Button>
          </div>
        </div>

        {/* Token Summary */}
        <div className="mt-8 p-6 bg-purple-900/20 border border-purple-500/30 rounded-lg">
          <h3 className="text-lg font-semibold text-purple-300 mb-4">Token Summary</h3>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-400">Token Name:</span>
              <span className="text-white ml-2">{form.watch("tokenName") || "Not specified"}</span>
            </div>
            <div>
              <span className="text-gray-400">Token Symbol:</span>
              <span className="text-white ml-2">{form.watch("tokenSymbol") || "Not specified"}</span>
            </div>
            <div>
              <span className="text-gray-400">Total Supply:</span>
              <span className="text-white ml-2">{form.watch("totalSupply")?.toLocaleString() || "Not specified"}</span>
            </div>
            <div>
              <span className="text-gray-400">Token Price:</span>
              <span className="text-white ml-2">${form.watch("tokenPrice")?.toFixed(2) || "Not specified"}</span>
            </div>
            <div>
              <span className="text-gray-400">Minimum Purchase:</span>
              <span className="text-white ml-2">{form.watch("minimumPurchase") || "Not specified"} tokens</span>
            </div>
            <div>
              <span className="text-gray-400">Blockchain:</span>
              <span className="text-white ml-2">{form.watch("blockchain")}</span>
            </div>
          </div>
        </div>

        {/* Metadata Preview */}
        <div className="mt-6 p-6 bg-blue-900/20 border border-blue-500/30 rounded-lg">
          <h3 className="text-lg font-semibold text-blue-300 mb-4">Metadata Preview</h3>
          <div className="space-y-2 text-sm">
            <div>
              <span className="text-gray-400">Name:</span>
              <span className="text-white ml-2">{form.watch("tokenMetadata.name") || "Not specified"}</span>
            </div>
            <div>
              <span className="text-gray-400">Description:</span>
              <span className="text-white ml-2">{form.watch("tokenMetadata.description") || "Not specified"}</span>
            </div>
            <div>
              <span className="text-gray-400">Attributes:</span>
              <span className="text-white ml-2">{tokenAttributes.length} attributes</span>
            </div>
          </div>
        </div>

        {/* Validation Summary */}
        <div className="mt-6 p-4 bg-yellow-900/20 border border-yellow-500/30 rounded-lg">
          <h4 className="text-sm font-semibold text-yellow-300 mb-2">Token Requirements</h4>
          <ul className="text-xs text-gray-300 space-y-1">
            <li className={form.watch("tokenName") ? "text-green-400" : "text-red-400"}>
              {form.watch("tokenName") ? "✓" : "✗"} Token name specified
            </li>
            <li className={form.watch("tokenSymbol") ? "text-green-400" : "text-red-400"}>
              {form.watch("tokenSymbol") ? "✓" : "✗"} Token symbol specified
            </li>
            <li className={form.watch("totalSupply") > 0 ? "text-green-400" : "text-red-400"}>
              {form.watch("totalSupply") > 0 ? "✓" : "✗"} Total supply specified
            </li>
            <li className={form.watch("tokenPrice") > 0 ? "text-green-400" : "text-red-400"}>
              {form.watch("tokenPrice") > 0 ? "✓" : "✗"} Token price specified
            </li>
            <li className={tokenAttributes.length > 0 ? "text-green-400" : "text-yellow-400"}>
              {tokenAttributes.length > 0 ? "✓" : "⚠"} Token attributes specified
            </li>
          </ul>
        </div>

        <div className="flex justify-between">
          <Button
            type="button"
            onClick={onPrevious}
            variant="outline"
            className="px-8 py-2"
          >
            Back to Property Details
          </Button>
          <Button
            type="button"
            onClick={handleNext}
            disabled={!form.watch("tokenName") || !form.watch("tokenSymbol") || form.watch("totalSupply") <= 0}
            className="px-8 py-2"
          >
            Continue to Beneficiary Rights
          </Button>
        </div>
      </form>
    </Form>
  );
}
