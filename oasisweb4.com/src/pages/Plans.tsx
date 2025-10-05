import { Card, CardContent, CardHeader, CardActions, Button, Grid, Typography, List, ListItem, ListItemText } from '@mui/material';
import { createCheckoutSession } from '../services/subscriptionApi';

type Plan = {
  id: string;
  name: string;
  price: string;
  features: string[];
};

const plans: Plan[] = [
  { id: 'bronze', name: 'Bronze', price: '$9/mo', features: ['Starter API limits', 'Community support'] },
  { id: 'silver', name: 'Silver', price: '$29/mo', features: ['Higher API limits', 'Email support', 'Basic analytics'] },
  { id: 'gold', name: 'Gold', price: '$99/mo', features: ['Premium API limits', 'Priority support', 'Advanced analytics'] },
  { id: 'enterprise', name: 'Enterprise', price: 'Contact us', features: ['Custom limits', 'SLA & SSO', 'Dedicated support'] }
];

export default function Plans() {
  const handleCheckout = async (planId: string) => {
    await createCheckoutSession(planId);
  };

  return (
    <Grid container spacing={3}>
      {plans.map((plan) => (
        <Grid item xs={12} md={3} key={plan.id}>
          <Card>
            <CardHeader title={plan.name} subheader={plan.price} />
            <CardContent>
              <List dense>
                {plan.features.map((f) => (
                  <ListItem key={f} disableGutters>
                    <ListItemText primary={f} />
                  </ListItem>
                ))}
              </List>
            </CardContent>
            <CardActions>
              <Button variant="contained" fullWidth onClick={() => handleCheckout(plan.id)}>
                {plan.id === 'enterprise' ? 'Contact Sales' : `Choose ${plan.name}`}
              </Button>
            </CardActions>
          </Card>
        </Grid>
      ))}
    </Grid>
  );
}


