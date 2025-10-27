import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Typography,
  Paper,
  Grid,
  Card,
  CardContent,
  LinearProgress,
  Chip,
  Alert,
  CircularProgress,
  Button,
  Switch,
  FormControlLabel,
} from '@mui/material';
import {
  TrendingUp as TrendingUpIcon,
  Speed as SpeedIcon,
  AccountBalanceWallet as WalletIcon,
  Warning as WarningIcon,
  CheckCircle as CheckIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-hot-toast';

interface UsageData {
  currentMonth: {
    requests: number;
    limit: number;
    remaining: number;
    overage: number;
  };
  payAsYouGoEnabled: boolean;
  overageCharges: {
    currentMonth: number;
    currency: string;
  };
}

const UsagePage: React.FC = () => {
  const navigate = useNavigate();
  const [usageData, setUsageData] = useState<UsageData | null>(null);
  const [loading, setLoading] = useState(true);
  const [payAsYouGoEnabled, setPayAsYouGoEnabled] = useState(false);

  useEffect(() => {
    fetchUsageData();
  }, []);

  const fetchUsageData = async () => {
    try {
      setLoading(true);
      
      // TODO: Replace with actual API call
      // const response = await fetch('/api/subscription/usage', {
      //   headers: {
      //     'Authorization': `Bearer ${localStorage.getItem('authToken')}`,
      //   },
      // });
      
      // Mock data for now
      setTimeout(() => {
        setUsageData({
          currentMonth: {
            requests: 75000,
            limit: 100000,
            remaining: 25000,
            overage: 0,
          },
          payAsYouGoEnabled: false,
          overageCharges: {
            currentMonth: 0.00,
            currency: 'USD',
          },
        });
        setPayAsYouGoEnabled(false);
        setLoading(false);
      }, 1000);
    } catch (error) {
      console.error('Error fetching usage data:', error);
      toast.error('Failed to load usage data');
      setLoading(false);
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
      setPayAsYouGoEnabled(enabled);
      fetchUsageData();
    } catch (error) {
      console.error('Error toggling pay-as-you-go:', error);
      toast.error('Failed to update pay-as-you-go settings');
    }
  };

  const getUsagePercentage = () => {
    if (!usageData) return 0;
    return Math.min((usageData.currentMonth.requests / usageData.currentMonth.limit) * 100, 100);
  };

  const getUsageColor = () => {
    const percentage = getUsagePercentage();
    if (percentage >= 90) return 'error';
    if (percentage >= 75) return 'warning';
    return 'primary';
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <CircularProgress size={60} />
      </Box>
    );
  }

  if (!usageData) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Alert severity="error">
          Failed to load usage data. Please try again.
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom sx={{ fontWeight: 'bold' }}>
        Usage & Billing
      </Typography>
      
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Monitor your API usage and manage your billing preferences.
      </Typography>

      <Grid container spacing={4}>
        {/* Current Usage */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center' }}>
                <SpeedIcon sx={{ mr: 1 }} />
                Current Month Usage
              </Typography>
              
              <Box sx={{ mb: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                  <Typography variant="body2" color="text.secondary">
                    API Requests
                  </Typography>
                  <Typography variant="body2" fontWeight="medium">
                    {usageData.currentMonth.requests.toLocaleString()} / {usageData.currentMonth.limit.toLocaleString()}
                  </Typography>
                </Box>
                
                <LinearProgress
                  variant="determinate"
                  value={getUsagePercentage()}
                  color={getUsageColor()}
                  sx={{ height: 8, borderRadius: 4 }}
                />
                
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 1 }}>
                  <Typography variant="caption" color="text.secondary">
                    {usageData.currentMonth.remaining.toLocaleString()} requests remaining
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {getUsagePercentage().toFixed(1)}% used
                  </Typography>
                </Box>
              </Box>

              {usageData.currentMonth.overage > 0 && (
                <Alert severity="warning" sx={{ mb: 2 }}>
                  <Typography variant="body2">
                    You have exceeded your plan limit by {usageData.currentMonth.overage.toLocaleString()} requests.
                    {payAsYouGoEnabled ? ' You will be charged for overage.' : ' Consider upgrading your plan or enabling pay-as-you-go.'}
                  </Typography>
                </Alert>
              )}

              {getUsagePercentage() >= 90 && usageData.currentMonth.overage === 0 && (
                <Alert severity="info" sx={{ mb: 2 }}>
                  <Typography variant="body2">
                    You're approaching your plan limit. Consider upgrading or enabling pay-as-you-go to avoid service interruption.
                  </Typography>
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Pay-as-you-go Settings */}
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center' }}>
                <WalletIcon sx={{ mr: 1 }} />
                Billing Settings
              </Typography>
              
              <Box sx={{ mb: 3 }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={payAsYouGoEnabled}
                      onChange={(e) => handleTogglePayAsYouGo(e.target.checked)}
                      color="primary"
                    />
                  }
                  label="Pay-as-you-go Billing"
                />
                
                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                  When enabled, you'll be charged for requests over your plan limit.
                </Typography>
              </Box>

              {payAsYouGoEnabled && (
                <Box sx={{ p: 2, bgcolor: 'primary.50', borderRadius: 1, mb: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Pay-as-you-go Pricing
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    $0.0005 per request over your plan limit
                  </Typography>
                </Box>
              )}

              <Button
                variant="outlined"
                fullWidth
                onClick={() => navigate('/subscription/plans')}
                sx={{ mb: 2 }}
              >
                Upgrade Plan
              </Button>
              
              <Button
                variant="text"
                fullWidth
                onClick={() => navigate('/subscription/manage')}
              >
                Manage Subscription
              </Button>
            </CardContent>
          </Card>
        </Grid>

        {/* Usage Statistics */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Usage Statistics
              </Typography>
              
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6} md={3}>
                  <Paper variant="outlined" sx={{ p: 2, textAlign: 'center' }}>
                    <Typography variant="h4" color="primary.main" gutterBottom>
                      {usageData.currentMonth.requests.toLocaleString()}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Requests This Month
                    </Typography>
                  </Paper>
                </Grid>
                
                <Grid item xs={12} sm={6} md={3}>
                  <Paper variant="outlined" sx={{ p: 2, textAlign: 'center' }}>
                    <Typography variant="h4" color="success.main" gutterBottom>
                      {usageData.currentMonth.remaining.toLocaleString()}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Remaining Requests
                    </Typography>
                  </Paper>
                </Grid>
                
                <Grid item xs={12} sm={6} md={3}>
                  <Paper variant="outlined" sx={{ p: 2, textAlign: 'center' }}>
                    <Typography variant="h4" color="warning.main" gutterBottom>
                      {usageData.currentMonth.overage.toLocaleString()}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Overage Requests
                    </Typography>
                  </Paper>
                </Grid>
                
                <Grid item xs={12} sm={6} md={3}>
                  <Paper variant="outlined" sx={{ p: 2, textAlign: 'center' }}>
                    <Typography variant="h4" color="info.main" gutterBottom>
                      ${usageData.overageCharges.currentMonth.toFixed(2)}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Overage Charges
                    </Typography>
                  </Paper>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Container>
  );
};

export default UsagePage;
