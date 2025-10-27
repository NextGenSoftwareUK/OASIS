/**
 * STAR CLI Page
 * Web interface for STAR CLI commands
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
  Terminal,
  PlayArrow,
  Stop,
  Refresh,
  Download,
  Upload,
  Code,
  Build,
  Rocket,
  Star,
  Settings,
  Help,
  Info,
  Warning,
  Error,
  CheckCircle,
  ExpandMore,
  Add,
  Edit,
  Delete,
  Visibility,
  VisibilityOff,
  Security,
  TrendingUp,
  AttachMoney,
  AccountBalance,
  CreditCard,
  QrCode,
  History,
  FilterList,
  Search,
  Sort,
  MoreVert,
  CloudDone,
  CloudOff,
  Speed,
  Warning as WarningIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
  CloudDone as CloudDoneIcon,
  CloudOff as CloudOffIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { starCoreService } from '../services';
// Using console.log for notifications - can be replaced with proper toast system later

const STARCLIPage: React.FC = () => {
  const [selectedCommand, setSelectedCommand] = useState<any | null>(null);
  const [commandDialogOpen, setCommandDialogOpen] = useState(false);
  const [commandHistory, setCommandHistory] = useState<string[]>([]);
  const [currentCommand, setCurrentCommand] = useState('');
  const [isRunning, setIsRunning] = useState(false);
  const [output, setOutput] = useState<string[]>([]);

  const queryClient = useQueryClient();

  // STAR CLI Commands
  const starCommands = [
    {
      id: 'ignite',
      name: 'Ignite STAR',
      description: 'Ignite STAR & Boot The OASIS',
      icon: <Rocket />,
      category: 'System',
      color: 'success',
      command: 'ignite',
      parameters: []
    },
    {
      id: 'extinguish',
      name: 'Extinguish STAR',
      description: 'Extinguish STAR & Shutdown The OASIS',
      icon: <Stop />,
      category: 'System',
      color: 'error',
      command: 'extinguish',
      parameters: []
    },
    {
      id: 'light',
      name: 'Light',
      description: 'Creates a new OAPP (Zomes/Holons/Star/Planet/Moon)',
      icon: <Star />,
      category: 'Development',
      color: 'primary',
      command: 'light',
      parameters: [
        { name: 'genesisFolder', type: 'string', required: true, description: 'Genesis folder location' },
        { name: 'wiz', type: 'boolean', required: false, description: 'Start the Light Wizard' },
        { name: 'transmute', type: 'boolean', required: false, description: 'Create Planet from hApp DNA' }
      ]
    },
    {
      id: 'bang',
      name: 'Bang',
      description: 'Generate a whole metaverse or part of one',
      icon: <Build />,
      category: 'Development',
      color: 'primary',
      command: 'bang',
      parameters: [
        { name: 'type', type: 'string', required: true, description: 'Metaverse type (Multiverse, Universe, Dimension)' },
        { name: 'name', type: 'string', required: true, description: 'Metaverse name' }
      ]
    },
    {
      id: 'seed',
      name: 'Seed',
      description: 'Deploy/Publish an OAPP to the STARNET Store',
      icon: <Upload />,
      category: 'OAPP Management',
      color: 'info',
      command: 'seed',
      parameters: [
        { name: 'oappId', type: 'string', required: true, description: 'OAPP ID to publish' },
        { name: 'version', type: 'string', required: false, description: 'Version to publish' }
      ]
    },
    {
      id: 'unseed',
      name: 'Unseed',
      description: 'Undeploy/Unpublish an OAPP from the STARNET Store',
      icon: <Download />,
      category: 'OAPP Management',
      color: 'warning',
      command: 'unseed',
      parameters: [
        { name: 'oappId', type: 'string', required: true, description: 'OAPP ID to unpublish' }
      ]
    },
    {
      id: 'dust',
      name: 'Dust',
      description: 'Delete an OAPP (removes from STARNET if published)',
      icon: <Delete />,
      category: 'OAPP Management',
      color: 'error',
      command: 'dust',
      parameters: [
        { name: 'oappId', type: 'string', required: true, description: 'OAPP ID to delete' }
      ]
    },
    {
      id: 'emit',
      name: 'Emit',
      description: 'Show how much light the OAPP is emitting (karma score)',
      icon: <TrendingUp />,
      category: 'Analytics',
      color: 'info',
      command: 'emit',
      parameters: [
        { name: 'oappId', type: 'string', required: true, description: 'OAPP ID to analyze' }
      ]
    },
    {
      id: 'reflect',
      name: 'Reflect',
      description: 'Show stats of the OAPP',
      icon: <Info />,
      category: 'Analytics',
      color: 'info',
      command: 'reflect',
      parameters: [
        { name: 'oappId', type: 'string', required: true, description: 'OAPP ID to analyze' }
      ]
    },
    {
      id: 'net',
      name: 'Net',
      description: 'Launch the STARNET Library/Store',
      icon: <CloudDone />,
      category: 'Network',
      color: 'primary',
      command: 'net',
      parameters: []
    },
    {
      id: 'gate',
      name: 'Gate',
      description: 'Opens the STARGATE to the OASIS Portal',
      icon: <Security />,
      category: 'Network',
      color: 'primary',
      command: 'gate',
      parameters: []
    },
    {
      id: 'api',
      name: 'API',
      description: 'Opens the WEB5 STAR API (or WEB4 OASIS API)',
      icon: <Code />,
      category: 'Network',
      color: 'primary',
      command: 'api',
      parameters: [
        { name: 'oasis', type: 'boolean', required: false, description: 'Open WEB4 OASIS API instead of WEB5 STAR API' }
      ]
    }
  ];

  const categories = ['System', 'Development', 'OAPP Management', 'Analytics', 'Network'];

  const handleCommandClick = (command: any) => {
    setSelectedCommand(command);
    setCommandDialogOpen(true);
  };

  const handleRunCommand = (command: any, parameters: any) => {
    setIsRunning(true);
    setOutput([`Running command: ${command.command}`, '...']);
    
    // Simulate command execution
    setTimeout(() => {
      setOutput(prev => [...prev, 'Command executed successfully!', 'Output: Demo mode - command simulated']);
      setIsRunning(false);
      setCommandHistory(prev => [...prev, `${command.command} ${Object.values(parameters).join(' ')}`]);
    }, 2000);
  };

  const handleTerminalCommand = (command: string) => {
    setCurrentCommand(command);
    setOutput([`> ${command}`, 'Command not recognized. Use the command cards above.']);
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" component="h1" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Terminal color="primary" />
          STAR CLI
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<Refresh />}
            onClick={() => setOutput([])}
          >
            Clear Output
          </Button>
          <Button
            variant="contained"
            startIcon={<PlayArrow />}
            onClick={() => setOutput(['STAR CLI initialized', 'Ready for commands...'])}
          >
            Initialize
          </Button>
        </Box>
      </Box>

      {/* Terminal Output */}
      <Card sx={{ mb: 3, bgcolor: 'black', color: 'lime' }}>
        <CardContent>
          <Typography variant="h6" gutterBottom sx={{ color: 'lime' }}>
            STAR CLI Terminal
          </Typography>
          <Box sx={{ 
            bgcolor: 'black', 
            color: 'lime', 
            p: 2, 
            borderRadius: 1, 
            fontFamily: 'monospace',
            minHeight: 200,
            maxHeight: 400,
            overflow: 'auto'
          }}>
            {output.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                STAR CLI ready. Select a command below to get started.
              </Typography>
            ) : (
              output.map((line, index) => (
                <Typography key={index} variant="body2" sx={{ mb: 0.5 }}>
                  {line}
                </Typography>
              ))
            )}
            {isRunning && (
              <Typography variant="body2" sx={{ color: 'yellow' }}>
                <LinearProgress sx={{ mb: 1 }} />
                Executing command...
              </Typography>
            )}
          </Box>
        </CardContent>
      </Card>

      {/* Command Categories */}
      {categories.map((category) => (
        <Box key={category} sx={{ mb: 4 }}>
          <Typography variant="h5" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            {category} Commands
          </Typography>
          <Grid container spacing={2}>
            {starCommands
              .filter(cmd => cmd.category === category)
              .map((command) => (
                <Grid item xs={12} sm={6} md={4} lg={3} key={command.id}>
                  <Card sx={{ height: '100%', cursor: 'pointer' }} onClick={() => handleCommandClick(command)}>
                    <CardContent>
                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                        <Avatar sx={{ bgcolor: `${command.color}.main`, mr: 2 }}>
                          {command.icon}
                        </Avatar>
                        <Box sx={{ flexGrow: 1 }}>
                          <Typography variant="h6" noWrap>
                            {command.name}
                          </Typography>
                          <Chip
                            label={command.command}
                            size="small"
                            color={command.color as any}
                            variant="outlined"
                          />
                        </Box>
                      </Box>
                      <Typography variant="body2" color="text.secondary">
                        {command.description}
                      </Typography>
                    </CardContent>
                    <CardActions>
                      <Button size="small" startIcon={<PlayArrow />}>
                        Run Command
                      </Button>
                    </CardActions>
                  </Card>
                </Grid>
              ))}
          </Grid>
        </Box>
      ))}

      {/* Command Dialog */}
      <Dialog open={commandDialogOpen} onClose={() => setCommandDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>
          {selectedCommand?.name} - {selectedCommand?.command}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            <Typography variant="body1" sx={{ mb: 2 }}>
            {selectedCommand?.description}
          </Typography>
          
          {selectedCommand?.parameters && selectedCommand.parameters.length > 0 && (
            <Box sx={{ mb: 3 }}>
              <Typography variant="h6" gutterBottom>
                Parameters
              </Typography>
              {selectedCommand.parameters.map((param: any, index: number) => (
                <Box key={index} sx={{ mb: 2 }}>
                  <TextField
                    fullWidth
                    label={param.name}
                    type={param.type === 'boolean' ? 'text' : param.type}
                    placeholder={param.description}
                    required={param.required}
                    helperText={param.description}
                  />
                </Box>
              ))}
            </Box>
          )}
          
          <Box sx={{ mb: 2 }}>
            <Typography variant="h6" gutterBottom>
              Command Preview
            </Typography>
            <Box sx={{ 
              bgcolor: 'grey.100', 
              p: 2, 
              borderRadius: 1, 
              fontFamily: 'monospace',
              border: '1px solid',
              borderColor: 'grey.300'
            }}>
              {selectedCommand?.command} [parameters...]
            </Box>
          </Box>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCommandDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => {
              if (selectedCommand) {
                handleRunCommand(selectedCommand, {});
                setCommandDialogOpen(false);
              }
            }}
            disabled={isRunning}
          >
            {isRunning ? 'Running...' : 'Run Command'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Command History */}
      {commandHistory.length > 0 && (
        <Card sx={{ mt: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Command History
            </Typography>
            <List>
              {commandHistory.slice(-10).reverse().map((command, index) => (
                <ListItem key={index}>
                  <ListItemIcon>
                    <Terminal />
                  </ListItemIcon>
                  <ListItemText 
                    primary={command}
                    secondary={`Executed ${new Date().toLocaleString()}`}
                  />
                  <ListItemSecondaryAction>
                    <IconButton size="small" onClick={() => handleTerminalCommand(command)}>
                      <PlayArrow />
                    </IconButton>
                  </ListItemSecondaryAction>
                </ListItem>
              ))}
            </List>
          </CardContent>
        </Card>
      )}
    </Box>
  );
};

export default STARCLIPage;
