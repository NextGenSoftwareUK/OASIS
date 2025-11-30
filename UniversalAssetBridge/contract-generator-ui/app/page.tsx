'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Code, Sparkles, FileText, Rocket, Zap, Shield } from 'lucide-react';
import Link from 'next/link';

export default function HomePage() {
  return (
    <div className="min-h-screen">
      {/* Header */}
      <header className="border-b border-[var(--card-border)]/30 bg-[rgba(5,8,18,0.8)] backdrop-blur-sm sticky top-0 z-50">
        <div className="container mx-auto px-4 py-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Code className="w-6 h-6 text-[var(--accent)]" />
            <span className="text-xl font-bold text-[var(--foreground)]">AssetRail</span>
            <span className="text-sm text-[var(--muted)]">Contract Generator</span>
          </div>
          <nav className="flex items-center gap-4">
            <Link href="/docs" className="text-sm text-[var(--muted)] hover:text-[var(--foreground)] transition">
              Docs
            </Link>
            <Link href="/api" className="text-sm text-[var(--muted)] hover:text-[var(--foreground)] transition">
              API
            </Link>
            <Link href="/x402-dashboard" className="text-sm text-[var(--accent)] hover:text-[var(--accent)]/80 transition font-medium">
              x402 Dashboard
            </Link>
          </nav>
        </div>
      </header>

      <div className="container mx-auto px-4 py-16">
        {/* Hero */}
        <div className="text-center mb-16 space-y-6">
          <div className="inline-block rounded-full border border-[var(--accent)]/30 bg-[var(--accent-soft)] px-4 py-1.5 text-sm text-[var(--accent)] mb-4">
            Developer Tools
          </div>
          
          <h1 className="text-5xl md:text-6xl font-bold text-[var(--foreground)] mb-4">
            Smart Contract Generator
          </h1>
          
          <p className="text-xl text-[var(--muted)] max-w-2xl mx-auto mb-8">
            Generate production-ready smart contracts for any blockchain with template-based or AI-powered generation
          </p>
          
          {/* Blockchain Selector */}
          <div className="flex flex-wrap justify-center gap-3 mb-8">
            <div className="flex items-center gap-2 rounded-xl border border-[var(--accent)]/50 bg-[var(--accent-soft)] px-4 py-2">
              <div className="w-2 h-2 rounded-full bg-[var(--accent)]" />
              <span className="text-sm font-semibold text-[var(--accent)]">Solana (Rust)</span>
            </div>
            <div className="flex items-center gap-2 rounded-xl border border-[var(--card-border)] bg-[rgba(8,11,26,0.6)] px-4 py-2">
              <div className="w-2 h-2 rounded-full bg-[var(--muted)]" />
              <span className="text-sm text-[var(--muted)]">Ethereum (Solidity)</span>
            </div>
            <div className="flex items-center gap-2 rounded-xl border border-[var(--card-border)] bg-[rgba(8,11,26,0.6)] px-4 py-2">
              <div className="w-2 h-2 rounded-full bg-[var(--muted)]" />
              <span className="text-sm text-[var(--muted)]">Radix (Scrypto)</span>
            </div>
          </div>
        </div>

        {/* Generation Methods */}
        <div className="grid md:grid-cols-2 gap-8 max-w-5xl mx-auto mb-16">
          {/* Template-Based */}
          <Card className="glass-card gradient-ring relative overflow-hidden">
            <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(34,211,238,0.15),transparent_70%)]" />
            <div className="relative">
              <CardHeader>
                <CardTitle className="flex items-center gap-3">
                  <div className="p-2 rounded-lg bg-[var(--accent-soft)]">
                    <FileText className="w-6 h-6 text-[var(--accent)]" />
                  </div>
                  Template-Based Generation
                </CardTitle>
                <CardDescription className="text-base">
                  Use JSON specifications for consistent, predictable smart contracts
                </CardDescription>
              </CardHeader>
              <CardContent>
                <ul className="text-sm text-[var(--muted)] space-y-3 mb-8">
                  <li className="flex items-start gap-2">
                    <Zap className="w-4 h-4 text-[var(--accent)] mt-0.5 flex-shrink-0" />
                    <span><strong className="text-[var(--foreground)]">Fast generation</strong> - Contract ready in 2 seconds</span>
                  </li>
                  <li className="flex items-start gap-2">
                    <Shield className="w-4 h-4 text-[var(--accent)] mt-0.5 flex-shrink-0" />
                    <span><strong className="text-[var(--foreground)]">100% consistent</strong> - Same input, same output</span>
                  </li>
                  <li className="flex items-start gap-2">
                    <Code className="w-4 h-4 text-[var(--accent)] mt-0.5 flex-shrink-0" />
                    <span><strong className="text-[var(--foreground)]">Full control</strong> - Specify exact structure</span>
                  </li>
                  <li className="flex items-start gap-2">
                    <Rocket className="w-4 h-4 text-[var(--accent)] mt-0.5 flex-shrink-0" />
                    <span><strong className="text-[var(--foreground)]">Production ready</strong> - Deploy with confidence</span>
                  </li>
                </ul>
                <Link href="/generate/template" className="block">
                  <Button className="w-full" size="lg">
                    Start with Template
                  </Button>
                </Link>
              </CardContent>
            </div>
          </Card>

          {/* AI-Powered */}
          <Card className="glass-card gradient-ring relative overflow-hidden">
            <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_right,rgba(129,140,248,0.15),transparent_70%)]" />
            <div className="relative">
              <CardHeader>
                <CardTitle className="flex items-center gap-3">
                  <div className="p-2 rounded-lg bg-gradient-to-br from-purple-500/20 to-pink-500/20">
                    <Sparkles className="w-6 h-6 text-purple-400" />
                  </div>
                  AI-Powered Generation
                </CardTitle>
                <CardDescription className="text-base">
                  Describe your contract in plain English and let AI generate it
                </CardDescription>
              </CardHeader>
              <CardContent>
                <ul className="text-sm text-[var(--muted)] space-y-3 mb-8">
                  <li className="flex items-start gap-2">
                    <Sparkles className="w-4 h-4 text-purple-400 mt-0.5 flex-shrink-0" />
                    <span><strong className="text-[var(--foreground)]">Natural language</strong> - No JSON required</span>
                  </li>
                  <li className="flex items-start gap-2">
                    <Code className="w-4 h-4 text-purple-400 mt-0.5 flex-shrink-0" />
                    <span><strong className="text-[var(--foreground)]">Intelligent</strong> - Understands complex logic</span>
                  </li>
                  <li className="flex items-start gap-2">
                    <Shield className="w-4 h-4 text-purple-400 mt-0.5 flex-shrink-0" />
                    <span><strong className="text-[var(--foreground)]">Best practices</strong> - Security patterns included</span>
                  </li>
                  <li className="flex items-start gap-2">
                    <Zap className="w-4 h-4 text-purple-400 mt-0.5 flex-shrink-0" />
                    <span><strong className="text-[var(--foreground)]">Rapid prototyping</strong> - Idea to code in seconds</span>
                  </li>
                </ul>
                <Link href="/generate/ai" className="block">
                  <Button 
                    className="w-full bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700" 
                    size="lg"
                  >
                    Describe Contract
                  </Button>
                </Link>
              </CardContent>
            </div>
          </Card>
        </div>

        {/* Features */}
        <div className="text-center mb-16">
          <h2 className="text-3xl font-bold text-[var(--foreground)] mb-8">
            Complete Developer Workflow
          </h2>
          <div className="flex flex-wrap justify-center items-center gap-4 text-[var(--muted)]">
            <div className="flex items-center gap-2">
              <FileText className="w-5 h-5 text-[var(--accent)]" />
              <span>Specify</span>
            </div>
            <span className="text-[var(--accent)]">→</span>
            <div className="flex items-center gap-2">
              <Code className="w-5 h-5 text-[var(--accent)]" />
              <span>Generate</span>
            </div>
            <span className="text-[var(--accent)]">→</span>
            <div className="flex items-center gap-2">
              <Shield className="w-5 h-5 text-[var(--accent)]" />
              <span>Review</span>
            </div>
            <span className="text-[var(--accent)]">→</span>
            <div className="flex items-center gap-2">
              <Rocket className="w-5 h-5 text-[var(--accent)]" />
              <span>Playground</span>
            </div>
            <span className="text-[var(--accent)]">→</span>
            <div className="flex items-center gap-2">
              <Zap className="w-5 h-5 text-[var(--accent)]" />
              <span>Deploy</span>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="glass-card rounded-2xl border border-[var(--card-border)]/40 bg-[rgba(7,10,26,0.75)] p-6 text-center">
          <p className="text-[var(--foreground)] font-semibold mb-2">AssetRail Smart Contract Generator</p>
          <p className="text-sm text-[var(--muted)]">
            Powered by Smart Contract Generator API • Multi-chain deployment • Solana Playground integration
          </p>
        </div>
      </div>
    </div>
  );
}
