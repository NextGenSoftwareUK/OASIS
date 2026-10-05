import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Card,
  CardContent,
  IconButton,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Button,
  TextField,
  Divider,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  ExpandMore as ExpandMoreIcon,
  Help as HelpIcon,
  ContactSupport as ContactSupportIcon,
  Email as EmailIcon,
  Phone as PhoneIcon,
  Chat as ChatIcon,
  Article as ArticleIcon,
  VideoLibrary as VideoLibraryIcon,
  Security as SecurityIcon,
  Payment as PaymentIcon,
  DirectionsCar as DirectionsCarIcon,
} from '@mui/icons-material';
import { TimoColors } from '../../utils/theme';

const HelpScreen = () => {
  const navigate = useNavigate();
  const [expanded, setExpanded] = useState(false);
  const [contactMessage, setContactMessage] = useState('');

  const faqs = [
    {
      id: 1,
      question: 'How do I accept a ride request?',
      answer: 'When you receive a ride request, you\'ll see a notification. Tap on it to view the details, then tap "Accept" to confirm. You can also see pending requests on your home screen.',
      icon: <DirectionsCarIcon />,
    },
    {
      id: 2,
      question: 'How do I get paid?',
      answer: 'Earnings are automatically added to your account after each completed ride. You can withdraw your earnings from the Earnings screen. Payments are processed weekly.',
      icon: <PaymentIcon />,
    },
    {
      id: 3,
      question: 'What if I need to cancel a ride?',
      answer: 'You can cancel a ride before starting it. However, frequent cancellations may affect your driver rating. If you need to cancel, tap the "Cancel" button on the active ride screen.',
      icon: <HelpIcon />,
    },
    {
      id: 4,
      question: 'How does the rating system work?',
      answer: 'Riders rate you after each completed ride. Your overall rating is the average of all your ratings. Maintaining a high rating helps you get more ride requests.',
      icon: <SecurityIcon />,
    },
    {
      id: 5,
      question: 'What should I do if I have a problem with a rider?',
      answer: 'If you encounter any issues, you can contact support through this Help screen. For emergencies, use the emergency button in the app or call our 24/7 support line.',
      icon: <ContactSupportIcon />,
    },
  ];

  const handleChange = (panel) => (event, isExpanded) => {
    setExpanded(isExpanded ? panel : false);
  };

  const quickLinks = [
    { icon: <ArticleIcon />, text: 'Driver Guide', action: () => {} },
    { icon: <VideoLibraryIcon />, text: 'Video Tutorials', action: () => {} },
    { icon: <SecurityIcon />, text: 'Safety Tips', action: () => {} },
  ];

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: TimoColors.backgroundLight }}>
      {/* Header */}
      <Box
        sx={{
          backgroundColor: TimoColors.primary,
          color: 'white',
          p: 2,
          display: 'flex',
          alignItems: 'center',
          gap: 2,
        }}
      >
        <IconButton onClick={() => navigate('/home')} sx={{ color: 'white' }}>
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h6" sx={{ flex: 1, fontWeight: 600 }}>
          Help & Support
        </Typography>
      </Box>

      <Box sx={{ p: 2, pb: 10 }}>
        {/* Quick Links */}
        <Box sx={{ mb: 3 }}>
          <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
            Quick Links
          </Typography>
          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
            {quickLinks.map((link, index) => (
              <Card
                key={index}
                sx={{
                  flex: 1,
                  minWidth: 100,
                  cursor: 'pointer',
                  background: 'rgba(255, 255, 255, 0.95)',
                  backdropFilter: 'blur(10px)',
                  border: '1px solid rgba(40, 71, 188, 0.1)',
                  transition: 'all 0.3s ease',
                  '&:hover': {
                    boxShadow: '0 8px 30px rgba(40, 71, 188, 0.15)',
                    transform: 'translateY(-2px)',
                  },
                }}
                onClick={link.action}
              >
                <CardContent sx={{ textAlign: 'center', p: 2 }}>
                  <Box
                    sx={{
                      color: TimoColors.primary,
                      mb: 1,
                      display: 'flex',
                      justifyContent: 'center',
                    }}
                  >
                    {link.icon}
                  </Box>
                  <Typography variant="body2" fontWeight={500}>
                    {link.text}
                  </Typography>
                </CardContent>
              </Card>
            ))}
          </Box>
        </Box>

        {/* FAQs */}
        <Box sx={{ mb: 3 }}>
          <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
            Frequently Asked Questions
          </Typography>
          {faqs.map((faq) => (
            <Accordion
              key={faq.id}
              expanded={expanded === faq.id}
              onChange={handleChange(faq.id)}
              sx={{
                mb: 1,
                background: 'rgba(255, 255, 255, 0.95)',
                backdropFilter: 'blur(10px)',
                border: '1px solid rgba(40, 71, 188, 0.1)',
                '&:before': { display: 'none' },
                boxShadow: 'none',
              }}
            >
              <AccordionSummary
                expandIcon={<ExpandMoreIcon sx={{ color: TimoColors.primary }} />}
                sx={{
                  '& .MuiAccordionSummary-content': {
                    alignItems: 'center',
                    gap: 2,
                  },
                }}
              >
                <Box sx={{ color: TimoColors.primary }}>{faq.icon}</Box>
                <Typography variant="subtitle1" fontWeight={600}>
                  {faq.question}
                </Typography>
              </AccordionSummary>
              <AccordionDetails>
                <Typography variant="body2" color="text.secondary">
                  {faq.answer}
                </Typography>
              </AccordionDetails>
            </Accordion>
          ))}
        </Box>

        {/* Contact Support */}
        <Card
          sx={{
            background: 'rgba(255, 255, 255, 0.95)',
            backdropFilter: 'blur(10px)',
            border: '1px solid rgba(40, 71, 188, 0.1)',
          }}
        >
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
              Contact Support
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Have a question or need help? Send us a message and we'll get back to you as soon as possible.
            </Typography>
            <TextField
              fullWidth
              multiline
              rows={4}
              placeholder="Describe your issue or question..."
              value={contactMessage}
              onChange={(e) => setContactMessage(e.target.value)}
              sx={{ mb: 2 }}
            />
            <Button
              fullWidth
              variant="contained"
              sx={{
                bgcolor: TimoColors.primary,
                mb: 2,
                '&:hover': {
                  bgcolor: TimoColors.primaryDark,
                },
              }}
            >
              Send Message
            </Button>
            <Divider sx={{ my: 2 }} />
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <EmailIcon sx={{ color: TimoColors.primary }} />
                <Typography variant="body2">support@timorides.com</Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <PhoneIcon sx={{ color: TimoColors.primary }} />
                <Typography variant="body2">+27 82 123 4567</Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <ChatIcon sx={{ color: TimoColors.primary }} />
                <Typography variant="body2">Live Chat (24/7)</Typography>
              </Box>
            </Box>
          </CardContent>
        </Card>
      </Box>
    </Box>
  );
};

export default HelpScreen;


