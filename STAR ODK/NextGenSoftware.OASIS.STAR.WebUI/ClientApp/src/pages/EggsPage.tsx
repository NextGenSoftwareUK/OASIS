import React, { useMemo } from 'react';
import { Box, Typography, Card, CardContent, Button, Grid, Chip, CircularProgress } from '@mui/material';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { eggsService } from '../services';

interface EggItem {
  id: string;
  type?: string;
  discoveredAt?: string;
  status?: string;
}

const EggsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const { data: galleryData, isLoading, refetch } = useQuery(
    ['eggs-gallery'],
    async () => {
      const res = await eggsService.getGallery();
      return res.result || [];
    }
  );

  const discoverMutation = useMutation(
    async () => (await eggsService.discover('00000000-0000-0000-0000-000000000000')).result,
    {
      onSuccess: () => { queryClient.invalidateQueries(['eggs-gallery']); },
    }
  );

  const hatchMutation = useMutation(
    async (eggId: string) => (await eggsService.hatch(eggId)).result,
    {
      onSuccess: () => { queryClient.invalidateQueries(['eggs-gallery']); },
    }
  );

  return (
    <Box sx={{ mb: 4, mt: 4 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" gutterBottom className="page-heading">🥚 Eggs</Typography>
          <Typography variant="subtitle1" color="text.secondary">Discover, hatch and manage your eggs</Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button variant="outlined" onClick={() => refetch()} disabled={isLoading}>Refresh</Button>
          <Button variant="contained" onClick={() => discoverMutation.mutate()} disabled={discoverMutation.isLoading}>
            {discoverMutation.isLoading ? 'Discovering...' : 'Discover Eggs'}
          </Button>
        </Box>
      </Box>

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress size={60} />
        </Box>
      ) : (
        <Grid container spacing={3}>
          {(galleryData as EggItem[]).map((egg) => (
            <Grid item xs={12} sm={6} md={4} key={egg.id}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                    <Typography variant="h6">Egg {egg.id.substring(0, 6)}</Typography>
                    <Chip label={egg.status || 'unknown'} size="small" />
                  </Box>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    Type: {egg.type || 'mystery'}
                  </Typography>
                  <Button
                    variant="contained"
                    onClick={() => hatchMutation.mutate(egg.id)}
                    disabled={hatchMutation.isLoading || egg.status === 'hatched'}
                  >
                    {hatchMutation.isLoading ? 'Hatching...' : egg.status === 'hatched' ? 'Hatched' : 'Hatch'}
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

export default EggsPage;


