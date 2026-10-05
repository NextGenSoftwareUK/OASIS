'use client';

import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/shadcn-card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Egg, Trophy, Star, Zap, Flame, Snowflake, Droplets, Wind, Mountain, Sparkles, Crown, Medal } from 'lucide-react';
import { ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, PieChart, Pie, Cell, LineChart, Line } from 'recharts';

interface EggCollectionStats {
  totalEggs: number;
  uniqueTypes: number;
  rarityDistribution: Array<{ rarity: string; count: number; percentage: number }>;
  typeDistribution: Array<{ type: string; count: number; color: string }>;
  discoveryMethods: Array<{ method: string; count: number }>;
  recentDiscoveries: Array<{ name: string; type: string; rarity: string; discoveredAt: string }>;
  leaderboard: Array<{ rank: number; avatarName: string; eggCount: number; uniqueTypes: number }>;
}

export default function EggCollectionPage() {
  const [stats, setStats] = useState<EggCollectionStats | null>(null);
  const [loading, setLoading] = useState(true);

  const eggTypeIcons = {
    Bronze: <Trophy className="w-5 h-5 text-amber-600" />,
    Silver: <Trophy className="w-5 h-5 text-gray-400" />,
    Gold: <Trophy className="w-5 h-5 text-yellow-500" />,
    Platinum: <Trophy className="w-5 h-5 text-blue-400" />,
    Diamond: <Trophy className="w-5 h-5 text-cyan-400" />,
    Dragon: <Zap className="w-5 h-5 text-purple-500" />,
    Fire: <Flame className="w-5 h-5 text-red-500" />,
    Ice: <Snowflake className="w-5 h-5 text-blue-300" />,
    Lightning: <Zap className="w-5 h-5 text-yellow-400" />,
    Storm: <Wind className="w-5 h-5 text-gray-600" />,
    Wind: <Wind className="w-5 h-5 text-green-400" />,
    Earth: <Mountain className="w-5 h-5 text-green-600" />,
    Water: <Droplets className="w-5 h-5 text-blue-500" />,
    Air: <Wind className="w-5 h-5 text-sky-400" />,
    Spirit: <Sparkles className="w-5 h-5 text-purple-400" />,
    Cosmic: <Star className="w-5 h-5 text-indigo-500" />,
    Celestial: <Star className="w-5 h-5 text-cyan-300" />,
    Mystic: <Sparkles className="w-5 h-5 text-pink-400" />,
    Ancient: <Crown className="w-5 h-5 text-amber-800" />,
    Legendary: <Crown className="w-5 h-5 text-yellow-600" />,
    Mythic: <Crown className="w-5 h-5 text-purple-600" />,
    Divine: <Crown className="w-5 h-5 text-gold-500" />
  };

  const rarityColors = {
    Common: '#6B7280',
    Uncommon: '#10B981',
    Rare: '#3B82F6',
    Epic: '#8B5CF6',
    Legendary: '#F59E0B',
    Mythic: '#EF4444',
    Divine: '#F97316',
    Celestial: '#06B6D4',
    Transcendent: '#8B5CF6',
    Omnipotent: '#F59E0B'
  };

  const discoveryMethodColors = {
    'Quest Completion': '#10B981',
    'Puzzle Solved': '#3B82F6',
    'Secret Location': '#8B5CF6',
    'Exploration': '#F59E0B',
    'Social Activity': '#EF4444',
    'Random Discovery': '#6B7280'
  };

  useEffect(() => {
    fetchEggCollectionStats();
  }, []);

  const fetchEggCollectionStats = async () => {
    try {
      setLoading(true);
      // Mock data for demonstration
      const mockStats: EggCollectionStats = {
        totalEggs: 247,
        uniqueTypes: 23,
        rarityDistribution: [
          { rarity: 'Common', count: 89, percentage: 36 },
          { rarity: 'Uncommon', count: 67, percentage: 27 },
          { rarity: 'Rare', count: 45, percentage: 18 },
          { rarity: 'Epic', count: 28, percentage: 11 },
          { rarity: 'Legendary', count: 12, percentage: 5 },
          { rarity: 'Mythic', count: 6, percentage: 2 }
        ],
        typeDistribution: [
          { type: 'Bronze', count: 45, color: '#CD7F32' },
          { type: 'Silver', count: 38, color: '#C0C0C0' },
          { type: 'Gold', count: 32, color: '#FFD700' },
          { type: 'Dragon', count: 28, color: '#8B5CF6' },
          { type: 'Fire', count: 25, color: '#EF4444' },
          { type: 'Ice', count: 22, color: '#06B6D4' },
          { type: 'Lightning', count: 19, color: '#F59E0B' },
          { type: 'Storm', count: 16, color: '#6B7280' },
          { type: 'Earth', count: 13, color: '#10B981' },
          { type: 'Water', count: 9, color: '#3B82F6' }
        ],
        discoveryMethods: [
          { method: 'Quest Completion', count: 89 },
          { method: 'Puzzle Solved', count: 67 },
          { method: 'Secret Location', count: 45 },
          { method: 'Exploration', count: 28 },
          { method: 'Social Activity', count: 12 },
          { method: 'Random Discovery', count: 6 }
        ],
        recentDiscoveries: [
          { name: 'Ancient Dragon Egg', type: 'Dragon', rarity: 'Legendary', discoveredAt: '2 hours ago' },
          { name: 'Cosmic Fire Egg', type: 'Fire', rarity: 'Epic', discoveredAt: '5 hours ago' },
          { name: 'Mystic Ice Crystal', type: 'Ice', rarity: 'Rare', discoveredAt: '1 day ago' },
          { name: 'Storm Thunder Egg', type: 'Storm', rarity: 'Uncommon', discoveredAt: '2 days ago' },
          { name: 'Earth Guardian Stone', type: 'Earth', rarity: 'Common', discoveredAt: '3 days ago' }
        ],
        leaderboard: [
          { rank: 1, avatarName: 'DragonMaster', eggCount: 156, uniqueTypes: 18 },
          { rank: 2, avatarName: 'EggHunter', eggCount: 142, uniqueTypes: 16 },
          { rank: 3, avatarName: 'CollectorPro', eggCount: 128, uniqueTypes: 15 },
          { rank: 4, avatarName: 'TreasureSeeker', eggCount: 115, uniqueTypes: 14 },
          { rank: 5, avatarName: 'MysticFinder', eggCount: 98, uniqueTypes: 12 }
        ]
      };
      setStats(mockStats);
    } catch (error) {
      console.error('Error fetching egg collection stats:', error);
    } finally {
      setLoading(false);
    }
  };

  const getRarityBadgeClassName = (rarity: string) => {
    const colorMap: Record<string, string> = {
      Common: 'bg-gray-600 text-white',
      Uncommon: 'bg-green-600 text-white',
      Rare: 'bg-blue-600 text-white',
      Epic: 'bg-purple-600 text-white',
      Legendary: 'bg-yellow-600 text-white',
      Mythic: 'bg-red-600 text-white',
      Divine: 'bg-orange-600 text-white',
      Celestial: 'bg-cyan-600 text-white',
      Transcendent: 'bg-purple-700 text-white',
      Omnipotent: 'bg-yellow-700 text-white'
    };
    return colorMap[rarity] || 'bg-gray-600 text-white';
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="text-center">
        <h1 className="text-4xl font-bold text-gray-900 mb-2">🥚 Egg Collection Competition</h1>
        <p className="text-gray-600">Discover, collect, and compete with the rarest eggs in the OASIS!</p>
      </div>

      {/* Stats Overview */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card className="bg-gradient-to-r from-yellow-50 to-orange-50">
          <CardContent className="p-6 text-center">
            <Egg className="w-12 h-12 text-yellow-500 mx-auto mb-4" />
            <h3 className="text-2xl font-bold text-gray-900">{stats?.totalEggs}</h3>
            <p className="text-gray-600">Total Eggs Collected</p>
          </CardContent>
        </Card>
        <Card className="bg-gradient-to-r from-blue-50 to-purple-50">
          <CardContent className="p-6 text-center">
            <Star className="w-12 h-12 text-blue-500 mx-auto mb-4" />
            <h3 className="text-2xl font-bold text-gray-900">{stats?.uniqueTypes}</h3>
            <p className="text-gray-600">Unique Egg Types</p>
          </CardContent>
        </Card>
        <Card className="bg-gradient-to-r from-green-50 to-teal-50">
          <CardContent className="p-6 text-center">
            <Trophy className="w-12 h-12 text-green-500 mx-auto mb-4" />
            <h3 className="text-2xl font-bold text-gray-900">#3</h3>
            <p className="text-gray-600">Your Rank</p>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="collection" className="space-y-6">
        <TabsList>
          <TabsTrigger value="collection">My Collection</TabsTrigger>
          <TabsTrigger value="leaderboard">Leaderboard</TabsTrigger>
          <TabsTrigger value="analytics">Analytics</TabsTrigger>
          <TabsTrigger value="discoveries">Recent Discoveries</TabsTrigger>
        </TabsList>

        {/* Collection Tab */}
        <TabsContent value="collection">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Rarity Distribution */}
            <Card>
              <CardHeader>
                <CardTitle>Collection by Rarity</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie
                      data={stats?.rarityDistribution || []}
                      cx="50%"
                      cy="50%"
                      labelLine={false}
                      label={({ rarity, percentage }) => `${rarity} (${percentage}%)`}
                      outerRadius={80}
                      fill="#8884d8"
                      dataKey="count"
                    >
                      {stats?.rarityDistribution?.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={rarityColors[entry.rarity as keyof typeof rarityColors] || '#8884d8'} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            {/* Type Distribution */}
            <Card>
              <CardHeader>
                <CardTitle>Collection by Type</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={stats?.typeDistribution || []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="type" />
                    <YAxis />
                    <Tooltip />
                    <Bar dataKey="count" fill="#8884d8" />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        {/* Leaderboard Tab */}
        <TabsContent value="leaderboard">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Trophy className="w-5 h-5" />
                Egg Collection Leaderboard
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {stats?.leaderboard.map((entry, index) => (
                  <div
                    key={entry.rank}
                    className={`flex items-center justify-between p-4 rounded-lg border ${
                      index < 3 ? 'bg-gradient-to-r from-yellow-50 to-orange-50 border-yellow-200' : 'bg-white'
                    }`}
                  >
                    <div className="flex items-center gap-4">
                      <div className="flex items-center gap-2">
                        {index === 0 && <Crown className="w-6 h-6 text-yellow-500" />}
                        {index === 1 && <Medal className="w-6 h-6 text-gray-400" />}
                        {index === 2 && <Medal className="w-6 h-6 text-amber-600" />}
                        {index > 2 && <Trophy className="w-5 h-5 text-blue-500" />}
                        <span className="font-bold text-lg">#{entry.rank}</span>
                      </div>
                      <div>
                        <p className="font-semibold">{entry.avatarName}</p>
                        <p className="text-sm text-gray-600">{entry.uniqueTypes} unique types</p>
                      </div>
                    </div>
                    <div className="text-right">
                      <p className="font-bold text-lg">{entry.eggCount}</p>
                      <p className="text-sm text-gray-600">eggs collected</p>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Analytics Tab */}
        <TabsContent value="analytics">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Discovery Methods */}
            <Card>
              <CardHeader>
                <CardTitle>Discovery Methods</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={stats?.discoveryMethods || []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="method" />
                    <YAxis />
                    <Tooltip />
                    <Bar dataKey="count" fill="#8884d8" />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            {/* Collection Progress */}
            <Card>
              <CardHeader>
                <CardTitle>Collection Progress</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div>
                    <div className="flex justify-between mb-2">
                      <span className="text-sm font-medium">Bronze Eggs</span>
                      <span className="text-sm text-gray-600">45/50</span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div className="bg-amber-600 h-2 rounded-full" style={{ width: '90%' }}></div>
                    </div>
                  </div>
                  <div>
                    <div className="flex justify-between mb-2">
                      <span className="text-sm font-medium">Silver Eggs</span>
                      <span className="text-sm text-gray-600">38/45</span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div className="bg-gray-400 h-2 rounded-full" style={{ width: '84%' }}></div>
                    </div>
                  </div>
                  <div>
                    <div className="flex justify-between mb-2">
                      <span className="text-sm font-medium">Gold Eggs</span>
                      <span className="text-sm text-gray-600">32/40</span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div className="bg-yellow-500 h-2 rounded-full" style={{ width: '80%' }}></div>
                    </div>
                  </div>
                  <div>
                    <div className="flex justify-between mb-2">
                      <span className="text-sm font-medium">Dragon Eggs</span>
                      <span className="text-sm text-gray-600">28/35</span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div className="bg-purple-500 h-2 rounded-full" style={{ width: '80%' }}></div>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        {/* Recent Discoveries Tab */}
        <TabsContent value="discoveries">
          <Card>
            <CardHeader>
              <CardTitle>Recent Discoveries</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {stats?.recentDiscoveries.map((discovery, index) => (
                  <div key={index} className="flex items-center justify-between p-4 rounded-lg border bg-gradient-to-r from-blue-50 to-purple-50">
                    <div className="flex items-center gap-4">
                      {eggTypeIcons[discovery.type as keyof typeof eggTypeIcons] || <Egg className="w-5 h-5 text-gray-500" />}
                      <div>
                        <p className="font-semibold">{discovery.name}</p>
                        <p className="text-sm text-gray-600">{discovery.type} • {discovery.discoveredAt}</p>
                      </div>
                    </div>
                    <Badge className={getRarityBadgeClassName(discovery.rarity)}>
                      {discovery.rarity}
                    </Badge>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

