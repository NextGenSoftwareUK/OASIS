import React, { useMemo, useState, useEffect } from 'react';
import { Box, Typography, Card, CardContent, Button, Grid, TextField, CircularProgress, Tabs, Tab, List, ListItem, ListItemText } from '@mui/material';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { messagingService } from '../services';
import signalRService from '../services/signalRService';

const MessagingPage: React.FC = () => {
  const queryClient = useQueryClient();
  const avatarId = useMemo(() => localStorage.getItem('avatarId') || 'demo-avatar', []);
  const [tab, setTab] = useState(0);
  const [to, setTo] = useState('');
  const [content, setContent] = useState('');
  const [liveInbox, setLiveInbox] = useState<any[]>([]);

  useEffect(() => {
    (async () => { try { await signalRService.start(); } catch {} })();
    const onMessage = ({ user, message }: any) => {
      setLiveInbox(prev => [{ id: `live-${Date.now()}`, fromAvatarId: user, toAvatarId: avatarId, content: message, timestamp: new Date().toISOString() }, ...prev]);
    };
    signalRService.on('messageReceived', onMessage);
    return () => { signalRService.off('messageReceived', onMessage); };
  }, [avatarId]);

  const { data: inbox, isLoading: inboxLoading, refetch: refetchInbox } = useQuery(
    ['inbox'],
    async () => (await messagingService.getInbox()).result || []
  );

  const { data: sent, isLoading: sentLoading, refetch: refetchSent } = useQuery(
    ['sent', avatarId],
    async () => (await messagingService.getSent(avatarId)).result || []
  );

  const sendMutation = useMutation(
    async () => (await messagingService.sendMessage(to, content)).result,
    { onSuccess: () => { queryClient.invalidateQueries(['sent', avatarId]); setTo(''); setContent(''); } }
  );

  return (
    <Box sx={{ mb: 4, mt: 4 }}>
      <Typography variant="h4" gutterBottom className="page-heading">✉️ Messaging</Typography>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>Send Message</Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={4}>
              <TextField fullWidth label="To Avatar ID" value={to} onChange={(e) => setTo(e.target.value)} />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField fullWidth label="Content" value={content} onChange={(e) => setContent(e.target.value)} />
            </Grid>
            <Grid item xs={12} md={2}>
              <Button fullWidth variant="contained" onClick={() => sendMutation.mutate()} disabled={!to || !content || sendMutation.isLoading}>
                {sendMutation.isLoading ? 'Sending...' : 'Send'}
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Tabs value={tab} onChange={(e, v) => setTab(v)} sx={{ mb: 2 }}>
            <Tab label="Inbox" />
            <Tab label="Sent" />
          </Tabs>
          {tab === 0 && (inboxLoading ? <CircularProgress /> : (
            <List>
              {[...liveInbox, ...(inbox as any[])].map((m: any) => (
                <ListItem key={`${m.id}-${m.timestamp}`} divider>
                  <ListItemText primary={`From: ${m.fromAvatarId}`} secondary={`${m.content} • ${new Date(m.timestamp).toLocaleString()}`} />
                </ListItem>
              ))}
            </List>
          ))}
          {tab === 1 && (sentLoading ? <CircularProgress /> : (
            <List>
              {(sent as any[]).map((m: any) => (
                <ListItem key={`${m.id}-${m.timestamp}`} divider>
                  <ListItemText primary={`To: ${m.toAvatarId}`} secondary={`${m.content} • ${new Date(m.timestamp).toLocaleString()}`} />
                </ListItem>
              ))}
            </List>
          ))}
        </CardContent>
      </Card>
    </Box>
  );
};

export default MessagingPage;


