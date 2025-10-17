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
import ProtectedRoute from './components/ProtectedRoute';

// Contexts
import { DemoModeProvider } from './contexts/DemoModeContext';
import { AvatarProvider } from './contexts/AvatarContext';

// Pages
import HomePage from './pages/HomePage';
import Dashboard from './pages/Dashboard';
import OAPPsPage from './pages/OAPPsPage';
import OAPPDetailPage from './pages/OAPPDetailPage';
import OAPPBuilderPage from './pages/OAPPBuilderPage';
import MetaDataPage from './pages/MetaDataPage';
import QuestsPage from './pages/QuestsPage';
import NFTsPage from './pages/NFTsPage';
import NFTDetailPage from './pages/NFTDetailPage';
import NFTMintingPage from './pages/NFTMintingPage';
import GeoNFTsPage from './pages/GeoNFTsPage';
import GeoNFTDetailPage from './pages/GeoNFTDetailPage';
import InventoryDetailPage from './pages/InventoryDetailPage';
import MissionDetailPage from './pages/MissionDetailPage';
import QuestDetailPage from './pages/QuestDetailPage';
import ChapterDetailPage from './pages/ChapterDetailPage';
import RuntimeDetailPage from './pages/RuntimeDetailPage';
import LibraryDetailPage from './pages/LibraryDetailPage';
import TemplateDetailPage from './pages/TemplateDetailPage';
import CelestialBodyDetailPage from './pages/CelestialBodyDetailPage';
import CelestialSpaceDetailPage from './pages/CelestialSpaceDetailPage';
import MissionsPage from './pages/MissionsPage';
import AvatarSigninPage from './pages/AvatarSigninPage';
import AvatarSignupPage from './pages/AvatarSignupPage';
import ChaptersPage from './pages/ChaptersPage';
import AvatarsPage from './pages/AvatarsPage';
import AvatarDetailPage from './pages/AvatarDetailPage';
import CelestialBodiesPage from './pages/CelestialBodiesPage';
import CelestialSpacesPage from './pages/CelestialSpacesPage';
import RuntimesPage from './pages/RuntimesPage';
import LibrariesPage from './pages/LibrariesPage';
import TemplatesPage from './pages/TemplatesPage';
import InventoryPage from './pages/InventoryPage';
import PluginsPage from './pages/PluginsPage';
import PluginDetailPage from './pages/PluginDetailPage';
import GeoHotSpotsPage from './pages/GeoHotSpotsPage';
import STARNETStorePage from './pages/STARNETStorePage';
import STARNETDetailPage from './pages/STARNETDetailPage';
import SettingsPage from './pages/SettingsPage';
import HyperDrivePage from './pages/HyperDrivePage';
import KarmaDetailPage from './pages/KarmaDetailPage';
import MyDataDetailPage from './pages/MyDataDetailPage';
import KarmaPage from './pages/KarmaPage';
import MyDataPage from './pages/MyDataPage';
import DevPortalPage from './pages/DevPortalPage';
import HolonsPage from './pages/HolonsPage';
import ZomesPage from './pages/ZomesPage';
import STARPluginsPage from './pages/STARPluginsPage';
import WalletsPage from './pages/WalletsPage';
import STARCLIPage from './pages/STARCLIPage';
import SubscriptionPlansPage from './pages/SubscriptionPlansPage';
import SubscriptionSuccessPage from './pages/SubscriptionSuccessPage';
import SubscriptionManagePage from './pages/SubscriptionManagePage';
import UsagePage from './pages/UsagePage';
import EggsPage from './pages/EggsPage';
import VideoCallsPage from './pages/VideoCallsPage';
import CompetitionPage from './pages/CompetitionPage';
import MessagingPage from './pages/MessagingPage';
import ChatPage from './pages/ChatPage';
import SocialPage from './pages/SocialPage';
import ContactPage from './pages/ContactPage';

// Services
import { starCoreService } from './services';
import { useSTARConnection } from './hooks/useSTARConnection';

// Types
import { STARStatus } from './types/star';

const App: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const { isConnected, connectionStatus, igniteSTAR, extinguishStar, reconnect } = useSTARConnection();

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
    <DemoModeProvider>
      <AvatarProvider>
        <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>
        <CssBaseline />
      
      {/* Navigation */}
      <Navbar 
        onMenuClick={handleSidebarToggle}
        isConnected={isConnected}
        connectionStatus={connectionStatus}
        igniteSTAR={igniteSTAR}
        extinguishStar={extinguishStar}
        reconnect={reconnect}
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
              <Route path="/" element={<Navigate to="/home" replace />} />
              <Route 
                path="/eggs" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <EggsPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/video" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <VideoCallsPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/competition" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <CompetitionPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/messaging" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <MessagingPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/chat" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <ChatPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/social" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <SocialPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/home" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <HomePage />
                  </motion.div>
                } 
              />
              <Route 
                path="/dashboard" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <Dashboard isConnected={isConnected} />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/oapps" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <OAPPsPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/oapps/:id" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <OAPPDetailPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/quests" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <QuestsPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/quests/:id" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <QuestDetailPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/nfts" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <NFTsPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/nfts/:id" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <NFTDetailPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/nfts/mint" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <NFTMintingPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/geonfts" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <GeoNFTsPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/geonfts/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <GeoNFTDetailPage />
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
                path="/missions/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <MissionDetailPage />
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
                path="/chapters/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <ChapterDetailPage />
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
                path="/avatars/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <AvatarDetailPage />
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
                path="/celestial-bodies/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <CelestialBodyDetailPage />
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
                path="/celestial-spaces/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <CelestialSpaceDetailPage />
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
                path="/runtimes/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <RuntimeDetailPage />
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
                path="/libraries/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <LibraryDetailPage />
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
                path="/templates/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <TemplateDetailPage />
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
                path="/inventory/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <InventoryDetailPage />
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
                path="/wallets" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <WalletsPage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/star-cli" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <STARCLIPage />
                    </motion.div>
                  </ProtectedRoute>
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
                path="/hyperdrive" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <HyperDrivePage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/avatar/signin" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <AvatarSigninPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/avatar/signup" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <AvatarSignupPage />
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
              <Route 
                path="/karma/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <KarmaDetailPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/my-data/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <MyDataDetailPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/plugins/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <PluginDetailPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/starnet-store/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <STARNETDetailPage />
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
                path="/oapps/:id" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <OAPPDetailPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/oapp-builder" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <OAPPBuilderPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/metadata" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <MetaDataPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/dev-portal" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <DevPortalPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/holons" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <HolonsPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/zomes" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <ZomesPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/star-plugins" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <STARPluginsPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/subscription/plans" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <SubscriptionPlansPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/subscription/success" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <SubscriptionSuccessPage />
                  </motion.div>
                } 
              />
              <Route 
                path="/subscription/manage" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <SubscriptionManagePage />
                    </motion.div>
                  </ProtectedRoute>
                } 
              />
              <Route 
                path="/subscription/usage" 
                element={
                  <ProtectedRoute>
                    <motion.div
                      initial="initial"
                      animate="in"
                      exit="out"
                      variants={pageVariants}
                      transition={pageTransition}
                    >
                      <UsagePage />
                    </motion.div>
                  </ProtectedRoute>
                }
              />
              <Route 
                path="/contact" 
                element={
                  <motion.div
                    initial="initial"
                    animate="in"
                    exit="out"
                    variants={pageVariants}
                    transition={pageTransition}
                  >
                    <ContactPage />
                  </motion.div>
                }
              />
            </Routes>
          </AnimatePresence>
        </Container>
      </Box>
    </Box>
      </AvatarProvider>
    </DemoModeProvider>
  );
};

export default App;
