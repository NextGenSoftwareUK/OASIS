import React, { useMemo, useState } from 'react';
import { Box, Typography, Card, CardContent, Button, Grid, TextField, CircularProgress, List, ListItem, ListItemText } from '@mui/material';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { socialService } from '../services';

const SocialPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [holonId, setHolonId] = useState('');
  const [message, setMessage] = useState('');
  const avatarId = 'current-user'; // Placeholder for current user

  const { data: feed, isLoading, refetch } = useQuery(
    ['social-feed'],
    async () => (await socialService.getFeed()).result || []
  );

  const shareMutation = useMutation(
    async () => (await socialService.shareHolon(holonId, message)).result,
    { onSuccess: () => { queryClient.invalidateQueries(['social-feed', avatarId]); setHolonId(''); setMessage(''); } }
  );

  return (
    <Box sx={{ mb: 4, mt: 4 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" gutterBottom className="page-heading">🌐 Social</Typography>
          <Typography variant="subtitle1" color="text.secondary">Feed and sharing</Typography>
        </Box>
        <Button variant="outlined" onClick={() => refetch()} disabled={isLoading}>Refresh</Button>
      </Box>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} md={4}>
              <TextField fullWidth label="Holon ID" value={holonId} onChange={(e) => setHolonId(e.target.value)} />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField fullWidth label="Message (optional)" value={message} onChange={(e) => setMessage(e.target.value)} />
            </Grid>
            <Grid item xs={12} md={2}>
              <Button fullWidth variant="contained" onClick={() => shareMutation.mutate()} disabled={!holonId || shareMutation.isLoading}>
                {shareMutation.isLoading ? 'Sharing...' : 'Share'}
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {isLoading ? <CircularProgress /> : (
        <Card>
          <CardContent>
            <List>
              {(feed as any[]).map((item: any) => (
                <ListItem key={`${item.id}-${item.timestamp}`} divider>
                  <ListItemText primary={item.message || 'Shared content'} secondary={`Holon: ${item.holonId} • ${new Date(item.timestamp).toLocaleString()}`} />
                </ListItem>
              ))}
            </List>
          </CardContent>
        </Card>
      )}
    </Box>
  );
};

export default SocialPage;


