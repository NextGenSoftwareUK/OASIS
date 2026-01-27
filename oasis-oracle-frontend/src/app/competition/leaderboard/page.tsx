'use client';

import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/shadcn-card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Trophy, Medal, Crown, Star, TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, BarChart, Bar, PieChart, Pie, Cell } from 'recharts';

interface LeaderboardEntry {
  id: string;
  avatarId: string;
  avatarName: string;
  avatarUsername: string;
  competitionType: string;
  score: number;
  rank: number;
  previousRank: number;
  rankChange: number;
  lastUpdated: string;
  seasonStart: string;
  seasonEnd: string;
  seasonType: string;
  currentLeague: string;
  previousLeague: string;
  leaguePromoted: boolean;
  leagueDemoted: boolean;
  stats: Record<string, unknown>;
  achievements: Record<string, unknown>;
  badges: string[];
}

interface CompetitionStats {
  totalParticipants: number;
  averageScore: number;
  topScore: number;
  myRank: number;
  myScore: number;
  leagueDistribution: Array<{ league: string; count: number; percentage: number }>;
  scoreDistribution: Array<{ range: string; count: number }>;
  timeSeriesData: Array<{ date: string; score: number; rank: number }>;
}

export default function LeaderboardPage() {
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);
  const [stats, setStats] = useState<CompetitionStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [competitionType, setCompetitionType] = useState('Karma');
  const [seasonType, setSeasonType] = useState('Weekly');

  const competitionTypes = ['Karma', 'Experience', 'EggCollection', 'QuestCompletion', 'SocialActivity', 'Messaging', 'FileSharing', 'VideoCalls', 'ChatSessions'];
  const seasonTypes = ['Daily', 'Weekly', 'Monthly', 'Quarterly', 'Yearly'];

  const colors = {
    karma: '#FF6B6B',
    experience: '#4ECDC4',
    eggcollection: '#45B7D1',
    questcompletion: '#96CEB4',
    socialactivity: '#FFEAA7',
    messaging: '#DDA0DD',
    filesharing: '#98D8C8',
    videocalls: '#F7DC6F',
    chatsessions: '#BB8FCE'
  };

  const leagueColors = {
    Bronze: '#CD7F32',
    Silver: '#C0C0C0',
    Gold: '#FFD700',
    Platinum: '#E5E4E2',
    Diamond: '#B9F2FF',
    Master: '#8A2BE2',
    GrandMaster: '#FF1493',
    Legend: '#FF4500',
    Mythic: '#8B008B',
    Transcendent: '#00CED1'
  };

  useEffect(() => {
    fetchLeaderboard();
    fetchStats();
  }, [competitionType, seasonType]);

  const fetchLeaderboard = async () => {
    try {
      setLoading(true);
      // Mock data for demonstration
      const mockLeaderboard: LeaderboardEntry[] = Array.from({ length: 20 }, (_, i) => ({
        id: `${i + 1}`,
        avatarId: `avatar-${i + 1}`,
        avatarName: `Player${i + 1}`,
        avatarUsername: `player${i + 1}`,
        competitionType,
        score: 10000 - i * 500,
        rank: i + 1,
        previousRank: i + 1 + Math.floor(Math.random() * 5 - 2),
        rankChange: Math.floor(Math.random() * 5 - 2),
        lastUpdated: new Date().toISOString(),
        seasonStart: new Date().toISOString(),
        seasonEnd: new Date().toISOString(),
        seasonType,
        currentLeague: i < 3 ? 'Diamond' : i < 7 ? 'Platinum' : i < 12 ? 'Gold' : 'Silver',
        previousLeague: 'Silver',
        leaguePromoted: false,
        leagueDemoted: false,
        stats: {},
        achievements: {},
        badges: []
      }));
      setLeaderboard(mockLeaderboard);
    } catch (error) {
      console.error('Error fetching leaderboard:', error);
    } finally {
      setLoading(false);
    }
  };

  const fetchStats = async () => {
    try {
      const mockStats: CompetitionStats = {
        totalParticipants: 1543,
        averageScore: 5420,
        topScore: 15780,
        myRank: 142,
        myScore: 7250,
        leagueDistribution: [
          { league: 'Bronze', count: 450, percentage: 29 },
          { league: 'Silver', count: 380, percentage: 25 },
          { league: 'Gold', count: 320, percentage: 21 },
          { league: 'Platinum', count: 220, percentage: 14 },
          { league: 'Diamond', count: 173, percentage: 11 }
        ],
        scoreDistribution: [
          { range: '0-1000', count: 250 },
          { range: '1001-5000', count: 580 },
          { range: '5001-10000', count: 420 },
          { range: '10001+', count: 293 }
        ],
        timeSeriesData: [
          { date: '2025-11-01', score: 5200, rank: 180 },
          { date: '2025-11-02', score: 5800, rank: 165 },
          { date: '2025-11-03', score: 6400, rank: 152 },
          { date: '2025-11-04', score: 6850, rank: 145 },
          { date: '2025-11-05', score: 7100, rank: 143 },
          { date: '2025-11-06', score: 7200, rank: 142 },
          { date: '2025-11-07', score: 7250, rank: 142 }
        ]
      };
      setStats(mockStats);
    } catch (error) {
      console.error('Error fetching stats:', error);
    }
  };

  const getRankIcon = (rank: number) => {
    if (rank === 1) return <Crown className="w-5 h-5 text-yellow-500" />;
    if (rank === 2) return <Medal className="w-5 h-5 text-gray-400" />;
    if (rank === 3) return <Medal className="w-5 h-5 text-amber-600" />;
    if (rank <= 10) return <Trophy className="w-4 h-4 text-blue-500" />;
    return <Star className="w-4 h-4 text-gray-400" />;
  };

  const getRankChangeIcon = (change: number) => {
    if (change > 0) return <TrendingUp className="w-4 h-4 text-green-500" />;
    if (change < 0) return <TrendingDown className="w-4 h-4 text-red-500" />;
    return <Minus className="w-4 h-4 text-gray-400" />;
  };

  const formatScore = (score: number) => {
    if (score >= 1000000) return `${(score / 1000000).toFixed(1)}M`;
    if (score >= 1000) return `${(score / 1000).toFixed(1)}K`;
    return score.toString();
  };

  return (
    <div className="container mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Competition Leaderboards</h1>
          <p className="text-gray-600">Track your progress and compete with other avatars across the OASIS</p>
        </div>
        <div className="flex gap-2">
          <Select value={competitionType} onValueChange={setCompetitionType}>
            <SelectTrigger className="w-48">
              <SelectValue placeholder="Competition Type" />
            </SelectTrigger>
            <SelectContent>
              {competitionTypes.map(type => (
                <SelectItem key={type} value={type}>{type}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select value={seasonType} onValueChange={setSeasonType}>
            <SelectTrigger className="w-32">
              <SelectValue placeholder="Season" />
            </SelectTrigger>
            <SelectContent>
              {seasonTypes.map(season => (
                <SelectItem key={season} value={season}>{season}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Stats Overview */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Total Participants</p>
                  <p className="text-2xl font-bold">{stats.totalParticipants.toLocaleString()}</p>
                </div>
                <Trophy className="w-8 h-8 text-blue-500" />
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Your Rank</p>
                  <p className="text-2xl font-bold">#{stats.myRank.toLocaleString()}</p>
                </div>
                <Star className="w-8 h-8 text-yellow-500" />
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Your Score</p>
                  <p className="text-2xl font-bold">{formatScore(stats.myScore)}</p>
                </div>
                <TrendingUp className="w-8 h-8 text-green-500" />
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Top Score</p>
                  <p className="text-2xl font-bold">{formatScore(stats.topScore)}</p>
                </div>
                <Crown className="w-8 h-8 text-purple-500" />
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      <Tabs defaultValue="leaderboard" className="space-y-6">
        <TabsList>
          <TabsTrigger value="leaderboard">Leaderboard</TabsTrigger>
          <TabsTrigger value="analytics">Analytics</TabsTrigger>
          <TabsTrigger value="leagues">Leagues</TabsTrigger>
        </TabsList>

        {/* Leaderboard Tab */}
        <TabsContent value="leaderboard">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Trophy className="w-5 h-5" />
                {competitionType} Leaderboard - {seasonType}
              </CardTitle>
            </CardHeader>
            <CardContent>
              {loading ? (
                <div className="flex items-center justify-center h-64">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
                </div>
              ) : (
                <div className="space-y-2">
                  {leaderboard.map((entry, index) => (
                    <div
                      key={entry.id}
                      className={`flex items-center justify-between p-4 rounded-lg border ${
                        index < 3 ? 'bg-gradient-to-r from-yellow-50 to-orange-50 border-yellow-200' : 'bg-white'
                      }`}
                    >
                      <div className="flex items-center gap-4">
                        <div className="flex items-center gap-2">
                          {getRankIcon(entry.rank)}
                          <span className="font-bold text-lg">#{entry.rank}</span>
                        </div>
                        <div>
                          <p className="font-semibold">{entry.avatarName}</p>
                          <p className="text-sm text-gray-600">@{entry.avatarUsername}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-4">
                        <div className="text-right">
                          <p className="font-bold text-lg">{formatScore(entry.score)}</p>
                          <div className="flex items-center gap-1">
                            {getRankChangeIcon(entry.rankChange)}
                            <span className={`text-sm ${entry.rankChange > 0 ? 'text-green-500' : entry.rankChange < 0 ? 'text-red-500' : 'text-gray-500'}`}>
                              {entry.rankChange > 0 ? `+${entry.rankChange}` : entry.rankChange}
                            </span>
                          </div>
                        </div>
                        <Badge className="bg-blue-100 text-blue-800 border-blue-200">
                          {entry.currentLeague}
                        </Badge>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Analytics Tab */}
        <TabsContent value="analytics">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Score Distribution */}
            <Card>
              <CardHeader>
                <CardTitle>Score Distribution</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={stats?.scoreDistribution || []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="range" />
                    <YAxis />
                    <Tooltip />
                    <Bar dataKey="count" fill={colors[competitionType.toLowerCase() as keyof typeof colors] || '#8884d8'} />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            {/* League Distribution */}
            <Card>
              <CardHeader>
                <CardTitle>League Distribution</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie
                      data={stats?.leagueDistribution || []}
                      cx="50%"
                      cy="50%"
                      labelLine={false}
                      label={({ league, percentage }) => `${league} (${percentage}%)`}
                      outerRadius={80}
                      fill="#8884d8"
                      dataKey="count"
                    >
                      {stats?.leagueDistribution?.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={leagueColors[entry.league as keyof typeof leagueColors] || '#8884d8'} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            {/* Time Series */}
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle>Score Over Time</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={stats?.timeSeriesData || []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" />
                    <YAxis />
                    <Tooltip />
                    <Line type="monotone" dataKey="score" stroke={colors[competitionType.toLowerCase() as keyof typeof colors] || '#8884d8'} strokeWidth={2} />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        {/* Leagues Tab */}
        <TabsContent value="leagues">
          <Card>
            <CardHeader>
              <CardTitle>League System</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {Object.entries(leagueColors).map(([league, color]) => (
                  <div key={league} className="p-4 rounded-lg border" style={{ borderLeftColor: color, borderLeftWidth: '4px' }}>
                    <div className="flex items-center gap-2 mb-2">
                      <div className="w-4 h-4 rounded-full" style={{ backgroundColor: color }}></div>
                      <h3 className="font-semibold">{league} League</h3>
                    </div>
                    <p className="text-sm text-gray-600">Score Range: {league === 'Bronze' ? '0-1,000' : league === 'Silver' ? '1,001-5,000' : '5,001+'}</p>
                    <p className="text-sm text-gray-600">Players: {Math.floor(Math.random() * 1000) + 100}</p>
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

