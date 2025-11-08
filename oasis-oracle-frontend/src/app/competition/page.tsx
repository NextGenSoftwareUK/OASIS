'use client';

import React from 'react';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/shadcn-card';
import { Trophy, Medal, Zap, Egg } from 'lucide-react';

export default function CompetitionPage() {
  return (
    <div className="container mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="text-center mb-8">
        <h1 className="text-4xl font-bold text-gray-900 mb-2">🏆 STAR Competition Hub</h1>
        <p className="text-gray-600">Compete, collect, and climb the leaderboards in the OASIS!</p>
      </div>

      {/* Main Competition Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Link href="/competition/leaderboard">
          <Card className="hover:shadow-lg transition-all cursor-pointer bg-gradient-to-br from-blue-50 to-indigo-50 border-blue-200">
            <CardHeader>
              <div className="flex items-center justify-center mb-4">
                <Trophy className="w-16 h-16 text-blue-500" />
              </div>
              <CardTitle className="text-center text-2xl">Leaderboards</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-center text-gray-600">
                Track your ranking across multiple competition types including Karma, Experience, Quests, and more.
              </p>
            </CardContent>
          </Card>
        </Link>

        <Link href="/competition/eggs">
          <Card className="hover:shadow-lg transition-all cursor-pointer bg-gradient-to-br from-yellow-50 to-orange-50 border-yellow-200">
            <CardHeader>
              <div className="flex items-center justify-center mb-4">
                <Egg className="w-16 h-16 text-yellow-500" />
              </div>
              <CardTitle className="text-center text-2xl">Egg Collection</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-center text-gray-600">
                Discover and collect rare eggs across the OASIS. View your collection stats and compete with other collectors.
              </p>
            </CardContent>
          </Card>
        </Link>

        <Link href="/competition/tournaments">
          <Card className="hover:shadow-lg transition-all cursor-pointer bg-gradient-to-br from-purple-50 to-pink-50 border-purple-200">
            <CardHeader>
              <div className="flex items-center justify-center mb-4">
                <Medal className="w-16 h-16 text-purple-500" />
              </div>
              <CardTitle className="text-center text-2xl">Tournaments</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-center text-gray-600">
                Join tournaments, compete for prizes, and track your tournament history and performance.
              </p>
            </CardContent>
          </Card>
        </Link>
      </div>

      {/* Stats Overview */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mt-8">
        <Card>
          <CardContent className="p-6 text-center">
            <Trophy className="w-8 h-8 text-blue-500 mx-auto mb-2" />
            <h3 className="text-xl font-bold">1,543</h3>
            <p className="text-sm text-gray-600">Active Competitors</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6 text-center">
            <Egg className="w-8 h-8 text-yellow-500 mx-auto mb-2" />
            <h3 className="text-xl font-bold">23</h3>
            <p className="text-sm text-gray-600">Egg Types</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6 text-center">
            <Medal className="w-8 h-8 text-purple-500 mx-auto mb-2" />
            <h3 className="text-xl font-bold">5</h3>
            <p className="text-sm text-gray-600">Active Tournaments</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6 text-center">
            <Zap className="w-8 h-8 text-green-500 mx-auto mb-2" />
            <h3 className="text-xl font-bold">42</h3>
            <p className="text-sm text-gray-600">Achievements</p>
          </CardContent>
        </Card>
      </div>

      {/* Feature Highlights */}
      <Card className="mt-8">
        <CardHeader>
          <CardTitle>Competition Features</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <h3 className="font-semibold text-lg mb-2">🎯 Multiple Competition Types</h3>
              <p className="text-gray-600">Compete in Karma, Experience, Quest Completion, Social Activity, Messaging, File Sharing, Video Calls, and Chat Sessions.</p>
            </div>
            <div>
              <h3 className="font-semibold text-lg mb-2">🏅 League System</h3>
              <p className="text-gray-600">Progress through Bronze, Silver, Gold, Platinum, Diamond, Master, Grand Master, Legend, Mythic, and Transcendent leagues.</p>
            </div>
            <div>
              <h3 className="font-semibold text-lg mb-2">🥚 Rare Egg Collection</h3>
              <p className="text-gray-600">Discover eggs through quests, puzzles, exploration, and secret locations. Collect all types and rarities!</p>
            </div>
            <div>
              <h3 className="font-semibold text-lg mb-2">🎪 Tournament Formats</h3>
              <p className="text-gray-600">Participate in Single/Double Elimination, Round Robin, Swiss, Bracket, League, and Knockout tournaments.</p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

