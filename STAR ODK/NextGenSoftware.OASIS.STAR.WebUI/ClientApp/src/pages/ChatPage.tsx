import React, { useEffect, useMemo, useState } from 'react';
import { Box, Typography, Card, CardContent, Button, Grid, TextField, CircularProgress, MenuItem, Select, InputLabel, FormControl, List, ListItem, ListItemText } from '@mui/material';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { chatService } from '../services';
import signalRService from '../services/signalRService';

const ChatPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [channelId, setChannelId] = useState('');
  const [message, setMessage] = useState('');
  const [liveChannelMessages, setLiveChannelMessages] = useState<any[]>([]);

  useEffect(() => {
    (async () => { try { await signalRService.start(); } catch {} })();
    const onMessage = ({ user, message }: any) => {
      setLiveChannelMessages(prev => [{ id: `live-${Date.now()}`, from: user, content: message, timestamp: new Date().toISOString() }, ...prev]);
    };
    signalRService.on('messageReceived', onMessage);
    return () => { signalRService.off('messageReceived', onMessage); };
  }, []);

  const { data: channels, isLoading: channelsLoading } = useQuery(
    ['chat-channels'],
    async () => (await chatService.getChannels()).result || []
  );

  const { data: messages, isLoading: messagesLoading, refetch } = useQuery(
    ['chat-messages', channelId],
    async () => channelId ? (await chatService.getChannelMessages(channelId)).result || [] : [],
    { enabled: !!channelId }
  );

  const sendMutation = useMutation(
    async () => (await chatService.sendChannelMessage(channelId, message)).result,
    { onSuccess: () => { queryClient.invalidateQueries(['chat-messages', channelId]); setMessage(''); } }
  );

  return (
    <Box sx={{ mb: 4, mt: 4 }}>
      <Typography variant="h4" gutterBottom className="page-heading">💬 Chat</Typography>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} md={4}>
              <FormControl fullWidth>
                <InputLabel>Channel</InputLabel>
                <Select value={channelId} label="Channel" onChange={(e) => setChannelId(e.target.value)}>
                  {(channels || []).map((ch: any) => (
                    <MenuItem key={ch.id} value={ch.id}>{ch.name}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField fullWidth label="Message" value={message} onChange={(e) => setMessage(e.target.value)} />
            </Grid>
            <Grid item xs={12} md={2}>
              <Button fullWidth variant="contained" onClick={() => sendMutation.mutate()} disabled={!channelId || !message || sendMutation.isLoading}>
                {sendMutation.isLoading ? 'Sending...' : 'Send'}
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {channelsLoading ? <CircularProgress /> : null}

      <Card>
        <CardContent>
          {messagesLoading ? <CircularProgress /> : (
            <List>
              {[...liveChannelMessages, ...(messages as any[])].map((m: any) => (
                <ListItem key={`${m.id}-${m.timestamp}`} divider>
                  <ListItemText primary={`${m.from || 'user'}`} secondary={`${m.content} • ${new Date(m.timestamp || Date.now()).toLocaleString()}`} />
                </ListItem>
              ))}
            </List>
          )}
        </CardContent>
      </Card>
    </Box>
  );
};

export default ChatPage;


