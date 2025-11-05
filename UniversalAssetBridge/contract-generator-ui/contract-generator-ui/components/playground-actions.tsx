'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { ExternalLink, Download, Zap, Code, Github, Loader2 } from 'lucide-react';
import { openInSolanaPlayground, downloadZip } from '@/lib/playground';
import { createContractGist, openPlaygroundWithGist, isGitHubTokenConfigured } from '@/lib/github-gist';

interface PlaygroundActionsProps {
  contractCode: string;
  contractZip: Blob;
  programName?: string;
  onCompile?: () => void;
  onDeploy?: () => void;
  language?: 'rust' | 'solidity';
  isCompiling?: boolean;
  isDeploying?: boolean;
  isCompiled?: boolean;
  isDeployed?: boolean;
}

export function PlaygroundActions({
  contractCode,
  contractZip,
  programName = 'contract',
  onCompile,
  onDeploy,
  language = 'rust',
  isCompiling = false,
  isDeploying = false,
  isCompiled = false,
  isDeployed = false,
}: PlaygroundActionsProps) {
  const [uploadingToGist, setUploadingToGist] = useState(false);
  const hasGitHubToken = isGitHubTokenConfigured();

  const handleOpenPlayground = () => {
    openInSolanaPlayground(contractCode, programName);
  };

  const handleOpenWithGist = async () => {
    try {
      setUploadingToGist(true);
      
      // Upload to GitHub Gist
      const gist = await createContractGist(contractCode, programName);
      
      // Open Playground with Gist URL
      openPlaygroundWithGist(gist.html_url, programName);
      
    } catch (error: any) {
      console.error('Failed to upload to Gist:', error);
      alert(`Failed to upload to GitHub Gist:\n\n${error.message}\n\nPlease use "Download ZIP" and import manually.`);
    } finally {
      setUploadingToGist(false);
    }
  };

  const handleDownload = () => {
    downloadZip(contractZip, `${programName}.zip`);
  };

  return (
    <div className="glass-card gradient-ring relative overflow-hidden rounded-2xl border border-[var(--card-border)]/60 p-6">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_right,rgba(34,211,238,0.15),transparent_70%)]" />
      
      <div className="relative space-y-4">
        <div className="flex items-center gap-2 mb-2">
          <Code className="w-5 h-5 text-[var(--accent)]" />
          <h3 className="text-lg font-semibold text-[var(--foreground)]">
            Contract Generated Successfully!
          </h3>
        </div>
        
        <p className="text-sm text-[var(--muted)] mb-6">
          Your smart contract is ready. Choose how you'd like to proceed:
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
          {/* Open with GitHub Gist (Recommended) */}
          {language === 'rust' && hasGitHubToken && (
            <Button
              onClick={handleOpenWithGist}
              disabled={uploadingToGist}
              variant="default"
              className="h-auto py-4 flex flex-col items-center gap-2"
            >
              <Github className="w-5 h-5" />
              <div className="text-center">
                <div className="font-semibold text-sm">
                  {uploadingToGist ? 'Uploading...' : 'Share via Gist'}
                </div>
                <div className="text-xs opacity-90">Auto-import (best)</div>
              </div>
            </Button>
          )}

          {/* Open in Playground (Fallback) */}
          {language === 'rust' && (
            <Button
              onClick={handleOpenPlayground}
              variant={hasGitHubToken ? "outline" : "default"}
              className="h-auto py-4 flex flex-col items-center gap-2"
            >
              <ExternalLink className="w-5 h-5" />
              <div className="text-center">
                <div className="font-semibold text-sm">Open Playground</div>
                <div className="text-xs opacity-90">Manual import</div>
              </div>
            </Button>
          )}

          {/* Download ZIP */}
          <Button
            onClick={handleDownload}
            variant="secondary"
            className="h-auto py-4 flex flex-col items-center gap-2"
          >
            <Download className="w-5 h-5" />
            <div className="text-center">
              <div className="font-semibold text-sm">Download ZIP</div>
              <div className="text-xs opacity-90">Full project files</div>
            </div>
          </Button>

          {/* Compile */}
          {onCompile && (
            <Button
              onClick={onCompile}
              disabled={isCompiling || isCompiled || isDeployed}
              variant={isCompiled ? "secondary" : "outline"}
              className="h-auto py-4 flex flex-col items-center gap-2"
            >
              {isCompiling ? (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" />
                  <div className="text-center">
                    <div className="font-semibold text-sm">Compiling...</div>
                    <div className="text-xs opacity-90">~30-60s</div>
                  </div>
                </>
              ) : (
                <>
                  <Code className="w-5 h-5" />
                  <div className="text-center">
                    <div className="font-semibold text-sm">
                      {isCompiled ? 'Compiled ✓' : 'Compile'}
                    </div>
                    <div className="text-xs opacity-90">Via API</div>
                  </div>
                </>
              )}
            </Button>
          )}

          {/* Deploy */}
          {onDeploy && (
            <Button
              onClick={onDeploy}
              disabled={isDeploying || isDeployed}
              variant={isDeployed ? "secondary" : "outline"}
              className="h-auto py-4 flex flex-col items-center gap-2"
            >
              {isDeploying ? (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" />
                  <div className="text-center">
                    <div className="font-semibold text-sm">Deploying...</div>
                    <div className="text-xs opacity-90">Please wait</div>
                  </div>
                </>
              ) : (
                <>
                  <Zap className="w-5 h-5" />
                  <div className="text-center">
                    <div className="font-semibold text-sm">
                      {isDeployed ? 'Deployed ✓' : 'Deploy'}
                    </div>
                    <div className="text-xs opacity-90">To Devnet</div>
                  </div>
                </>
              )}
            </Button>
          )}
        </div>

        {/* Helpful Tips */}
        <div className="mt-6 rounded-xl border border-[var(--accent)]/20 bg-[rgba(34,211,238,0.05)] p-4">
          <h4 className="font-semibold text-sm mb-2 text-[var(--foreground)]">💡 Workflow Guide</h4>
          <ul className="text-sm text-[var(--muted)] space-y-1">
            {language === 'rust' && hasGitHubToken && (
              <li>• <strong>Share via Gist</strong> - Uploads to GitHub for easy Playground import (recommended!)</li>
            )}
            {language === 'rust' && !hasGitHubToken && (
              <li>• <strong>💡 Add GitHub token</strong> to enable auto-import via Gist (see GITHUB_TOKEN_SETUP.md)</li>
            )}
            {language === 'rust' && (
              <li>• <strong>Open Playground</strong> - Test & edit in browser (manual import)</li>
            )}
            <li>• <strong>Download ZIP</strong> - Full project for local development</li>
            {onCompile && (
              <li>• <strong>Compile</strong> - Click to compile via API (~30-60 seconds)</li>
            )}
            {onDeploy && (
              <li>• <strong>Deploy</strong> - {isCompiled ? 'Ready to deploy!' : 'Auto-compiles then deploys to devnet'}</li>
            )}
            {isDeployed && (
              <li className="text-green-400">• <strong>✅ Deployed!</strong> - Check Program ID above</li>
            )}
          </ul>
        </div>
      </div>
    </div>
  );
}

