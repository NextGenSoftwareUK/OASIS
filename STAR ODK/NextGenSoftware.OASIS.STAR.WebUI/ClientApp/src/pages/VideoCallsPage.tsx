import React, { useMemo, useState } from 'react';
import { Box, Typography, Card, CardContent, Button, Grid, TextField, CircularProgress } from '@mui/material';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { videoService } from '../services';

const VideoCallsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const avatarId = useMemo(() => localStorage.getItem('avatarId') || 'demo-avatar', []);
  const [calleeId, setCalleeId] = useState('');

  const { data: activeCalls, isLoading, refetch } = useQuery(
    ['active-calls', avatarId],
    async () => {
      const res = await videoService.getActiveCalls(avatarId);
      return res.result || [];
    }
  );

  const startCallMutation = useMutation(
    async () => (await videoService.startCall([avatarId, calleeId])).result,
    { onSuccess: () => queryClient.invalidateQueries(['active-calls', avatarId]) }
  );

  const endCallMutation = useMutation(
    async (sessionId: string) => (await videoService.endCall(sessionId)).result,
    { onSuccess: () => queryClient.invalidateQueries(['active-calls', avatarId]) }
  );

  return (
    <Box sx={{ mb: 4, mt: 4 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" gutterBottom className="page-heading">🎥 Video Calls</Typography>
          <Typography variant="subtitle1" color="text.secondary">Start and manage your calls</Typography>
        </Box>
        <Button variant="outlined" onClick={() => refetch()} disabled={isLoading}>Refresh</Button>
      </Box>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>Start a Call</Typography>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <TextField label="Callee Avatar ID" value={calleeId} onChange={(e) => setCalleeId(e.target.value)} fullWidth />
            <Button variant="contained" onClick={() => startCallMutation.mutate()} disabled={!calleeId || startCallMutation.isLoading}>
              {startCallMutation.isLoading ? 'Starting...' : 'Start Call'}
            </Button>
          </Box>
        </CardContent>
      </Card>

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress size={60} />
        </Box>
      ) : (
        <Grid container spacing={3}>
          {(activeCalls as any[]).map((call: any) => (
            <Grid item xs={12} md={6} key={call.sessionId}>
              <Card>
                <CardContent>
                  <Typography variant="h6" sx={{ mb: 1 }}>Session {call.sessionId}</Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>Started: {call.startedAt || 'unknown'}</Typography>
                  <Button variant="outlined" onClick={() => endCallMutation.mutate(call.sessionId)} disabled={endCallMutation.isLoading}>
                    {endCallMutation.isLoading ? 'Ending...' : 'End Call'}
                  </Button>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </Box>
  );
};

export default VideoCallsPage;


