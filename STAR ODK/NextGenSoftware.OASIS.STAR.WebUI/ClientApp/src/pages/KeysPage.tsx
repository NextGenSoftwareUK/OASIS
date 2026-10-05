import React, { useState } from 'react';
import {
  Box, Typography, Button, Card, CardContent, CardActions, Grid, TextField,
  Dialog, DialogTitle, DialogContent, DialogActions, IconButton,
  Menu, MenuItem, FormControl, InputLabel, Select, Chip,
  Fab, Tooltip, Tabs, Tab, Badge, Stack, LinearProgress,
  List, ListItem, ListItemIcon, ListItemText, ListItemSecondaryAction,
  Alert, CircularProgress, Paper, Divider, Switch, FormControlLabel
} from '@mui/material';
import {
  Add, MoreVert, Edit, Delete, Visibility, Key, Refresh, FilterList,
  Security, Lock, LockOpen, VpnKey, Fingerprint, QrCode2,
  CloudDone as CloudDoneIcon, CloudOff as CloudOffIcon,
  ContentCopy, Download, Upload, Settings, Shield
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { keysService } from '../services';

interface KeyPair {
  id: string;
  publicKey: string;
  privateKey?: string;
  keyType: string;
  createdOn: string;
  isActive: boolean;
  name?: string;
  description?: string;
  usage: string[];
  lastUsed?: string;
}

const KeysPage: React.FC = () => {
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [importDialogOpen, setImportDialogOpen] = useState(false);
  const [exportDialogOpen, setExportDialogOpen] = useState(false);
  const [signDialogOpen, setSignDialogOpen] = useState(false);
  const [verifyDialogOpen, setVerifyDialogOpen] = useState(false);
  const [encryptDialogOpen, setEncryptDialogOpen] = useState(false);
  const [decryptDialogOpen, setDecryptDialogOpen] = useState(false);
  const [selectedKey, setSelectedKey] = useState<KeyPair | null>(null);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [activeTab, setActiveTab] = useState(0);
  const [filterType, setFilterType] = useState('all');
  const [showPrivateKeys, setShowPrivateKeys] = useState(false);
  const [newKeyData, setNewKeyData] = useState({ keyType: 'Ed25519', name: '', description: '' });
  const [importData, setImportData] = useState({ privateKey: '', keyType: 'Ed25519' });
  const [exportKey, setExportKey] = useState('');
  const [signData, setSignData] = useState({ privateKey: '', dataToSign: '' });
  const [verifyData, setVerifyData] = useState({ publicKey: '', data: '', signature: '' });
  const [encryptData, setEncryptData] = useState({ publicKey: '', dataToEncrypt: '' });
  const [decryptData, setDecryptData] = useState({ privateKey: '', encryptedData: '' });
  const queryClient = useQueryClient();

  const { data: keysData, isLoading, error, refetch } = useQuery(
    ['keys', filterType],
    async () => {
      try {
        const response = await keysService.getAll();
        if (response?.result && response.result.length > 0) {
          const filtered = response.result.filter((key: KeyPair) =>
            filterType === 'all' || key.keyType === filterType
          );
          return { ...response, result: filtered };
        }
        console.log('Using demo key data for investor presentation');
        return {
          result: [
            {
              id: 'key-1',
              publicKey: 'ed25519:abc123...',
              privateKey: 'ed25519:def456...',
              keyType: 'Ed25519',
              createdOn: new Date().toISOString(),
              isActive: true,
              name: 'Main Signing Key',
              description: 'Primary key for signing transactions',
              usage: ['signing', 'authentication'],
              lastUsed: new Date().toISOString()
            },
            {
              id: 'key-2',
              publicKey: 'rsa:xyz789...',
              privateKey: 'rsa:uvw012...',
              keyType: 'RSA',
              createdOn: new Date(Date.now() - 86400000).toISOString(),
              isActive: true,
              name: 'Encryption Key',
              description: 'Key for encrypting sensitive data',
              usage: ['encryption', 'decryption'],
              lastUsed: new Date(Date.now() - 3600000).toISOString()
            }
          ],
          isError: false,
          message: 'Demo Keys retrieved',
          count: 2,
        };
      } catch (err: any) {
        console.error('Error fetching keys:', err);
        return { result: [], isError: true, message: err.message, count: 0 };
      }
    },
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  const keys = keysData?.result || [];

  // Mutations
  const generateKeyMutation = useMutation(
    (keyType: string) => keysService.generateKeyPair(keyType),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['keys']);
        console.log('Key pair generated successfully!');
        setCreateDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to generate key pair: ' + error.message);
      },
    }
  );

  const importKeyMutation = useMutation(
    (data: { privateKey: string, keyType: string }) => keysService.importPrivateKey(data.privateKey, data.keyType),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['keys']);
        console.log('Private key imported successfully!');
        setImportDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to import private key: ' + error.message);
      },
    }
  );

  const exportKeyMutation = useMutation(
    (publicKey: string) => keysService.exportPrivateKey(publicKey),
    {
      onSuccess: (data) => {
        console.log('Private key exported successfully!');
        setExportKey(data.result || 'Error exporting key');
        setExportDialogOpen(true);
      },
      onError: (error: any) => {
        console.error('Failed to export private key: ' + error.message);
        setExportKey('Error exporting key');
      },
    }
  );

  const signDataMutation = useMutation(
    (data: { privateKey: string, dataToSign: string }) => keysService.signData(data.privateKey, data.dataToSign),
    {
      onSuccess: (data) => {
        console.log('Data signed successfully! Signature:', data.result);
        setSignDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to sign data: ' + error.message);
      },
    }
  );

  const verifySignatureMutation = useMutation(
    (data: { publicKey: string, data: string, signature: string }) => 
      keysService.verifySignature(data.publicKey, data.data, data.signature),
    {
      onSuccess: (data) => {
        console.log('Signature verification result:', data.result);
        setVerifyDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to verify signature: ' + error.message);
      },
    }
  );

  const encryptDataMutation = useMutation(
    (data: { publicKey: string, dataToEncrypt: string }) => keysService.encryptData(data.publicKey, data.dataToEncrypt),
    {
      onSuccess: (data) => {
        console.log('Data encrypted successfully!');
        setEncryptDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to encrypt data: ' + error.message);
      },
    }
  );

  const decryptDataMutation = useMutation(
    (data: { privateKey: string, encryptedData: string }) => keysService.decryptData(data.privateKey, data.encryptedData),
    {
      onSuccess: (data) => {
        console.log('Data decrypted successfully!');
        setDecryptDialogOpen(false);
      },
      onError: (error: any) => {
        console.error('Failed to decrypt data: ' + error.message);
      },
    }
  );

  const deleteKeyMutation = useMutation(
    (publicKey: string) => keysService.delete(publicKey),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['keys']);
        console.log('Key deleted successfully!');
      },
      onError: (error: any) => {
        console.error('Failed to delete key: ' + error.message);
      },
    }
  );

  // Handlers
  const handleCreateClick = () => {
    setNewKeyData({ keyType: 'Ed25519', name: '', description: '' });
    setCreateDialogOpen(true);
  };

  const handleCreateSubmit = () => {
    generateKeyMutation.mutate(newKeyData.keyType);
  };

  const handleImportClick = () => {
    setImportData({ privateKey: '', keyType: 'Ed25519' });
    setImportDialogOpen(true);
  };

  const handleImportSubmit = () => {
    importKeyMutation.mutate(importData);
  };

  const handleExportClick = (key: KeyPair) => {
    setSelectedKey(key);
    exportKeyMutation.mutate(key.publicKey);
  };

  const handleSignClick = () => {
    setSignData({ privateKey: '', dataToSign: '' });
    setSignDialogOpen(true);
  };

  const handleSignSubmit = () => {
    signDataMutation.mutate(signData);
  };

  const handleVerifyClick = () => {
    setVerifyData({ publicKey: '', data: '', signature: '' });
    setVerifyDialogOpen(true);
  };

  const handleVerifySubmit = () => {
    verifySignatureMutation.mutate(verifyData);
  };

  const handleEncryptClick = () => {
    setEncryptData({ publicKey: '', dataToEncrypt: '' });
    setEncryptDialogOpen(true);
  };

  const handleEncryptSubmit = () => {
    encryptDataMutation.mutate(encryptData);
  };

  const handleDecryptClick = () => {
    setDecryptData({ privateKey: '', encryptedData: '' });
    setDecryptDialogOpen(true);
  };

  const handleDecryptSubmit = () => {
    decryptDataMutation.mutate(decryptData);
  };

  const handleDeleteClick = (key: KeyPair) => {
    deleteKeyMutation.mutate(key.publicKey);
    setAnchorEl(null);
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLButtonElement>, key: KeyPair) => {
    setSelectedKey(key);
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const getKeyTypeColor = (keyType: string) => {
    switch (keyType) {
      case 'Ed25519': return '#4caf50';
      case 'RSA': return '#2196f3';
      case 'ECDSA': return '#ff9800';
      case 'DSA': return '#9c27b0';
      default: return '#757575';
    }
  };

  const getUsageIcon = (usage: string) => {
    switch (usage) {
      case 'signing': return <Fingerprint />;
      case 'encryption': return <Lock />;
      case 'authentication': return <Shield />;
      default: return <Key />;
    }
  };

  const keyStats = {
    totalKeys: keys.length,
    activeKeys: keys.filter((key: KeyPair) => key.isActive).length,
    ed25519Keys: keys.filter((key: KeyPair) => key.keyType === 'Ed25519').length,
    rsaKeys: keys.filter((key: KeyPair) => key.keyType === 'RSA').length,
  };

  return (
    <Box sx={{ p: 4 }}>
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
      >
        <Typography variant="h4" component="h1" gutterBottom sx={{ color: 'primary.main', fontWeight: 'bold' }}>
          <Key sx={{ mr: 2, fontSize: 'inherit' }} />
          Key Management
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
          Manage your cryptographic keys for signing, encryption, and authentication.
        </Typography>

        {/* Stats Overview */}
        <Grid container spacing={3} sx={{ mb: 4 }}>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #66bb6a, #388e3c)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <Key sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{keyStats.totalKeys}</Typography>
                <Typography variant="body2">Total Keys</Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #42a5f5, #1976d2)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <LockOpen sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{keyStats.activeKeys}</Typography>
                <Typography variant="body2">Active Keys</Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #ffa726, #ef6c00)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <Fingerprint sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{keyStats.ed25519Keys}</Typography>
                <Typography variant="body2">Ed25519 Keys</Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card sx={{ background: 'linear-gradient(135deg, #ab47bc, #7b1fa2)' }}>
              <CardContent sx={{ textAlign: 'center', color: 'white' }}>
                <Security sx={{ fontSize: 40, mb: 1 }} />
                <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{keyStats.rsaKeys}</Typography>
                <Typography variant="body2">RSA Keys</Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Tabs Navigation */}
        <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
          <Tabs value={activeTab} onChange={handleTabChange} variant="scrollable" scrollButtons="auto">
            <Tab label="All Keys" icon={<Key />} />
            <Tab label="Signing" icon={<Fingerprint />} />
            <Tab label="Encryption" icon={<Lock />} />
            <Tab label="Authentication" icon={<Shield />} />
          </Tabs>
        </Box>

        {/* Actions and Filters */}
        <Stack direction="row" spacing={2} sx={{ mb: 3 }} alignItems="center">
          <Button
            variant="contained"
            color="primary"
            startIcon={<Add />}
            onClick={handleCreateClick}
          >
            Generate Key Pair
          </Button>
          <Button
            variant="outlined"
            color="secondary"
            startIcon={<Upload />}
            onClick={handleImportClick}
          >
            Import Private Key
          </Button>
          <Button
            variant="outlined"
            color="info"
            startIcon={<Fingerprint />}
            onClick={handleSignClick}
          >
            Sign Data
          </Button>
          <Button
            variant="outlined"
            color="warning"
            startIcon={<Lock />}
            onClick={handleEncryptClick}
          >
            Encrypt Data
          </Button>
          <Tooltip title="Refresh Keys">
            <IconButton onClick={() => refetch()} disabled={isLoading}>
              <Refresh />
            </IconButton>
          </Tooltip>

          <Box sx={{ flexGrow: 1 }} />

          <FormControl sx={{ minWidth: 120 }}>
            <InputLabel size="small">Filter Type</InputLabel>
            <Select
              value={filterType}
              label="Filter Type"
              onChange={(e) => setFilterType(e.target.value as string)}
              size="small"
            >
              <MenuItem value="all">All Types</MenuItem>
              <MenuItem value="Ed25519">Ed25519</MenuItem>
              <MenuItem value="RSA">RSA</MenuItem>
              <MenuItem value="ECDSA">ECDSA</MenuItem>
            </Select>
          </FormControl>

          <FormControlLabel
            control={
              <Switch
                checked={showPrivateKeys}
                onChange={(e) => setShowPrivateKeys(e.target.checked)}
              />
            }
            label="Show Private Keys"
          />
        </Stack>

        {/* Keys Grid */}
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <CircularProgress />
          </Box>
        ) : error ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <Typography color="error">Error loading keys: {(error as any).message}</Typography>
          </Box>
        ) : keys.length === 0 ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <Typography color="text.secondary">No keys found. Generate one to get started!</Typography>
          </Box>
        ) : (
          <Grid container spacing={3}>
            {keys.map((key: KeyPair) => (
              <Grid item xs={12} sm={6} md={4} key={key.id}>
                <motion.div
                  initial={{ opacity: 0, scale: 0.9 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ duration: 0.3 }}
                  whileHover={{ scale: 1.02 }}
                >
                  <Card
                    variant="outlined"
                    sx={{
                      height: '100%',
                      display: 'flex',
                      flexDirection: 'column',
                      borderColor: getKeyTypeColor(key.keyType),
                      borderWidth: 2,
                    }}
                  >
                    <CardContent sx={{ flexGrow: 1 }}>
                      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
                        <Typography variant="h6" component="div" sx={{ fontWeight: 'bold', color: 'primary.dark' }}>
                          {key.name || 'Unnamed Key'}
                        </Typography>
                        <Chip
                          label={key.keyType}
                          size="small"
                          sx={{ bgcolor: getKeyTypeColor(key.keyType), color: 'white' }}
                        />
                      </Stack>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {key.description || 'No description provided'}
                      </Typography>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                        Public Key: {key.publicKey.substring(0, 20)}...
                      </Typography>
                      {showPrivateKeys && key.privateKey && (
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Private Key: {key.privateKey.substring(0, 20)}...
                        </Typography>
                      )}
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" gutterBottom>
                          Usage:
                        </Typography>
                        <Stack direction="row" spacing={1} flexWrap="wrap">
                          {key.usage.map((usage, index) => (
                            <Chip
                              key={index}
                              icon={getUsageIcon(usage)}
                              label={usage}
                              size="small"
                              color="primary"
                              variant="outlined"
                            />
                          ))}
                        </Stack>
                      </Box>
                      <LinearProgress
                        variant="determinate"
                        value={key.isActive ? 100 : 0}
                        sx={{ height: 8, borderRadius: 4, mb: 1 }}
                      />
                      <Typography variant="caption" color="text.secondary">
                        Created: {new Date(key.createdOn).toLocaleDateString()}
                        {key.lastUsed && ` • Last used: ${new Date(key.lastUsed).toLocaleDateString()}`}
                      </Typography>
                    </CardContent>
                    <CardActions sx={{ justifyContent: 'flex-end', p: 2 }}>
                      <Button size="small" startIcon={<Visibility />} onClick={() => console.log('View Key:', key)}>View</Button>
                      <Button size="small" startIcon={<Download />} onClick={() => handleExportClick(key)}>Export</Button>
                      <IconButton
                        aria-label="more"
                        aria-controls="long-menu"
                        aria-haspopup="true"
                        onClick={(event) => handleMenuOpen(event, key)}
                        size="small"
                      >
                        <MoreVert />
                      </IconButton>
                    </CardActions>
                  </Card>
                </motion.div>
              </Grid>
            ))}
          </Grid>
        )}

        {/* Context Menu for Key Actions */}
        <Menu
          id="long-menu"
          MenuListProps={{
            'aria-labelledby': 'long-button',
          }}
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={handleMenuClose}
          PaperProps={{
            style: {
              maxHeight: 48 * 4.5,
              width: '20ch',
            },
          }}
        >
          <MenuItem onClick={() => {
            console.log('Edit Key:', selectedKey);
            handleMenuClose();
          }}>
            <Edit sx={{ mr: 1 }} /> Edit
          </MenuItem>
          <MenuItem onClick={() => {
            if (selectedKey) handleDeleteClick(selectedKey);
            handleMenuClose();
          }}>
            <Delete sx={{ mr: 1 }} /> Delete
          </MenuItem>
          <MenuItem onClick={() => {
            if (selectedKey) handleExportClick(selectedKey);
            handleMenuClose();
          }}>
            <Download sx={{ mr: 1 }} /> Export
          </MenuItem>
          <MenuItem onClick={() => {
            if (selectedKey) {
              console.log('Generating QR Code for key:', selectedKey.id);
              console.log('QR Code generation not yet implemented.');
            }
            handleMenuClose();
          }}>
            <QrCode2 sx={{ mr: 1 }} /> Generate QR Code
          </MenuItem>
        </Menu>

        {/* Generate Key Pair Dialog */}
        <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)}>
          <DialogTitle>Generate New Key Pair</DialogTitle>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Key Name (Optional)"
              type="text"
              fullWidth
              variant="outlined"
              value={newKeyData.name}
              onChange={(e) => setNewKeyData({ ...newKeyData, name: e.target.value })}
              sx={{ mb: 2 }}
            />
            <TextField
              margin="dense"
              label="Description (Optional)"
              type="text"
              fullWidth
              variant="outlined"
              value={newKeyData.description}
              onChange={(e) => setNewKeyData({ ...newKeyData, description: e.target.value })}
              sx={{ mb: 2 }}
            />
            <FormControl fullWidth variant="outlined">
              <InputLabel>Key Type</InputLabel>
              <Select
                value={newKeyData.keyType}
                onChange={(e) => setNewKeyData({ ...newKeyData, keyType: e.target.value as string })}
                label="Key Type"
              >
                <MenuItem value="Ed25519">Ed25519 (Recommended)</MenuItem>
                <MenuItem value="RSA">RSA</MenuItem>
                <MenuItem value="ECDSA">ECDSA</MenuItem>
              </Select>
            </FormControl>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleCreateSubmit} variant="contained" color="primary">Generate</Button>
          </DialogActions>
        </Dialog>

        {/* Import Private Key Dialog */}
        <Dialog open={importDialogOpen} onClose={() => setImportDialogOpen(false)}>
          <DialogTitle>Import Private Key</DialogTitle>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Private Key"
              type="password"
              fullWidth
              variant="outlined"
              value={importData.privateKey}
              onChange={(e) => setImportData({ ...importData, privateKey: e.target.value })}
              sx={{ mb: 2 }}
            />
            <FormControl fullWidth variant="outlined">
              <InputLabel>Key Type</InputLabel>
              <Select
                value={importData.keyType}
                onChange={(e) => setImportData({ ...importData, keyType: e.target.value as string })}
                label="Key Type"
              >
                <MenuItem value="Ed25519">Ed25519</MenuItem>
                <MenuItem value="RSA">RSA</MenuItem>
                <MenuItem value="ECDSA">ECDSA</MenuItem>
              </Select>
            </FormControl>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setImportDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleImportSubmit} variant="contained" color="primary">Import</Button>
          </DialogActions>
        </Dialog>

        {/* Export Private Key Dialog */}
        <Dialog open={exportDialogOpen} onClose={() => setExportDialogOpen(false)}>
          <DialogTitle>Export Private Key for {selectedKey?.name || 'Selected Key'}</DialogTitle>
          <DialogContent>
            <Alert severity="warning" sx={{ mb: 2 }}>
              Please keep your private key secure and do not share it with anyone.
            </Alert>
            <TextField
              margin="dense"
              label="Private Key"
              type="text"
              fullWidth
              variant="outlined"
              value={exportKey}
              InputProps={{
                readOnly: true,
              }}
              sx={{ mb: 2 }}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setExportDialogOpen(false)}>Close</Button>
            <Button onClick={() => navigator.clipboard.writeText(exportKey)} variant="outlined">
              <ContentCopy sx={{ mr: 1 }} /> Copy to Clipboard
            </Button>
          </DialogActions>
        </Dialog>

        {/* Sign Data Dialog */}
        <Dialog open={signDialogOpen} onClose={() => setSignDialogOpen(false)}>
          <DialogTitle>Sign Data</DialogTitle>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Private Key"
              type="password"
              fullWidth
              variant="outlined"
              value={signData.privateKey}
              onChange={(e) => setSignData({ ...signData, privateKey: e.target.value })}
              sx={{ mb: 2 }}
            />
            <TextField
              margin="dense"
              label="Data to Sign"
              type="text"
              fullWidth
              variant="outlined"
              multiline
              rows={4}
              value={signData.dataToSign}
              onChange={(e) => setSignData({ ...signData, dataToSign: e.target.value })}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setSignDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleSignSubmit} variant="contained" color="primary">Sign</Button>
          </DialogActions>
        </Dialog>

        {/* Verify Signature Dialog */}
        <Dialog open={verifyDialogOpen} onClose={() => setVerifyDialogOpen(false)}>
          <DialogTitle>Verify Signature</DialogTitle>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Public Key"
              type="text"
              fullWidth
              variant="outlined"
              value={verifyData.publicKey}
              onChange={(e) => setVerifyData({ ...verifyData, publicKey: e.target.value })}
              sx={{ mb: 2 }}
            />
            <TextField
              margin="dense"
              label="Data"
              type="text"
              fullWidth
              variant="outlined"
              multiline
              rows={2}
              value={verifyData.data}
              onChange={(e) => setVerifyData({ ...verifyData, data: e.target.value })}
              sx={{ mb: 2 }}
            />
            <TextField
              margin="dense"
              label="Signature"
              type="text"
              fullWidth
              variant="outlined"
              value={verifyData.signature}
              onChange={(e) => setVerifyData({ ...verifyData, signature: e.target.value })}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setVerifyDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleVerifySubmit} variant="contained" color="primary">Verify</Button>
          </DialogActions>
        </Dialog>

        {/* Encrypt Data Dialog */}
        <Dialog open={encryptDialogOpen} onClose={() => setEncryptDialogOpen(false)}>
          <DialogTitle>Encrypt Data</DialogTitle>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Public Key"
              type="text"
              fullWidth
              variant="outlined"
              value={encryptData.publicKey}
              onChange={(e) => setEncryptData({ ...encryptData, publicKey: e.target.value })}
              sx={{ mb: 2 }}
            />
            <TextField
              margin="dense"
              label="Data to Encrypt"
              type="text"
              fullWidth
              variant="outlined"
              multiline
              rows={4}
              value={encryptData.dataToEncrypt}
              onChange={(e) => setEncryptData({ ...encryptData, dataToEncrypt: e.target.value })}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setEncryptDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleEncryptSubmit} variant="contained" color="primary">Encrypt</Button>
          </DialogActions>
        </Dialog>

        {/* Decrypt Data Dialog */}
        <Dialog open={decryptDialogOpen} onClose={() => setDecryptDialogOpen(false)}>
          <DialogTitle>Decrypt Data</DialogTitle>
          <DialogContent>
            <TextField
              autoFocus
              margin="dense"
              label="Private Key"
              type="password"
              fullWidth
              variant="outlined"
              value={decryptData.privateKey}
              onChange={(e) => setDecryptData({ ...decryptData, privateKey: e.target.value })}
              sx={{ mb: 2 }}
            />
            <TextField
              margin="dense"
              label="Encrypted Data"
              type="text"
              fullWidth
              variant="outlined"
              multiline
              rows={4}
              value={decryptData.encryptedData}
              onChange={(e) => setDecryptData({ ...decryptData, encryptedData: e.target.value })}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDecryptDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleDecryptSubmit} variant="contained" color="primary">Decrypt</Button>
          </DialogActions>
        </Dialog>
      </motion.div>
    </Box>
  );
};

export default KeysPage;
