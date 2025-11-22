# Driver CRM Database Schema

## Driver Table

```sql
drivers (
    id: UUID (Primary Key)
    timo_driver_id: String (Foreign Key to TimoRides)
    pathpulse_account_id: String (Foreign Key to PathPulse)
    
    -- Contact Information
    name: String
    phone: String
    email: String
    whatsapp_number: String
    telegram_username: String
    
    -- Status
    onboarding_status: Enum [
        'not_contacted',
        'contacted',
        'invited',
        'onboarding_started',
        'onboarding_completed',
        'active',
        'inactive',
        'opted_out'
    ]
    
    -- Dates
    first_contact_date: DateTime
    onboarding_start_date: DateTime
    onboarding_complete_date: DateTime
    last_active_date: DateTime
    
    -- Communication
    preferred_channel: Enum ['whatsapp', 'telegram', 'email']
    communication_opt_in: Boolean
    last_contact_date: DateTime
    next_follow_up_date: DateTime
    
    -- Metrics
    total_communications: Integer
    response_rate: Float
    engagement_score: Float
    
    -- Metadata
    created_at: DateTime
    updated_at: DateTime
    notes: Text
)
```

## Communication History Table

```sql
communications (
    id: UUID (Primary Key)
    driver_id: UUID (Foreign Key)
    
    -- Communication Details
    channel: Enum ['whatsapp', 'telegram', 'email', 'phone']
    direction: Enum ['inbound', 'outbound']
    message_type: Enum ['welcome', 'onboarding', 'reminder', 'support', 'update']
    
    -- Content
    subject: String
    message_body: Text
    attachments: JSON
    
    -- Status
    status: Enum ['sent', 'delivered', 'read', 'replied', 'failed']
    response_received: Boolean
    response_time: Integer (seconds)
    
    -- Metadata
    sent_at: DateTime
    delivered_at: DateTime
    read_at: DateTime
    replied_at: DateTime
    created_by: String
)
```

## Support Tickets Table

```sql
support_tickets (
    id: UUID (Primary Key)
    driver_id: UUID (Foreign Key)
    
    -- Ticket Details
    issue_type: Enum ['technical', 'account', 'payment', 'other']
    priority: Enum ['low', 'medium', 'high', 'urgent']
    status: Enum ['open', 'in_progress', 'resolved', 'closed']
    
    -- Content
    subject: String
    description: Text
    resolution: Text
    
    -- Tracking
    created_at: DateTime
    resolved_at: DateTime
    assigned_to: String
    response_time: Integer (minutes)
)
```

## Engagement Metrics Table

```sql
engagement_metrics (
    id: UUID (Primary Key)
    driver_id: UUID (Foreign Key)
    
    -- Metrics
    date: Date
    app_usage_hours: Float
    data_quality_score: Float
    participation_score: Float
    
    -- Rewards
    base_reward: Decimal
    bonus_reward: Decimal
    total_reward: Decimal
    payment_status: Enum ['pending', 'paid', 'failed']
    
    -- Metadata
    created_at: DateTime
)
```

---

## Status Workflow

```
not_contacted
    ↓ (Send welcome message)
contacted
    ↓ (Send invitation)
invited
    ↓ (Driver starts onboarding)
onboarding_started
    ↓ (Driver completes onboarding)
onboarding_completed
    ↓ (Pilot starts)
active
    ↓ (Driver stops participating)
inactive
    ↓ (Driver opts out)
opted_out
```

---

## Key Reports Needed

1. **Onboarding Progress Report**
   - Total drivers contacted
   - Onboarding completion rate
   - Time to complete onboarding

2. **Engagement Report**
   - Active drivers
   - Participation rates
   - Communication effectiveness

3. **Support Report**
   - Ticket volume
   - Response times
   - Resolution rates

4. **Payment Report**
   - Rewards distributed
   - Payment status
   - Outstanding payments

