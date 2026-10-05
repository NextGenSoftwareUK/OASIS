# CRM Setup for Driver Communication

## Purpose
Customer Relationship Management system to track driver communications, status, and engagement for the PathPulse pilot program.

---

## 📚 Documentation Files

### Setup & Configuration
- **`CRM_SETUP_GUIDE.md`** - Overview of CRM options and recommendations (WATI recommended)
- **`WATI_INTEGRATION_GUIDE.md`** - Complete guide for integrating WATI with MongoDB and Excel driver list ⭐
- **`QUICK_SETUP_SCRIPT.md`** - Step-by-step quick setup instructions
- **`database_schema_template.md`** - Database schema for driver CRM tracking

### Comparison & Analysis
- **`WHATSAPP_CRM_COMPARISON.md`** - Comparison of WhatsApp CRM solutions

---

## 🚀 Quick Start

### Option 1: WATI Integration (Recommended)
1. Read **`WATI_INTEGRATION_GUIDE.md`** for complete setup
2. Follow **`QUICK_SETUP_SCRIPT.md`** for step-by-step commands
3. Import drivers from Excel: `node scripts/importDriversFromExcel.js`
4. Sync to WATI: `POST /api/wati/sync-all`

### Option 2: Google Sheets (Quick Manual Setup)
See **`CRM_SETUP_GUIDE.md`** for Google Sheets template and setup.

---

## 📊 Current Setup Status

### ✅ Completed
- [x] CRM setup guide with WATI recommendation
- [x] WATI integration guide with code examples
- [x] Database schema template
- [x] Quick setup script
- [x] Excel driver list identified: `driverdetails/List driver 1 (1) (1).xlsx`

### 🔄 To Do
- [ ] Install required npm packages (`axios`, `xlsx`, `multer`, `dotenv`)
- [ ] Create `driverCrmModel.js` in backend
- [ ] Create `watiService.js` in backend
- [ ] Create `watiRoutes.js` in backend
- [ ] Create `importDriversFromExcel.js` script
- [ ] Configure WATI account and get API token
- [ ] Set up environment variables
- [ ] Import drivers from Excel
- [ ] Sync drivers to WATI
- [ ] Configure WATI webhook
- [ ] Test message sending
- [ ] Set up automated follow-ups

---

## 📋 CRM Requirements

### Data to Track
- Driver contact information (from Excel)
- Onboarding status (8 states: not_contacted → active)
- Communication history (WhatsApp messages)
- Response rates and engagement metrics
- Follow-up scheduling

### Workflows
```
not_contacted → contacted → invited → onboarding_started 
→ onboarding_completed → active → inactive/opted_out
```

### Integration Points
- **WATI (WhatsApp Team Inbox)** - Primary communication channel ⭐
- **MongoDB Backend** - Driver data storage
- **Excel Spreadsheet** - Driver list source (`driverdetails/List driver 1 (1) (1).xlsx`)
- **Webhooks** - Two-way sync with WATI

---

## 🎯 Key Features

1. **Excel Import** - Import 200 drivers from Excel spreadsheet
2. **WATI Sync** - Automatically sync drivers to WATI contacts
3. **Message Sending** - Send individual or broadcast WhatsApp messages
4. **Webhook Handling** - Receive and process driver responses
5. **Status Tracking** - Track onboarding progress for each driver
6. **Communication Logging** - Log all WhatsApp interactions

---

## 📞 Driver List

**Location:** `/Volumes/Storage/OASIS_CLEAN/TimoRides/driverdetails/List driver 1 (1) (1).xlsx`

**Expected Columns:**
- Name / Full Name
- Phone / Phone Number
- Email (optional)
- City (optional)

---

## 🔗 Related Files

- **Driver Outreach:** `../DRIVER_OUTREACH_MESSAGING.md`
- **7-Day Action Plan:** `../PATHPULSE_PILOT_7DAY_ACTION_PLAN.md`
- **WhatsApp Templates:** `../DriverCommunication/WhatsApp/welcome_message.txt`

---

## 📖 Next Steps

1. **Read** `WATI_INTEGRATION_GUIDE.md` for detailed implementation
2. **Follow** `QUICK_SETUP_SCRIPT.md` for setup commands
3. **Import** drivers from Excel spreadsheet
4. **Sync** drivers to WATI
5. **Send** welcome messages to all drivers
6. **Monitor** responses and track onboarding progress

---

**Last Updated:** [Current Date]  
**Status:** Documentation complete, ready for implementation

