import { AppBar, Box, Button, Container, Toolbar, Typography } from '@mui/material';
import { Link as RouterLink, Route, Routes } from 'react-router-dom';
import Home from './pages/Home';
import Plans from './pages/Plans';
import Providers from './pages/Providers';
import APIs from './pages/APIs';

export default function App() {
  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <AppBar position="static" color="transparent" elevation={0}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1 }}>OASIS Web4</Typography>
          <Button component={RouterLink} to="/" color="primary">Home</Button>
          <Button component={RouterLink} to="/plans" color="primary">Plans</Button>
          <Button component={RouterLink} to="/providers" color="primary">Providers</Button>
          <Button component={RouterLink} to="/apis" color="primary">APIs</Button>
        </Toolbar>
      </AppBar>
      <Box component="main" sx={{ flexGrow: 1, py: 6 }}>
        <Container>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/plans" element={<Plans />} />
            <Route path="/providers" element={<Providers />} />
            <Route path="/apis" element={<APIs />} />
          </Routes>
        </Container>
      </Box>
      <Box component="footer" sx={{ py: 4, bgcolor: 'grey.100', textAlign: 'center' }}>
        <Typography variant="h6" gutterBottom>OASIS Web4</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Universal Web4/Web5 Infrastructure for Apps, Games, and Platforms
        </Typography>
        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 3, mb: 2 }}>
          <Button component={RouterLink} to="/" size="small">Home</Button>
          <Button component={RouterLink} to="/plans" size="small">Plans</Button>
          <Button component={RouterLink} to="/providers" size="small">Providers</Button>
          <Button component={RouterLink} to="/apis" size="small">APIs</Button>
        </Box>
        <Typography variant="body2" color="text.secondary">
          © {new Date().getFullYear()} OASIS. All rights reserved.
        </Typography>
      </Box>
    </Box>
  );
}


