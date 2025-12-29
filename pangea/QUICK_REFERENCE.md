# Quick Reference Guide

**For:** Agents and Coordinator  
**Purpose:** Quick lookup for common tasks and information

---

## 📁 Key Documents

| Document | Purpose | When to Use |
|----------|---------|-------------|
| `COORDINATION_MASTER.md` | Main coordination hub | Daily reference, task status |
| `AGENT_ASSIGNMENTS.md` | Task assignments | Agent onboarding, task details |
| `PROGRESS_TRACKER.md` | Progress tracking | Daily updates, milestone tracking |
| `COORDINATOR_GUIDE.md` | Coordinator handbook | Coordinator reference |
| `IMPLEMENTATION_PLAN.md` | Full implementation plan | Architecture, technical details |
| `task-briefs/` | Individual task briefs | Task-specific requirements |

---

## 🚀 Quick Start for Agents

1. **Get Assigned Task**
   - Check `AGENT_ASSIGNMENTS.md` for your task
   - Read the task brief in `task-briefs/`
   - Review dependencies

2. **Start Working**
   - Wait for dependencies to complete
   - Read task brief thoroughly
   - Set up development environment
   - Begin implementation

3. **Report Progress**
   - Update `PROGRESS_TRACKER.md` daily
   - Report blockers immediately
   - Notify coordinator on completion

4. **Complete Task**
   - Verify acceptance criteria
   - Write tests
   - Update documentation
   - Request coordinator review

---

## 📊 Status Codes

- ⏳ **Pending** - Not started
- 🔄 **In Progress** - Actively working
- ✅ **Complete** - Finished and verified
- ⚠️ **Blocked** - Waiting on dependency/issue
- 🔍 **Review** - Awaiting coordinator review
- ❌ **Failed** - Critical error encountered

---

## 🔗 Dependency Quick Reference

**Task 01** → No dependencies  
**Task 02** → Needs Task 01  
**Task 03** → Needs Tasks 01, 02  
**Task 04** → Needs Tasks 02, 03  
**Task 05** → Needs Task 01  
**Task 06** → Needs Tasks 02, 05  
**Task 07** → Needs Tasks 02, 06  
**Task 08** → Needs Tasks 07, 09  
**Task 09** → Needs Tasks 02, 08  
**Task 10** → Needs Tasks 02, 04, 05  
**Task 11** → Needs all previous  
**Task 12** → Needs Tasks 07, 08, 09  
**Task 13** → Needs all previous  
**Task 14** → Needs all previous  

---

## 📞 Communication Templates

### Daily Update
```
Agent: [Your Agent ID]
Task: [Task Number] - [Task Name]
Status: [Current Status]
Progress: [X%]
Blockers: [None/List blockers]
Next Steps: [What you're doing next]
ETA: [Expected completion date]
```

### Task Completion
```
Agent: [Your Agent ID]
Task: [Task Number] - [Task Name]
Status: ✅ Complete
Deliverables: [List deliverables]
Notes: [Any important notes]
Ready for: [Next task or review]
```

### Blocker Report
```
Agent: [Your Agent ID]
Task: [Task Number] - [Task Name]
Blocker: [Description]
Severity: [P0/P1/P2/P3]
Impact: [What's affected]
Request: [What you need]
```

---

## 🎯 Acceptance Criteria Checklist

Before marking task complete, verify:

- [ ] All acceptance criteria from task brief met
- [ ] Code follows project standards
- [ ] Tests written and passing
- [ ] Documentation updated
- [ ] No breaking changes
- [ ] Integration tested
- [ ] Coordinator notified

---

## 🔍 Common Issues & Solutions

### Issue: Dependency not ready
**Solution:** Check PROGRESS_TRACKER.md, notify coordinator if delay

### Issue: Integration problem
**Solution:** Coordinate with related agent, escalate if needed

### Issue: Unclear requirements
**Solution:** Review task brief, check IMPLEMENTATION_PLAN.md, ask coordinator

### Issue: Test failures
**Solution:** Debug locally, check dependencies, ask for help if stuck

---

## 📅 Milestone Dates

- **Milestone 1 (Phase 1):** End of Week 2
- **Milestone 2 (Phase 2):** End of Week 4
- **Milestone 3 (Phase 3):** End of Week 6
- **Milestone 4 (Phase 4):** End of Week 7
- **Milestone 5 (Phase 5):** End of Week 8
- **Milestone 6 (Phase 6):** End of Week 10

---

## 🔗 External Resources

- **OASIS API:** `https://api.oasisplatform.world`
- **SmartContractGenerator:** `/Volumes/Storage 4/OASIS_CLEAN/SmartContractGenerator`
- **Frontend:** `https://pangea.rkund.com/home`
- **Solana Docs:** `https://docs.solana.com/`
- **NestJS Docs:** `https://docs.nestjs.com/`

---

## ⚡ Quick Actions

**Need to start a task?**
1. Check dependencies in PROGRESS_TRACKER.md
2. Read task brief
3. Notify coordinator you're starting
4. Begin work

**Task complete?**
1. Verify acceptance criteria
2. Update PROGRESS_TRACKER.md
3. Notify coordinator
4. Wait for verification

**Blocked?**
1. Document blocker
2. Notify coordinator immediately
3. Check if you can work on other tasks
4. Wait for resolution

---

**Last Updated:** 2025-01-27












