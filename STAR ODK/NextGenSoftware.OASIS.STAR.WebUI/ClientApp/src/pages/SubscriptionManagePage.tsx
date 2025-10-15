import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Typography,
  Paper,
  Button,
  Alert,
  CircularProgress,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  Chip,
  Divider,
  Grid,
  Card,
  CardContent,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Switch,
} from '@mui/material';
import {
  CreditCard as CreditCardIcon,
  Download as DownloadIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Add as AddIcon,
  History as HistoryIcon,
  Settings as SettingsIcon,
  Warning as WarningIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-hot-toast';

interface Subscription {
  id: string;
  planName: string;
  status: 'active' | 'canceled' | 'past_due' | 'incomplete';
  amount: number;
  currency: string;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  cancelAtPeriodEnd: boolean;
  payAsYouGoEnabled?: boolean;
}

interface PaymentMethod {
  id: string;
  type: 'card';
  last4: string;
  brand: string;
  expMonth: number;
  expYear: number;
  isDefault: boolean;
}

interface Invoice {
  id: string;
  amount: number;
  currency: string;
  status: 'paid' | 'open' | 'void';
  created: string;
  invoiceUrl: string;
}

const SubscriptionManagePage: React.FC = () => {
  const navigate = useNavigate();
  const [subscription, setSubscription] = useState<Subscription | null>(null);
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethod[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [loading, setLoading] = useState(true);
  const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
  const [updateDialogOpen, setUpdateDialogOpen] = useState(false);
  const [selectedPlan, setSelectedPlan] = useState('');

  useEffect(() => {
    fetchSubscriptionData();
  }, []);

  const fetchSubscriptionData = async () => {
    try {
      setLoading(true);
      
      // TODO: Replace with actual API calls
      // const [subscriptionRes, paymentMethodsRes, invoicesRes] = await Promise.all([
      //   fetch('/api/subscription/me'),
      //   fetch('/api/subscription/payment-methods'),
      //   fetch('/api/subscription/invoices'),
      // ]);
      
      // Mock data for now
      setTimeout(() => {
        setSubscription({
          id: 'sub_1234567890',
          planName: 'Silver Plan',
          status: 'active',
          amount: 29,
          currency: 'USD',
          currentPeriodStart: '2024-01-01',
          currentPeriodEnd: '2024-02-01',
          cancelAtPeriodEnd: false,
          payAsYouGoEnabled: false,
        });

        setPaymentMethods([
          {
            id: 'pm_1234567890',
            type: 'card',
            last4: '4242',
            brand: 'Visa',
            expMonth: 12,
            expYear: 2025,
            isDefault: true,
          },
        ]);

        setInvoices([
          {
            id: 'in_1234567890',
            amount: 29,
            currency: 'USD',
            status: 'paid',
            created: '2024-01-01',
            invoiceUrl: '#',
          },
        ]);

        setLoading(false);
      }, 1000);
    } catch (error) {
      console.error('Error fetching subscription data:', error);
      toast.error('Failed to load subscription data');
      setLoading(false);
    }
  };

  const handleCancelSubscription = async () => {
    try {
      // TODO: Replace with actual API call
      // await fetch('/api/subscription/cancel', { method: 'POST' });
      
      toast.success('Subscription will be canceled at the end of the current period');
      setCancelDialogOpen(false);
      fetchSubscriptionData();
    } catch (error) {
      console.error('Error canceling subscription:', error);
      toast.error('Failed to cancel subscription');
    }
  };

  const handleUpdatePlan = async () => {
    try {
      // TODO: Replace with actual API call
      // await fetch('/api/subscription/update', {
      //   method: 'POST',
      //   headers: { 'Content-Type': 'application/json' },
      //   body: JSON.stringify({ planId: selectedPlan }),
      // });
      
      toast.success('Plan updated successfully');
      setUpdateDialogOpen(false);
      fetchSubscriptionData();
    } catch (error) {
      console.error('Error updating plan:', error);
      toast.error('Failed to update plan');
    }
  };

  const handleTogglePayAsYouGo = async (enabled: boolean) => {
    try {
      const response = await fetch('/api/subscription/toggle-pay-as-you-go', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('authToken')}`,
        },
        body: JSON.stringify({ enabled }),
      });

      if (!response.ok) {
        throw new Error('Failed to update pay-as-you-go settings');
      }

      const data = await response.json();
      toast.success(data.message);
      fetchSubscriptionData();
    } catch (error) {
      console.error('Error toggling pay-as-you-go:', error);
      toast.error('Failed to update pay-as-you-go settings');
    }
  };

  const handleDownloadInvoice = (invoiceId: string) => {
    // TODO: Implement invoice download
    console.log('Download invoice:', invoiceId);
    toast.success('Invoice download started');
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
        return 'success';
      case 'canceled':
        return 'error';
      case 'past_due':
        return 'warning';
      case 'incomplete':
        return 'default';
      default:
        return 'default';
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <CircularProgress size={60} />
      </Box>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom sx={{ fontWeight: 'bold' }}>
        Manage Subscription
      </Typography>
      
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Manage your OASIS subscription, payment methods, and billing history.
      </Typography>

      <Grid container spacing={4}>
        {/* Current Subscription */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center' }}>
                <CreditCardIcon sx={{ mr: 1 }} />
                Current Subscription
              </Typography>
              
              {subscription && (
                <Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Typography variant="h6">{subscription.planName}</Typography>
                    <Chip
                      label={subscription.status}
                      color={getStatusColor(subscription.status) as any}
                      size="small"
                    />
                  </Box>
                  
                  <Typography variant="h4" sx={{ mb: 2 }}>
                    ${subscription.amount}/{subscription.currency === 'USD' ? 'month' : subscription.currency}
                  </Typography>
                  
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    Current period: {formatDate(subscription.currentPeriodStart)} - {formatDate(subscription.currentPeriodEnd)}
                  </Typography>
                  
                  {subscription.cancelAtPeriodEnd && (
                    <Alert severity="warning" sx={{ mb: 2 }}>
                      Your subscription will be canceled at the end of the current period.
                    </Alert>
                  )}
                  
                  <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                    <Button
                      variant="outlined"
                      size="small"
                      onClick={() => setUpdateDialogOpen(true)}
                      startIcon={<EditIcon />}
                    >
                      Change Plan
                    </Button>
                    
                    {subscription.status === 'active' && !subscription.cancelAtPeriodEnd && (
                      <Button
                        variant="outlined"
                        color="error"
                        size="small"
                        onClick={() => setCancelDialogOpen(true)}
                        startIcon={<DeleteIcon />}
                      >
                        Cancel
                      </Button>
                    )}
                  </Box>

                  {/* Pay-as-you-go toggle */}
                  <Box sx={{ mt: 2, p: 2, bgcolor: 'grey.50', borderRadius: 1 }}>
                    <Typography variant="subtitle2" gutterBottom>
                      Pay-as-you-go Billing
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      When enabled, you'll be charged for requests over your plan limit.
                    </Typography>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Switch
                        checked={subscription.payAsYouGoEnabled || false}
                        onChange={(e) => handleTogglePayAsYouGo(e.target.checked)}
                        color="primary"
                      />
                      <Typography variant="body2">
                        {subscription.payAsYouGoEnabled ? 'Enabled' : 'Disabled'}
                      </Typography>
                    </Box>
                    {subscription.payAsYouGoEnabled && (
                      <Typography variant="caption" color="primary" sx={{ mt: 1, display: 'block' }}>
                        You will be charged for overage requests at your plan's pay-as-you-go rate.
                      </Typography>
                    )}
                  </Box>
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Payment Methods */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center' }}>
                <CreditCardIcon sx={{ mr: 1 }} />
                Payment Methods
              </Typography>
              
              <List>
                {paymentMethods.map((method) => (
                  <ListItem key={method.id}>
                    <ListItemText
                      primary={`${method.brand} •••• ${method.last4}`}
                      secondary={`Expires ${method.expMonth}/${method.expYear}`}
                    />
                    <ListItemSecondaryAction>
                      {method.isDefault && (
                        <Chip label="Default" size="small" color="primary" />
                      )}
                    </ListItemSecondaryAction>
                  </ListItem>
                ))}
              </List>
              
              <Button
                variant="outlined"
                size="small"
                startIcon={<AddIcon />}
                onClick={() => navigate('/subscription/payment-methods')}
              >
                Add Payment Method
              </Button>
            </CardContent>
          </Card>
        </Grid>

        {/* Billing History */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center' }}>
                <HistoryIcon sx={{ mr: 1 }} />
                Billing History
              </Typography>
              
              <List>
                {invoices.map((invoice) => (
                  <ListItem key={invoice.id}>
                    <ListItemText
                      primary={`Invoice ${invoice.id}`}
                      secondary={formatDate(invoice.created)}
                    />
                    <ListItemSecondaryAction>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body1" sx={{ mr: 2 }}>
                          ${invoice.amount}
                        </Typography>
                        <Chip
                          label={invoice.status}
                          color={getStatusColor(invoice.status) as any}
                          size="small"
                        />
                        <IconButton
                          size="small"
                          onClick={() => handleDownloadInvoice(invoice.id)}
                        >
                          <DownloadIcon />
                        </IconButton>
                      </Box>
                    </ListItemSecondaryAction>
                  </ListItem>
                ))}
              </List>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Cancel Subscription Dialog */}
      <Dialog open={cancelDialogOpen} onClose={() => setCancelDialogOpen(false)}>
        <DialogTitle>Cancel Subscription</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to cancel your subscription? You will continue to have access until the end of your current billing period.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCancelDialogOpen(false)}>Keep Subscription</Button>
          <Button onClick={handleCancelSubscription} color="error" variant="contained">
            Cancel Subscription
          </Button>
        </DialogActions>
      </Dialog>

      {/* Update Plan Dialog */}
      <Dialog open={updateDialogOpen} onClose={() => setUpdateDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Change Plan</DialogTitle>
        <DialogContent>
          <FormControl fullWidth sx={{ mt: 2 }}>
            <InputLabel>Select New Plan</InputLabel>
            <Select
              value={selectedPlan}
              onChange={(e) => setSelectedPlan(e.target.value)}
              label="Select New Plan"
            >
              <MenuItem value="bronze">Bronze - $9/month</MenuItem>
              <MenuItem value="silver">Silver - $29/month</MenuItem>
              <MenuItem value="gold">Gold - $99/month</MenuItem>
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setUpdateDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleUpdatePlan} variant="contained">
            Update Plan
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default SubscriptionManagePage;
