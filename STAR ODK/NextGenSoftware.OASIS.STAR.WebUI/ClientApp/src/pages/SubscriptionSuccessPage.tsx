import React, { useEffect, useState } from 'react';
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
  ListItemIcon,
  ListItemText,
  Divider,
  Chip,
} from '@mui/material';
import {
  CheckCircle as CheckIcon,
  AccountCircle as AccountIcon,
  CreditCard as CreditCardIcon,
  Email as EmailIcon,
  Download as DownloadIcon,
  ArrowForward as ArrowForwardIcon,
} from '@mui/icons-material';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { motion } from 'framer-motion';

interface SubscriptionDetails {
  planName: string;
  planId: string;
  amount: number;
  currency: string;
  status: string;
  customerEmail: string;
  subscriptionId: string;
  nextBillingDate: string;
}

const SubscriptionSuccessPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [loading, setLoading] = useState(true);
  const [subscriptionDetails, setSubscriptionDetails] = useState<SubscriptionDetails | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const sessionId = searchParams.get('session_id');
    
    if (!sessionId) {
      setError('No session ID provided');
      setLoading(false);
      return;
    }

    // Simulate fetching subscription details
    const fetchSubscriptionDetails = async () => {
      try {
        setLoading(true);
        
        // TODO: Replace with actual API call
        // const response = await fetch(`/api/subscription/session/${sessionId}`);
        // const data = await response.json();
        
        // Mock data for now
        setTimeout(() => {
          setSubscriptionDetails({
            planName: 'Silver Plan',
            planId: 'silver',
            amount: 29,
            currency: 'USD',
            status: 'active',
            customerEmail: 'user@example.com',
            subscriptionId: 'sub_' + Math.random().toString(36).substr(2, 9),
            nextBillingDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toLocaleDateString(),
          });
          setLoading(false);
        }, 2000);
      } catch (error) {
        console.error('Error fetching subscription details:', error);
        setError('Failed to load subscription details');
        setLoading(false);
      }
    };

    fetchSubscriptionDetails();
  }, [searchParams]);

  const handleContinueToDashboard = () => {
    navigate('/dashboard');
  };

  const handleViewSubscription = () => {
    navigate('/subscription/manage');
  };

  const handleDownloadInvoice = () => {
    // TODO: Implement invoice download
    console.log('Download invoice');
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <CircularProgress size={60} />
      </Box>
    );
  }

  if (error) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
        <Button variant="contained" onClick={() => navigate('/subscription/plans')}>
          Back to Plans
        </Button>
      </Container>
    );
  }

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6 }}
      >
        <Box sx={{ textAlign: 'center', mb: 6 }}>
          <motion.div
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            transition={{ delay: 0.2, type: 'spring', stiffness: 200 }}
          >
            <CheckIcon sx={{ fontSize: 80, color: 'success.main', mb: 2 }} />
          </motion.div>
          
          <Typography variant="h3" component="h1" gutterBottom sx={{ fontWeight: 'bold' }}>
            Welcome to OASIS!
          </Typography>
          
          <Typography variant="h6" color="text.secondary" sx={{ mb: 4 }}>
            Your subscription has been successfully activated
          </Typography>
        </Box>

        <Paper elevation={3} sx={{ p: 4, mb: 4 }}>
          <Typography variant="h5" gutterBottom sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
            <AccountIcon sx={{ mr: 1 }} />
            Subscription Details
          </Typography>

          <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 3, mb: 3 }}>
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                Plan
              </Typography>
              <Typography variant="h6">
                {subscriptionDetails?.planName}
              </Typography>
            </Box>
            
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                Status
              </Typography>
              <Chip
                label={subscriptionDetails?.status}
                color="success"
                size="small"
              />
            </Box>
            
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                Amount
              </Typography>
              <Typography variant="h6">
                ${subscriptionDetails?.amount}/{subscriptionDetails?.currency === 'USD' ? 'month' : subscriptionDetails?.currency}
              </Typography>
            </Box>
            
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                Next Billing
              </Typography>
              <Typography variant="body1">
                {subscriptionDetails?.nextBillingDate}
              </Typography>
            </Box>
          </Box>

          <Divider sx={{ my: 3 }} />

          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            Customer Email
          </Typography>
          <Typography variant="body1" sx={{ mb: 3 }}>
            {subscriptionDetails?.customerEmail}
          </Typography>

          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            Subscription ID
          </Typography>
          <Typography variant="body2" sx={{ fontFamily: 'monospace', bgcolor: 'grey.100', p: 1, borderRadius: 1 }}>
            {subscriptionDetails?.subscriptionId}
          </Typography>
        </Paper>

        <Paper elevation={2} sx={{ p: 3, mb: 4 }}>
          <Typography variant="h6" gutterBottom>
            What's Next?
          </Typography>
          
          <List>
            <ListItem>
              <ListItemIcon>
                <CheckIcon color="success" />
              </ListItemIcon>
              <ListItemText
                primary="Access your OASIS dashboard"
                secondary="Start building with our APIs and tools"
              />
            </ListItem>
            
            <ListItem>
              <ListItemIcon>
                <CheckIcon color="success" />
              </ListItemIcon>
              <ListItemText
                primary="Set up your development environment"
                secondary="Configure your API keys and start coding"
              />
            </ListItem>
            
            <ListItem>
              <ListItemIcon>
                <CheckIcon color="success" />
              </ListItemIcon>
              <ListItemText
                primary="Join our developer community"
                secondary="Get support and share your projects"
              />
            </ListItem>
          </List>
        </Paper>

        <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', flexWrap: 'wrap' }}>
          <Button
            variant="contained"
            size="large"
            onClick={handleContinueToDashboard}
            endIcon={<ArrowForwardIcon />}
            sx={{ minWidth: 200 }}
          >
            Go to Dashboard
          </Button>
          
          <Button
            variant="outlined"
            size="large"
            onClick={handleViewSubscription}
            sx={{ minWidth: 200 }}
          >
            Manage Subscription
          </Button>
          
          <Button
            variant="text"
            size="large"
            onClick={handleDownloadInvoice}
            startIcon={<DownloadIcon />}
            sx={{ minWidth: 200 }}
          >
            Download Invoice
          </Button>
        </Box>

        <Box sx={{ textAlign: 'center', mt: 4 }}>
          <Typography variant="body2" color="text.secondary">
            Need help getting started?{' '}
            <Button variant="text" size="small" onClick={() => navigate('/support')}>
              Contact Support
            </Button>
          </Typography>
        </Box>
      </motion.div>
    </Container>
  );
};

export default SubscriptionSuccessPage;
