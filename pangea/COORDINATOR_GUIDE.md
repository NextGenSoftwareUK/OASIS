# Coordinator Guide - Pangea Markets Backend

**Role:** AI Oversight Agent / Project Coordinator  
**Purpose:** Guide for coordinating 14 implementation tasks across multiple agents

---

## 🎯 Coordinator Responsibilities

### 1. Task Management

**Assignment:**
- Review task dependencies before assignment
- Assign tasks to appropriate agents
- Ensure agents have all necessary information
- Track task assignments in COORDINATION_MASTER.md

**Monitoring:**
- Track daily progress updates
- Identify blockers early
- Update PROGRESS_TRACKER.md
- Monitor milestone completion

**Verification:**
- Review completed tasks
- Verify acceptance criteria met
- Test integrations at checkpoints
- Approve task completion

---

## 📋 Daily Workflow

### Morning Review
1. Check PROGRESS_TRACKER.md for updates
2. Review any blockers or issues
3. Identify tasks ready to start (dependencies met)
4. Assign new tasks if needed

### During Day
1. Monitor agent communications
2. Resolve blockers as they arise
3. Update documentation as needed
4. Coordinate between agents

### End of Day
1. Update PROGRESS_TRACKER.md
2. Review COORDINATION_MASTER.md
3. Plan next day's assignments
4. Document any issues or decisions

---

## 🔍 Task Verification Checklist

When an agent reports task completion, verify:

### Code Quality
- [ ] Code follows project standards
- [ ] Proper error handling
- [ ] Input validation present
- [ ] Type safety (TypeScript)
- [ ] No obvious bugs

### Documentation
- [ ] Code comments where needed
- [ ] API documentation updated
- [ ] README updated if applicable
- [ ] Task brief acceptance criteria met

### Testing
- [ ] Unit tests written
- [ ] Integration tests passing
- [ ] Edge cases covered
- [ ] Tests are meaningful

### Integration
- [ ] Works with existing code
- [ ] No breaking changes
- [ ] Dependencies properly handled
- [ ] API contracts maintained

### Performance
- [ ] No obvious performance issues
- [ ] Database queries optimized
- [ ] Proper indexing
- [ ] Efficient algorithms

---

## 🚨 Issue Resolution Process

### Step 1: Identify Issue
- Agent reports issue
- Coordinator assesses severity
- Document in issue tracker

### Step 2: Triage
- **P0 (Critical):** Immediate attention
- **P1 (High):** Review within 4 hours
- **P2 (Medium):** Review within 24 hours
- **P3 (Low):** Log for future

### Step 3: Resolve
- Assign fix to appropriate agent
- Provide guidance if needed
- Unblock dependent tasks
- Verify resolution

### Step 4: Document
- Update issue tracker
- Document solution
- Update relevant docs
- Share learnings

---

## 🔗 Integration Checkpoints

### Checkpoint 1: After Phase 1
**Tasks:** 01, 02, 03

**Verification:**
1. Start the application
2. Connect to database
3. Run migrations
4. Test authentication endpoints
5. Verify OASIS API integration

**Success Criteria:**
- ✅ Server starts without errors
- ✅ Database connection works
- ✅ Migrations run successfully
- ✅ Auth endpoints respond correctly
- ✅ OASIS API calls succeed

---

### Checkpoint 2: After Phase 2
**Tasks:** 04, 05, 06

**Verification:**
1. Test wallet connection
2. Generate and deploy test contract
3. Create test asset
4. Verify asset endpoints
5. Check balance synchronization

**Success Criteria:**
- ✅ Wallet connection works
- ✅ Contracts can be deployed
- ✅ Assets can be created
- ✅ Asset endpoints functional
- ✅ Balances sync correctly

---

### Checkpoint 3: After Phase 3
**Tasks:** 07, 08, 09

**Verification:**
1. Create test order
2. Verify order matching
3. Execute test trade
4. Check trade records
5. Verify balance updates

**Success Criteria:**
- ✅ Orders can be created
- ✅ Matching algorithm works
- ✅ Trades execute correctly
- ✅ Trade records created
- ✅ Balances update properly

---

### Checkpoint 4: After Phase 4
**Tasks:** 10, 12

**Verification:**
1. Test deposit flow
2. Test withdrawal flow
3. Verify WebSocket events
4. Check real-time updates
5. Monitor transactions

**Success Criteria:**
- ✅ Deposits work
- ✅ Withdrawals work
- ✅ WebSocket connects
- ✅ Events emit correctly
- ✅ Real-time updates work

---

### Checkpoint 5: After Phase 5
**Tasks:** 11

**Verification:**
1. Test admin authentication
2. Verify user management
3. Check asset management
4. Test platform statistics
5. Verify analytics

**Success Criteria:**
- ✅ Admin access works
- ✅ User management functional
- ✅ Asset management works
- ✅ Statistics accurate
- ✅ Analytics working

---

### Checkpoint 6: After Phase 6
**Tasks:** 13, 14

**Verification:**
1. Run full test suite
2. Check test coverage
3. Verify deployment
4. Test health checks
5. Verify monitoring

**Success Criteria:**
- ✅ All tests pass
- ✅ Coverage ≥ 80%
- ✅ Deployment successful
- ✅ Health checks work
- ✅ Monitoring active

---

## 📊 Progress Monitoring

### Daily Metrics
- Tasks completed today
- Tasks in progress
- Tasks blocked
- Overall progress %

### Weekly Metrics
- Phase completion status
- Milestone progress
- Velocity (tasks/week)
- Blocker resolution time

### Project Metrics
- Overall completion %
- Estimated completion date
- Risk assessment
- Quality metrics

---

## 🤝 Agent Communication

### Daily Standup Format
Request agents report:
```
Agent: [ID]
Task: [Number] - [Name]
Status: [Status]
Progress: [X%]
Blockers: [Any]
Next Steps: [What's next]
ETA: [When done]
```

### Communication Channels
- **Progress Updates:** PROGRESS_TRACKER.md
- **Issues/Blockers:** COORDINATION_MASTER.md
- **Task Completion:** COORDINATION_MASTER.md + verification
- **Integration Issues:** Direct coordination between agents

---

## 🎯 Success Metrics

### Project Success
- ✅ All 14 tasks completed
- ✅ All checkpoints passed
- ✅ Test coverage ≥ 80%
- ✅ All tests passing
- ✅ Deployment successful
- ✅ Documentation complete

### Quality Success
- ✅ Code quality standards met
- ✅ No critical bugs
- ✅ Performance acceptable
- ✅ Security best practices
- ✅ Maintainable codebase

---

## 📝 Documentation Updates

### When to Update
- Daily: PROGRESS_TRACKER.md
- On task completion: COORDINATION_MASTER.md
- On milestone: All relevant docs
- On issue: Issue tracker + relevant docs
- On decision: COORDINATION_MASTER.md

### What to Document
- Task assignments
- Progress updates
- Blockers and resolutions
- Integration issues
- Architecture decisions
- Lessons learned

---

## 🚀 Getting Started

### Initial Setup
1. Review all task briefs
2. Understand dependencies
3. Set up tracking documents
4. Assign first task (Task 01)

### First Week
1. Monitor Task 01 progress
2. Prepare Task 02 assignment
3. Set up communication channels
4. Establish daily update rhythm

---

## 🔄 Continuous Improvement

### Weekly Retrospective
- What went well?
- What could be improved?
- Any process changes needed?
- Update coordination process

### Process Refinement
- Adjust based on learnings
- Improve communication
- Streamline verification
- Optimize task assignment

---

**Remember:** Your role is to ensure smooth coordination, timely completion, and high quality. Be proactive, communicative, and thorough in verification.

**Good luck! 🚀**












