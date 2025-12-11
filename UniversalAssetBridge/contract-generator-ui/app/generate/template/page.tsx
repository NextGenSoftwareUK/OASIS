'use client';

import { useState, useMemo } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Textarea } from '@/components/ui/textarea';
import { ContractEditor } from '@/components/contract-editor';
import { PlaygroundActions } from '@/components/playground-actions';
import { useConsoleLogger } from '@/components/ui/mini-console';
import { generateContract, compileContract, getCacheStats, PaymentRequiredError, type CacheStats } from '@/lib/api-client';
import { SOLANA_TEMPLATES, getTemplate } from '@/lib/templates';
import { PaymentModal } from '@/components/payment-modal';
import { CreditsPurchaseModal } from '@/components/credits-purchase-modal';
import { CreditBalance } from '@/components/credit-balance';
import { getPhantomWallet } from '@/lib/solana-wallet';
import { ArrowLeft, Code, Loader2 } from 'lucide-react';
import Link from 'next/link';
import { useEffect, useRef } from 'react';

const EXAMPLE_SPEC = SOLANA_TEMPLATES[0].spec; // Default to first template

export default function TemplateGeneratorPage() {
  const [jsonSpec, setJsonSpec] = useState(JSON.stringify(EXAMPLE_SPEC, null, 2));
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
  const [walletKeypair, setWalletKeypair] = useState<string>('');
  const [cacheStats, setCacheStats] = useState<CacheStats | null>(null);
  
  // Payment modal state
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [paymentToken, setPaymentToken] = useState<string>('');
  const [pendingOperation, setPendingOperation] = useState<'generate' | 'compile' | null>(null);
  
  // Credits system state
  const [showCreditsModal, setShowCreditsModal] = useState(false);
  const [walletAddress, setWalletAddress] = useState<string | null>(null);
  const [creditBalance, setCreditBalance] = useState<number>(0);
  const [creditsRefreshTrigger, setCreditsRefreshTrigger] = useState<number>(0);
  
  // IDL and testing state
  const [idl, setIdl] = useState<any>(null);
  const [selectedInstruction, setSelectedInstruction] = useState<string>('');
  const [instructionArgs, setInstructionArgs] = useState<Record<string, string>>({});
  const [testingTx, setTestingTx] = useState(false);
  const [testResult, setTestResult] = useState<{ success: boolean; message: string; signature?: string } | null>(null);

  // Console logger for compilation progress
  const { logs, isActive, setIsActive, log, clearLogs } = useConsoleLogger();
  
  // Separate console logger for deployment progress
  const { 
    logs: deployLogs, 
    isActive: isDeployActive, 
    setIsActive: setIsDeployActive, 
    log: deployLog, 
    clearLogs: clearDeployLogs 
  } = useConsoleLogger();
  
  // Refs for cache stats polling
  const cacheStatsInterval = useRef<NodeJS.Timeout | null>(null);
  const initialCacheStats = useRef<CacheStats | null>(null);

  // Derived state
  const isCompiled = !!compiledBlob;
  const isDeployed = !!deploymentResult;
  
  // Extract deployment info from result
  const deploymentInfo = useMemo(() => {
    if (!deploymentResult) return null;
    const data = deploymentResult.data || deploymentResult;
    return {
      programId: data.contractAddress || data.ContractAddress || data.programId || data.address || 'Unknown',
      txHash: data.transactionHash || data.TransactionHash || 'n/a'
    };
  }, [deploymentResult]);

  // Poll cache stats during compilation
  const startCacheStatsPolling = async () => {
    // Get initial stats
    const initial = await getCacheStats();
    initialCacheStats.current = initial;
    setCacheStats(initial);

    if (initial.enabled) {
      log.data(`📊 Cache: ${initial.cacheHitRate?.toFixed(1)}% hit rate, ${initial.cacheSize}`);
    }

    // Poll every 3 seconds
    cacheStatsInterval.current = setInterval(async () => {
      const stats = await getCacheStats();
      setCacheStats(stats);

      if (stats.enabled && initialCacheStats.current?.enabled) {
        const newCompiles = (stats.compileRequests || 0) - (initialCacheStats.current.compileRequests || 0);
        const newHits = (stats.cacheHits || 0) - (initialCacheStats.current.cacheHits || 0);
        const newMisses = (stats.cacheMisses || 0) - (initialCacheStats.current.cacheMisses || 0);

        if (newCompiles > 0) {
          const sessionHitRate = newHits > 0 ? ((newHits / (newHits + newMisses)) * 100) : 0;
          log.data(
            `⚡ Compiled: ${newCompiles} deps | Cache: ${newHits} hits, ${newMisses} misses (${sessionHitRate.toFixed(0)}% hit rate)`
          );
        }
      }
    }, 3000);
  };

  const stopCacheStatsPolling = () => {
    if (cacheStatsInterval.current) {
      clearInterval(cacheStatsInterval.current);
      cacheStatsInterval.current = null;
    }
  };

  // Check for wallet connection on mount
  useEffect(() => {
    const phantom = getPhantomWallet();
    if (phantom && phantom.connected && phantom.publicKey) {
      setWalletAddress(phantom.publicKey.toString());
    }
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      stopCacheStatsPolling();
    };
  }, []);

  const handleGenerate = async () => {
    try {
      setGenerating(true);
      setError(null);

      // Parse JSON spec
      const spec = JSON.parse(jsonSpec);

      // Generate contract (with payment token and wallet address if available)
      const result = await generateContract(
        spec, 
        'Rust', 
        paymentToken || undefined,
        walletAddress || undefined
      );
      setGeneratedContract(result);
      setPaymentToken(''); // Clear payment token after use
      setCreditsRefreshTrigger(prev => prev + 1); // Refresh credit balance
    } catch (err: any) {
      console.error('Generation error:', err);
      
      // Check if payment is required (PaymentRequiredError)
      if (err instanceof PaymentRequiredError || err.name === 'PaymentRequiredError') {
        setPendingOperation('generate');
        setShowPaymentModal(true);
        setError('Payment required to generate contract');
      } else {
        setError(err.message || 'Failed to generate contract. Please check your JSON specification.');
      }
    } finally {
      setGenerating(false);
    }
  };

  const handlePaymentSuccess = (token: string) => {
    setPaymentToken(token);
    setShowPaymentModal(false);
    
    // Retry the operation that required payment
    if (pendingOperation === 'generate') {
      setTimeout(() => handleGenerate(), 500);
    } else if (pendingOperation === 'compile') {
      setTimeout(() => handleCompile(), 500);
    }
    
    setPendingOperation(null);
  };

  const handleCreditsPurchaseSuccess = (newBalance: number) => {
    setCreditBalance(newBalance);
    setCreditsRefreshTrigger(prev => prev + 1);
    setShowCreditsModal(false);
    
    // Retry the pending operation with credits
    if (pendingOperation === 'generate') {
      setTimeout(() => handleGenerate(), 500);
    } else if (pendingOperation === 'compile') {
      setTimeout(() => handleCompile(), 500);
    }
    
    setPendingOperation(null);
  };

  const handleValidate = () => {
    try {
      JSON.parse(jsonSpec);
      alert('✅ JSON is valid!');
    } catch (err: any) {
      alert(`❌ Invalid JSON:\n\n${err.message}`);
    }
  };

  const loadTemplate = (templateId: string) => {
    const template = getTemplate(templateId);
    if (template) {
      setJsonSpec(JSON.stringify(template.spec, null, 2));
      setError(null);
      setGeneratedContract(null); // Clear previous generation
      setCompiledBlob(null);
      setDeploymentResult(null);
    }
  };

  const handleCompile = async () => {
    if (!generatedContract) return;

    try {
      setCompiling(true);
      setError(null);
      clearLogs();
      setIsActive(true);

      log.info('🚀 Starting compilation process...', '🚀');
      log.data(`📦 Project size: ${(generatedContract.zipBlob.size / 1024).toFixed(2)} KB`);
      
      // Start polling cache stats
      await startCacheStatsPolling();
      
      log.info('⏳ First compilation may take 2-3 minutes...');
      log.info('📥 Downloading Rust dependencies...');
      log.info('🔨 Building with Anchor...');

      // Compile the generated contract ZIP (with payment token and wallet address if available)
      const compiled = await compileContract(
        generatedContract.zipBlob,
        'Rust',
        'contract.zip',
        1200000, // timeout
        paymentToken || undefined,
        walletAddress || undefined
      );

      setCompiledBlob(compiled);
      
      // Stop polling and show final cache stats
      stopCacheStatsPolling();
      
      // 80s ASCII art success message
      log.success('', '');
      log.success('╔═══════════════════════════════════════════════════════╗', '');
      log.success('║                                                       ║', '');
      log.success('║   ░█▀▀░█░█░█▀▀░█▀▀░█▀▀░█▀▀░█▀▀░█                     ║', '');
      log.success('║   ░▀▀█░█░█░█░░░█░░░█▀▀░▀▀█░▀▀█░▀                     ║', '');
      log.success('║   ░▀▀▀░▀▀▀░▀▀▀░▀▀▀░▀▀▀░▀▀▀░▀▀▀░▀                     ║', '');
      log.success('║                                                       ║', '');
      log.success('║        🎮  C O M P I L A T I O N  🎮                 ║', '');
      log.success('║                 C O M P L E T E                       ║', '');
      log.success('║                                                       ║', '');
      log.success('╚═══════════════════════════════════════════════════════╝', '');
      log.success('', '');
      
      if (cacheStats?.enabled && initialCacheStats.current?.enabled) {
        const totalCompiles = (cacheStats.compileRequests || 0) - (initialCacheStats.current.compileRequests || 0);
        const totalHits = (cacheStats.cacheHits || 0) - (initialCacheStats.current.cacheHits || 0);
        const timeSaved = totalHits * (cacheStats.averageCompileTime || 0);
        
        log.data(`📦 Compiled artifact: ${(compiled.size / 1024).toFixed(2)} KB`);
        log.data(`⚡ Cache performance: ${totalHits}/${totalCompiles} hits (saved ~${timeSaved.toFixed(1)}s)`);
      } else {
        log.data(`📦 Compiled artifact: ${(compiled.size / 1024).toFixed(2)} KB`);
      }
      
      log.info('✨ Ready to deploy to Solana devnet');
      
      setIsActive(false);
      alert('✅ Contract compiled successfully!\n\nYou can now deploy it.');
    } catch (err: any) {
      console.error('Compilation error:', err);
      
      // Stop polling on error
      stopCacheStatsPolling();
      
      // Check if payment is required
      if (err instanceof PaymentRequiredError || err.name === 'PaymentRequiredError') {
        setPendingOperation('compile');
        setShowPaymentModal(true);
        setError('Payment required to compile contract (0.15 SOL)');
        log.error('💳 Payment required for compilation', '💳');
        setIsActive(false);
        return;
      }
      
      log.error('❌ Compilation failed', '💥');
      log.error(err.message);
      
      // Show more helpful error message
      let errorMsg = 'Compilation failed. ';
      
      if (err.message.includes('Cargo.toml')) {
        errorMsg += 'The generated project structure may be incomplete. This is likely an issue with the contract generation. Please try:\n\n1. Download the ZIP and check if Cargo.toml exists\n2. Try generating a different contract\n3. Check if your Smart Contract Generator API is working correctly';
        log.warning('⚠️ Missing Cargo.toml - check project structure');
      } else if (err.message.includes('timeout')) {
        errorMsg += err.message;
        log.error('⏱️ Compilation took too long');
      } else {
        errorMsg += err.message;
      }
      
      setError(errorMsg);
      setIsActive(false);
      alert('❌ ' + errorMsg);
    } finally {
      setCompiling(false);
    }
  };

  const handleDeploy = async () => {
    if (!compiledBlob && !generatedContract) return;

    // Check if wallet keypair is provided
    if (!walletKeypair.trim()) {
      alert('⚠️ Wallet keypair required!\n\nPlease provide a wallet keypair JSON in the deployment section to deploy your contract.');
      return;
    }

    try {
      setDeploying(true);
      setError(null);
      setIsDeployActive(true);
      clearDeployLogs();

      deployLog.info('🚀 Starting deployment process...', '🚀');
      deployLog.data('🔐 Wallet keypair configured');

      // If not compiled yet, compile first
      let blobToDeploy = compiledBlob;
      
      if (!blobToDeploy && generatedContract) {
        deployLog.info('⚙️ Compiling contract before deployment...');
        const { compileContract } = await import('@/lib/api-client');
        blobToDeploy = await compileContract(
          generatedContract.zipBlob,
          'Rust'
        );
        setCompiledBlob(blobToDeploy);
        deployLog.success('✅ Compilation complete');
      }
      
      deployLog.info('📤 Deploying to Solana devnet...');
      deployLog.data('⏳ This may take 3-5 minutes (writing 200+ chunks)...');

      // Extract IDL and bytecode (.so file) from compiled ZIP
      deployLog.info('📄 Extracting deployment files...');
      const JSZip = (await import('jszip')).default;
      const zip = await JSZip.loadAsync(blobToDeploy!);
      
      const files = Object.keys(zip.files);
      deployLog.data(`Found ${files.length} files in artifact`);
      
      // Find IDL file (typically target/idl/*.json)
      let idlContent = '';
      const idlFile = files.find(f => f.includes('idl') && f.endsWith('.json'));
      if (idlFile) {
        const file = zip.file(idlFile);
        if (file) {
          idlContent = await file.async('string');
          deployLog.data(`✓ Found IDL: ${idlFile}`);
        }
      }
      
      // Find bytecode file - try multiple patterns
      let soFile = files.find(f => f.endsWith('.so'));
      
      if (!soFile) {
        // Log available files for debugging
        deployLog.warning('⚠️ Could not find .so file. Available files:');
        files.slice(0, 10).forEach(f => deployLog.data(`  - ${f}`));
        if (files.length > 10) {
          deployLog.data(`  ... and ${files.length - 10} more files`);
        }
        throw new Error('Could not find compiled bytecode (.so file) in the compiled artifact');
      }
      
      const soFileObj = zip.file(soFile);
      if (!soFileObj) {
        throw new Error('Could not extract bytecode file');
      }
      
      const bytecodeBlob = await soFileObj.async('blob');
      const bytecodeFilename = soFile.split('/').pop() || 'contract.so';
      deployLog.data(`✓ Found bytecode: ${bytecodeFilename} (${(bytecodeBlob.size / 1024).toFixed(2)} KB)`);

      // Deploy the compiled contract with wallet keypair and schema
      deployLog.info('🚀 Starting buffer-based deployment...');
      deployLog.data('   Creating buffer account...');
      deployLog.data('   Initializing buffer...');
      deployLog.data('   Writing chunks (this will take 2-3 minutes)...');
      
      const { deployContract } = await import('@/lib/api-client');
      
      // Show progress updates every 15 seconds
      const progressInterval = setInterval(() => {
        deployLog.data('   ⏳ Still writing chunks to Solana buffer...');
      }, 15000);
      
      try {
        const result = await deployContract(
          bytecodeBlob, 
          'Rust', 
          walletKeypair,
          idlContent || undefined,
          bytecodeFilename  // Pass the .so filename
        );
        
        clearInterval(progressInterval);
        
        // Debug: Log the entire response to see field names
        console.log('🔍 Full deployment result:', result);
        console.log('🔍 Available keys:', Object.keys(result));
        
        // Unwrap the Result<T> wrapper if present
        const data = result.data || result;
        console.log('🔍 Data object:', data);
        console.log('🔍 Data keys:', Object.keys(data));
        
        const programId = data.contractAddress || data.ContractAddress || data.programId || data.address || 'Unknown';
        const txHash = data.transactionHash || data.TransactionHash || 'n/a';
        
        console.log('🔍 Final extracted - Program ID:', programId);
        console.log('🔍 Final extracted - TX Hash:', txHash);
        
        setDeploymentResult(data);
        
        // Parse and store IDL for testing
        if (idlContent) {
          try {
            const parsedIdl = JSON.parse(idlContent);
            setIdl(parsedIdl);
            deployLog.data(`✓ IDL loaded: ${parsedIdl.instructions?.length || 0} instructions available`);
          } catch (err) {
            console.error('Failed to parse IDL:', err);
            deployLog.warning('⚠️ IDL found but could not be parsed');
          }
        }
        
        deployLog.success('✅ Deployment successful!', '🎉');
        deployLog.data(`📝 Program ID: ${programId}`);
        if (txHash && txHash !== 'n/a') {
          deployLog.data(`🔗 Transaction: ${txHash}`);
        }
        
        setIsDeployActive(false);
        alert(`✅ Contract deployed successfully!\n\nProgram ID: ${programId}\n\nCheck deployment console for full details.`);
        console.log('Deployment result:', data);
      } catch (deployErr: any) {
        clearInterval(progressInterval);
        throw deployErr; // Re-throw to outer catch
      }
    } catch (err: any) {
      console.error('Deployment error:', err);
      
      deployLog.error('❌ Deployment failed', '💥');
      deployLog.error(err.message || 'Unknown deployment error');
      
      setError(err.message || 'Deployment failed. Make sure your API is running, wallet keypair is valid, and configured correctly.');
      setIsDeployActive(false);
    } finally {
      setDeploying(false);
    }
  };

  const handleWalletFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (event) => {
      try {
        const content = event.target?.result as string;
        // Validate it's valid JSON
        JSON.parse(content);
        setWalletKeypair(content);
      } catch (err) {
        alert('❌ Invalid wallet keypair file. Must be valid JSON.');
      }
    };
    reader.readAsText(file);
  };

  const handleDownloadCompiled = () => {
    if (!compiledBlob) return;

    const url = URL.createObjectURL(compiledBlob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${generatedContract?.programName || 'contract'}_compiled.zip`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
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
              <Code className="w-6 h-6 text-[var(--accent)]" />
              <span className="text-xl font-bold text-[var(--foreground)]">Template Generator</span>
            </div>
          </div>
          
          <div className="flex items-center gap-6">
            <Link href="/x402-dashboard" className="text-sm text-[var(--accent)] hover:text-[var(--accent)]/80 transition font-medium">
              x402 Dashboard
            </Link>
            
            {/* Credit Balance */}
            <CreditBalance 
              walletAddress={walletAddress}
              onPurchaseClick={() => setShowCreditsModal(true)}
              refreshTrigger={creditsRefreshTrigger}
            />
          </div>
        </div>
      </header>

      <div className="container mx-auto px-4 py-8">
        <div className="max-w-[1800px] mx-auto">
          {/* Instructions */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-[var(--foreground)] mb-2">
              Template-Based Generation
            </h1>
            <p className="text-[var(--muted)] max-w-3xl">
              Create smart contracts using JSON specifications. This method provides consistent, predictable output perfect for production deployments.
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
              <Card>
                <CardHeader>
                  <CardTitle>Contract Specification</CardTitle>
                  <CardDescription>
                    Enter your contract specification in JSON format
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <Textarea
                    value={jsonSpec}
                    onChange={(e) => setJsonSpec(e.target.value)}
                    placeholder="Enter JSON specification..."
                    className="font-mono text-xs min-h-[400px]"
                  />

                  {error && (
                    <div className="rounded-xl border border-red-500/50 bg-red-500/10 p-4 text-sm text-red-400">
                      {error}
                    </div>
                  )}

                  <div className="flex gap-2">
                    <Button
                      onClick={handleValidate}
                      variant="outline"
                      className="flex-1"
                    >
                      Validate JSON
                    </Button>
                    <Button
                      onClick={() => setJsonSpec(JSON.stringify(EXAMPLE_SPEC, null, 2))}
                      variant="ghost"
                      className="flex-1"
                    >
                      Load Example
                    </Button>
                  </div>

                  <Button
                    onClick={handleGenerate}
                    disabled={generating}
                    className="w-full"
                    size="lg"
                  >
                    {generating ? (
                      <>
                        <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                        Generating Contract...
                      </>
                    ) : (
                      <>Generate Contract</>
                    )}
                  </Button>
                </CardContent>
              </Card>

              {/* Template Examples */}
              <Card>
                <CardHeader>
                  <CardTitle>Template Library</CardTitle>
                  <CardDescription>
                    Click to load common contract patterns
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-2">
                  {SOLANA_TEMPLATES.map((template) => (
                    <button
                      key={template.id}
                      onClick={() => loadTemplate(template.id)}
                      className="w-full text-left rounded-lg border border-[var(--card-border)] bg-[rgba(8,11,26,0.6)] p-3 text-sm hover:bg-[var(--accent-soft)] hover:border-[var(--accent)]/40 transition"
                    >
                      <div className="font-semibold text-[var(--foreground)]">{template.name}</div>
                      <div className="text-xs text-[var(--muted)]">{template.description}</div>
                    </button>
                  ))}
                </CardContent>
              </Card>
              </div>

              {/* Column 2: Generated Contract */}
              <div className="w-[700px] flex-shrink-0">
                {!generatedContract ? (
                  <Card className="flex items-center justify-center h-full min-h-[600px]">
                    <div className="text-center p-8">
                      <Code className="w-16 h-16 text-[var(--muted)] mx-auto mb-4" />
                      <p className="text-[var(--muted)]">
                        Your generated contract will appear here
                      </p>
                    </div>
                  </Card>
                ) : (
                  <Card className="h-full">
                    <CardHeader>
                      <CardTitle>Generated Contract</CardTitle>
                      <CardDescription>
                        {generatedContract.programName || 'contract'} • Solana (Rust/Anchor)
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
                )}
              </div>

              {/* Column 3: Compile Panel */}
              {generatedContract && (
                <div className="w-[700px] flex-shrink-0">
                  <Card className="h-full">
                    <CardHeader>
                      <CardTitle className="flex items-center gap-2">
                        <Code className="w-5 h-5 text-[var(--accent)]" />
                        Compile
                      </CardTitle>
                      <CardDescription>
                        Compile your contract to bytecode
                      </CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4 flex flex-col" style={{ height: '600px' }}>
                      <div className="space-y-4 flex-shrink-0">
                          {/* Compile Section */}
                          <div className="space-y-3">
                            <div className="flex items-center justify-between">
                              <h4 className="font-semibold text-sm text-[var(--foreground)]">
                                Compile Contract
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
                              {compiling 
                                ? 'First build may take 2-3 min (downloads dependencies). Subsequent builds are faster (~30s).'
                                : 'Compiles your contract via API using Anchor'}
                            </p>
                          </div>

                          {/* Compilation Status */}
                          {compiledBlob && (
                            <div className="rounded-xl border border-green-500/30 bg-green-500/10 p-4 space-y-3">
                              <div className="flex items-center gap-2">
                                <div className="w-2 h-2 rounded-full bg-green-400" />
                                <span className="font-semibold text-sm text-green-400">Compilation Successful</span>
                              </div>
                              <p className="text-xs text-[var(--muted)]">
                                Your contract has been compiled to bytecode
                              </p>
                              
                              {/* Action Buttons */}
                              <div className="flex gap-2 pt-2">
                                <Button
                                  onClick={handleDownloadCompiled}
                                  variant="secondary"
                                  size="sm"
                                  className="w-full"
                                >
                                  <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-2">
                                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                                    <polyline points="7 10 12 15 17 10"/>
                                    <line x1="12" x2="12" y1="15" y2="3"/>
                                  </svg>
                                  Download Compiled Contract
                                </Button>
                              </div>
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
                      </div>

                      {/* Compilation Console - Embedded */}
                      {(logs.length > 0 || isActive) && (
                        <div className="flex-1 min-h-0 flex flex-col border-t border-[var(--card-border)] pt-4">
                          <div className="flex items-center justify-between mb-3">
                            <div className="flex items-center gap-2">
                              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-[var(--accent)]">
                                <polyline points="4 17 10 11 4 5"/>
                                <line x1="12" x2="20" y1="19" y2="19"/>
                              </svg>
                              <h4 className="font-semibold text-sm text-[var(--foreground)]">
                                Console
                              </h4>
                            </div>
                            {logs.length > 0 && (
                              <button
                                onClick={clearLogs}
                                className="text-xs text-[var(--muted)] hover:text-[var(--foreground)] transition"
                              >
                                Clear
                              </button>
                            )}
                          </div>
                          <div className="flex-1 overflow-y-auto rounded-lg border border-[var(--card-border)] bg-[rgba(8,11,26,0.4)] p-3 font-mono text-xs">
                            {logs.map((log, i) => (
                              <div
                                key={i}
                                className={`py-1 ${
                                  log.type === 'error' ? 'text-red-400' :
                                  log.type === 'success' ? 'text-green-400' :
                                  log.type === 'warning' ? 'text-yellow-400' :
                                  log.type === 'data' ? 'text-blue-400' :
                                  'text-[var(--muted)]'
                                }`}
                              >
                                <span className="text-[var(--muted)] mr-2">
                                  {log.timestamp.toLocaleTimeString()}
                                </span>
                                {log.icon && <span className="mr-2">{log.icon}</span>}
                                {log.message}
                              </div>
                            ))}
                            {isActive && logs.length === 0 && (
                              <div className="text-[var(--muted)] animate-pulse">
                                Waiting for output...
                              </div>
                            )}
                          </div>
                        </div>
                      )}
                    </CardContent>
                  </Card>
                </div>
              )}

              {/* Deploy Card - Separate Section */}
              {compiledBlob && (
                <div className="w-[700px] flex-shrink-0">
                  <Card className="glass-card gradient-ring h-full">
                    <CardHeader>
                      <CardTitle className="flex items-center gap-2">
                        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-[var(--accent)]">
                          <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>
                        </svg>
                        Deploy
                      </CardTitle>
                      <CardDescription>
                        Deploy your compiled contract to Solana devnet
                      </CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4 flex flex-col" style={{ height: '600px' }}>
                      {!deploymentResult && (
                        <div className="space-y-4 flex-shrink-0">
                          {/* Wallet Configuration */}
                          <div className="space-y-3">
                            <label className="text-sm font-semibold text-[var(--foreground)] flex items-center gap-2">
                              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-[var(--accent)]">
                                <path d="M19 7V4a1 1 0 0 0-1-1H5a2 2 0 0 0 0 4h15a1 1 0 0 1 1 1v4h-3a2 2 0 0 0 0 4h3a1 1 0 0 0 1-1v-2a1 1 0 0 0-1-1"/>
                                <path d="M3 5v14a2 2 0 0 0 2 2h15a1 1 0 0 0 1-1v-4"/>
                              </svg>
                              Your Wallet Keypair (Pays for Gas)
                            </label>
                            <div className="text-xs text-[var(--muted)] bg-blue-500/10 border border-blue-500/30 rounded-lg p-3">
                              <div className="font-semibold text-blue-400 mb-1">💡 What to provide:</div>
                              <div>Your Solana wallet keypair (the one with SOL to pay for deployment).</div>
                              <div className="mt-2 font-mono text-xs">
                                Generate: <span className="text-[var(--accent)]">solana-keygen new</span>
                              </div>
                              <div className="mt-1 text-xs opacity-75">
                                Note: The program keypair is auto-included in compiled files
                              </div>
                            </div>

                            {/* File Upload */}
                            <div className="flex gap-2">
                              <input
                                type="file"
                                accept=".json"
                                onChange={handleWalletFileUpload}
                                className="hidden"
                                id="wallet-file-upload-inline"
                              />
                              <label
                                htmlFor="wallet-file-upload-inline"
                                className="flex-1 cursor-pointer inline-flex items-center justify-center gap-2 rounded-lg border border-[var(--card-border)] bg-[rgba(8,11,26,0.6)] hover:bg-[rgba(8,11,26,0.8)] px-4 py-3 text-sm font-medium transition-colors"
                              >
                                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                  <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                                  <polyline points="17 8 12 3 7 8"/>
                                  <line x1="12" x2="12" y1="3" y2="15"/>
                                </svg>
                                Upload keypair.json
                              </label>
                            </div>

                            {/* Or paste */}
                            <div className="text-xs text-[var(--muted)] text-center">or paste JSON:</div>
                            <Textarea
                              value={walletKeypair}
                              onChange={(e) => setWalletKeypair(e.target.value)}
                              placeholder='[234,71,174,117,90,168,40,163,...]'
                              className="font-mono text-xs h-24 resize-none"
                              disabled={deploying}
                            />
                            <p className="text-xs text-[var(--muted)]">
                              {walletKeypair.trim() ? (
                                <span className="text-green-400">✓ Wallet configured</span>
                              ) : (
                                <span>Paste your Solana wallet keypair array</span>
                              )}
                            </p>
                          </div>

                          {/* Deploy Button */}
                          <Button
                            onClick={handleDeploy}
                            disabled={deploying || isDeployed || !walletKeypair.trim()}
                            variant="default"
                            className="w-full"
                            size="lg"
                          >
                            {deploying ? (
                              <>
                                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                                Deploying... (~3-5 minutes)
                              </>
                            ) : (
                              <>
                                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-2">
                                  <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>
                                </svg>
                                Deploy to Solana Devnet
                              </>
                            )}
                          </Button>
                        </div>
                      )}

                      {/* Deployment Success - Code Editor Style */}
                      {deploymentResult && deploymentInfo && (
                        <div className="flex-1 min-h-0 flex flex-col">
                          <ContractEditor
                            code={`// 🎉 DEPLOYMENT SUCCESSFUL
// Your contract is now live on Solana devnet

// Program ID (use this to interact with your contract)
const PROGRAM_ID = "${deploymentInfo.programId}";

// Transaction Hash
const TX_HASH = "${deploymentInfo.txHash}";

// Example: Connect to your deployed program
import { Connection, PublicKey, Keypair } from '@solana/web3.js';

const connection = new Connection('http://127.0.0.1:8899', 'confirmed');
const programId = new PublicKey('${deploymentInfo.programId}');

console.log(\`Program deployed at: \${programId.toBase58()}\`);
console.log(\`Transaction: \${TX_HASH}\`);

// ✅ You can now interact with your program!
// Use this Program ID in your frontend, tests, or other programs`}
                            language="typescript"
                            readOnly
                            height="400px"
                          />
                        </div>
                      )}

                      {/* Deployment Console - Embedded */}
                      {(deployLogs.length > 0 || isDeployActive) && (
                        <div className="flex-1 min-h-0 flex flex-col border-t border-[var(--card-border)] pt-4">
                          <div className="flex items-center justify-between mb-3">
                            <div className="flex items-center gap-2">
                              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-[var(--accent)]">
                                <polyline points="4 17 10 11 4 5"/>
                                <line x1="12" x2="20" y1="19" y2="19"/>
                              </svg>
                              <h4 className="font-semibold text-sm text-[var(--foreground)]">
                                Console
                              </h4>
                            </div>
                            {deployLogs.length > 0 && (
                              <button
                                onClick={clearDeployLogs}
                                className="text-xs text-[var(--muted)] hover:text-[var(--foreground)] transition"
                              >
                                Clear
                              </button>
                            )}
                          </div>
                          <div className="flex-1 overflow-y-auto rounded-lg border border-[var(--card-border)] bg-[rgba(8,11,26,0.4)] p-3 font-mono text-xs">
                            {deployLogs.map((log, i) => (
                              <div
                                key={i}
                                className={`py-1 ${
                                  log.type === 'error' ? 'text-red-400' :
                                  log.type === 'success' ? 'text-green-400' :
                                  log.type === 'warning' ? 'text-yellow-400' :
                                  log.type === 'data' ? 'text-blue-400' :
                                  'text-[var(--muted)]'
                                }`}
                              >
                                <span className="text-[var(--muted)] mr-2">
                                  {log.timestamp.toLocaleTimeString()}
                                </span>
                                {log.icon && <span className="mr-2">{log.icon}</span>}
                                {log.message}
                              </div>
                            ))}
                            {isDeployActive && deployLogs.length === 0 && (
                              <div className="text-[var(--muted)] animate-pulse">
                                Waiting for output...
                              </div>
                            )}
                          </div>
                        </div>
                      )}
                    </CardContent>
                  </Card>
                </div>
              )}

              {/* Test Card - Interactive Testing (like Solana Playground) */}
              {deploymentResult && idl && deploymentInfo && (
                <div className="w-[700px] flex-shrink-0">
                  <Card className="glass-card gradient-ring h-full">
                    <CardHeader>
                      <CardTitle className="flex items-center gap-2">
                        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-[var(--accent)]">
                          <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                          <polyline points="22 4 12 14.01 9 11.01"/>
                        </svg>
                        Test
                      </CardTitle>
                      <CardDescription>
                        Call instructions on your deployed contract
                      </CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4" style={{ height: '600px', overflowY: 'auto' }}>
                      <div className="space-y-4">
                        {/* Program Info */}
                        <div className="text-xs bg-purple-500/10 border border-purple-500/30 rounded-lg p-3">
                          <div className="font-semibold text-purple-400 mb-2">📋 Program Information</div>
                          <div className="space-y-1 font-mono text-[var(--muted)]">
                            <div>Name: <span className="text-[var(--foreground)]">{idl.name || 'Unknown'}</span></div>
                            <div>Version: <span className="text-[var(--foreground)]">{idl.version || 'Unknown'}</span></div>
                            <div>Instructions: <span className="text-green-400">{idl.instructions?.length || 0}</span></div>
                            <div className="break-all">Program ID: <span className="text-[var(--accent)]">{deploymentInfo.programId}</span></div>
                          </div>
                        </div>

                        {/* Instruction Selector */}
                        <div className="space-y-2">
                          <label className="text-sm font-semibold text-[var(--foreground)]">
                            Select Instruction
                          </label>
                          <select
                            value={selectedInstruction}
                            onChange={(e) => {
                              setSelectedInstruction(e.target.value);
                              setInstructionArgs({});
                              setTestResult(null);
                            }}
                            className="w-full px-3 py-2 rounded-lg border border-[var(--card-border)] bg-[rgba(8,11,26,0.6)] text-[var(--foreground)] focus:outline-none focus:ring-2 focus:ring-[var(--accent)]"
                          >
                            <option value="">-- Choose an instruction --</option>
                            {idl.instructions?.map((ix: any) => (
                              <option key={ix.name} value={ix.name}>
                                {ix.name} ({ix.args?.length || 0} args, {ix.accounts?.length || 0} accounts)
                              </option>
                            ))}
                          </select>
                        </div>

                        {/* Instruction Details */}
                        {selectedInstruction && (() => {
                          const instruction = idl.instructions?.find((ix: any) => ix.name === selectedInstruction);
                          if (!instruction) return null;

                          return (
                            <div className="space-y-4">
                              {/* Documentation */}
                              {instruction.docs && instruction.docs.length > 0 && (
                                <div className="text-xs bg-blue-500/10 border border-blue-500/30 rounded-lg p-3">
                                  <div className="font-semibold text-blue-400 mb-1">📖 Documentation</div>
                                  <div className="text-[var(--muted)]">{instruction.docs.join(' ')}</div>
                                </div>
                              )}

                              {/* Arguments */}
                              {instruction.args && instruction.args.length > 0 && (
                                <div className="space-y-3">
                                  <div className="text-sm font-semibold text-[var(--foreground)]">Arguments</div>
                                  {instruction.args.map((arg: any) => (
                                    <div key={arg.name} className="space-y-1">
                                      <label className="text-xs text-[var(--muted)]">
                                        {arg.name} <span className="text-[var(--accent)]">({typeof arg.type === 'string' ? arg.type : JSON.stringify(arg.type)})</span>
                                      </label>
                                      <input
                                        type="text"
                                        value={instructionArgs[arg.name] || ''}
                                        onChange={(e) => setInstructionArgs(prev => ({ ...prev, [arg.name]: e.target.value }))}
                                        placeholder={`Enter ${arg.name}...`}
                                        className="w-full px-3 py-2 rounded-lg border border-[var(--card-border)] bg-[rgba(8,11,26,0.6)] text-[var(--foreground)] text-sm font-mono focus:outline-none focus:ring-2 focus:ring-[var(--accent)]"
                                      />
                                    </div>
                                  ))}
                                </div>
                              )}

                              {/* Accounts Info */}
                              {instruction.accounts && instruction.accounts.length > 0 && (
                                <div className="space-y-2">
                                  <div className="text-sm font-semibold text-[var(--foreground)]">Required Accounts</div>
                                  <div className="text-xs bg-yellow-500/10 border border-yellow-500/30 rounded-lg p-3">
                                    <div className="space-y-1 font-mono text-[var(--muted)]">
                                      {instruction.accounts.map((acc: any, idx: number) => (
                                        <div key={idx}>
                                          {idx + 1}. {acc.name} <span className="text-yellow-400">({acc.isMut ? 'writable' : 'readonly'}, {acc.isSigner ? 'signer' : 'no-signer'})</span>
                                        </div>
                                      ))}
                                    </div>
                                    <div className="mt-2 text-[var(--muted)]">
                                      ℹ️ Accounts will be auto-populated by Anchor client
                                    </div>
                                  </div>
                                </div>
                              )}

                              {/* Test Button */}
                              <div className="pt-4">
                                <Button
                                  onClick={async () => {
                                    setTestingTx(true);
                                    setTestResult(null);
                                    try {
                                      // Note: This is a placeholder - actual implementation would need @coral-xyz/anchor
                                      setTestResult({
                                        success: false,
                                        message: '⚠️ Coming soon: Full Anchor client integration for browser-based testing. For now, use the Node.js test script or build a client manually with @coral-xyz/anchor.'
                                      });
                                    } catch (err: any) {
                                      setTestResult({
                                        success: false,
                                        message: err.message
                                      });
                                    } finally {
                                      setTestingTx(false);
                                    }
                                  }}
                                  disabled={testingTx || !walletKeypair.trim()}
                                  variant="default"
                                  className="w-full"
                                  size="lg"
                                >
                                  {testingTx ? (
                                    <>
                                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                                      Sending Transaction...
                                    </>
                                  ) : (
                                    <>
                                      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-2">
                                        <polygon points="5 3 19 12 5 21 5 3"/>
                                      </svg>
                                      Call {selectedInstruction}
                                    </>
                                  )}
                                </Button>
                                {!walletKeypair.trim() && (
                                  <p className="text-xs text-yellow-400 mt-2">⚠️ Wallet keypair required for testing</p>
                                )}
                              </div>

                              {/* Test Result */}
                              {testResult && (
                                <div className={`text-xs rounded-lg p-3 border ${
                                  testResult.success 
                                    ? 'bg-green-500/10 border-green-500/30 text-green-400' 
                                    : 'bg-yellow-500/10 border-yellow-500/30 text-yellow-400'
                                }`}>
                                  <div className="font-semibold mb-1">
                                    {testResult.success ? '✅ Success' : 'ℹ️ Info'}
                                  </div>
                                  <div className="font-mono">{testResult.message}</div>
                                  {testResult.signature && (
                                    <div className="mt-2 break-all">
                                      Signature: <span className="text-[var(--accent)]">{testResult.signature}</span>
                                    </div>
                                  )}
                                </div>
                              )}
                            </div>
                          );
                        })()}

                        {/* Helper Info */}
                        {!selectedInstruction && (
                          <div className="text-xs bg-blue-500/10 border border-blue-500/30 rounded-lg p-3">
                            <div className="font-semibold text-blue-400 mb-2">💡 How to Test</div>
                            <div className="space-y-2 text-[var(--muted)]">
                              <div>1. Select an instruction from the dropdown above</div>
                              <div>2. Fill in the required arguments</div>
                              <div>3. Click "Call" to execute on the blockchain</div>
                              <div className="mt-3 pt-3 border-t border-blue-500/30">
                                <div className="font-semibold text-blue-400 mb-1">Alternative: Use Node.js Test Script</div>
                                <div className="font-mono text-xs bg-[rgba(8,11,26,0.6)] p-2 rounded mt-1">
                                  node test-with-idl.js {deploymentInfo.programId.slice(0, 10)}... ./idl/your.json ./wallet.json
                                </div>
                              </div>
                            </div>
                          </div>
                        )}
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

      {/* Payment Modal */}
      <PaymentModal
        operation={pendingOperation || 'generate'}
        blockchain="Rust"
        isOpen={showPaymentModal}
        onClose={() => setShowPaymentModal(false)}
        onSuccess={handlePaymentSuccess}
      />

      {/* Credits Purchase Modal */}
      <CreditsPurchaseModal
        isOpen={showCreditsModal}
        onClose={() => setShowCreditsModal(false)}
        onSuccess={handleCreditsPurchaseSuccess}
        currentBalance={creditBalance}
      />
    </div>
  );
}

