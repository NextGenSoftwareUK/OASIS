import React, { useMemo } from 'react';
import { Box, Typography, Card, CardContent, Button, Grid, Chip, CircularProgress } from '@mui/material';
import { useQuery } from 'react-query';
import { competitionService } from '../services';

const CompetitionPage: React.FC = () => {
  const avatarId = useMemo(() => localStorage.getItem('avatarId') || 'demo-avatar', []);

  const { data: seasonData, isLoading: loadingSeason, refetch: refetchSeason } = useQuery(
    ['competition-season'],
    async () => {
      const res = await competitionService.getCurrentSeason();
      return res.result;
    }
  );

  const { data: rankData, isLoading: loadingRank, refetch: refetchRank } = useQuery(
    ['competition-rank', avatarId],
    async () => {
      const res = await competitionService.getAvatarRank(avatarId);
      return res.result;
    }
  );

  const isLoading = loadingSeason || loadingRank;

  return (
    <Box sx={{ mb: 4, mt: 4 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" gutterBottom className="page-heading">🏆 Competition</Typography>
          <Typography variant="subtitle1" color="text.secondary">Season status, ranks, and leagues</Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button variant="outlined" onClick={() => { refetchSeason(); refetchRank(); }} disabled={isLoading}>Refresh</Button>
        </Box>
      </Box>

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress size={60} />
        </Box>
      ) : (
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" sx={{ mb: 1 }}>Current Season</Typography>
                <Typography variant="body2" color="text.secondary">{seasonData?.name} • {seasonData?.status}</Typography>
                <Typography variant="caption" color="text.secondary">Started: {seasonData?.startedOn ? new Date(seasonData.startedOn).toLocaleString() : 'unknown'}</Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" sx={{ mb: 1 }}>My Rank</Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <Chip label={`#${rankData?.rank ?? '-'}`} color="primary" />
                  <Chip label={`Score: ${rankData?.score ?? 0}`} />
                  <Chip label={`League: ${rankData?.league ?? 'Unranked'}`} color="secondary" />
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}
    </Box>
  );
};

export default CompetitionPage;


