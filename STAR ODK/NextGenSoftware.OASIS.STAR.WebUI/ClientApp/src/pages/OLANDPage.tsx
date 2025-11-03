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
  AccountBalance, Business, Payment, Favorite, Star, QrCode,
  TrendingUp, History, Add, Send, Group, AttachMoney
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useQueryClient } from 'react-query';
import { olandService } from '../services/core/olandService';

const OLANDPage: React.FC = () => {
  const [selectedAvatarId] = useState('demo-avatar-1');
  const [paymentDialogOpen, setPaymentDialogOpen] = useState(false);
  const [donationDialogOpen, setDonationDialogOpen] = useState(false);
  const [organizationDialogOpen, setOrganizationDialogOpen] = useState(false);
  const [qrDialogOpen, setQrDialogOpen] = useState(false);
  const [paymentAmount, setPaymentAmount] = useState('');
  const [paymentTo, setPaymentTo] = useState('');
  const [paymentDescription, setPaymentDescription] = useState('');
  const [donationAmount, setDonationAmount] = useState('');
  const [donationOrg, setDonationOrg] = useState('');
  const [donationDescription, setDonationDescription] = useState('');
  const [orgName, setOrgName] = useState('');
  const [orgDescription, setOrgDescription] = useState('');
  const [qrAmount, setQrAmount] = useState('');
  const [qrDescription, setQrDescription] = useState('');
  const queryClient = useQueryClient();

  // Queries
  const { data: balanceData, isLoading: balanceLoading } = useQuery(
    'oland-balance',
    () => olandService.getBalance(selectedAvatarId)
  );

  const { data: organizationsData, isLoading: organizationsLoading } = useQuery(
    'oland-organizations',
    () => olandService.getOrganizations()
  );

  const { data: priceData, isLoading: priceLoading } = useQuery(
    'oland-price',
    () => olandService.getPrice()
  );

  const { data: transactionsData, isLoading: transactionsLoading } = useQuery(
    'oland-transactions',
    () => olandService.getTransactionHistory(selectedAvatarId, 20)
  );

  const handlePayment = async () => {
    try {
      await olandService.pay(paymentTo, parseFloat(paymentAmount), paymentDescription);
      queryClient.invalidateQueries('oland-balance');
      queryClient.invalidateQueries('oland-transactions');
      setPaymentDialogOpen(false);
      setPaymentAmount('');
      setPaymentTo('');
      setPaymentDescription('');
    } catch (error) {
      console.error('Payment failed:', error);
    }
  };

  const handleDonation = async () => {
    try {
      await olandService.donate(donationOrg, parseFloat(donationAmount), donationDescription);
      queryClient.invalidateQueries('oland-balance');
      queryClient.invalidateQueries('oland-transactions');
      setDonationDialogOpen(false);
      setDonationAmount('');
      setDonationOrg('');
      setDonationDescription('');
    } catch (error) {
      console.error('Donation failed:', error);
    }
  };

  const handleCreateOrganization = async () => {
    try {
      await olandService.createOrganization({
        name: orgName,
        description: orgDescription
      });
      queryClient.invalidateQueries('oland-organizations');
      setOrganizationDialogOpen(false);
      setOrgName('');
      setOrgDescription('');
    } catch (error) {
      console.error('Organization creation failed:', error);
    }
  };

  const handleGenerateQR = async () => {
    try {
      await olandService.generateQRCode(parseFloat(qrAmount), qrDescription);
      setQrDialogOpen(false);
      setQrAmount('');
      setQrDescription('');
    } catch (error) {
      console.error('QR code generation failed:', error);
    }
  };

  const getTransactionIcon = (type: string) => {
    switch (type) {
      case 'payment': return <Send />;
      case 'donation': return <Favorite />;
      case 'reward': return <Star />;
      case 'purchase': return <AttachMoney />;
      default: return <Payment />;
    }
  };

  const getTransactionColor = (type: string) => {
    switch (type) {
      case 'payment': return 'primary';
      case 'donation': return 'success';
      case 'reward': return 'warning';
      case 'purchase': return 'info';
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
            <AccountBalance color="primary" />
            OLAND Token Management
          </Typography>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              variant="outlined"
              startIcon={<QrCode />}
              onClick={() => setQrDialogOpen(true)}
            >
              Generate QR
            </Button>
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => setOrganizationDialogOpen(true)}
            >
              Create Organization
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
                  Your OLAND Balance
                </Typography>
                {balanceLoading ? (
                  <CircularProgress />
                ) : balanceData?.result ? (
                  <Box>
                    <Typography variant="h3" color="primary" sx={{ mb: 1 }}>
                      {balanceData.result.balance?.toLocaleString()} OLAND
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
                  OLAND Price
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
                  onClick={() => setPaymentDialogOpen(true)}
                >
                  Send Payment
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
                  Buy OLAND
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
                  <Business color="primary" />
                  Organizations
                </Typography>
                {organizationsLoading ? (
                  <CircularProgress />
                ) : organizationsData?.result ? (
                  <List>
                    {organizationsData.result.map((org: any) => (
                      <ListItem key={org.id} divider>
                        <ListItemIcon>
                          <Business color="primary" />
                        </ListItemIcon>
                        <ListItemText
                          primary={org.name}
                          secondary={`${org.members} members • ${org.balance} OLAND`}
                        />
                        <ListItemSecondaryAction>
                          <Chip 
                            label={`${org.balance} OLAND`}
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
                          secondary={`${tx.amount} OLAND • ${new Date(tx.timestamp).toLocaleString()}`}
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

        {/* Payment Dialog */}
        <Dialog open={paymentDialogOpen} onClose={() => setPaymentDialogOpen(false)} maxWidth="sm" fullWidth>
          <DialogTitle>Send Payment</DialogTitle>
          <DialogContent>
            <TextField
              fullWidth
              label="Recipient Avatar ID"
              value={paymentTo}
              onChange={(e) => setPaymentTo(e.target.value)}
              sx={{ mb: 2, mt: 1 }}
            />
            <TextField
              fullWidth
              label="Amount (OLAND)"
              type="number"
              value={paymentAmount}
              onChange={(e) => setPaymentAmount(e.target.value)}
              sx={{ mb: 2 }}
            />
            <TextField
              fullWidth
              label="Description (Optional)"
              value={paymentDescription}
              onChange={(e) => setPaymentDescription(e.target.value)}
              multiline
              rows={3}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setPaymentDialogOpen(false)}>Cancel</Button>
            <Button onClick={handlePayment} variant="contained">Send Payment</Button>
          </DialogActions>
        </Dialog>

        {/* Donation Dialog */}
        <Dialog open={donationDialogOpen} onClose={() => setDonationDialogOpen(false)} maxWidth="sm" fullWidth>
          <DialogTitle>Make Donation</DialogTitle>
          <DialogContent>
            <FormControl fullWidth sx={{ mb: 2, mt: 1 }}>
              <InputLabel>Organization</InputLabel>
              <Select
                value={donationOrg}
                onChange={(e) => setDonationOrg(e.target.value)}
                label="Organization"
              >
                {organizationsData?.result?.map((org: any) => (
                  <MenuItem key={org.id} value={org.id}>
                    {org.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              fullWidth
              label="Amount (OLAND)"
              type="number"
              value={donationAmount}
              onChange={(e) => setDonationAmount(e.target.value)}
              sx={{ mb: 2 }}
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

        {/* Organization Dialog */}
        <Dialog open={organizationDialogOpen} onClose={() => setOrganizationDialogOpen(false)} maxWidth="sm" fullWidth>
          <DialogTitle>Create Organization</DialogTitle>
          <DialogContent>
            <TextField
              fullWidth
              label="Organization Name"
              value={orgName}
              onChange={(e) => setOrgName(e.target.value)}
              sx={{ mb: 2, mt: 1 }}
            />
            <TextField
              fullWidth
              label="Description"
              value={orgDescription}
              onChange={(e) => setOrgDescription(e.target.value)}
              multiline
              rows={3}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOrganizationDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleCreateOrganization} variant="contained">Create Organization</Button>
          </DialogActions>
        </Dialog>

        {/* QR Code Dialog */}
        <Dialog open={qrDialogOpen} onClose={() => setQrDialogOpen(false)} maxWidth="sm" fullWidth>
          <DialogTitle>Generate Payment QR Code</DialogTitle>
          <DialogContent>
            <TextField
              fullWidth
              label="Amount (OLAND)"
              type="number"
              value={qrAmount}
              onChange={(e) => setQrAmount(e.target.value)}
              sx={{ mb: 2, mt: 1 }}
            />
            <TextField
              fullWidth
              label="Description (Optional)"
              value={qrDescription}
              onChange={(e) => setQrDescription(e.target.value)}
              multiline
              rows={3}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setQrDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleGenerateQR} variant="contained">Generate QR Code</Button>
          </DialogActions>
        </Dialog>
      </motion.div>
    </Box>
  );
};

export default OLANDPage;
