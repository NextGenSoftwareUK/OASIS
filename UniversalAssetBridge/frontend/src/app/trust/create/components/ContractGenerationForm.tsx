"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Download, Copy, CheckCircle, AlertCircle, Loader2, Eye } from "lucide-react";
import { TrustSetupSchema } from "@/schemas/trust/trustSetup.schema";
import { PropertyDetailsSchema } from "@/schemas/trust/propertyDetails.schema";
import { TokenConfigurationSchema } from "@/schemas/trust/tokenConfiguration.schema";
import { BeneficiaryRightsSchema } from "@/schemas/trust/beneficiaryRights.schema";

interface ContractGenerationFormProps {
  onGenerate: () => void;
  onPrevious: () => void;
  isGenerating: boolean;
  generatedContract: string;
  trustData?: TrustSetupSchema | null;
  propertyData?: PropertyDetailsSchema | null;
  tokenData?: TokenConfigurationSchema | null;
  beneficiaryData?: BeneficiaryRightsSchema | null;
}

export default function ContractGenerationForm({
  onGenerate,
  onPrevious,
  isGenerating,
  generatedContract,
  trustData,
  propertyData,
  tokenData,
  beneficiaryData,
}: ContractGenerationFormProps) {
  const [copied, setCopied] = useState(false);
  const [contractHash, setContractHash] = useState<string>("");

  const handleCopyContract = async () => {
    try {
      await navigator.clipboard.writeText(generatedContract);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (error) {
      console.error('Failed to copy contract:', error);
    }
  };

  const handleDownloadContract = () => {
    const blob = new Blob([generatedContract], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${trustData?.trustName || 'trust'}-contract.sol`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const handleUploadToIPFS = async () => {
    try {
      // Simulate IPFS upload
      const hash = `Qm${Math.random().toString(36).substring(2, 15)}${Math.random().toString(36).substring(2, 15)}`;
      setContractHash(hash);
    } catch (error) {
      console.error('Failed to upload to IPFS:', error);
    }
  };

  const generateContractData = () => {
    return {
      contractType: "WyomingTrustTokenization",
      blockchain: "Kadena",
      trustData,
      propertyData,
      tokenData,
      beneficiaryData,
      generatedAt: new Date().toISOString(),
    };
  };

  return (
    <div className="space-y-8">
      {/* Data Summary */}
      <div className="space-y-6">
        <h3 className="text-xl font-semibold text-white border-b border-gray-600 pb-2">
          Trust Data Summary
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Trust Information */}
          <div className="p-4 bg-blue-900/20 border border-blue-500/30 rounded-lg">
            <h4 className="text-lg font-semibold text-blue-300 mb-3">Trust Information</h4>
            <div className="space-y-2 text-sm">
              <div>
                <span className="text-gray-400">Trust Name:</span>
                <span className="text-white ml-2">{trustData?.trustName || "Not specified"}</span>
              </div>
              <div>
                <span className="text-gray-400">Settlor:</span>
                <span className="text-white ml-2">{trustData?.settlorName || "Not specified"}</span>
              </div>
              <div>
                <span className="text-gray-400">Trustee:</span>
                <span className="text-white ml-2">{trustData?.trusteeName || "Not specified"}</span>
              </div>
              <div>
                <span className="text-gray-400">Duration:</span>
                <span className="text-white ml-2">{trustData?.trustDuration || 0} years</span>
              </div>
              <div>
                <span className="text-gray-400">Distribution Rate:</span>
                <span className="text-white ml-2">{trustData?.annualDistributionRate || 0}%</span>
              </div>
            </div>
          </div>

          {/* Property Information */}
          <div className="p-4 bg-green-900/20 border border-green-500/30 rounded-lg">
            <h4 className="text-lg font-semibold text-green-300 mb-3">Property Information</h4>
            <div className="space-y-2 text-sm">
              <div>
                <span className="text-gray-400">Address:</span>
                <span className="text-white ml-2">{propertyData?.propertyAddress || "Not specified"}</span>
              </div>
              <div>
                <span className="text-gray-400">Value:</span>
                <span className="text-white ml-2">${propertyData?.propertyValue?.toLocaleString() || "Not specified"}</span>
              </div>
              <div>
                <span className="text-gray-400">Square Footage:</span>
                <span className="text-white ml-2">{propertyData?.totalSquareFootage?.toLocaleString() || "Not specified"} sq ft</span>
              </div>
              <div>
                <span className="text-gray-400">Net Income:</span>
                <span className="text-white ml-2">${propertyData?.netIncome?.toLocaleString() || "Not specified"}</span>
              </div>
              <div>
                <span className="text-gray-400">Token Supply:</span>
                <span className="text-white ml-2">{propertyData?.tokenSupply?.toLocaleString() || "Not specified"}</span>
              </div>
            </div>
          </div>

          {/* Token Information */}
          <div className="p-4 bg-purple-900/20 border border-purple-500/30 rounded-lg">
            <h4 className="text-lg font-semibold text-purple-300 mb-3">Token Information</h4>
            <div className="space-y-2 text-sm">
              <div>
                <span className="text-gray-400">Token Name:</span>
                <span className="text-white ml-2">{tokenData?.tokenName || "Not specified"}</span>
              </div>
              <div>
                <span className="text-gray-400">Token Symbol:</span>
                <span className="text-white ml-2">{tokenData?.tokenSymbol || "Not specified"}</span>
              </div>
              <div>
                <span className="text-gray-400">Total Supply:</span>
                <span className="text-white ml-2">{tokenData?.totalSupply?.toLocaleString() || "Not specified"}</span>
              </div>
              <div>
                <span className="text-gray-400">Token Price:</span>
                <span className="text-white ml-2">${tokenData?.tokenPrice?.toFixed(2) || "Not specified"}</span>
              </div>
              <div>
                <span className="text-gray-400">Blockchain:</span>
                <span className="text-white ml-2">{tokenData?.blockchain || "Not specified"}</span>
              </div>
            </div>
          </div>

          {/* Beneficiary Rights */}
          <div className="p-4 bg-yellow-900/20 border border-yellow-500/30 rounded-lg">
            <h4 className="text-lg font-semibold text-yellow-300 mb-3">Beneficiary Rights</h4>
            <div className="space-y-2 text-sm">
              <div>
                <span className="text-gray-400">Occupancy:</span>
                <span className="text-white ml-2">{beneficiaryData?.occupancyRights ? "Yes" : "No"}</span>
              </div>
              <div>
                <span className="text-gray-400">Visitation:</span>
                <span className="text-white ml-2">{beneficiaryData?.visitationRights?.threshold || 0}% threshold</span>
              </div>
              <div>
                <span className="text-gray-400">Voting:</span>
                <span className="text-white ml-2">{beneficiaryData?.votingRights ? "Yes" : "No"}</span>
              </div>
              <div>
                <span className="text-gray-400">Distributions:</span>
                <span className="text-white ml-2">{beneficiaryData?.distributionFrequency || "Not specified"}</span>
              </div>
              <div>
                <span className="text-gray-400">Transfers:</span>
                <span className="text-white ml-2">{beneficiaryData?.transferRights ? "Yes" : "No"}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Contract Generation */}
      <div className="space-y-6">
        <h3 className="text-xl font-semibold text-white border-b border-gray-600 pb-2">
          Smart Contract Generation
        </h3>

        {!generatedContract ? (
          <div className="p-6 bg-gray-700/20 border border-gray-500/30 rounded-lg text-center">
            <div className="space-y-4">
              <div className="w-16 h-16 bg-blue-600 rounded-full flex items-center justify-center mx-auto">
                <AlertCircle className="w-8 h-8 text-white" />
              </div>
              <h4 className="text-lg font-semibold text-white">Ready to Generate Contract</h4>
              <p className="text-gray-400 max-w-md mx-auto">
                Click the button below to generate your Wyoming Trust smart contract. 
                This will create a Solidity contract that implements all the trust terms and tokenization logic.
              </p>
              <Button
                onClick={onGenerate}
                disabled={isGenerating}
                className="px-8 py-2"
              >
                {isGenerating ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    Generating Contract...
                  </>
                ) : (
                  "Generate Smart Contract"
                )}
              </Button>
            </div>
          </div>
        ) : (
          <div className="space-y-4">
            {/* Success Message */}
            <div className="p-4 bg-green-900/20 border border-green-500/30 rounded-lg">
              <div className="flex items-center space-x-3">
                <CheckCircle className="w-6 h-6 text-green-400" />
                <div>
                  <h4 className="text-lg font-semibold text-green-300">Contract Generated Successfully!</h4>
                  <p className="text-sm text-gray-300">
                    Your Wyoming Trust smart contract has been generated and is ready for deployment.
                  </p>
                </div>
              </div>
            </div>

            {/* Contract Actions */}
            <div className="flex space-x-4">
              <Button
                onClick={handleCopyContract}
                variant="outline"
                className="flex items-center space-x-2"
              >
                {copied ? <CheckCircle className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                <span>{copied ? "Copied!" : "Copy Contract"}</span>
              </Button>
              <Button
                onClick={handleDownloadContract}
                variant="outline"
                className="flex items-center space-x-2"
              >
                <Download className="w-4 h-4" />
                <span>Download Contract</span>
              </Button>
              <Button
                onClick={handleUploadToIPFS}
                variant="outline"
                className="flex items-center space-x-2"
              >
                <span>Upload to IPFS</span>
              </Button>
              <Button
                onClick={() => {
                  const newWindow = window.open('', '_blank', 'width=800,height=600,scrollbars=yes');
                  if (newWindow) {
                    newWindow.document.write(`
                      <html>
                        <head>
                          <title>Complete Smart Contract</title>
                          <style>
                            body { 
                              font-family: 'Courier New', monospace; 
                              background: #1a1a1a; 
                              color: #e0e0e0; 
                              padding: 20px; 
                              margin: 0;
                              white-space: pre-wrap;
                              font-size: 14px;
                              line-height: 1.5;
                            }
                            .header {
                              background: #2a2a2a;
                              padding: 15px;
                              margin: -20px -20px 20px -20px;
                              border-bottom: 1px solid #444;
                            }
                            .title {
                              font-size: 18px;
                              font-weight: bold;
                              margin-bottom: 5px;
                            }
                            .subtitle {
                              font-size: 12px;
                              color: #888;
                            }
                          </style>
                        </head>
                        <body>
                          <div class="header">
                            <div class="title">Wyoming Trust Tokenization Contract</div>
                            <div class="subtitle">Generated: ${new Date().toLocaleString()}</div>
                          </div>
                          ${generatedContract}
                        </body>
                      </html>
                    `);
                    newWindow.document.close();
                  }
                }}
                variant="outline"
                className="flex items-center space-x-2"
              >
                <Eye className="w-4 h-4" />
                <span>View Full Contract</span>
              </Button>
            </div>

            {/* Contract Preview */}
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h4 className="text-lg font-semibold text-white">Contract Preview</h4>
                <div className="text-sm text-gray-400">
                  {generatedContract.length.toLocaleString()} characters
                </div>
              </div>
              <div className="bg-gray-900 border border-gray-600 rounded-lg p-4 max-h-[600px] overflow-y-auto">
                <pre className="text-sm text-gray-300 whitespace-pre-wrap font-mono leading-relaxed">
                  {generatedContract}
                </pre>
              </div>
            </div>

            {/* Contract Hash */}
            {contractHash && (
              <div className="p-4 bg-blue-900/20 border border-blue-500/30 rounded-lg">
                <h4 className="text-sm font-semibold text-blue-300 mb-2">IPFS Hash</h4>
                <p className="text-sm text-gray-300 font-mono">{contractHash}</p>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Next Steps */}
      <div className="space-y-6">
        <h3 className="text-xl font-semibold text-white border-b border-gray-600 pb-2">
          Next Steps
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="p-4 bg-gray-700/20 border border-gray-500/30 rounded-lg">
            <h4 className="text-lg font-semibold text-white mb-2">1. Contract Review</h4>
            <p className="text-sm text-gray-400">
              Review the generated contract to ensure it matches your trust requirements and Wyoming law compliance.
            </p>
          </div>
          <div className="p-4 bg-gray-700/20 border border-gray-500/30 rounded-lg">
            <h4 className="text-lg font-semibold text-white mb-2">2. Compilation</h4>
            <p className="text-sm text-gray-400">
              Compile the contract to generate bytecode and ABI for deployment to the Kadena blockchain.
            </p>
          </div>
          <div className="p-4 bg-gray-700/20 border border-gray-500/30 rounded-lg">
            <h4 className="text-lg font-semibold text-white mb-2">3. Deployment</h4>
            <p className="text-sm text-gray-400">
              Deploy the contract to Kadena testnet for testing, then to mainnet for production use.
            </p>
          </div>
        </div>
      </div>

      {/* Navigation */}
      <div className="flex justify-between">
        <Button
          onClick={onPrevious}
          variant="outline"
          className="px-8 py-2"
        >
          Back to Beneficiary Rights
        </Button>
        
        {generatedContract && (
          <Button
            onClick={() => {
              // Store the generated contract in sessionStorage for the deployment page
              sessionStorage.setItem('generatedContract', generatedContract);
              sessionStorage.setItem('trustData', JSON.stringify(trustData));
              sessionStorage.setItem('propertyData', JSON.stringify(propertyData));
              sessionStorage.setItem('tokenData', JSON.stringify(tokenData));
              sessionStorage.setItem('beneficiaryData', JSON.stringify(beneficiaryData));
              window.location.href = '/trust/deploy';
            }}
            className="px-8 py-2"
          >
            Continue to Deployment
          </Button>
        )}
      </div>
    </div>
  );
}
