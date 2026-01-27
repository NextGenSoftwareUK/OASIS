'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Textarea } from '@/components/ui/textarea';
import { ContractEditor } from '@/components/contract-editor';
import { PlaygroundActions } from '@/components/playground-actions';
import { generateWithAI } from '@/lib/api-client';
import { ArrowLeft, Sparkles, Loader2, AlertCircle, Code } from 'lucide-react';
import Link from 'next/link';

const EXAMPLE_DESCRIPTION = `Create a token vesting contract that:
- Locks tokens for a specified period
- Allows gradual release over time  
- Supports multiple beneficiaries
- Has an admin who can update vesting schedules
- Includes proper access controls and error handling

This is for a DAO that needs to vest team tokens over a 2-year period with a 6-month cliff.`;

export default function AIGeneratorPage() {
  const [description, setDescription] = useState('');
  const [additionalContext, setAdditionalContext] = useState('');
  const [generating, setGenerating] = useState(false);
  const [compiling, setCompiling] = useState(false);
  const [deploying, setDeploying] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [generatedContract, setGeneratedContract] = useState<{
    code: string;
    zipBlob: Blob;
    programName?: string;
  } | null>(null);
  const [compiledBlob, setCompiledBlob] = useState<Blob | null>(null);
  const [deploymentResult, setDeploymentResult] = useState<any>(null);

  // Derived state
  const isCompiled = !!compiledBlob;
  const isDeployed = !!deploymentResult;

  const handleGenerate = async () => {
    if (!description.trim()) {
      setError('Please describe your contract');
      return;
    }

    try {
      setGenerating(true);
      setError(null);

      // Generate contract with AI
      const result = await generateWithAI(
        description,
        'solana',
        additionalContext || undefined
      );
      
      setGeneratedContract({
        ...result,
        programName: 'ai_generated_contract',
      });
    } catch (err: any) {
      console.error('AI generation error:', err);
      setError(err.message || 'Failed to generate contract. Please try again or simplify your description.');
    } finally {
      setGenerating(false);
    }
  };

  const loadExample = () => {
    setDescription(EXAMPLE_DESCRIPTION);
    setAdditionalContext('This contract will be deployed on Solana devnet for testing before mainnet deployment.');
  };

  const handleCompile = async () => {
    if (!generatedContract) return;

    try {
      setCompiling(true);
      setError(null);

      const { compileContract } = await import('@/lib/api-client');
      const compiled = await compileContract(
        generatedContract.zipBlob,
        'Rust'
      );

      setCompiledBlob(compiled);
      alert('✅ Contract compiled successfully!\n\nYou can now deploy it.');
    } catch (err: any) {
      console.error('Compilation error:', err);
      setError(err.message || 'Compilation failed. Please check the contract code.');
    } finally {
      setCompiling(false);
    }
  };

  const handleDeploy = async () => {
    if (!compiledBlob && !generatedContract) return;

    try {
      setDeploying(true);
      setError(null);

      let blobToDeploy = compiledBlob;
      
      if (!blobToDeploy && generatedContract) {
        const { compileContract } = await import('@/lib/api-client');
        blobToDeploy = await compileContract(
          generatedContract.zipBlob,
          'Rust'
        );
        setCompiledBlob(blobToDeploy);
      }

      const { deployContract } = await import('@/lib/api-client');
      const result = await deployContract(blobToDeploy!, 'Rust');

      setDeploymentResult(result);
      alert(`✅ Contract deployed successfully!\n\nProgram ID: ${result.programId || result.address || 'See console'}\n\nCheck browser console for full details.`);
      console.log('Deployment result:', result);
    } catch (err: any) {
      console.error('Deployment error:', err);
      setError(err.message || 'Deployment failed. Make sure your API is running and configured correctly.');
    } finally {
      setDeploying(false);
    }
  };

  return (
    <div className="min-h-screen">
      {/* Header */}
      <header className="border-b border-[var(--card-border)]/30 bg-[rgba(5,8,18,0.8)] backdrop-blur-sm sticky top-0 z-50">
        <div className="container mx-auto px-4 py-4 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href="/">
              <Button variant="ghost" size="sm">
                <ArrowLeft className="w-4 h-4 mr-2" />
                Back
              </Button>
            </Link>
            <div className="flex items-center gap-2">
              <Sparkles className="w-6 h-6 text-purple-400" />
              <span className="text-xl font-bold text-[var(--foreground)]">AI Generator</span>
            </div>
          </div>
          
          <Link href="/x402-dashboard" className="text-sm text-[var(--accent)] hover:text-[var(--accent)]/80 transition font-medium">
            x402 Dashboard
          </Link>
        </div>
      </header>

      <div className="container mx-auto px-4 py-8">
        <div className="max-w-7xl mx-auto">
          {/* Instructions */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-[var(--foreground)] mb-2">
              AI-Powered Generation
            </h1>
            <p className="text-[var(--muted)] max-w-3xl">
              Describe your smart contract in plain English. Our AI will generate production-ready code with best practices and security patterns included.
            </p>
          </div>

          {/* Scroll Hint */}
          <div className="flex items-center justify-center gap-2 mb-4 text-sm text-[var(--muted)]">
            <span>← Scroll horizontally to see all panels →</span>
          </div>

          {/* Three-column horizontal scroll layout */}
          <div className="overflow-x-auto scrollbar-thin">
            <div className="flex gap-6 min-w-max pb-4">
              {/* Column 1: Input Section */}
              <div className="w-[500px] flex-shrink-0 space-y-6">
              <Card className="glass-card gradient-ring relative overflow-hidden">
                <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(129,140,248,0.1),transparent_70%)]" />
                <div className="relative">
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Sparkles className="w-5 h-5 text-purple-400" />
                      Describe Your Contract
                    </CardTitle>
                    <CardDescription>
                      Tell us what you want to build in plain English
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div>
                      <label className="text-sm font-semibold text-[var(--foreground)] mb-2 block">
                        Contract Description *
                      </label>
                      <Textarea
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        placeholder="Describe what you want your contract to do..."
                        className="min-h-[200px]"
                      />
                      <p className="text-xs text-[var(--muted)] mt-2">
                        Be specific about functionality, access controls, and any special requirements
                      </p>
                    </div>

                    <div>
                      <label className="text-sm font-semibold text-[var(--foreground)] mb-2 block">
                        Additional Context (optional)
                      </label>
                      <Textarea
                        value={additionalContext}
                        onChange={(e) => setAdditionalContext(e.target.value)}
                        placeholder="Any additional context or requirements..."
                        className="min-h-[100px]"
                      />
                    </div>

                    {error && (
                      <div className="rounded-xl border border-red-500/50 bg-red-500/10 p-4 text-sm text-red-400 flex items-start gap-2">
                        <AlertCircle className="w-4 h-4 flex-shrink-0 mt-0.5" />
                        <span>{error}</span>
                      </div>
                    )}

                    <div className="flex gap-2">
                      <Button
                        onClick={loadExample}
                        variant="outline"
                        className="flex-1"
                      >
                        Load Example
                      </Button>
                      <Button
                        onClick={() => {
                          setDescription('');
                          setAdditionalContext('');
                          setError(null);
                        }}
                        variant="ghost"
                        className="flex-1"
                      >
                        Clear
                      </Button>
                    </div>

                    <Button
                      onClick={handleGenerate}
                      disabled={generating || !description.trim()}
                      className="w-full bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700"
                      size="lg"
                    >
                      {generating ? (
                        <>
                          <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                          Generating with AI... (~30s)
                        </>
                      ) : (
                        <>
                          <Sparkles className="w-4 h-4 mr-2" />
                          Generate Contract
                        </>
                      )}
                    </Button>

                    {generating && (
                      <div className="rounded-xl border border-[var(--accent)]/30 bg-[var(--accent-soft)] p-4 text-sm text-[var(--accent)]">
                        <div className="flex items-center gap-2 mb-2">
                          <Loader2 className="w-4 h-4 animate-spin" />
                          <span className="font-semibold">AI is thinking...</span>
                        </div>
                        <p className="text-xs">
                          Analyzing your requirements and generating optimized code with security best practices
                        </p>
                      </div>
                    )}
                  </CardContent>
                </div>
              </Card>

              {/* Tips */}
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">💡 Tips for Better Results</CardTitle>
                </CardHeader>
                <CardContent>
                  <ul className="text-sm text-[var(--muted)] space-y-2">
                    <li>• Be specific about the contract's purpose and functionality</li>
                    <li>• Mention any access control requirements (admin, user roles)</li>
                    <li>• Specify any time-based logic (vesting periods, lockups)</li>
                    <li>• Include error handling and edge case requirements</li>
                    <li>• Mention security considerations if applicable</li>
                  </ul>
                </CardContent>
              </Card>
              </div>

              {/* Column 2: Generated Contract */}
              <div className="w-[700px] flex-shrink-0 space-y-6">
                {!generatedContract ? (
                  <Card className="flex items-center justify-center h-full min-h-[600px]">
                    <div className="text-center p-8">
                      <Sparkles className="w-16 h-16 text-[var(--muted)] mx-auto mb-4" />
                      <p className="text-[var(--muted)]">
                        Your AI-generated contract will appear here
                      </p>
                      <p className="text-xs text-[var(--muted)] mt-2">
                        Generation typically takes 20-30 seconds
                      </p>
                    </div>
                  </Card>
                ) : (
                  <>
                    {/* AI Summary */}
                    <Card className="border-purple-500/30 bg-gradient-to-br from-purple-500/10 to-pink-500/10">
                      <CardHeader>
                        <CardTitle className="flex items-center gap-2 text-base">
                          <Sparkles className="w-5 h-5 text-purple-400" />
                          AI Generation Summary
                        </CardTitle>
                      </CardHeader>
                      <CardContent>
                        <div className="space-y-2 text-sm text-[var(--muted)]">
                          <div className="flex items-start gap-2">
                            <AlertCircle className="w-4 h-4 text-purple-400 mt-0.5 flex-shrink-0" />
                            <div>
                              <p className="text-[var(--foreground)] font-semibold mb-1">Please review carefully</p>
                              <p className="text-xs">
                                AI-generated code should be reviewed and tested before deployment. Verify all logic, security measures, and edge cases.
                              </p>
                            </div>
                          </div>
                        </div>
                      </CardContent>
                    </Card>

                    {/* Generated Contract */}
                    <Card className="h-full">
                      <CardHeader>
                        <CardTitle>AI-Generated Contract</CardTitle>
                        <CardDescription>
                          AI-generated • Solana (Rust/Anchor)
                        </CardDescription>
                      </CardHeader>
                      <CardContent>
                        <ContractEditor
                          code={generatedContract.code}
                          language="rust"
                          readOnly
                          height="600px"
                        />
                      </CardContent>
                    </Card>
                  </>
                )}
              </div>

              {/* Column 3: Build & Deploy Panel */}
              {generatedContract && (
                <div className="w-[400px] flex-shrink-0">
                  <Card className="h-full">
                    <CardHeader>
                      <CardTitle className="flex items-center gap-2">
                        <Code className="w-5 h-5 text-[var(--accent)]" />
                        Build & Deploy
                      </CardTitle>
                      <CardDescription>
                        Compile and deploy your contract
                      </CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4" style={{ height: '600px', overflow: 'auto' }}>
                          {/* AI Warning */}
                          <div className="rounded-xl border border-purple-500/30 bg-purple-500/10 p-4">
                            <div className="flex items-center gap-2 mb-2">
                              <Sparkles className="w-4 h-4 text-purple-400" />
                              <span className="font-semibold text-sm text-purple-400">AI-Generated Code</span>
                            </div>
                            <p className="text-xs text-[var(--muted)]">
                              Please review carefully before deploying. Verify all logic and security measures.
                            </p>
                          </div>

                          {/* Step 1: Compile */}
                          <div className="space-y-3">
                            <div className="flex items-center justify-between">
                              <h4 className="font-semibold text-sm text-[var(--foreground)]">
                                Step 1: Compile
                              </h4>
                              {isCompiled && (
                                <span className="text-xs text-green-400">✓ Complete</span>
                              )}
                            </div>
                            <Button
                              onClick={handleCompile}
                              disabled={compiling || isCompiled || isDeployed}
                              variant={isCompiled ? "secondary" : "default"}
                              className="w-full"
                              size="lg"
                            >
                              {compiling ? (
                                <>
                                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                                  Compiling... (~30-60s)
                                </>
                              ) : isCompiled ? (
                                <>Compiled ✓</>
                              ) : (
                                <>Compile Contract</>
                              )}
                            </Button>
                            <p className="text-xs text-[var(--muted)]">
                              Compiles your contract via API using Docker
                            </p>
                          </div>

                          {/* Compilation Status */}
                          {compiledBlob && (
                            <div className="rounded-xl border border-green-500/30 bg-green-500/10 p-4">
                              <div className="flex items-center gap-2 mb-2">
                                <div className="w-2 h-2 rounded-full bg-green-400" />
                                <span className="font-semibold text-sm text-green-400">Compilation Successful</span>
                              </div>
                              <p className="text-xs text-[var(--muted)]">
                                Your contract has been compiled and is ready to deploy
                              </p>
                            </div>
                          )}

                          {/* Divider */}
                          <div className="border-t border-[var(--card-border)]" />

                          {/* Step 2: Deploy */}
                          <div className="space-y-3">
                            <div className="flex items-center justify-between">
                              <h4 className="font-semibold text-sm text-[var(--foreground)]">
                                Step 2: Deploy
                              </h4>
                              {isDeployed && (
                                <span className="text-xs text-green-400">✓ Complete</span>
                              )}
                            </div>
                            <Button
                              onClick={handleDeploy}
                              disabled={deploying || isDeployed}
                              variant={isDeployed ? "secondary" : "default"}
                              className="w-full"
                              size="lg"
                            >
                              {deploying ? (
                                <>
                                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                                  Deploying... Please wait
                                </>
                              ) : isDeployed ? (
                                <>Deployed ✓</>
                              ) : (
                                <>Deploy to Devnet</>
                              )}
                            </Button>
                            <p className="text-xs text-[var(--muted)]">
                              {isCompiled 
                                ? 'Deploys your compiled contract to Solana devnet'
                                : 'Auto-compiles then deploys to Solana devnet'}
                            </p>
                          </div>

                          {/* Deployment Result */}
                          {deploymentResult && (
                            <div className="rounded-xl border border-green-500/30 bg-green-500/10 p-4 space-y-3">
                              <div className="flex items-center gap-2 mb-2">
                                <div className="w-2 h-2 rounded-full bg-green-400" />
                                <span className="font-semibold text-sm text-green-400">Deployment Successful</span>
                              </div>
                              
                              {deploymentResult.programId && (
                                <div>
                                  <div className="text-xs text-[var(--muted)] mb-1">Program ID:</div>
                                  <div className="font-mono text-xs bg-[rgba(8,11,26,0.6)] p-2 rounded border border-[var(--card-border)] break-all text-[var(--accent)]">
                                    {deploymentResult.programId}
                                  </div>
                                </div>
                              )}
                              
                              {deploymentResult.transactionHash && (
                                <div>
                                  <div className="text-xs text-[var(--muted)] mb-1">Transaction:</div>
                                  <div className="font-mono text-xs bg-[rgba(8,11,26,0.6)] p-2 rounded border border-[var(--card-border)] break-all text-[var(--accent)]">
                                    {deploymentResult.transactionHash}
                                  </div>
                                </div>
                              )}
                            </div>
                          )}

                          {/* Divider */}
                          <div className="border-t border-[var(--card-border)]" />

                          {/* Other Actions */}
                          <div className="space-y-3">
                            <h4 className="font-semibold text-sm text-[var(--foreground)]">
                              Other Actions
                            </h4>
                            <p className="text-xs text-[var(--muted)]">
                              Alternative ways to test and deploy your contract
                            </p>
                      </div>
                    </CardContent>
                  </Card>
                </div>
              )}
            </div>
          </div>

          {/* Playground Actions Below */}
          {generatedContract && (
            <div className="mt-8">
              <PlaygroundActions
                contractCode={generatedContract.code}
                contractZip={generatedContract.zipBlob}
                programName={generatedContract.programName}
                language="rust"
              />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

