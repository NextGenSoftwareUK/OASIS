import React from 'react';
import { Box, Typography, Card, CardContent } from '@mui/material';
import { SpaceDashboard } from '@mui/icons-material';
import { motion } from 'framer-motion';

const CelestialBodiesPage: React.FC = () => {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
    >
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          Celestial Bodies
        </Typography>
        <Typography variant="subtitle1" color="text.secondary">
          Planets, stars, moons and celestial objects
        </Typography>
      </Box>

      <Card>
        <CardContent sx={{ textAlign: 'center', py: 8 }}>
          <SpaceDashboard sx={{ fontSize: 80, color: 'text.secondary', mb: 2 }} />
          <Typography variant="h6" color="text.secondary">
            Celestial Body Management
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Celestial body creation and management features coming soon
          </Typography>
        </CardContent>
      </Card>
    </motion.div>
  );
};

export default CelestialBodiesPage;
