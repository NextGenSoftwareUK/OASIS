"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form } from "@/components/ui/form";
import { Button } from "@/components/ui/button";
import { FormFieldRenderer } from "@/components/form/FormFieldRenderer";
import { trustSetupSchema, TrustSetupSchema, trustSetupSchemaFields } from "@/schemas/trust/trustSetup.schema";
import { useEffect } from "react";

interface TrustSetupFormProps {
  onComplete: (data: TrustSetupSchema) => void;
  onNext: () => void;
  initialData?: TrustSetupSchema | null;
}

export default function TrustSetupForm({ onComplete, onNext, initialData }: TrustSetupFormProps) {
  const form = useForm<TrustSetupSchema>({
    resolver: zodResolver(trustSetupSchema),
    defaultValues: initialData || {
      trustName: "",
      settlorName: "Quantum Street, Inc.",
      trusteeName: "",
      trustProtector: "",
      qualifiedCustodian: "",
      jurisdiction: "Wyoming",
      trustDuration: 1000,
      trustType: "Statutory Trust",
      minimumPropertyValue: 25000000,
      annualDistributionRate: 90,
      reserveFundRate: 10,
      trusteeFeeRate: 1,
    },
  });

  const onSubmit = (data: TrustSetupSchema) => {
    onComplete(data);
    onNext();
  };

  const handleNext = () => {
    form.handleSubmit(onSubmit)();
  };

  // Auto-generate trust name based on settlor and current date
  useEffect(() => {
    const settlorName = form.getValues("settlorName");
    if (settlorName && !form.getValues("trustName")) {
      const date = new Date();
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, '0');
      const day = String(date.getDate()).padStart(2, '0');
      const trustName = `SFR-AAA-TRUST-FH${year}${month}${day}`;
      form.setValue("trustName", trustName);
    }
  }, [form]);

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-8">
        {trustSetupSchemaFields.map((group, groupIndex) => (
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

        {/* Trust Summary */}
        <div className="mt-8 p-6 bg-blue-900/20 border border-blue-500/30 rounded-lg">
          <h3 className="text-lg font-semibold text-blue-300 mb-4">Trust Summary</h3>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-400">Trust Name:</span>
              <span className="text-white ml-2">{form.watch("trustName") || "Not specified"}</span>
            </div>
            <div>
              <span className="text-gray-400">Settlor:</span>
              <span className="text-white ml-2">{form.watch("settlorName") || "Not specified"}</span>
            </div>
            <div>
              <span className="text-gray-400">Trustee:</span>
              <span className="text-white ml-2">{form.watch("trusteeName") || "Not specified"}</span>
            </div>
            <div>
              <span className="text-gray-400">Jurisdiction:</span>
              <span className="text-white ml-2">{form.watch("jurisdiction")}</span>
            </div>
            <div>
              <span className="text-gray-400">Duration:</span>
              <span className="text-white ml-2">{form.watch("trustDuration")} years</span>
            </div>
            <div>
              <span className="text-gray-400">Distribution Rate:</span>
              <span className="text-white ml-2">{form.watch("annualDistributionRate")}%</span>
            </div>
          </div>
        </div>

        {/* Validation Summary */}
        <div className="mt-6 p-4 bg-yellow-900/20 border border-yellow-500/30 rounded-lg">
          <h4 className="text-sm font-semibold text-yellow-300 mb-2">Wyoming Trust Requirements</h4>
          <ul className="text-xs text-gray-300 space-y-1">
            <li>✓ Trust must be governed by Wyoming law</li>
            <li>✓ Minimum 1,000-year duration for perpetuity</li>
            <li>✓ Qualified custodian required for digital assets</li>
            <li>✓ Trustee must be Wyoming-regulated entity</li>
            <li>✓ Annual distributions to beneficiaries required</li>
          </ul>
        </div>

        <div className="flex justify-end">
          <Button
            type="button"
            onClick={handleNext}
            className="px-8 py-2"
          >
            Continue to Property Details
          </Button>
        </div>
      </form>
    </Form>
  );
}
