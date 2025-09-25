import React, { useState, useEffect } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { Box, Container, CssBaseline } from '@mui/material';
import { motion, AnimatePresence } from 'framer-motion';
import { useQuery } from 'react-query';
import { toast } from 'react-hot-toast';

// Components
import Navbar from './components/Navbar';
import Sidebar from './components/Sidebar';
import LoadingSpinner from './components/LoadingSpinner';

// Pages
import Dashboard from './pages/Dashboard';
import OAPPsPage from './pages/OAPPsPage';
import QuestsPage from './pages/QuestsPage';
import NFTsPage from './pages/NFTsPage';
import GeoNFTsPage from './pages/GeoNFTsPage';
import MissionsPage from './pages/MissionsPage';
import ChaptersPage from './pages/ChaptersPage';
import AvatarsPage from './pages/AvatarsPage';
import CelestialBodiesPage from './pages/CelestialBodiesPage';
import CelestialSpacesPage from './pages/CelestialSpacesPage';
import RuntimesPage from './pages/RuntimesPage';
import LibrariesPage from './pages/LibrariesPage';
import TemplatesPage from './pages/TemplatesPage';
import InventoryPage from './pages/InventoryPage';
import PluginsPage from './pages/PluginsPage';
import GeoHotSpotsPage from './pages/GeoHotSpotsPage';
import STARNETStorePage from './pages/STARNETStorePage';
import SettingsPage from './pages/SettingsPage';
import KarmaPage from './pages/KarmaPage';
import MyDataPage from './pages/MyDataPage';

// Services
import { starService } from './services/starService';
import { useSTARConnection } from './hooks/useSTARConnection';

// Types
import { STARStatus } from './types/star';

const App: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const { isConnected, connectionStatus } = useSTARConnection();

  // STAR status is now managed by useSTARConnection hook

  const handleSidebarToggle = () => {
    setSidebarOpen(!sidebarOpen);
  };

  const pageVariants = {
    initial: { opacity: 0, x: -20 },
    in: { opacity: 1, x: 0 },
    out: { opacity: 0, x: 20 }
  };

  const pageTransition = {
    type: 'tween',
    ease: 'anticipate',
    duration: 0.3
  };

  // Remove loading check since useSTARConnection handles this

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>
      <CssBaseline />
      
      {/* Navigation */}
      <Navbar 
        onMenuClick={handleSidebarToggle}
        isConnected={isConnected}
        connectionStatus={connectionStatus}
      />
      
      {/* Sidebar */}
      <Sidebar 
        open={sidebarOpen} 
        onClose={() => setSidebarOpen(false)}
        isConnected={isConnected}
      />

      {/* Main Content */}
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          width: { sm: `calc(100% - ${sidebarOpen ? 240 : 0}px)` },
          ml: { sm: sidebarOpen ? '240px' : 0 },
          transition: 'margin 0.3s ease',
          pt: 8, // Account for navbar height
        }}
      >
        <Container maxWidth="xl">
          <AnimatePresence mode="wait">
            <Routes>
              <Route path="/" element={<Navigate to="/dashboard" replace />} />
              <Route 
                path="/dashboard" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <Dashboard isConnected={isConnected} />
                  </motion.div>
                } 
              />
              <Route 
                path="/oapps" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <OAPPsPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/quests" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <QuestsPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/nfts" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <NFTsPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/geonfts" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <GeoNFTsPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/missions" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <MissionsPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/chapters" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <ChaptersPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/avatars" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <AvatarsPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/celestial-bodies" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <CelestialBodiesPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/celestial-spaces" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <CelestialSpacesPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/runtimes" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <RuntimesPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/libraries" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <LibrariesPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/templates" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <TemplatesPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/inventory" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <InventoryPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/plugins" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <PluginsPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/geo-hotspots" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <GeoHotSpotsPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/starnet-store" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <STARNETStorePage />
                  </motion.div>
                } 
              />
              <Route 
                path="/settings" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <SettingsPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/karma" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <KarmaPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/my-data" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <MyDataPage />
                  </motion.div>
                } 
              />
            </Routes>
          </AnimatePresence>
        </Container>
      </Box>
    </Box>
  );
};

export default App;
