import React from 'react';
import { Box, Typography, Card, CardContent } from '@mui/material';
import { Public } from '@mui/icons-material';
import { motion } from 'framer-motion';

const CelestialSpacesPage: React.FC = () => {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
    >
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          Celestial Spaces
        </Typography>
        <Typography variant="subtitle1" color="text.secondary">
          Galaxies, universes and cosmic environments
        </Typography>
      </Box>

      <Card>
        <CardContent sx={{ textAlign: 'center', py: 8 }}>
          <Public sx={{ fontSize: 80, color: 'text.secondary', mb: 2 }} />
          <Typography variant="h6" color="text.secondary">
            Celestial Space Management
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Celestial space creation and management features coming soon
          </Typography>
        </CardContent>
      </Card>
    </motion.div>
  );
};

export default CelestialSpacesPage;
