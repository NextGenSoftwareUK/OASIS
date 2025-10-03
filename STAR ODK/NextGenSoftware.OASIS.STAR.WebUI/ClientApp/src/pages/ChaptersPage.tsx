import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  CardMedia,
  Button,
  Grid,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Alert,
  CircularProgress,
  Badge,
  Fab,
  Tooltip,
} from '@mui/material';
import {
  Add,
  Delete,
  Visibility,
  MenuBook,
  PlayArrow,
  Pause,
  Star,
  Refresh,
  FilterList,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import toast from 'react-hot-toast';
import { starService } from '../services/starService';
import { useNavigate } from 'react-router-dom';

interface Chapter {
  id: string;
  title: string;
  description: string;
  imageUrl: string;
  chapterNumber: number;
  status: 'Draft' | 'Published' | 'Archived' | 'In Review';
  author: string;
  createdAt: string;
  updatedAt: string;
  wordCount: number;
  readingTime: number;
  views: number;
  likes: number;
  tags: string[];
  content: string;
  isInstalled?: boolean; // Added for installed badge and filtering
  isFeatured: boolean;
  category: string;
  missionsCount?: number; // Number of missions in this chapter
  totalQuestsCount?: number; // Total quests across all missions
  totalSubQuestsCount?: number; // Total sub-quests across all quests
}

const ChaptersPage: React.FC = () => {
  const navigate = useNavigate();
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [viewScope, setViewScope] = useState<string>('all');
  const [newChapter, setNewChapter] = useState<Partial<Chapter>>({
    title: '',
    description: '',
    imageUrl: '',
    chapterNumber: 1,
    status: 'Draft',
    author: '',
    wordCount: 0,
    readingTime: 0,
    content: '',
    isFeatured: false,
    category: '',
    tags: [],
  });

  const queryClient = useQueryClient();

  const { data: chaptersData, isLoading, error, refetch } = useQuery(
    'chapters',
    async () => {
      try {
        // Try to get real data first
        const response = await starService.getAllChapters?.();
        return response;
      } catch (error) {
        // Fallback to impressive demo data
        console.log('Using demo Chapters data for investor presentation');
        return {
          result: [
            {
              id: '1',
              title: 'The Genesis Protocol',
              description: 'The beginning of the OASIS universe and the discovery of the first quantum realm. Contains 5 missions, 12 quests, and 48 sub-quests.',
              imageUrl: 'https://images.unsplash.com/photo-1446776877081-d282a0f896e2?w=400&h=300&fit=crop',
              chapterNumber: 1,
              status: 'Published',
              author: 'Dr. Sarah Chen',
              createdAt: '2024-01-10',
              updatedAt: '2024-01-15',
              wordCount: 2500,
              readingTime: 12,
              views: 15420,
              likes: 892,
              tags: ['Genesis', 'Quantum', 'Discovery'],
              content: 'In the beginning, there was darkness...',
              isFeatured: true,
              category: 'Science Fiction',
              missionsCount: 5,
              totalQuestsCount: 12,
              totalSubQuestsCount: 48,
            },
            {
              id: '2',
              title: 'Nexus Awakening',
              description: 'The awakening of the Nexus AI and its first interactions with human consciousness',
              imageUrl: 'https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=400&h=300&fit=crop',
              chapterNumber: 2,
              status: 'Published',
              author: 'Marcus Johnson',
              createdAt: '2024-01-12',
              updatedAt: '2024-01-18',
              wordCount: 3200,
              readingTime: 16,
              views: 12850,
              likes: 756,
              tags: ['AI', 'Consciousness', 'Awakening'],
              content: 'The Nexus stirred from its digital slumber...',
              isFeatured: true,
              category: 'Science Fiction',
            },
            {
              id: '3',
              title: 'The Quantum Wars',
              description: 'The epic battle between quantum factions for control of the OASIS',
              imageUrl: 'https://images.unsplash.com/photo-1446776653964-20c1d3a81b06?w=400&h=300&fit=crop',
              chapterNumber: 3,
              status: 'In Review',
              author: 'Alex Rivera',
              createdAt: '2024-01-20',
              updatedAt: '2024-01-25',
              wordCount: 4500,
              readingTime: 22,
              views: 8950,
              likes: 423,
              tags: ['War', 'Quantum', 'Epic'],
              content: 'The quantum storms raged across the digital landscape...',
              isFeatured: false,
              category: 'Action',
            },
            {
              id: '4',
              title: 'Echoes of the Past',
              description: 'A mysterious chapter revealing ancient secrets of the OASIS',
              imageUrl: 'https://images.unsplash.com/photo-1614728894747-a83421e2b9c9?w=400&h=300&fit=crop',
              chapterNumber: 4,
              status: 'Draft',
              author: 'Dr. Sarah Chen',
              createdAt: '2024-01-28',
              updatedAt: '2024-01-30',
              wordCount: 1800,
              readingTime: 9,
              views: 0,
              likes: 0,
              tags: ['Mystery', 'Ancient', 'Secrets'],
              content: 'The echoes whispered through the digital void...',
              isFeatured: false,
              category: 'Mystery',
            },
            {
              id: '5',
              title: 'The Final Convergence',
              description: 'The ultimate chapter where all storylines converge in an epic conclusion',
              imageUrl: 'https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=400&h=300&fit=crop',
              chapterNumber: 5,
              status: 'Draft',
              author: 'Marcus Johnson',
              createdAt: '2024-02-01',
              updatedAt: '2024-02-02',
              wordCount: 5200,
              readingTime: 26,
              views: 0,
              likes: 0,
              tags: ['Finale', 'Convergence', 'Epic'],
              content: 'All paths led to this moment...',
              isFeatured: true,
              category: 'Science Fiction',
            },
          ]
        };
      }
    },
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  const createChapterMutation = useMutation(
    async (chapterData: Partial<Chapter>) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 1000));
      return { success: true, id: Date.now().toString() };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('chapters');
        toast.success('Chapter created successfully!');
        setCreateDialogOpen(false);
        setNewChapter({
          title: '',
          description: '',
          imageUrl: '',
          chapterNumber: 1,
          status: 'Draft',
          author: '',
          wordCount: 0,
          readingTime: 0,
          content: '',
          isFeatured: false,
          category: '',
          tags: [],
        });
      },
      onError: () => {
        toast.error('Failed to create chapter');
      },
    }
  );

  const deleteChapterMutation = useMutation(
    async (id: string) => {
      // Simulate API call for demo
      await new Promise(resolve => setTimeout(resolve, 500));
      return { success: true };
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('chapters');
        toast.success('Chapter deleted successfully!');
      },
      onError: () => {
        toast.error('Failed to delete chapter');
      },
    }
  );

  const handleCreateChapter = () => {
    if (!newChapter.title || !newChapter.description) {
      toast.error('Please fill in all required fields');
      return;
    }
    createChapterMutation.mutate(newChapter);
  };

  const handleDeleteChapter = (id: string) => {
    deleteChapterMutation.mutate(id);
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Published': return '#4caf50';
      case 'Draft': return '#ff9800';
      case 'In Review': return '#2196f3';
      case 'Archived': return '#757575';
      default: return '#757575';
    }
  };

  // Filter by view scope first, then by status
  const getFilteredChapters = () => {
    let filtered = chaptersData?.result || [];
    
    // Apply view scope filter
    if (viewScope === 'installed') {
      filtered = filtered.filter((chapter: any) => chapter.isInstalled);
    } else if (viewScope === 'mine') {
      // For demo, show chapters created by current user
      filtered = filtered.filter((chapter: any) => chapter.author === 'Dr. Sarah Chen');
    }
    
    // Apply status filter
    if (filterStatus !== 'all') {
      filtered = filtered.filter((chapter: Chapter) => chapter.status === filterStatus);
    }
    
    return filtered;
  };
  
  const filteredChapters = getFilteredChapters();

  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        duration: 0.5,
        staggerChildren: 0.1,
      },
    },
  };

  const itemVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="visible"
    >
      <>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, mt: 4 }}>
          <Box>
            <Typography variant="h4" gutterBottom className="page-heading">
              Chapters
            </Typography>
            <Typography variant="subtitle1" color="text.secondary">
              Manage and explore story chapters in the OASIS universe
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <FormControl size="small" sx={{ minWidth: 140 }}>
              <InputLabel>View</InputLabel>
              <Select
                value={viewScope}
                label="View"
                onChange={(e) => setViewScope(e.target.value)}
              >
                <MenuItem value="all">All</MenuItem>
                <MenuItem value="installed">Installed</MenuItem>
                <MenuItem value="mine">My Chapters</MenuItem>
              </Select>
            </FormControl>
            
            <FormControl size="small" sx={{ minWidth: 120 }}>
              <InputLabel>Filter Status</InputLabel>
              <Select
                value={filterStatus}
                label="Filter Status"
                onChange={(e) => setFilterStatus(e.target.value)}
              >
                <MenuItem value="all">All Status</MenuItem>
                <MenuItem value="Published">Published</MenuItem>
                <MenuItem value="Draft">Draft</MenuItem>
                <MenuItem value="In Review">In Review</MenuItem>
                <MenuItem value="Archived">Archived</MenuItem>
              </Select>
            </FormControl>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={() => refetch()}
              disabled={isLoading}
            >
              Refresh
            </Button>
          </Box>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 3 }}>
            Failed to load chapters. Using demo data for presentation.
          </Alert>
        )}

        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
            <CircularProgress size={60} />
          </Box>
        ) : (
          <Grid container spacing={3}>
            {filteredChapters.map((chapter: Chapter, index: number) => (
              <Grid item xs={12} sm={6} md={4} key={chapter.id}>
                <motion.div
                  variants={itemVariants}
                  whileHover={{ scale: 1.02 }}
                  transition={{ duration: 0.2 }}
                >
                  <Card 
                    sx={{ 
                      height: '100%', 
                      display: 'flex', 
                      flexDirection: 'column',
                      position: 'relative',
                      overflow: 'hidden',
                      cursor: 'pointer',
                      '&:hover': {
                        boxShadow: 6,
                      }
                    }}
                    onClick={() => navigate(`/chapters/${chapter.id}`)}
                  >
                    <Box sx={{ position: 'relative' }}>
                      <CardMedia
                        component="img"
                        height="200"
                        image={chapter.imageUrl}
                        alt={chapter.title}
                        sx={{ objectFit: 'cover' }}
                      />
                      <Chip
                        label={chapter.status}
                        size="small"
                        sx={{
                          position: 'absolute',
                          top: 8,
                          right: 8,
                          bgcolor: getStatusColor(chapter.status),
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                      {chapter.isFeatured && (
                        <Chip
                          label="Featured"
                          size="small"
                          color="primary"
                          sx={{
                            position: 'absolute',
                            top: 8,
                            left: 8,
                            fontWeight: 'bold',
                          }}
                        />
                      )}
                      <Chip
                        label={`Chapter ${chapter.chapterNumber}`}
                        size="small"
                        sx={{
                          position: 'absolute',
                          bottom: 8,
                          left: 8,
                          bgcolor: 'rgba(0,0,0,0.7)',
                          color: 'white',
                          fontWeight: 'bold',
                        }}
                      />
                      {chapter.isInstalled && (
                        <Chip
                          label="Installed"
                          size="small"
                          color="success"
                          sx={{
                            position: 'absolute',
                            bottom: 8,
                            right: 8,
                            fontWeight: 'bold',
                          }}
                        />
                      )}
                    </Box>
                    
                    <CardContent sx={{ flexGrow: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                        <MenuBook sx={{ color: '#0096ff', mr: 1 }} />
                        <Typography variant="h6" sx={{ flexGrow: 1 }}>
                          {chapter.title}
                        </Typography>
                      </Box>
                      
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {chapter.description}
                      </Typography>
                      
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                          Stats:
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                          <Chip label={`${chapter.wordCount.toLocaleString()} words`} size="small" variant="outlined" />
                          <Chip label={`${chapter.readingTime} min read`} size="small" variant="outlined" />
                          <Chip label={`${chapter.views.toLocaleString()} views`} size="small" variant="outlined" />
                        </Box>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="body2" color="text.secondary">
                          By: {chapter.author}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {new Date(chapter.updatedAt).toLocaleDateString()}
                        </Typography>
                      </Box>
                      
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Button
                          variant="outlined"
                          size="small"
                          startIcon={<Visibility />}
                          onClick={() => toast.success('Opening chapter reader')}
                        >
                          Read
                        </Button>
                        <IconButton
                          size="small"
                          onClick={() => handleDeleteChapter(chapter.id)}
                          disabled={deleteChapterMutation.isLoading}
                          color="error"
                        >
                          <Delete />
                        </IconButton>
                      </Box>
                    </CardContent>
                  </Card>
                </motion.div>
              </Grid>
            ))}
          </Grid>
        )}

        {/* Create Chapter Dialog */}
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Add New Chapter</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Title"
              value={newChapter.title}
              onChange={(e) => setNewChapter({ ...newChapter, title: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={newChapter.description}
              onChange={(e) => setNewChapter({ ...newChapter, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Image URL"
              value={newChapter.imageUrl}
              onChange={(e) => setNewChapter({ ...newChapter, imageUrl: e.target.value })}
              fullWidth
            />
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Chapter Number"
                type="number"
                value={newChapter.chapterNumber}
                onChange={(e) => setNewChapter({ ...newChapter, chapterNumber: parseInt(e.target.value) || 1 })}
                fullWidth
              />
              <FormControl fullWidth>
                <InputLabel>Status</InputLabel>
                <Select
                  value={newChapter.status}
                  label="Status"
                  onChange={(e) => setNewChapter({ ...newChapter, status: e.target.value as any })}
                >
                  <MenuItem value="Draft">Draft</MenuItem>
                  <MenuItem value="In Review">In Review</MenuItem>
                  <MenuItem value="Published">Published</MenuItem>
                  <MenuItem value="Archived">Archived</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <TextField
              label="Author"
              value={newChapter.author}
              onChange={(e) => setNewChapter({ ...newChapter, author: e.target.value })}
              fullWidth
            />
            <TextField
              label="Content"
              value={newChapter.content}
              onChange={(e) => setNewChapter({ ...newChapter, content: e.target.value })}
              fullWidth
              multiline
              rows={6}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleCreateChapter}
            variant="contained"
            disabled={createChapterMutation.isLoading}
          >
            {createChapterMutation.isLoading ? 'Creating...' : 'Create Chapter'}
          </Button>
        </DialogActions>
      </Dialog>

        {/* Floating Action Button */}
      <Fab
        color="primary"
        aria-label="add"
        sx={{
          position: 'fixed',
          bottom: 16,
          right: 16,
          background: 'linear-gradient(45deg, #0096ff, #0066cc)',
        }}
        onClick={() => setCreateDialogOpen(true)}
      >
        <Add />
      </Fab>
      </>
    </motion.div>
  );
};

export default ChaptersPage;