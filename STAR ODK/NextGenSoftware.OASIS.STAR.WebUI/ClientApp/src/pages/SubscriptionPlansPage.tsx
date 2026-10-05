import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Typography,
  Grid,
  Card,
  CardContent,
  CardActions,
  Button,
  List,
  ListItem,
  ListItemText,
  Chip,
  Alert,
  CircularProgress,
  Divider,
  Paper,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  CheckCircle as CheckIcon,
  Star as StarIcon,
  Security as SecurityIcon,
  Speed as SpeedIcon,
  Support as SupportIcon,
  Business as BusinessIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-hot-toast';
import { subscriptionService } from '../services/subscriptionService';

// Types
interface Plan {
  id: string;
  name: string;
  price: string;
  originalPrice?: string;
  description: string;
  features: string[];
  popular?: boolean;
  recommended?: boolean;
  limits: {
    apiCalls: string;
    storage: string;
    support: string;
  };
}

const SubscriptionPlansPage: React.FC = () => {
  const navigate = useNavigate();
  const [plans, setPlans] = useState<Plan[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedPlan, setSelectedPlan] = useState<string | null>(null);

  const defaultPlans: Plan[] = [
    {
      id: 'bronze',
      name: 'Bronze',
      price: '$9/mo',
      description: 'Perfect for getting started with OASIS',
      features: [
        '10,000 API calls/month',
        '1GB storage',
        'Community support',
        'Basic analytics',
        'Standard uptime SLA',
      ],
      limits: {
        apiCalls: '10,000/month',
        storage: '1GB',
        support: 'Community',
      },
    },
    {
      id: 'silver',
      name: 'Silver',
      price: '$29/mo',
      originalPrice: '$39/mo',
      description: 'Great for growing applications',
      features: [
        '100,000 API calls/month',
        '10GB storage',
        'Email support',
        'Advanced analytics',
        'Priority uptime SLA',
        'Webhook support',
      ],
      popular: true,
      limits: {
        apiCalls: '100,000/month',
        storage: '10GB',
        support: 'Email',
      },
    },
    {
      id: 'gold',
      name: 'Gold',
      price: '$99/mo',
      originalPrice: '$129/mo',
      description: 'For serious applications',
      features: [
        '1,000,000 API calls/month',
        '100GB storage',
        'Priority support',
        'Advanced analytics & reporting',
        '99.9% uptime SLA',
        'Custom webhooks',
        'API rate limit increases',
      ],
      recommended: true,
      limits: {
        apiCalls: '1,000,000/month',
        storage: '100GB',
        support: 'Priority',
      },
    },
    {
      id: 'enterprise',
      name: 'Enterprise',
      price: 'Contact us',
      description: 'Custom solutions for large organizations',
      features: [
        'Unlimited API calls',
        'Unlimited storage',
        'Dedicated support',
        'Custom analytics',
        '99.99% uptime SLA',
        'SSO integration',
        'Custom integrations',
        'SLA guarantees',
      ],
      limits: {
        apiCalls: 'Unlimited',
        storage: 'Unlimited',
        support: 'Dedicated',
      },
    },
  ];

  useEffect(() => {
    const fetchPlans = async () => {
      try {
        setLoading(true);
        const plansData = await subscriptionService.getPlans();
        setPlans(plansData);
        setLoading(false);
      } catch (error) {
        console.error('Error fetching plans:', error);
        setPlans(defaultPlans);
        setLoading(false);
      }
    };

    fetchPlans();
  }, []);

  const handleSelectPlan = async (planId: string) => {
    setSelectedPlan(planId);
    
    try {
      const response = await subscriptionService.createCheckoutSession({
        planId,
        successUrl: `${window.location.origin}/subscription/success`,
        cancelUrl: `${window.location.origin}/subscription/plans`,
      });
      
      if (response.sessionUrl) {
        window.location.href = response.sessionUrl;
      } else {
        toast.error('Checkout not configured yet. Please contact support.');
      }
    } catch (error) {
      console.error('Checkout error:', error);
      toast.error('Failed to start checkout. Please try again.');
    } finally {
      setSelectedPlan(null);
    }
  };

  const getPlanIcon = (planId: string) => {
    switch (planId) {
      case 'bronze':
        return <SpeedIcon />;
      case 'silver':
        return <StarIcon />;
      case 'gold':
        return <SecurityIcon />;
      case 'enterprise':
        return <BusinessIcon />;
      default:
        return <StarIcon />;
    }
  };

  const getPlanColor = (planId: string) => {
    switch (planId) {
      case 'bronze':
        return '#CD7F32';
      case 'silver':
        return '#C0C0C0';
      case 'gold':
        return '#FFD700';
      case 'enterprise':
        return '#8B5CF6';
      default:
        return 'primary';
    }
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
      <Box sx={{ textAlign: 'center', mb: 6 }}>
        <Typography variant="h3" component="h1" gutterBottom sx={{ fontWeight: 'bold' }}>
          Choose Your OASIS Plan
        </Typography>
        <Typography variant="h6" color="text.secondary" sx={{ mb: 4 }}>
          Unlock the full power of the OASIS ecosystem with our flexible subscription plans
        </Typography>
        
        <Alert severity="info" sx={{ maxWidth: 600, mx: 'auto', mb: 4 }}>
          <Typography variant="body2">
            All plans include access to the Universal Wallet System, STAR CLI, and cross-platform STARNETHolons sharing.
          </Typography>
        </Alert>
      </Box>

      <Grid container spacing={4} sx={{ mb: 6 }}>
        {plans.map((plan) => (
          <Grid item xs={12} md={6} lg={3} key={plan.id}>
            <Card
              sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                position: 'relative',
                border: plan.popular ? '2px solid' : '1px solid',
                borderColor: plan.popular ? 'primary.main' : 'divider',
                '&:hover': {
                  boxShadow: 6,
                  transform: 'translateY(-4px)',
                  transition: 'all 0.3s ease',
                },
              }}
            >
              {plan.popular && (
                <Chip
                  label="Most Popular"
                  color="primary"
                  sx={{
                    position: 'absolute',
                    top: 8,
                    left: '50%',
                    transform: 'translateX(-50%)',
                    zIndex: 1,
                  }}
                />
              )}
              
              {plan.recommended && (
                <Chip
                  label="Recommended"
                  color="secondary"
                  sx={{
                    position: 'absolute',
                    top: 8,
                    right: 16,
                    zIndex: 1,
                  }}
                />
              )}

              <CardContent sx={{ flexGrow: 1, pt: 4 }}>
                <Box sx={{ textAlign: 'center', mb: 3 }}>
                  <Box
                    sx={{
                      display: 'inline-flex',
                      p: 2,
                      borderRadius: '50%',
                      bgcolor: `${getPlanColor(plan.id)}20`,
                      color: getPlanColor(plan.id),
                      mb: 2,
                    }}
                  >
                    {getPlanIcon(plan.id)}
                  </Box>
                  
                  <Typography variant="h5" component="h2" gutterBottom>
                    {plan.name}
                  </Typography>
                  
                  <Box sx={{ mb: 2 }}>
                    <Typography variant="h3" component="span" sx={{ fontWeight: 'bold' }}>
                      {plan.price}
                    </Typography>
                    {plan.originalPrice && (
                      <Typography
                        variant="h6"
                        component="span"
                        sx={{
                          ml: 1,
                          textDecoration: 'line-through',
                          color: 'text.secondary',
                        }}
                      >
                        {plan.originalPrice}
                      </Typography>
                    )}
                  </Box>
                  
                  <Typography variant="body2" color="text.secondary">
                    {plan.description}
                  </Typography>
                </Box>

                <Divider sx={{ my: 2 }} />

                <List dense>
                  {plan.features.map((feature, index) => (
                    <ListItem key={index} sx={{ px: 0 }}>
                      <CheckIcon sx={{ color: 'success.main', mr: 1, fontSize: 20 }} />
                      <ListItemText
                        primary={feature}
                        primaryTypographyProps={{ variant: 'body2' }}
                      />
                    </ListItem>
                  ))}
                </List>

                <Paper
                  variant="outlined"
                  sx={{
                    p: 2,
                    mt: 3,
                    bgcolor: '#0d47a1',
                    color: 'white',
                  }}
                >
                  <Typography variant="subtitle2" gutterBottom sx={{ color: 'white' }}>
                    Plan Limits
                  </Typography>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2" sx={{ color: 'rgba(255,255,255,0.8)' }}>
                      API Calls:
                    </Typography>
                    <Typography variant="body2" fontWeight="medium" sx={{ color: 'white' }}>
                      {plan.limits.apiCalls}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2" sx={{ color: 'rgba(255,255,255,0.8)' }}>
                      Storage:
                    </Typography>
                    <Typography variant="body2" fontWeight="medium" sx={{ color: 'white' }}>
                      {plan.limits.storage}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Typography variant="body2" sx={{ color: 'rgba(255,255,255,0.8)' }}>
                      Support:
                    </Typography>
                    <Typography variant="body2" fontWeight="medium" sx={{ color: 'white' }}>
                      {plan.limits.support}
                    </Typography>
                  </Box>
                </Paper>
              </CardContent>

              <CardActions sx={{ p: 3, pt: 0 }}>
                <Button
                  variant={plan.popular ? 'contained' : 'outlined'}
                  fullWidth
                  size="large"
                  onClick={() => handleSelectPlan(plan.id)}
                  disabled={selectedPlan === plan.id}
                  startIcon={selectedPlan === plan.id ? <CircularProgress size={20} /> : null}
                >
                  {selectedPlan === plan.id
                    ? 'Processing...'
                    : plan.id === 'enterprise'
                    ? 'Contact Sales'
                    : `Choose ${plan.name}`}
                </Button>
              </CardActions>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Box sx={{ textAlign: 'center', mt: 6 }}>
        <Typography variant="h6" gutterBottom>
          Need help choosing a plan?
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Our team is here to help you find the perfect solution for your needs.
        </Typography>
        <Button
          variant="outlined"
          size="large"
          startIcon={<SupportIcon />}
          onClick={() => navigate('/contact')}
        >
          Contact Support
        </Button>
      </Box>
    </Container>
  );
};

export default SubscriptionPlansPage;
