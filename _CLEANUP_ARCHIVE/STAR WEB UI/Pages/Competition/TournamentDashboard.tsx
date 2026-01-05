import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Trophy, Crown, Medal, Star, Clock, Users, Target, Zap, Flame, Snowflake, Droplets, Wind, Mountain } from 'lucide-react';
import { ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, PieChart, Pie, Cell, LineChart, Line } from 'recharts';

interface Tournament {
  id: string;
  name: string;
  description: string;
  type: string;
  status: string;
  startDate: string;
  endDate: string;
  registrationStart: string;
  registrationEnd: string;
  maxParticipants: number;
  currentParticipants: number;
  minLevel: number;
  maxLevel: number;
  rewards: Array<{ position: number; type: string; name: string; value: number }>;
  participants: Array<{ id: string; name: string; score: number; rank: number }>;
  matches: Array<{ id: string; participant1: string; participant2: string; status: string; winner?: string }>;
}

interface TournamentStats {
  totalTournaments: number;
  activeTournaments: number;
  completedTournaments: number;
  totalParticipants: number;
  averageParticipants: number;
  winRate: number;
  bestFinish: number;
  totalWinnings: number;
  tournamentTypes: Array<{ type: string; count: number }>;
  participationHistory: Array<{ date: string; tournaments: number; wins: number }>;
}

const TournamentDashboard: React.FC = () => {
  const [tournaments, setTournaments] = useState<Tournament[]>([]);
  const [stats, setStats] = useState<TournamentStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedTournament, setSelectedTournament] = useState<string | null>(null);

  const tournamentTypeIcons = {
    'Single Elimination': <Target className="w-5 h-5 text-red-500" />,
    'Double Elimination': <Target className="w-5 h-5 text-orange-500" />,
    'Round Robin': <Users className="w-5 h-5 text-blue-500" />,
    'Swiss': <Star className="w-5 h-5 text-purple-500" />,
    'Bracket': <Trophy className="w-5 h-5 text-yellow-500" />,
    'League': <Crown className="w-5 h-5 text-green-500" />,
    'Knockout': <Zap className="w-5 h-5 text-pink-500" />,
    'Group Stage': <Users className="w-5 h-5 text-indigo-500" />
  };

  const statusColors = {
    'Upcoming': 'bg-blue-100 text-blue-800',
    'Registration': 'bg-green-100 text-green-800',
    'Active': 'bg-yellow-100 text-yellow-800',
    'Completed': 'bg-gray-100 text-gray-800',
    'Cancelled': 'bg-red-100 text-red-800',
    'Paused': 'bg-orange-100 text-orange-800'
  };

  const competitionIcons = {
    'Karma': <Star className="w-4 h-4 text-yellow-500" />,
    'Experience': <Zap className="w-4 h-4 text-blue-500" />,
    'EggCollection': <Trophy className="w-4 h-4 text-purple-500" />,
    'QuestCompletion': <Target className="w-4 h-4 text-green-500" />,
    'SocialActivity': <Users className="w-4 h-4 text-pink-500" />,
    'Messaging': <Users className="w-4 h-4 text-indigo-500" />,
    'FileSharing': <Users className="w-4 h-4 text-cyan-500" />,
    'VideoCalls': <Users className="w-4 h-4 text-red-500" />,
    'ChatSessions': <Users className="w-4 h-4 text-orange-500" />
  };

  useEffect(() => {
    fetchTournaments();
    fetchStats();
  }, []);

  const fetchTournaments = async () => {
    try {
      setLoading(true);
      // Mock data for demonstration
      const mockTournaments: Tournament[] = [
        {
          id: '1',
          name: 'Weekly Karma Championship',
          description: 'Compete for the highest karma score this week!',
          type: 'Single Elimination',
          status: 'Registration',
          startDate: '2024-01-15T10:00:00Z',
          endDate: '2024-01-22T18:00:00Z',
          registrationStart: '2024-01-08T00:00:00Z',
          registrationEnd: '2024-01-15T09:00:00Z',
          maxParticipants: 64,
          currentParticipants: 42,
          minLevel: 1,
          maxLevel: 100,
          rewards: [
            { position: 1, type: 'Karma', name: 'Champion Karma', value: 10000 },
            { position: 2, type: 'Karma', name: 'Runner-up Karma', value: 5000 },
            { position: 3, type: 'Karma', name: 'Third Place Karma', value: 2500 }
          ],
          participants: [],
          matches: []
        },
        {
          id: '2',
          name: 'Egg Collection Masters',
          description: 'Who can collect the most unique eggs?',
          type: 'Round Robin',
          status: 'Active',
          startDate: '2024-01-10T00:00:00Z',
          endDate: '2024-01-17T23:59:59Z',
          registrationStart: '2024-01-03T00:00:00Z',
          registrationEnd: '2024-01-10T00:00:00Z',
          maxParticipants: 32,
          currentParticipants: 32,
          minLevel: 5,
          maxLevel: 50,
          rewards: [
            { position: 1, type: 'Egg', name: 'Golden Dragon Egg', value: 1 },
            { position: 2, type: 'Egg', name: 'Silver Phoenix Egg', value: 1 },
            { position: 3, type: 'Egg', name: 'Bronze Unicorn Egg', value: 1 }
          ],
          participants: [
            { id: '1', name: 'DragonMaster', score: 156, rank: 1 },
            { id: '2', name: 'EggHunter', score: 142, rank: 2 },
            { id: '3', name: 'CollectorPro', score: 128, rank: 3 }
          ],
          matches: []
        }
      ];
      setTournaments(mockTournaments);
    } catch (error) {
      console.error('Error fetching tournaments:', error);
    } finally {
      setLoading(false);
    }
  };

  const fetchStats = async () => {
    try {
      // Mock data for demonstration
      const mockStats: TournamentStats = {
        totalTournaments: 24,
        activeTournaments: 3,
        completedTournaments: 21,
        totalParticipants: 156,
        averageParticipants: 32,
        winRate: 68.5,
        bestFinish: 1,
        totalWinnings: 45000,
        tournamentTypes: [
          { type: 'Single Elimination', count: 12 },
          { type: 'Round Robin', count: 8 },
          { type: 'Double Elimination', count: 4 }
        ],
        participationHistory: [
          { date: '2024-01-01', tournaments: 2, wins: 1 },
          { date: '2024-01-08', tournaments: 1, wins: 1 },
          { date: '2024-01-15', tournaments: 3, wins: 2 }
        ]
      };
      setStats(mockStats);
    } catch (error) {
      console.error('Error fetching tournament stats:', error);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getTimeUntilStart = (startDate: string) => {
    const now = new Date();
    const start = new Date(startDate);
    const diff = start.getTime() - now.getTime();
    
    if (diff <= 0) return 'Started';
    
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    
    if (days > 0) return `${days}d ${hours}h`;
    if (hours > 0) return `${hours}h ${minutes}m`;
    return `${minutes}m`;
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
        <h1 className="text-4xl font-bold text-gray-900 mb-2">🏆 Tournament Dashboard</h1>
        <p className="text-gray-600">Compete in tournaments and climb the leaderboards!</p>
      </div>

      {/* Stats Overview */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Card className="bg-gradient-to-r from-blue-50 to-indigo-50">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Total Tournaments</p>
                  <p className="text-2xl font-bold">{stats.totalTournaments}</p>
                </div>
                <Trophy className="w-8 h-8 text-blue-500" />
              </div>
            </CardContent>
          </Card>
          <Card className="bg-gradient-to-r from-green-50 to-emerald-50">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Win Rate</p>
                  <p className="text-2xl font-bold">{stats.winRate}%</p>
                </div>
                <Target className="w-8 h-8 text-green-500" />
              </div>
            </CardContent>
          </Card>
          <Card className="bg-gradient-to-r from-yellow-50 to-orange-50">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Best Finish</p>
                  <p className="text-2xl font-bold">#{stats.bestFinish}</p>
                </div>
                <Crown className="w-8 h-8 text-yellow-500" />
              </div>
            </CardContent>
          </Card>
          <Card className="bg-gradient-to-r from-purple-50 to-pink-50">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Total Winnings</p>
                  <p className="text-2xl font-bold">{stats.totalWinnings.toLocaleString()}</p>
                </div>
                <Star className="w-8 h-8 text-purple-500" />
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      <Tabs defaultValue="active" className="space-y-6">
        <TabsList>
          <TabsTrigger value="active">Active Tournaments</TabsTrigger>
          <TabsTrigger value="upcoming">Upcoming</TabsTrigger>
          <TabsTrigger value="completed">Completed</TabsTrigger>
          <TabsTrigger value="analytics">Analytics</TabsTrigger>
        </TabsList>

        {/* Active Tournaments Tab */}
        <TabsContent value="active">
          <div className="space-y-6">
            {tournaments.filter(t => t.status === 'Active').map((tournament) => (
              <Card key={tournament.id} className="hover:shadow-lg transition-shadow">
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      {tournamentTypeIcons[tournament.type as keyof typeof tournamentTypeIcons]}
                      <div>
                        <CardTitle className="text-xl">{tournament.name}</CardTitle>
                        <p className="text-gray-600">{tournament.description}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge className={statusColors[tournament.status as keyof typeof statusColors]}>
                        {tournament.status}
                      </Badge>
                      <Button variant="outline" size="sm">View Details</Button>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div>
                      <p className="text-sm font-medium text-gray-600">Participants</p>
                      <p className="text-lg font-semibold">{tournament.currentParticipants}/{tournament.maxParticipants}</p>
                      <Progress value={(tournament.currentParticipants / tournament.maxParticipants) * 100} className="mt-2" />
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-600">Duration</p>
                      <p className="text-lg font-semibold">
                        {Math.ceil((new Date(tournament.endDate).getTime() - new Date(tournament.startDate).getTime()) / (1000 * 60 * 60 * 24))} days
                      </p>
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-600">Level Range</p>
                      <p className="text-lg font-semibold">{tournament.minLevel}-{tournament.maxLevel}</p>
                    </div>
                  </div>
                  
                  {tournament.participants.length > 0 && (
                    <div className="mt-6">
                      <h4 className="font-semibold mb-3">Current Standings</h4>
                      <div className="space-y-2">
                        {tournament.participants.slice(0, 5).map((participant, index) => (
                          <div key={participant.id} className="flex items-center justify-between p-3 rounded-lg bg-gray-50">
                            <div className="flex items-center gap-3">
                              {index === 0 && <Crown className="w-5 h-5 text-yellow-500" />}
                              {index === 1 && <Medal className="w-5 h-5 text-gray-400" />}
                              {index === 2 && <Medal className="w-5 h-5 text-amber-600" />}
                              {index > 2 && <Star className="w-4 h-4 text-blue-500" />}
                              <span className="font-semibold">#{participant.rank}</span>
                              <span>{participant.name}</span>
                            </div>
                            <span className="font-bold">{participant.score}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        {/* Upcoming Tournaments Tab */}
        <TabsContent value="upcoming">
          <div className="space-y-6">
            {tournaments.filter(t => t.status === 'Registration' || t.status === 'Upcoming').map((tournament) => (
              <Card key={tournament.id} className="hover:shadow-lg transition-shadow">
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      {tournamentTypeIcons[tournament.type as keyof typeof tournamentTypeIcons]}
                      <div>
                        <CardTitle className="text-xl">{tournament.name}</CardTitle>
                        <p className="text-gray-600">{tournament.description}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge className={statusColors[tournament.status as keyof typeof statusColors]}>
                        {tournament.status}
                      </Badge>
                      <Button size="sm">Join Tournament</Button>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                    <div>
                      <p className="text-sm font-medium text-gray-600">Starts In</p>
                      <p className="text-lg font-semibold">{getTimeUntilStart(tournament.startDate)}</p>
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-600">Participants</p>
                      <p className="text-lg font-semibold">{tournament.currentParticipants}/{tournament.maxParticipants}</p>
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-600">Registration</p>
                      <p className="text-lg font-semibold">
                        {new Date(tournament.registrationEnd) > new Date() ? 'Open' : 'Closed'}
                      </p>
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-600">Level Range</p>
                      <p className="text-lg font-semibold">{tournament.minLevel}-{tournament.maxLevel}</p>
                    </div>
                  </div>
                  
                  <div className="mt-6">
                    <h4 className="font-semibold mb-3">Rewards</h4>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                      {tournament.rewards.slice(0, 3).map((reward, index) => (
                        <div key={index} className="p-3 rounded-lg bg-gradient-to-r from-yellow-50 to-orange-50 border border-yellow-200">
                          <div className="flex items-center gap-2 mb-1">
                            {index === 0 && <Crown className="w-4 h-4 text-yellow-500" />}
                            {index === 1 && <Medal className="w-4 h-4 text-gray-400" />}
                            {index === 2 && <Medal className="w-4 h-4 text-amber-600" />}
                            <span className="font-semibold">#{reward.position}</span>
                          </div>
                          <p className="text-sm text-gray-600">{reward.name}</p>
                          <p className="font-bold text-lg">{reward.value.toLocaleString()} {reward.type}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        {/* Completed Tournaments Tab */}
        <TabsContent value="completed">
          <Card>
            <CardHeader>
              <CardTitle>Completed Tournaments</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8">
                <Trophy className="w-16 h-16 text-gray-400 mx-auto mb-4" />
                <h3 className="text-lg font-semibold text-gray-600 mb-2">No Completed Tournaments</h3>
                <p className="text-gray-500">Your tournament history will appear here once you complete tournaments.</p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Analytics Tab */}
        <TabsContent value="analytics">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Tournament Types */}
            <Card>
              <CardHeader>
                <CardTitle>Tournament Types</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie
                      data={stats?.tournamentTypes || []}
                      cx="50%"
                      cy="50%"
                      labelLine={false}
                      label={({ type, count }) => `${type} (${count})`}
                      outerRadius={80}
                      fill="#8884d8"
                      dataKey="count"
                    >
                      {stats?.tournamentTypes?.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={`hsl(${index * 60}, 70%, 50%)`} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            {/* Participation History */}
            <Card>
              <CardHeader>
                <CardTitle>Participation History</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={stats?.participationHistory || []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" />
                    <YAxis />
                    <Tooltip />
                    <Line type="monotone" dataKey="tournaments" stroke="#8884d8" strokeWidth={2} name="Tournaments" />
                    <Line type="monotone" dataKey="wins" stroke="#82ca9d" strokeWidth={2} name="Wins" />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default TournamentDashboard;
