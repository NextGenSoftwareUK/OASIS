import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
  Alert,
  Divider,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Paper,
  Avatar,
  Badge,
  LinearProgress,
} from '@mui/material';
import {
  ArrowBack,
  Edit,
  Delete,
  Share,
  Download,
  Visibility,
  Book,
  Star,
  Person,
  CalendarToday,
  Category,
  Code,
  Security,
  Speed,
  Memory,
  Storage,
  AttachMoney,
  TrendingUp,
  Flag,
  Assignment,
  MenuBook,
  School,
  CheckCircle,
} from '@mui/icons-material';
import { motion } from 'framer-motion';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { chapterService } from '../services';
import { Chapter } from '../types/star';
import { toast } from 'react-hot-toast';

const ChapterDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [chapter, setChapter] = useState<Chapter | null>(null);

  const queryClient = useQueryClient();

  // Fetch chapter details
  const { data: chapterData, isLoading, error, refetch } = useQuery(
    ['chapter', id],
    async () => {
      if (!id) throw new Error('Chapter ID is required');
      // For now, we'll get it from the list and filter by ID
      const response = await chapterService.getAll();
      const foundChapter = response.result?.find((c: Chapter) => c.id === id);
      if (!foundChapter) throw new Error('Chapter not found');
      return { result: foundChapter };
    },
    {
      enabled: !!id,
      onSuccess: (data) => {
        if (data?.result) {
          setChapter(data.result);
        }
      },
    }
  );

  // Update chapter mutation
  const updateChapterMutation = useMutation(
    async (data: any) => {
      if (!id) throw new Error('Chapter ID is required');
      return await chapterService.update(id, data);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries(['chapter', id]);
        queryClient.invalidateQueries('allChapters');
        toast.success('Chapter updated successfully!');
        setEditDialogOpen(false);
      },
      onError: (error: any) => {
        toast.error('Failed to update chapter');
        console.error('Update chapter error:', error);
      },
    }
  );

  // Delete chapter mutation
  const deleteChapterMutation = useMutation(
    async () => {
      if (!id) throw new Error('Chapter ID is required');
      return await chapterService.delete(id);
    },
    {
      onSuccess: () => {
        queryClient.invalidateQueries('allChapters');
        toast.success('Chapter deleted successfully!');
        navigate('/chapters');
      },
      onError: (error: any) => {
        toast.error('Failed to delete chapter');
        console.error('Delete chapter error:', error);
      },
    }
  );

  const handleEdit = () => {
    setEditDialogOpen(true);
  };

  const handleDelete = () => {
    setDeleteDialogOpen(true);
  };

  const handleShare = () => {
    setShareDialogOpen(true);
  };

  const handleEditSubmit = () => {
    updateChapterMutation.mutate({});
  };

  const handleDeleteConfirm = () => {
    deleteChapterMutation.mutate();
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'published': return 'success';
      case 'draft': return 'warning';
      case 'archived': return 'error';
      case 'review': return 'info';
      default: return 'default';
    }
  };

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty.toLowerCase()) {
      case 'advanced': return 'error';
      case 'intermediate': return 'warning';
      case 'beginner': return 'success';
      default: return 'default';
    }
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <CircularProgress size={60} />
      </Box>
    );
  }

  if (error) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load chapter details
        </Alert>
        <Button variant="contained" onClick={() => refetch()}>
          Try Again
        </Button>
      </Box>
    );
  }

  if (!chapter) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Chapter not found
        </Alert>
        <Button variant="contained" onClick={() => navigate('/chapters')} sx={{ mt: 2 }}>
          Back to Chapters
        </Button>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/chapters')} sx={{ mr: 2 }}>
          <ArrowBack />
        </IconButton>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h4" component="h1">
            {chapter.title}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Chapter {chapter.chapterNumber} • {chapter.difficulty} Level
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<Share />}
            onClick={handleShare}
          >
            Share
          </Button>
          <Button
            variant="outlined"
            startIcon={<Edit />}
            onClick={handleEdit}
          >
            Edit
          </Button>
          <Button
            variant="outlined"
            color="error"
            startIcon={<Delete />}
            onClick={handleDelete}
          >
            Delete
          </Button>
        </Box>
      </Box>

      <Grid container spacing={3}>
        {/* Chapter Content */}
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Chapter Content
              </Typography>
              <Typography variant="body1" paragraph>
                {chapter.content}
              </Typography>
              
              <Divider sx={{ my: 2 }} />
              
              <Grid container spacing={3}>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Category sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Category
                    </Typography>
                  </Box>
                  <Chip
                    label={chapter.category}
                    color="primary"
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Flag sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Difficulty
                    </Typography>
                  </Box>
                  <Chip
                    label={chapter.difficulty}
                    color={getDifficultyColor(chapter.difficulty || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <Book sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Chapter Number
                    </Typography>
                  </Box>
                  <Typography variant="body1">
                    {chapter.chapterNumber}
                  </Typography>
                </Grid>
                <Grid item xs={12} sm={6}>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <CheckCircle sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2" color="text.secondary">
                      Status
                    </Typography>
                  </Box>
                  <Chip
                    label={chapter.status}
                    color={getStatusColor(chapter.status || 'Unknown') as any}
                    size="small"
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Chapter Stats and Info */}
        <Grid item xs={12} md={4}>
          <Grid container spacing={2}>
            {/* Reading Progress Card */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <TrendingUp sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Reading Progress
                  </Typography>
                  <Box sx={{ mb: 2 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                      <Typography variant="body2">
                        Progress
                      </Typography>
                      <Typography variant="body2">
                        {chapter.readingProgress || 0}%
                      </Typography>
                    </Box>
                    <LinearProgress
                      variant="determinate"
                      value={chapter.readingProgress || 0}
                      sx={{ height: 8, borderRadius: 4 }}
                    />
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    {chapter.wordsRead || 0} of {chapter.totalWords || 0} words read
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            {/* Chapter Info */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Book sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Chapter Info
                  </Typography>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Person />
                      </ListItemIcon>
                      <ListItemText
                        primary="Author"
                        secondary={chapter.author || 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <CalendarToday />
                      </ListItemIcon>
                      <ListItemText
                        primary="Published"
                        secondary={chapter.publishedDate ? formatDate(chapter.publishedDate.toISOString()) : 'Not published'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <MenuBook />
                      </ListItemIcon>
                      <ListItemText
                        primary="Reading Time"
                        secondary={chapter.estimatedReadTime || 'Unknown'}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <School />
                      </ListItemIcon>
                      <ListItemText
                        primary="Level"
                        secondary={chapter.level || 'Unknown'}
                      />
                    </ListItem>
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Tags */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    <Category sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Tags
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {chapter.tags?.map((tag, index) => (
                      <Chip
                        key={index}
                        label={tag}
                        size="small"
                        color="secondary"
                      />
                    )) || (
                      <Typography variant="body2" color="text.secondary">
                        No tags
                      </Typography>
                    )}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Grid>

        {/* Chapter Sections */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                <Assignment sx={{ mr: 1, verticalAlign: 'middle' }} />
                Chapter Sections
              </Typography>
              <List>
                {chapter.sections?.map((section, index) => (
                  <ListItem key={index} divider>
                    <ListItemIcon>
                      <Book />
                    </ListItemIcon>
                    <ListItemText
                      primary={section}
                      secondary={`Section ${index + 1}`}
                    />
                    <Chip
                      label="Section"
                      size="small"
                      color="primary"
                    />
                  </ListItem>
                )) || (
                  <ListItem>
                    <ListItemText
                      primary="No sections defined"
                      secondary="This chapter doesn't have any sections yet."
                    />
                  </ListItem>
                )}
              </List>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Edit Dialog */}
      <Dialog open={editDialogOpen} onClose={() => setEditDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Chapter</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Title"
                value={chapter.title}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Content"
                value={chapter.content}
                multiline
                rows={6}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Category"
                value={chapter.category}
                disabled
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Difficulty"
                value={chapter.difficulty}
                disabled
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleEditSubmit}
            variant="contained"
            disabled={updateChapterMutation.isLoading}
          >
            {updateChapterMutation.isLoading ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Chapter</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete "{chapter.title}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleDeleteConfirm}
            color="error"
            variant="contained"
            disabled={deleteChapterMutation.isLoading}
          >
            {deleteChapterMutation.isLoading ? <CircularProgress size={20} /> : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)}>
        <DialogTitle>Share Chapter</DialogTitle>
        <DialogContent>
          <Typography>
            Share this chapter with others:
          </Typography>
          <TextField
            fullWidth
            value={`${window.location.origin}/chapters/${chapter.id}`}
            sx={{ mt: 2 }}
            InputProps={{
              readOnly: true,
            }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShareDialogOpen(false)}>Close</Button>
          <Button
            onClick={() => {
              navigator.clipboard.writeText(`${window.location.origin}/chapters/${chapter.id}`);
              toast.success('Link copied to clipboard!');
              setShareDialogOpen(false);
            }}
            variant="contained"
          >
            Copy Link
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default ChapterDetailPage;
