"use server";

import TrustCreationWizard from "@/app/trust/create/components/TrustCreationWizard";
import Header from "@/components/header/Header";
import { SearchParams } from "@/types/params.type";

export default async function page({ searchParams }: SearchParams) {
  return (
    <>
      <Header searchParams={searchParams} />
      <div className="mt-20 lg:mt-16 md:mt-10! xxs:mt-5!">
        <div className="grid grid-cols-11 gap-20 mb-10">
          <div className="col-span-5">
            <div className="space-y-4">
              <h2 className="text-2xl font-semibold text-white">Trust Creation Steps</h2>
              <div className="space-y-2">
                <div className="flex items-center space-x-3">
                  <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center text-white text-sm font-medium">1</div>
                  <span className="text-gray-300">Trust Setup</span>
                </div>
                <div className="flex items-center space-x-3">
                  <div className="w-8 h-8 bg-gray-600 rounded-full flex items-center justify-center text-white text-sm font-medium">2</div>
                  <span className="text-gray-300">Property Details</span>
                </div>
                <div className="flex items-center space-x-3">
                  <div className="w-8 h-8 bg-gray-600 rounded-full flex items-center justify-center text-white text-sm font-medium">3</div>
                  <span className="text-gray-300">Token Configuration</span>
                </div>
                <div className="flex items-center space-x-3">
                  <div className="w-8 h-8 bg-gray-600 rounded-full flex items-center justify-center text-white text-sm font-medium">4</div>
                  <span className="text-gray-300">Beneficiary Rights</span>
                </div>
                <div className="flex items-center space-x-3">
                  <div className="w-8 h-8 bg-gray-600 rounded-full flex items-center justify-center text-white text-sm font-medium">5</div>
                  <span className="text-gray-300">Contract Generation</span>
                </div>
              </div>
            </div>
          </div>
          <div className="col-span-6">
            <h1 className="text-5xl font-semibold leading-14">
              Create Your Wyoming Smart Trust
            </h1>
            <p className="p-sm text-secondary mt-4 max-w-[600px]">
              Transform your high-value residential property into a tokenized trust agreement. 
              Our system guides you through creating a Wyoming Statutory Trust with 1,000-year 
              perpetuity and fractional ownership tokens.
            </p>
            <div className="mt-6 p-4 bg-blue-900/20 border border-blue-500/30 rounded-lg">
              <h3 className="text-lg font-semibold text-blue-300 mb-2">Trust Requirements</h3>
              <ul className="text-sm text-gray-300 space-y-1">
                <li>• Minimum property value: $25,000,000</li>
                <li>• Single Family Residential properties only</li>
                <li>• 1 token = 1 square foot of property</li>
                <li>• 1,000-year trust duration</li>
                <li>• Wyoming law compliance</li>
              </ul>
            </div>
          </div>
        </div>
        <TrustCreationWizard />
      </div>
    </>
  );
}
