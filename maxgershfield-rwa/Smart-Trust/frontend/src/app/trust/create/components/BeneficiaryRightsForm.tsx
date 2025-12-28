"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form } from "@/components/ui/form";
import { Button } from "@/components/ui/button";
import { FormFieldRenderer } from "@/components/form/FormFieldRenderer";
import { beneficiaryRightsSchema, BeneficiaryRightsSchema, beneficiaryRightsSchemaFields } from "@/schemas/trust/beneficiaryRights.schema";

interface BeneficiaryRightsFormProps {
  onComplete: (data: BeneficiaryRightsSchema) => void;
  onNext: () => void;
  onPrevious: () => void;
  initialData?: BeneficiaryRightsSchema | null;
}

export default function BeneficiaryRightsForm({ 
  onComplete, 
  onNext, 
  onPrevious, 
  initialData 
}: BeneficiaryRightsFormProps) {
  const form = useForm<BeneficiaryRightsSchema>({
    resolver: zodResolver(beneficiaryRightsSchema),
    defaultValues: initialData || {
      occupancyRights: false,
      visitationRights: {
        threshold: 30,
        duration: 3,
        restrictions: ["No overnight stays", "Advance notice required", "Trustee supervision required"],
      },
      votingRights: false,
      decisionMaking: false,
      distributionFrequency: "Annual",
      distributionMethod: "Proportional",
      distributionTiming: "01-01",
      informationRights: true,
      auditRights: false,
      transferRights: true,
      transferRestrictions: ["Wyoming notarization required", "Beneficiary verification required"],
      governanceRestrictions: ["No management authority", "No voting rights", "No decision making"],
    },
  });

  const onSubmit = (data: BeneficiaryRightsSchema) => {
    onComplete(data);
    onNext();
  };

  const handleNext = () => {
    form.handleSubmit(onSubmit)();
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-8">
        {beneficiaryRightsSchemaFields.map((group, groupIndex) => (
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

        {/* Rights Summary */}
        <div className="mt-8 p-6 bg-green-900/20 border border-green-500/30 rounded-lg">
          <h3 className="text-lg font-semibold text-green-300 mb-4">Beneficiary Rights Summary</h3>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-400">Occupancy Rights:</span>
              <span className="text-white ml-2">{form.watch("occupancyRights") ? "Yes" : "No"}</span>
            </div>
            <div>
              <span className="text-gray-400">Visitation Threshold:</span>
              <span className="text-white ml-2">{form.watch("visitationRights.threshold")}%</span>
            </div>
            <div>
              <span className="text-gray-400">Visitation Duration:</span>
              <span className="text-white ml-2">{form.watch("visitationRights.duration")} days/year</span>
            </div>
            <div>
              <span className="text-gray-400">Voting Rights:</span>
              <span className="text-white ml-2">{form.watch("votingRights") ? "Yes" : "No"}</span>
            </div>
            <div>
              <span className="text-gray-400">Decision Making:</span>
              <span className="text-white ml-2">{form.watch("decisionMaking") ? "Yes" : "No"}</span>
            </div>
            <div>
              <span className="text-gray-400">Distribution Frequency:</span>
              <span className="text-white ml-2">{form.watch("distributionFrequency")}</span>
            </div>
            <div>
              <span className="text-gray-400">Distribution Method:</span>
              <span className="text-white ml-2">{form.watch("distributionMethod")}</span>
            </div>
            <div>
              <span className="text-gray-400">Distribution Timing:</span>
              <span className="text-white ml-2">{form.watch("distributionTiming")}</span>
            </div>
          </div>
        </div>

        {/* Additional Rights */}
        <div className="mt-6 p-6 bg-blue-900/20 border border-blue-500/30 rounded-lg">
          <h3 className="text-lg font-semibold text-blue-300 mb-4">Additional Rights</h3>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-400">Information Rights:</span>
              <span className="text-white ml-2">{form.watch("informationRights") ? "Yes" : "No"}</span>
            </div>
            <div>
              <span className="text-gray-400">Audit Rights:</span>
              <span className="text-white ml-2">{form.watch("auditRights") ? "Yes" : "No"}</span>
            </div>
            <div>
              <span className="text-gray-400">Transfer Rights:</span>
              <span className="text-white ml-2">{form.watch("transferRights") ? "Yes" : "No"}</span>
            </div>
            <div>
              <span className="text-gray-400">Transfer Restrictions:</span>
              <span className="text-white ml-2">{form.watch("transferRestrictions")?.length || 0} restrictions</span>
            </div>
          </div>
        </div>

        {/* Wyoming Trust Compliance */}
        <div className="mt-6 p-4 bg-yellow-900/20 border border-yellow-500/30 rounded-lg">
          <h4 className="text-sm font-semibold text-yellow-300 mb-2">Wyoming Trust Compliance</h4>
          <ul className="text-xs text-gray-300 space-y-1">
            <li className={!form.watch("occupancyRights") ? "text-green-400" : "text-red-400"}>
              {!form.watch("occupancyRights") ? "✓" : "✗"} No occupancy rights (Wyoming requirement)
            </li>
            <li className={!form.watch("votingRights") ? "text-green-400" : "text-red-400"}>
              {!form.watch("votingRights") ? "✓" : "✗"} No voting rights (Wyoming requirement)
            </li>
            <li className={!form.watch("decisionMaking") ? "text-green-400" : "text-red-400"}>
              {!form.watch("decisionMaking") ? "✓" : "✗"} No decision making (Wyoming requirement)
            </li>
            <li className={form.watch("visitationRights.threshold") >= 30 ? "text-green-400" : "text-yellow-400"}>
              {form.watch("visitationRights.threshold") >= 30 ? "✓" : "⚠"} Visitation threshold ≥ 30%
            </li>
            <li className={form.watch("visitationRights.duration") <= 3 ? "text-green-400" : "text-yellow-400"}>
              {form.watch("visitationRights.duration") <= 3 ? "✓" : "⚠"} Visitation duration ≤ 3 days/year
            </li>
            <li className={form.watch("distributionFrequency") === "Annual" ? "text-green-400" : "text-yellow-400"}>
              {form.watch("distributionFrequency") === "Annual" ? "✓" : "⚠"} Annual distributions recommended
            </li>
          </ul>
        </div>

        {/* Rights Matrix */}
        <div className="mt-6 p-6 bg-gray-700/20 border border-gray-500/30 rounded-lg">
          <h3 className="text-lg font-semibold text-gray-300 mb-4">Rights Matrix</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-600">
                  <th className="text-left py-2 text-gray-300">Right</th>
                  <th className="text-center py-2 text-gray-300">Status</th>
                  <th className="text-left py-2 text-gray-300">Description</th>
                </tr>
              </thead>
              <tbody className="space-y-2">
                <tr className="border-b border-gray-700">
                  <td className="py-2 text-gray-300">Occupancy</td>
                  <td className="text-center py-2">
                    <span className={`px-2 py-1 rounded text-xs ${
                      form.watch("occupancyRights") ? "bg-red-900 text-red-300" : "bg-green-900 text-green-300"
                    }`}>
                      {form.watch("occupancyRights") ? "Denied" : "Allowed"}
                    </span>
                  </td>
                  <td className="py-2 text-gray-400">Beneficiaries cannot occupy the property</td>
                </tr>
                <tr className="border-b border-gray-700">
                  <td className="py-2 text-gray-300">Visitation</td>
                  <td className="text-center py-2">
                    <span className="px-2 py-1 rounded text-xs bg-blue-900 text-blue-300">
                      Limited
                    </span>
                  </td>
                  <td className="py-2 text-gray-400">
                    {form.watch("visitationRights.threshold")}%+ ownership, {form.watch("visitationRights.duration")} days/year
                  </td>
                </tr>
                <tr className="border-b border-gray-700">
                  <td className="py-2 text-gray-300">Voting</td>
                  <td className="text-center py-2">
                    <span className={`px-2 py-1 rounded text-xs ${
                      form.watch("votingRights") ? "bg-red-900 text-red-300" : "bg-green-900 text-green-300"
                    }`}>
                      {form.watch("votingRights") ? "Denied" : "Allowed"}
                    </span>
                  </td>
                  <td className="py-2 text-gray-400">No governance voting rights</td>
                </tr>
                <tr className="border-b border-gray-700">
                  <td className="py-2 text-gray-300">Decision Making</td>
                  <td className="text-center py-2">
                    <span className={`px-2 py-1 rounded text-xs ${
                      form.watch("decisionMaking") ? "bg-red-900 text-red-300" : "bg-green-900 text-green-300"
                    }`}>
                      {form.watch("decisionMaking") ? "Denied" : "Allowed"}
                    </span>
                  </td>
                  <td className="py-2 text-gray-400">No management decision authority</td>
                </tr>
                <tr className="border-b border-gray-700">
                  <td className="py-2 text-gray-300">Distributions</td>
                  <td className="text-center py-2">
                    <span className="px-2 py-1 rounded text-xs bg-green-900 text-green-300">
                      Allowed
                    </span>
                  </td>
                  <td className="py-2 text-gray-400">
                    {form.watch("distributionFrequency")} {form.watch("distributionMethod")} distributions
                  </td>
                </tr>
                <tr className="border-b border-gray-700">
                  <td className="py-2 text-gray-300">Information</td>
                  <td className="text-center py-2">
                    <span className={`px-2 py-1 rounded text-xs ${
                      form.watch("informationRights") ? "bg-green-900 text-green-300" : "bg-red-900 text-red-300"
                    }`}>
                      {form.watch("informationRights") ? "Allowed" : "Denied"}
                    </span>
                  </td>
                  <td className="py-2 text-gray-400">Access to trust information</td>
                </tr>
                <tr className="border-b border-gray-700">
                  <td className="py-2 text-gray-300">Transfers</td>
                  <td className="text-center py-2">
                    <span className={`px-2 py-1 rounded text-xs ${
                      form.watch("transferRights") ? "bg-green-900 text-green-300" : "bg-red-900 text-red-300"
                    }`}>
                      {form.watch("transferRights") ? "Allowed" : "Denied"}
                    </span>
                  </td>
                  <td className="py-2 text-gray-400">Token transfer rights with restrictions</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div className="flex justify-between">
          <Button
            type="button"
            onClick={onPrevious}
            variant="outline"
            className="px-8 py-2"
          >
            Back to Token Configuration
          </Button>
          <Button
            type="button"
            onClick={handleNext}
            className="px-8 py-2"
          >
            Continue to Contract Generation
          </Button>
        </div>
      </form>
    </Form>
  );
}
