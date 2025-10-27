/**
 * Wallets Page
 * Comprehensive wallet management interface
 */

import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  CardActions,
  Button,
  Grid,
  Chip,
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
  Fab,
  Tooltip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Avatar,
  LinearProgress,
  Badge,
  Stack,
  Divider,
  Alert,
  Switch,
  FormControlLabel,
  Slider,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  ListItemSecondaryAction,
  IconButton as MuiIconButton,
} from '@mui/material';
import {
  AccountBalanceWallet,
  Add,
  Edit,
  Delete,
  Send,
  Download,
  Upload,
  Visibility,
  VisibilityOff,
  Security,
  TrendingUp,
  AttachMoney,
  AccountBalance,
  CreditCard,
  QrCode,
  History,
  Settings,
  Refresh,
  FilterList,
  Search,
  Sort,
  MoreVert,
  CheckCircle,
  Warning,
  Error,
  Info,
  CloudDone,
  CloudOff,
  Speed,
  ExpandMore,
  Warning as WarningIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
  CloudDone as CloudDoneIcon,
  CloudOff as CloudOffIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { walletService } from '../services';
// Using console.log for notifications - can be replaced with proper toast system later

const WalletsPage: React.FC = () => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [sendDialogOpen, setSendDialogOpen] = useState(false);
  const [importDialogOpen, setImportDialogOpen] = useState(false);
  const [exportDialogOpen, setExportDialogOpen] = useState(false);
  const [selectedWallet, setSelectedWallet] = useState<any>(null);
  const [filterType, setFilterType] = useState('all');
  const [searchTerm, setSearchTerm] = useState('');
  const [sortBy, setSortBy] = useState('name');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

  const queryClient = useQueryClient();

  // Fetch wallets
  const { data: walletsData, isLoading, error } = useQuery(
    ['wallets', filterType, searchTerm, sortBy, sortOrder],
    async () => {
      const response = await walletService.getAll();
      return response;
    }
  );

  // Fetch avatar wallets
  const { data: avatarWalletsData, isLoading: avatarWalletsLoading } = useQuery(
    ['avatar-wallets'],
    async () => {
      const response = await walletService.getForAvatar('demo-avatar-1');
      return response;
    }
  );

  // Fetch portfolio value
  const { data: portfolioData } = useQuery(
    ['portfolio-value'],
    async () => {
      const response = await walletService.getPortfolioValue();
      return response;
    }
  );

  // Fetch supported chains
  const { data: chainsData } = useQuery(
    ['supported-chains'],
    async () => {
      const response = await walletService.getSupportedChains();
      return response;
    }
  );

  // Fetch wallets by chain
  const { data: walletsByChainData } = useQuery(
    ['wallets-by-chain', filterType],
    async () => {
      if (filterType !== 'all') {
        const response = await walletService.getWalletsByChain('demo-avatar-1', filterType);
        return response;
      }
      return null;
    },
    { enabled: filterType !== 'all' }
  );

  // Create wallet mutation
  const createWalletMutation = useMutation(
    (walletData: any) => walletService.create(walletData),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['wallets']);
        console.log('Wallet created successfully!');
        setCreateDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to create wallet: ' + error.message);
      },
    }
  );

  // Update wallet mutation
  const updateWalletMutation = useMutation(
    ({ id, data }: { id: string; data: any }) => walletService.update(id, data),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['wallets']);
        console.log('Wallet updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to update wallet: ' + error.message);
      },
    }
  );

  // Delete wallet mutation
  const deleteWalletMutation = useMutation(
    (id: string) => walletService.delete(id),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['wallets']);
        console.log('Wallet deleted successfully!');
      },
      onError: (error: any) => {
        console.error('Failed to delete wallet: ' + error.message);
      },
    }
  );

  // Send tokens mutation
  const sendTokensMutation = useMutation(
    ({ id, toAddress, amount, tokenType }: { id: string; toAddress: string; amount: string; tokenType: string }) =>
      walletService.sendTokens(id, toAddress, amount, tokenType),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['wallets']);
        console.log('Tokens sent successfully!');
        setSendDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to send tokens: ' + error.message);
      },
    }
  );

  // Import wallet mutation
  const importWalletMutation = useMutation(
    ({ privateKey, name }: { privateKey: string; name: string }) => walletService.import(privateKey, name),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['wallets']);
        console.log('Wallet imported successfully!');
        setImportDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to import wallet: ' + error.message);
      },
    }
  );

  // Transfer between wallets mutation
  const transferBetweenWalletsMutation = useMutation(
    ({ fromWalletId, toWalletId, amount, tokenType }: { fromWalletId: string; toWalletId: string; amount: string; tokenType: string }) =>
      walletService.transferBetweenWallets(fromWalletId, toWalletId, amount, tokenType),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['wallets']);
        queryClient.invalidateQueries(['portfolio-value']);
        console.log('Transfer completed successfully!');
      },
      onError: (error: any) => {
        console.error('Failed to transfer: ' + error.message);
      },
    }
  );

  const wallets = walletsData?.result || [];

  const handleCreateWallet = (walletData: any) => {
    createWalletMutation.mutate(walletData);
  };

  const handleUpdateWallet = (id: string, data: any) => {
    updateWalletMutation.mutate({ id, data });
  };

  const handleDeleteWallet = (id: string) => {
    if (window.confirm('Are you sure you want to delete this wallet?')) {
      deleteWalletMutation.mutate(id);
    }
  };

  const handleSendTokens = (id: string, toAddress: string, amount: string, tokenType: string) => {
    sendTokensMutation.mutate({ id, toAddress, amount, tokenType });
  };

  const handleImportWallet = (privateKey: string, name: string) => {
    importWalletMutation.mutate({ privateKey, name });
  };

  if (isLoading) {
    return (
      <Box sx={{ p: 3 }}>
        <LinearProgress />
        <Typography variant="h6" sx={{ mt: 2 }}>
          Loading wallets...
        </Typography>
      </Box>
    );
  }

  if (error) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load wallets: {(error as any)?.message || 'Unknown error'}
        </Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" component="h1" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <AccountBalanceWallet color="primary" />
            OASIS Universal Wallet
          </Typography>
          <Box sx={{ mt: 1, p: 2, bgcolor: '#0d47a1', color: 'white', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
            <Info sx={{ color: 'white' }} />
            <Typography variant="body2" sx={{ color: 'white' }}>
              Universal wallet supporting 15+ blockchains and fiat currencies. One-click transfers between any wallet type.
            </Typography>
          </Box>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<Upload />}
            onClick={() => setImportDialogOpen(true)}
          >
            Import
          </Button>
          <Button
            variant="contained"
            startIcon={<Add />}
            onClick={() => setCreateDialogOpen(true)}
          >
            Create Wallet
          </Button>
        </Box>
      </Box>

      {/* Portfolio Overview */}
      {portfolioData?.result && (
        <Card sx={{ mb: 3, background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)', color: 'white' }}>
          <CardContent>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h5" component="h2">
                Portfolio Overview
              </Typography>
              <Chip 
                label={`${portfolioData.result.currency}`} 
                color="primary" 
                variant="outlined" 
                sx={{ color: 'white', borderColor: 'white' }}
              />
            </Box>
            <Typography variant="h3" component="div" sx={{ mb: 2 }}>
              ${portfolioData.result.totalValueUSD?.toLocaleString() || '0.00'}
            </Typography>
            <Grid container spacing={2}>
              {Object.entries(portfolioData.result.breakdown || {}).map(([chain, data]: [string, any]) => (
                <Grid item xs={6} sm={3} key={chain}>
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" sx={{ opacity: 0.8 }}>
                      {chain.charAt(0).toUpperCase() + chain.slice(1)}
                    </Typography>
                    <Typography variant="h6">
                      ${data.usdValue?.toLocaleString() || '0.00'}
                    </Typography>
                    <Typography variant="caption" sx={{ opacity: 0.7 }}>
                      {data.count} wallet{data.count !== 1 ? 's' : ''}
                    </Typography>
                  </Box>
                </Grid>
              ))}
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* Fund Distribution Graphs */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Fund Distribution by Chain
              </Typography>
              <Box sx={{ height: 200, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'grey.50', borderRadius: 1 }}>
                <Typography variant="body2" color="text.secondary">
                  📊 Chain Distribution Chart
                  <br />
                  Ethereum: 45% | Bitcoin: 25% | Solana: 15% | Others: 15%
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Asset Type Distribution
              </Typography>
              <Box sx={{ height: 200, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'grey.50', borderRadius: 1 }}>
                <Typography variant="body2" color="text.secondary">
                  📈 Asset Type Chart
                  <br />
                  Crypto: 70% | Fiat: 20% | NFTs: 8% | DeFi: 2%
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Filters and Search */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} md={3}>
              <FormControl fullWidth>
                <InputLabel>Filter by Type</InputLabel>
                <Select
                  value={filterType}
                  onChange={(e) => setFilterType(e.target.value)}
                >
                  <MenuItem value="all">All Types</MenuItem>
                  <MenuItem value="Ethereum">Ethereum</MenuItem>
                  <MenuItem value="Bitcoin">Bitcoin</MenuItem>
                  <MenuItem value="Solana">Solana</MenuItem>
                  <MenuItem value="Polygon">Polygon</MenuItem>
                  <MenuItem value="Avalanche">Avalanche</MenuItem>
                  <MenuItem value="BSC">BSC</MenuItem>
                  <MenuItem value="Arbitrum">Arbitrum</MenuItem>
                  <MenuItem value="Optimism">Optimism</MenuItem>
                  <MenuItem value="Cardano">Cardano</MenuItem>
                  <MenuItem value="Polkadot">Polkadot</MenuItem>
                  <MenuItem value="Cosmos">Cosmos</MenuItem>
                  <MenuItem value="USD">USD (Fiat)</MenuItem>
                  <MenuItem value="EUR">EUR (Fiat)</MenuItem>
                  <MenuItem value="GBP">GBP (Fiat)</MenuItem>
                  <MenuItem value="JPY">JPY (Fiat)</MenuItem>
                  <MenuItem value="Other">Other</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                placeholder="Search wallets..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                InputProps={{
                  startAdornment: <Search sx={{ mr: 1, color: 'text.secondary' }} />,
                }}
              />
            </Grid>
            <Grid item xs={12} md={3}>
              <FormControl fullWidth>
                <InputLabel>Sort By</InputLabel>
                <Select
                  value={sortBy}
                  onChange={(e) => setSortBy(e.target.value)}
                >
                  <MenuItem value="name">Name</MenuItem>
                  <MenuItem value="balance">Balance</MenuItem>
                  <MenuItem value="type">Type</MenuItem>
                  <MenuItem value="created">Created Date</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={2}>
              <Button
                variant="outlined"
                onClick={() => setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')}
                startIcon={<Sort />}
              >
                {sortOrder === 'asc' ? 'Ascending' : 'Descending'}
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Wallets Grid */}
      <Grid container spacing={3}>
        {wallets.map((wallet: any) => (
          <Grid item xs={12} sm={6} md={4} lg={3} key={wallet.id}>
            <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <CardContent sx={{ flexGrow: 1 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Avatar sx={{ bgcolor: 'primary.main', mr: 2 }}>
                    <AccountBalanceWallet />
                  </Avatar>
                  <Box sx={{ flexGrow: 1 }}>
                    <Typography variant="h6" noWrap>
                      {wallet.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {wallet.type}
                    </Typography>
                  </Box>
                  <IconButton size="small">
                    <MoreVert />
                  </IconButton>
                </Box>

                <Box sx={{ mb: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    Address
                  </Typography>
                  <Typography variant="body2" sx={{ fontFamily: 'monospace', wordBreak: 'break-all' }}>
                    {wallet.address}
                  </Typography>
                </Box>

                <Box sx={{ mb: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    Balance
                  </Typography>
                  <Typography variant="h6" color="primary">
                    {wallet.balance}
                  </Typography>
                </Box>

                <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
                  <Chip
                    label={wallet.type}
                    size="small"
                    color="primary"
                    variant="outlined"
                  />
                  <Chip
                    label="Active"
                    size="small"
                    color="success"
                    variant="outlined"
                  />
                </Stack>
              </CardContent>

              <CardActions sx={{ p: 2, pt: 0 }}>
                <Button
                  size="small"
                  startIcon={<Send />}
                  onClick={() => {
                    setSelectedWallet(wallet);
                    setSendDialogOpen(true);
                  }}
                >
                  Send
                </Button>
                <Button
                  size="small"
                  startIcon={<Edit />}
                  onClick={() => {
                    setSelectedWallet(wallet);
                    setEditDialogOpen(true);
                  }}
                >
                  Edit
                </Button>
                <Button
                  size="small"
                  startIcon={<Download />}
                  onClick={() => setExportDialogOpen(true)}
                >
                  Export
                </Button>
                <Button
                  size="small"
                  color="error"
                  startIcon={<Delete />}
                  onClick={() => handleDeleteWallet(wallet.id)}
                >
                  Delete
                </Button>
              </CardActions>
            </Card>
          </Grid>
        ))}
      </Grid>

      {/* Create Wallet Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New Wallet</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            <TextField
              fullWidth
              label="Wallet Name"
              margin="normal"
              required
            />
            <FormControl fullWidth margin="normal">
              <InputLabel>Wallet Type</InputLabel>
              <Select defaultValue="Ethereum">
                <MenuItem value="Ethereum">Ethereum</MenuItem>
                <MenuItem value="Bitcoin">Bitcoin</MenuItem>
                <MenuItem value="Other">Other</MenuItem>
              </Select>
            </FormControl>
            <TextField
              fullWidth
              label="Description (Optional)"
              margin="normal"
              multiline
              rows={3}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => handleCreateWallet({ name: 'New Wallet', type: 'Ethereum' })}
            disabled={createWalletMutation.isLoading}
          >
            {createWalletMutation.isLoading ? 'Creating...' : 'Create Wallet'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Send Tokens Dialog */}
      <Dialog open={sendDialogOpen} onClose={() => setSendDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Send Tokens</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            <TextField
              fullWidth
              label="To Address"
              margin="normal"
              required
              placeholder="0x..."
            />
            <TextField
              fullWidth
              label="Amount"
              margin="normal"
              required
              type="number"
            />
            <FormControl fullWidth margin="normal">
              <InputLabel>Token Type</InputLabel>
              <Select defaultValue="ETH">
                <MenuItem value="ETH">ETH</MenuItem>
                <MenuItem value="BTC">BTC</MenuItem>
                <MenuItem value="USDC">USDC</MenuItem>
              </Select>
            </FormControl>
            <TextField
              fullWidth
              label="Message (Optional)"
              margin="normal"
              multiline
              rows={2}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSendDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => handleSendTokens(selectedWallet?.id || '', '0x...', '0.1', 'ETH')}
            disabled={sendTokensMutation.isLoading}
          >
            {sendTokensMutation.isLoading ? 'Sending...' : 'Send Tokens'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Import Wallet Dialog */}
      <Dialog open={importDialogOpen} onClose={() => setImportDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Import Wallet</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            <TextField
              fullWidth
              label="Wallet Name"
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Private Key"
              margin="normal"
              required
              multiline
              rows={3}
              placeholder="Enter your private key..."
            />
            <Alert severity="warning" sx={{ mt: 2 }}>
              Never share your private key with anyone. This information is sensitive and should be kept secure.
            </Alert>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setImportDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => handleImportWallet('private-key', 'Imported Wallet')}
            disabled={importWalletMutation.isLoading}
          >
            {importWalletMutation.isLoading ? 'Importing...' : 'Import Wallet'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Floating Action Button */}
      <Fab
        color="primary"
        sx={{ position: 'fixed', bottom: 16, right: 16 }}
        onClick={() => setCreateDialogOpen(true)}
      >
        <Add />
      </Fab>
    </Box>
  );
};

export default WalletsPage;
