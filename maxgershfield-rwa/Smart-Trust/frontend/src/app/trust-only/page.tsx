"use client";

import TrustCreationWizard from "@/app/trust/create/components/TrustCreationWizard";

export default function TrustOnlyPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-purple-900">
      <div className="container mx-auto px-4 py-8">
        <div className="text-center mb-8">
          <h1 className="text-4xl font-bold text-white mb-4">
            Wyoming Smart Trust Creation Wizard
          </h1>
          <p className="text-xl text-gray-300 max-w-2xl mx-auto">
            Create your Wyoming Statutory Trust with 1,000-year perpetuity and fractional ownership tokens.
          </p>
        </div>
        
        <TrustCreationWizard />
      </div>
    </div>
  );
}




