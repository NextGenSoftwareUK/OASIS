import React, { useState } from 'react';
import {
  Box,
  Container,
  Typography,
  Grid,
  Card,
  CardContent,
  TextField,
  Button,
  Alert,
  CircularProgress,
  Paper,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Divider,
} from '@mui/material';
import {
  Email as EmailIcon,
  Phone as PhoneIcon,
  LocationOn as LocationIcon,
  Support as SupportIcon,
  Business as BusinessIcon,
  Schedule as ScheduleIcon,
  CheckCircle as CheckIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { toast } from 'react-hot-toast';

interface ContactForm {
  name: string;
  email: string;
  company: string;
  subject: string;
  message: string;
  inquiryType: string;
}

const ContactPage: React.FC = () => {
  const [formData, setFormData] = useState<ContactForm>({
    name: '',
    email: '',
    company: '',
    subject: '',
    message: '',
    inquiryType: 'general',
  });
  const [loading, setLoading] = useState(false);
  const [showInfoBar, setShowInfoBar] = useState(true);

  const handleInputChange = (field: keyof ContactForm) => (
    event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    setFormData(prev => ({
      ...prev,
      [field]: event.target.value,
    }));
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setLoading(true);

    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      toast.success('Message sent successfully! We\'ll get back to you within 24 hours.');
      setFormData({
        name: '',
        email: '',
        company: '',
        subject: '',
        message: '',
        inquiryType: 'general',
      });
    } catch (error) {
      toast.error('Failed to send message. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const contactInfo = [
    {
      icon: <EmailIcon />,
      title: 'Email Support',
      description: 'Get help with technical issues',
      contact: 'support@oasisplatform.com',
      responseTime: 'Within 24 hours',
    },
    {
      icon: <PhoneIcon />,
      title: 'Phone Support',
      description: 'Speak with our team directly',
      contact: '+1 (555) 123-4567',
      responseTime: 'Mon-Fri, 9AM-6PM EST',
    },
    {
      icon: <BusinessIcon />,
      title: 'Enterprise Sales',
      description: 'Custom solutions for large organizations',
      contact: 'sales@oasisplatform.com',
      responseTime: 'Within 4 hours',
    },
    {
      icon: <SupportIcon />,
      title: 'Developer Support',
      description: 'Technical assistance for developers',
      contact: 'dev@oasisplatform.com',
      responseTime: 'Within 12 hours',
    },
  ];

  const faqItems = [
    {
      question: 'How do I get started with OASIS?',
      answer: 'Start with our free tier and explore the platform. Check out our documentation and tutorials.',
    },
    {
      question: 'What subscription plans are available?',
      answer: 'We offer Bronze, Silver, Gold, and Enterprise plans. Each with different API limits and features.',
    },
    {
      question: 'How does the Universal Wallet work?',
      answer: 'The OASIS Universal Wallet supports 15+ blockchains and fiat currencies with one-click transfers.',
    },
    {
      question: 'Can I integrate OASIS with my existing system?',
      answer: 'Yes! OASIS provides REST APIs, GraphQL, and SDKs for seamless integration.',
    },
  ];

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      {/* Info Bar */}
      {showInfoBar && (
        <Box sx={{ mt: 1, p: 2, bgcolor: '#0d47a1', color: 'white', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1, mb: 4 }}>
          <InfoIcon sx={{ color: 'white' }} />
          <Typography variant="body2" sx={{ color: 'white', flexGrow: 1 }}>
            <strong>Need Help?</strong> Our support team is here to assist you. Choose the best contact method for your needs.
          </Typography>
          <Button
            size="small"
            onClick={() => setShowInfoBar(false)}
            sx={{ color: 'white', minWidth: 'auto', p: 0.5 }}
          >
            ×
          </Button>
        </Box>
      )}

      <Box sx={{ textAlign: 'center', mb: 6 }}>
        <Typography variant="h3" component="h1" gutterBottom sx={{ fontWeight: 'bold' }}>
          Contact OASIS Support
        </Typography>
        <Typography variant="h6" color="text.secondary" sx={{ mb: 4 }}>
          Get help from our expert team. We're here to support your OASIS journey.
        </Typography>
      </Box>

      <Grid container spacing={4}>
        {/* Contact Form */}
        <Grid item xs={12} lg={8}>
          <Card>
            <CardContent sx={{ p: 4 }}>
              <Typography variant="h5" gutterBottom>
                Send us a Message
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Fill out the form below and we'll get back to you as soon as possible.
              </Typography>

              <Box component="form" onSubmit={handleSubmit}>
                <Grid container spacing={3}>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      fullWidth
                      label="Full Name"
                      value={formData.name}
                      onChange={handleInputChange('name')}
                      required
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      fullWidth
                      label="Email Address"
                      type="email"
                      value={formData.email}
                      onChange={handleInputChange('email')}
                      required
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      fullWidth
                      label="Company (Optional)"
                      value={formData.company}
                      onChange={handleInputChange('company')}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      fullWidth
                      label="Inquiry Type"
                      select
                      value={formData.inquiryType}
                      onChange={handleInputChange('inquiryType')}
                      SelectProps={{ native: true }}
                    >
                      <option value="general">General Inquiry</option>
                      <option value="technical">Technical Support</option>
                      <option value="billing">Billing Question</option>
                      <option value="enterprise">Enterprise Sales</option>
                      <option value="partnership">Partnership</option>
                    </TextField>
                  </Grid>
                  <Grid item xs={12}>
                    <TextField
                      fullWidth
                      label="Subject"
                      value={formData.subject}
                      onChange={handleInputChange('subject')}
                      required
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField
                      fullWidth
                      label="Message"
                      multiline
                      rows={6}
                      value={formData.message}
                      onChange={handleInputChange('message')}
                      required
                      placeholder="Please provide as much detail as possible about your inquiry..."
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Button
                      type="submit"
                      variant="contained"
                      size="large"
                      disabled={loading}
                      startIcon={loading ? <CircularProgress size={20} /> : null}
                      sx={{ minWidth: 200 }}
                    >
                      {loading ? 'Sending...' : 'Send Message'}
                    </Button>
                  </Grid>
                </Grid>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Contact Information */}
        <Grid item xs={12} lg={4}>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
            {contactInfo.map((info, index) => (
              <Card key={index}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
                    <Box
                      sx={{
                        p: 1,
                        borderRadius: '50%',
                        bgcolor: 'primary.main',
                        color: 'white',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}
                    >
                      {info.icon}
                    </Box>
                    <Box sx={{ flexGrow: 1 }}>
                      <Typography variant="h6" gutterBottom>
                        {info.title}
                      </Typography>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                        {info.description}
                      </Typography>
                      <Typography variant="body2" fontWeight="medium" sx={{ mb: 1 }}>
                        {info.contact}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {info.responseTime}
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            ))}
          </Box>
        </Grid>
      </Grid>

      {/* FAQ Section */}
      <Box sx={{ mt: 8 }}>
        <Typography variant="h4" gutterBottom sx={{ textAlign: 'center', mb: 4 }}>
          Frequently Asked Questions
        </Typography>
        <Grid container spacing={3}>
          {faqItems.map((item, index) => (
            <Grid item xs={12} md={6} key={index}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <CheckIcon color="primary" />
                    {item.question}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {item.answer}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      </Box>

      {/* Additional Resources */}
      <Box sx={{ mt: 6 }}>
        <Paper sx={{ p: 4, bgcolor: '#0d47a1', color: 'white' }}>
          <Typography variant="h5" gutterBottom sx={{ color: 'white' }}>
            Additional Resources
          </Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={4}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <ScheduleIcon sx={{ color: 'white' }} />
                <Box>
                  <Typography variant="h6" sx={{ color: 'white' }}>
                    Documentation
                  </Typography>
                  <Typography variant="body2" sx={{ color: 'rgba(255,255,255,0.8)' }}>
                    Comprehensive guides and API references
                  </Typography>
                </Box>
              </Box>
            </Grid>
            <Grid item xs={12} md={4}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <SupportIcon sx={{ color: 'white' }} />
                <Box>
                  <Typography variant="h6" sx={{ color: 'white' }}>
                    Community Forum
                  </Typography>
                  <Typography variant="body2" sx={{ color: 'rgba(255,255,255,0.8)' }}>
                    Connect with other OASIS users
                  </Typography>
                </Box>
              </Box>
            </Grid>
            <Grid item xs={12} md={4}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <LocationIcon sx={{ color: 'white' }} />
                <Box>
                  <Typography variant="h6" sx={{ color: 'white' }}>
                    Status Page
                  </Typography>
                  <Typography variant="body2" sx={{ color: 'rgba(255,255,255,0.8)' }}>
                    Real-time system status and updates
                  </Typography>
                </Box>
              </Box>
            </Grid>
          </Grid>
        </Paper>
      </Box>
    </Container>
  );
};

export default ContactPage;
