import { useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Card,
  CardContent,
  IconButton,
  Button,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider,
  Chip,
} from '@mui/material'
import {
  ArrowBack as ArrowBackIcon,
  AccountBalanceWallet as WalletIcon,
  Add as AddIcon,
  History as HistoryIcon,
  Payment as PaymentIcon,
  AccountBalance as AccountBalanceIcon,
} from '@mui/icons-material'

const WalletScreen = () => {
  const navigate = useNavigate()

  const transactions = [
    {
      id: 1,
      type: 'ride',
      amount: -250,
      description: 'Ride to Umhlanga',
      date: '2025-01-15',
      status: 'completed',
    },
    {
      id: 2,
      type: 'topup',
      amount: 500,
      description: 'Wallet Top-up',
      date: '2025-01-10',
      status: 'completed',
    },
    {
      id: 3,
      type: 'ride',
      amount: -180,
      description: 'Ride to Gateway Mall',
      date: '2025-01-05',
      status: 'completed',
    },
  ]

  const balance = 1250.50

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: 'background.default' }}>
      {/* Header */}
      <Box
        sx={{
          backgroundColor: 'primary.main',
          color: 'white',
          p: 2,
          display: 'flex',
          alignItems: 'center',
          gap: 2,
        }}
      >
        <IconButton onClick={() => navigate('/home')} sx={{ color: 'white' }}>
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h6" sx={{ flex: 1, fontWeight: 600 }}>
          My Wallet
        </Typography>
      </Box>

      {/* Balance Card */}
      <Card
        sx={{
          m: 2,
          background: 'linear-gradient(135deg, #2847bc 0%, #3d5ed9 50%, #1534aa 100%)',
          backgroundSize: '200% 200%',
          color: 'white',
          boxShadow: '0 8px 32px rgba(40, 71, 188, 0.4), 0 0 40px rgba(40, 71, 188, 0.2)',
          animation: 'gradientShift 3s ease infinite',
          position: 'relative',
          overflow: 'hidden',
          '&::before': {
            content: '""',
            position: 'absolute',
            top: '-50%',
            right: '-50%',
            width: '200%',
            height: '200%',
            background: 'radial-gradient(circle, rgba(254, 217, 2, 0.1) 0%, transparent 70%)',
            animation: 'pulseGlow 4s ease-in-out infinite',
          },
        }}
      >
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
            <WalletIcon sx={{ fontSize: 40 }} />
            <Box>
              <Typography variant="body2" sx={{ opacity: 0.9 }}>
                Wallet Balance
              </Typography>
              <Typography 
                variant="h4" 
                fontWeight={700}
                sx={{
                  textShadow: '0 0 20px rgba(254, 217, 2, 0.5), 0 0 40px rgba(254, 217, 2, 0.3)',
                }}
              >
                R{balance.toFixed(2)}
              </Typography>
            </Box>
          </Box>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            sx={{
              background: 'linear-gradient(135deg, #fed902 0%, #fab700 50%, #fed902 100%)',
              backgroundSize: '200% 200%',
              color: 'black',
              boxShadow: '0 4px 20px rgba(254, 217, 2, 0.5), 0 0 30px rgba(254, 217, 2, 0.3)',
              animation: 'gradientShift 3s ease infinite',
              position: 'relative',
              zIndex: 1,
              '&:hover': {
                boxShadow: '0 6px 30px rgba(254, 217, 2, 0.7), 0 0 40px rgba(254, 217, 2, 0.5)',
                transform: 'translateY(-2px)',
                backgroundPosition: 'right center',
              },
            }}
            fullWidth
          >
            Add Money
          </Button>
        </CardContent>
      </Card>

      {/* Payment Methods */}
      <Box sx={{ px: 2, mb: 2 }}>
        <Typography variant="h6" sx={{ mb: 1, fontWeight: 600 }}>
          Payment Methods
        </Typography>
        <Card>
          <List>
            <ListItem button>
              <ListItemIcon>
                <AccountBalanceIcon color="primary" />
              </ListItemIcon>
              <ListItemText
                primary="M-Pesa"
                secondary="+27 82 123 4567"
              />
              <Chip label="Primary" size="small" color="primary" />
            </ListItem>
            <Divider />
            <ListItem button>
              <ListItemIcon>
                <PaymentIcon color="primary" />
              </ListItemIcon>
              <ListItemText
                primary="MTN Mobile Money"
                secondary="+27 82 123 4567"
              />
            </ListItem>
            <Divider />
            <ListItem button>
              <ListItemIcon>
                <WalletIcon color="primary" />
              </ListItemIcon>
              <ListItemText
                primary="Add Payment Method"
                secondary="Add new payment option"
              />
            </ListItem>
          </List>
        </Card>
      </Box>

      {/* Transaction History */}
      <Box sx={{ px: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
          <HistoryIcon color="primary" />
          <Typography variant="h6" fontWeight={600}>
            Transaction History
          </Typography>
        </Box>

        {transactions.map((transaction) => (
          <Card key={transaction.id} sx={{ mb: 2 }}>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Box>
                  <Typography variant="subtitle1" fontWeight={600}>
                    {transaction.description}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {transaction.date}
                  </Typography>
                </Box>
                <Typography
                  variant="h6"
                  fontWeight={700}
                  color={transaction.amount > 0 ? 'success.main' : 'text.primary'}
                  sx={{
                    textShadow: transaction.amount > 0 
                      ? '0 0 10px rgba(74, 204, 18, 0.4)' 
                      : 'none',
                  }}
                >
                  {transaction.amount > 0 ? '+' : ''}R{Math.abs(transaction.amount).toFixed(2)}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        ))}
      </Box>
    </Box>
  )
}

export default WalletScreen

