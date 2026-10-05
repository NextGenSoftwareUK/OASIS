"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form } from "@/components/ui/form";
import { Button } from "@/components/ui/button";
import { FormFieldRenderer } from "@/components/form/FormFieldRenderer";
import { propertyDetailsSchema, PropertyDetailsSchema, propertyDetailsSchemaFields } from "@/schemas/trust/propertyDetails.schema";
import { useEffect, useState } from "react";
import { TrustSetupSchema } from "@/schemas/trust/trustSetup.schema";

interface PropertyDetailsFormProps {
  onComplete: (data: PropertyDetailsSchema) => void;
  onNext: () => void;
  onPrevious: () => void;
  initialData?: PropertyDetailsSchema | null;
  trustData?: TrustSetupSchema | null;
}

export default function PropertyDetailsForm({ 
  onComplete, 
  onNext, 
  onPrevious, 
  initialData, 
  trustData 
}: PropertyDetailsFormProps) {
  const [tokenSupply, setTokenSupply] = useState(0);
  const [tokenPrice, setTokenPrice] = useState(0);
  const [netIncome, setNetIncome] = useState(0);

  const form = useForm<PropertyDetailsSchema>({
    resolver: zodResolver(propertyDetailsSchema),
    defaultValues: initialData || {
      propertyAddress: "",
      propertyType: "Single Family Residential",
      propertyValue: 0,
      totalSquareFootage: 0,
      annualRentalIncome: 0,
      annualExpenses: 0,
      netIncome: 0,
      titleDocument: undefined,
      appraisalDocument: undefined,
      insuranceDocument: undefined,
      surveyDocument: undefined,
      tokenSupply: 0,
      tokenPrice: 0,
      tokenNaming: "",
    },
  });

  const onSubmit = (data: PropertyDetailsSchema) => {
    onComplete(data);
    onNext();
  };

  const handleNext = () => {
    form.handleSubmit(onSubmit)();
  };

  // Watch form values for calculations
  const propertyValue = form.watch("propertyValue");
  const squareFootage = form.watch("totalSquareFootage");
  const rentalIncome = form.watch("annualRentalIncome");
  const expenses = form.watch("annualExpenses");

  // Calculate derived values
  useEffect(() => {
    if (squareFootage > 0) {
      setTokenSupply(squareFootage);
      form.setValue("tokenSupply", squareFootage);
    }
  }, [squareFootage, form]);

  useEffect(() => {
    if (propertyValue > 0 && tokenSupply > 0) {
      const price = propertyValue / tokenSupply;
      setTokenPrice(price);
      form.setValue("tokenPrice", price);
    }
  }, [propertyValue, tokenSupply, form]);

  useEffect(() => {
    const net = rentalIncome - expenses;
    setNetIncome(net);
    form.setValue("netIncome", net);
  }, [rentalIncome, expenses, form]);

  // Auto-generate token naming convention
  useEffect(() => {
    if (trustData?.trustName && propertyValue > 0 && squareFootage > 0) {
      const address = form.getValues("propertyAddress");
      const addressParts = address.split(" ").slice(0, 3).join("-").toUpperCase();
      const tokenNaming = `${trustData.trustName}-${addressParts}-${propertyValue}-${squareFootage}`;
      form.setValue("tokenNaming", tokenNaming);
    }
  }, [trustData, propertyValue, squareFootage, form]);

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-8">
        {propertyDetailsSchemaFields.map((group, groupIndex) => (
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

        {/* Property Summary */}
        <div className="mt-8 p-6 bg-green-900/20 border border-green-500/30 rounded-lg">
          <h3 className="text-lg font-semibold text-green-300 mb-4">Property Summary</h3>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-400">Property Address:</span>
              <span className="text-white ml-2">{form.watch("propertyAddress") || "Not specified"}</span>
            </div>
            <div>
              <span className="text-gray-400">Property Value:</span>
              <span className="text-white ml-2">${propertyValue.toLocaleString()}</span>
            </div>
            <div>
              <span className="text-gray-400">Square Footage:</span>
              <span className="text-white ml-2">{squareFootage.toLocaleString()} sq ft</span>
            </div>
            <div>
              <span className="text-gray-400">Annual Rental Income:</span>
              <span className="text-white ml-2">${rentalIncome.toLocaleString()}</span>
            </div>
            <div>
              <span className="text-gray-400">Annual Expenses:</span>
              <span className="text-white ml-2">${expenses.toLocaleString()}</span>
            </div>
            <div>
              <span className="text-gray-400">Net Income:</span>
              <span className="text-white ml-2">${netIncome.toLocaleString()}</span>
            </div>
          </div>
        </div>

        {/* Tokenization Preview */}
        <div className="mt-6 p-6 bg-purple-900/20 border border-purple-500/30 rounded-lg">
          <h3 className="text-lg font-semibold text-purple-300 mb-4">Tokenization Preview</h3>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-400">Token Supply:</span>
              <span className="text-white ml-2">{tokenSupply.toLocaleString()} tokens</span>
            </div>
            <div>
              <span className="text-gray-400">Token Price:</span>
              <span className="text-white ml-2">${tokenPrice.toFixed(2)} per token</span>
            </div>
            <div>
              <span className="text-gray-400">Total Value:</span>
              <span className="text-white ml-2">${propertyValue.toLocaleString()}</span>
            </div>
            <div>
              <span className="text-gray-400">Token Naming:</span>
              <span className="text-white ml-2 text-xs">{form.watch("tokenNaming") || "Not generated"}</span>
            </div>
          </div>
        </div>

        {/* Validation Summary */}
        <div className="mt-6 p-4 bg-yellow-900/20 border border-yellow-500/30 rounded-lg">
          <h4 className="text-sm font-semibold text-yellow-300 mb-2">Property Requirements</h4>
          <ul className="text-xs text-gray-300 space-y-1">
            <li className={propertyValue >= 25000000 ? "text-green-400" : "text-red-400"}>
              {propertyValue >= 25000000 ? "✓" : "✗"} Minimum property value: $25,000,000
            </li>
            <li className={squareFootage > 0 ? "text-green-400" : "text-red-400"}>
              {squareFootage > 0 ? "✓" : "✗"} Square footage specified
            </li>
            <li className={rentalIncome > 0 ? "text-green-400" : "text-yellow-400"}>
              {rentalIncome > 0 ? "✓" : "⚠"} Rental income specified
            </li>
            <li className={expenses > 0 ? "text-green-400" : "text-yellow-400"}>
              {expenses > 0 ? "✓" : "⚠"} Expenses specified
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
            Back to Trust Setup
          </Button>
          <Button
            type="button"
            onClick={handleNext}
            disabled={propertyValue < 25000000 || squareFootage <= 0}
            className="px-8 py-2"
          >
            Continue to Token Configuration
          </Button>
        </div>
      </form>
    </Form>
  );
}
