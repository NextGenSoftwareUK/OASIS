import React, { useState } from 'react';
import {
  Box, Typography, Card, CardContent, Grid, Button, Chip, TextField,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
  IconButton, Tooltip, Alert, AlertTitle, CircularProgress, Divider,
  List, ListItem, ListItemText, ListItemIcon, ListItemSecondaryAction,
  Dialog, DialogTitle, DialogContent, DialogActions, DialogContentText,
  FormControl, InputLabel, Select, MenuItem, Avatar, LinearProgress
} from '@mui/material';
import {
  Nature, Send, TrendingUp, History, Add, Star, AttachMoney,
  Payment, Favorite, Group, AccountBalance, Refresh
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useQueryClient } from 'react-query';
import { seedsService } from '../services/core/seedsService';

const SeedsPage: React.FC = () => {
  const [selectedAvatarId] = useState('demo-avatar-1');
  const [sendDialogOpen, setSendDialogOpen] = useState(false);
  const [donationDialogOpen, setDonationDialogOpen] = useState(false);
  const [sendTo, setSendTo] = useState('');
  const [sendAmount, setSendAmount] = useState('');
  const [sendDescription, setSendDescription] = useState('');
  const [donationAmount, setDonationAmount] = useState('');
  const [donationDescription, setDonationDescription] = useState('');
  const queryClient = useQueryClient();

  // Queries
  const { data: balanceData, isLoading: balanceLoading } = useQuery(
    'seeds-balance',
    () => seedsService.getBalance(selectedAvatarId)
  );

  const { data: priceData, isLoading: priceLoading } = useQuery(
    'seeds-price',
    () => seedsService.getPrice()
  );

  const { data: transactionsData, isLoading: transactionsLoading } = useQuery(
    'seeds-transactions',
    () => seedsService.getTransactionHistory(selectedAvatarId, 20)
  );

  const { data: organizationsData, isLoading: organizationsLoading } = useQuery(
    'seeds-organizations',
    () => seedsService.getOrganizations()
  );

  const handleSend = async () => {
    try {
      await seedsService.sendTokens(sendTo, parseFloat(sendAmount), sendDescription);
      queryClient.invalidateQueries('seeds-balance');
      queryClient.invalidateQueries('seeds-transactions');
      setSendDialogOpen(false);
      setSendTo('');
      setSendAmount('');
      setSendDescription('');
    } catch (error) {
      console.error('Send failed:', error);
    }
  };

  const handleDonation = async () => {
    try {
      await seedsService.donate(parseFloat(donationAmount), donationDescription);
      queryClient.invalidateQueries('seeds-balance');
      queryClient.invalidateQueries('seeds-transactions');
      setDonationDialogOpen(false);
      setDonationAmount('');
      setDonationDescription('');
    } catch (error) {
      console.error('Donation failed:', error);
    }
  };

  const getTransactionIcon = (type: string) => {
    switch (type) {
      case 'send': return <Send />;
      case 'receive': return <Payment />;
      case 'donation': return <Favorite />;
      case 'reward': return <Star />;
      default: return <Payment />;
    }
  };

  const getTransactionColor = (type: string) => {
    switch (type) {
      case 'send': return 'primary';
      case 'receive': return 'success';
      case 'donation': return 'success';
      case 'reward': return 'warning';
      default: return 'default';
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        {/* Header */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Typography variant="h4" component="h1" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Nature color="primary" />
            SEEDS Token Management
          </Typography>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={() => {
                queryClient.invalidateQueries('seeds-balance');
                queryClient.invalidateQueries('seeds-price');
                queryClient.invalidateQueries('seeds-transactions');
              }}
            >
              Refresh
            </Button>
            <Button
              variant="contained"
              startIcon={<Send />}
              onClick={() => setSendDialogOpen(true)}
            >
              Send SEEDS
            </Button>
          </Box>
        </Box>

        {/* Balance and Price Overview */}
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <AccountBalance color="primary" />
                  Your SEEDS Balance
                </Typography>
                {balanceLoading ? (
                  <CircularProgress />
                ) : balanceData?.result ? (
                  <Box>
                    <Typography variant="h3" color="primary" sx={{ mb: 1 }}>
                      {balanceData.result.balance?.toLocaleString()} SEEDS
                    </Typography>
                    <Typography variant="h6" color="text.secondary">
                      ${balanceData.result.usdValue?.toLocaleString()} USD
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Last updated: {new Date(balanceData.result.lastUpdated).toLocaleString()}
                    </Typography>
                  </Box>
                ) : (
                  <Typography color="text.secondary">No balance data available</Typography>
                )}
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <TrendingUp color="primary" />
                  SEEDS Price
                </Typography>
                {priceLoading ? (
                  <CircularProgress />
                ) : priceData?.result ? (
                  <Box>
                    <Typography variant="h3" color="primary" sx={{ mb: 1 }}>
                      ${priceData.result.price}
                    </Typography>
                    <Typography 
                      variant="h6" 
                      color={priceData.result.change24h >= 0 ? 'success.main' : 'error.main'}
                      sx={{ mb: 1 }}
                    >
                      {priceData.result.change24h >= 0 ? '+' : ''}{priceData.result.change24h}% (24h)
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Market Cap: ${priceData.result.marketCap?.toLocaleString()}
                    </Typography>
                  </Box>
                ) : (
                  <Typography color="text.secondary">No price data available</Typography>
                )}
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Quick Actions */}
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Payment color="primary" />
              Quick Actions
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6} md={3}>
                <Button
                  fullWidth
                  variant="outlined"
                  startIcon={<Send />}
                  onClick={() => setSendDialogOpen(true)}
                >
                  Send SEEDS
                </Button>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Button
                  fullWidth
                  variant="outlined"
                  startIcon={<Favorite />}
                  onClick={() => setDonationDialogOpen(true)}
                >
                  Make Donation
                </Button>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Button
                  fullWidth
                  variant="outlined"
                  startIcon={<Star />}
                  onClick={() => {/* Handle reward */}}
                >
                  Give Reward
                </Button>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Button
                  fullWidth
                  variant="outlined"
                  startIcon={<AttachMoney />}
                  onClick={() => {/* Handle purchase */}}
                >
                  Buy SEEDS
                </Button>
              </Grid>
            </Grid>
          </CardContent>
        </Card>

        <Grid container spacing={3}>
          {/* Organizations */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Group color="primary" />
                  SEEDS Organizations
                </Typography>
                {organizationsLoading ? (
                  <CircularProgress />
                ) : organizationsData?.result ? (
                  <List>
                    {organizationsData.result.map((org: any) => (
                      <ListItem key={org.id} divider>
                        <ListItemIcon>
                          <Group color="primary" />
                        </ListItemIcon>
                        <ListItemText
                          primary={org.name}
                          secondary={`${org.members} members • ${org.balance} SEEDS`}
                        />
                        <ListItemSecondaryAction>
                          <Chip 
                            label={`${org.balance} SEEDS`}
                            color="primary"
                            size="small"
                          />
                        </ListItemSecondaryAction>
                      </ListItem>
                    ))}
                  </List>
                ) : (
                  <Typography color="text.secondary">No organizations available</Typography>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* Recent Transactions */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <History color="primary" />
                  Recent Transactions
                </Typography>
                {transactionsLoading ? (
                  <CircularProgress />
                ) : transactionsData?.result ? (
                  <List>
                    {transactionsData.result.map((tx: any) => (
                      <ListItem key={tx.id} divider>
                        <ListItemIcon>
                          {getTransactionIcon(tx.type)}
                        </ListItemIcon>
                        <ListItemText
                          primary={`${tx.type.charAt(0).toUpperCase() + tx.type.slice(1)}`}
                          secondary={`${tx.amount} SEEDS • ${new Date(tx.timestamp).toLocaleString()}`}
                        />
                        <ListItemSecondaryAction>
                          <Chip 
                            label={tx.status}
                            color={getTransactionColor(tx.type) as any}
                            size="small"
                          />
                        </ListItemSecondaryAction>
                      </ListItem>
                    ))}
                  </List>
                ) : (
                  <Typography color="text.secondary">No transactions available</Typography>
                )}
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Send Dialog */}
        <Dialog open={sendDialogOpen} onClose={() => setSendDialogOpen(false)} maxWidth="sm" fullWidth>
          <DialogTitle>Send SEEDS</DialogTitle>
          <DialogContent>
            <TextField
              fullWidth
              label="Recipient Avatar ID"
              value={sendTo}
              onChange={(e) => setSendTo(e.target.value)}
              sx={{ mb: 2, mt: 1 }}
            />
            <TextField
              fullWidth
              label="Amount (SEEDS)"
              type="number"
              value={sendAmount}
              onChange={(e) => setSendAmount(e.target.value)}
              sx={{ mb: 2 }}
            />
            <TextField
              fullWidth
              label="Description (Optional)"
              value={sendDescription}
              onChange={(e) => setSendDescription(e.target.value)}
              multiline
              rows={3}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setSendDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleSend} variant="contained">Send SEEDS</Button>
          </DialogActions>
        </Dialog>

        {/* Donation Dialog */}
        <Dialog open={donationDialogOpen} onClose={() => setDonationDialogOpen(false)} maxWidth="sm" fullWidth>
          <DialogTitle>Make Donation</DialogTitle>
          <DialogContent>
            <TextField
              fullWidth
              label="Amount (SEEDS)"
              type="number"
              value={donationAmount}
              onChange={(e) => setDonationAmount(e.target.value)}
              sx={{ mb: 2, mt: 1 }}
            />
            <TextField
              fullWidth
              label="Description (Optional)"
              value={donationDescription}
              onChange={(e) => setDonationDescription(e.target.value)}
              multiline
              rows={3}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDonationDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleDonation} variant="contained">Make Donation</Button>
          </DialogActions>
        </Dialog>
      </motion.div>
    </Box>
  );
};

export default SeedsPage;
