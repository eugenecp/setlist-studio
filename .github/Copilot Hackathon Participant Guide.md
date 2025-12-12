# Copilot Hackathon: Participant Guide
 
**Welcome!** Today you'll learn how professional teams use GitHub Copilot to build production-ready software.
 
---
 
## 🎯 Your Mission
 
Build a **"Setlist Templates"** feature for Setlist Studio using the **Ask → Instruct → Implement** workflow.
 
**What are Setlist Templates?**
Musicians often perform similar types of events (weddings, corporate events, club nights). Instead of recreating setlists from scratch each time, they want reusable templates.
 
**Example:**
- Template: "Wedding Reception Set" (15 songs, 75 minutes, romantic/danceable mix)
- Use it for: Miller Wedding (Jan 15), Smith Wedding (Feb 3), etc.
 
---
 
## 🔄 The Professional Workflow
 
### Ask → Instruct → Implement
 
**1. ASK Copilot for Strategy** 💭
- Have conversations with Copilot Chat before coding
- Explore approaches, trade-offs, and best practices
- Ask "why" and "how", not just "what"
 
**2. INSTRUCT via Documentation** 📝
- Update `.github/copilot-instructions.md` with patterns
- Document so Copilot (and your team) follows your standards
- Make knowledge reusable
 
**3. IMPLEMENT with AI Assistance** ⚡
- Write code with Copilot's help
- Copilot now follows YOUR documented patterns
- Review and refine suggestions
 
---
 
## 🎯 The Five Core Principles
 
Every feature you build must meet all five:
 
### 1. 🔧 Make It Work
- Functional and solves the business problem
- Comprehensive tests prove it works
- **Ask Copilot**: "What test scenarios should I cover?"
 
### 2. 🔒 Make It Secure
- All inputs validated and sanitized
- Users can only access their own data
- Protected from attacks (XSS, SQL injection, etc.)
- **Ask Copilot**: "What security vulnerabilities could exist?"
 
### 3. 📈 Make It Scale
- Performs well under load
- Efficient database queries
- Proper pagination for large datasets
- **Ask Copilot**: "How will this perform with 10,000+ records?"
 
### 4. 📚 Make It Maintainable
- Clear, readable code
- Documented patterns and decisions
- Consistent with project conventions
- **Ask Copilot**: "How can I make this more maintainable?"
 
### 5. ✨ Deliver User Delight
- Solves real user problems
- Professional user experience
- Intuitive and reliable
- **Ask Copilot**: "How would musicians actually use this?"
 
---
 
## ⏱️ Session Schedule (2 Hours)
 
| Time | Challenge | Focus |
|------|-----------|-------|
| 0:00-0:15 | Challenge 1 | Ask Copilot for strategy (song filtering) |
| 0:15-0:35 | Challenge 2 | Document patterns in copilot-instructions.md |
| 0:35-0:40 | Break | Stretch, hydrate, recharge |
| 0:40-0:55 | Challenge 3 | Implement with TDD |
| 0:55-1:20 | Challenge 4 | Security-first development |
| 1:20-1:40 | Challenge 5 | Build Setlist Templates feature |
| 1:40-1:55 | Presentations | Share what you learned |
| 1:55-2:00 | Wrap-up | Next steps |
 
---
 
## 🎮 Challenge 1: Ask Copilot for Strategy (15 minutes)
 
### Goal
Learn to have strategic conversations with Copilot before writing code.
 
### The Task
Design a feature to **filter songs by genre with pagination**.
 
### Your Steps
 
1. **Open Copilot Chat** (Ctrl+Shift+I or Cmd+Shift+I)
 
2. **Ask strategic questions:**
   - "What's the best way to filter songs by genre with pagination?"
   - "What are the pros and cons of different approaches?"
   - "What patterns does this codebase already use?"
   - "What performance implications should I consider?"
 
3. **Explore existing code:**
   - Look at `src/SetlistStudio.Core/Services/SongService.cs`
   - Ask Copilot: "What patterns does this service follow?"
 
4. **Make a decision:**
   - Choose your approach
   - Be ready to explain WHY
 
### Success Criteria
- [ ] Had meaningful conversation with Copilot (5+ exchanges)
- [ ] Explored multiple approaches
- [ ] Understand trade-offs
- [ ] Can explain your design decision
 
### Key Questions to Ask
- "What's the best approach for [problem]?"
- "What are the performance implications?"
- "What patterns does this codebase follow?"
- "What edge cases should I consider?"
 
---
 
## 🎮 Challenge 2: Document Your Pattern (20 minutes)
 
### Goal
Update Copilot instructions so future work follows your patterns.
 
### The Task
Add your filtering pattern to `.github/copilot-instructions.md`
 
### Your Steps
 
1. **Review existing instructions:**
   - Open `.github/copilot-instructions.md`
   - See what's already documented
   - Notice: Copilot already knows these patterns!
 
2. **Document your pattern:**
   - Add a new section for your filtering approach
   - Include all five principles:
     - ✅ **Works**: How the pattern functions
     - 🔒 **Secure**: Validation and security requirements
     - 📈 **Scales**: Performance considerations
     - 📚 **Maintainable**: Code example and conventions
     - ✨ **User Delight**: Business value
 
3. **Test your instructions:**
   - Create a new file
   - Start typing related code
   - See if Copilot follows your documented pattern
 
### Success Criteria
- [ ] Added meaningful documentation
- [ ] Covered all five core principles
- [ ] Included code example
- [ ] Tested that Copilot follows instructions
 
### Documentation Template
```markdown
### [Your Pattern Name]
 
**When to use**: [Describe the scenario]
 
**Core Principles**:
- **Works**: [How it functions, what it does]
- **Secure**: [Security requirements]
- **Scales**: [Performance considerations]
- **Maintainable**: [Code structure, conventions]
- **User Delight**: [Business value]
 
**Example**:
```language
// Your code example
```
 
**Common pitfalls**:
- [What to avoid]
```
 
---
 
## ☕ Break (5 minutes)
 
Stretch, hydrate, and recharge!
 
---
 
## 🎮 Challenge 3: Implement with TDD (15 minutes)
 
### Goal
Build the filtering feature using Test-Driven Development.
 
### The Task
Implement the song filtering feature you designed, **tests first**.
 
### Your Steps
 
1. **Ask for testing strategy:**
   - "What scenarios should I test for song filtering?"
   - "What edge cases exist?"
   - "What mocks do I need?"
 
2. **Write tests FIRST:**
   - Create test file (follow naming convention: `{SourceClass}Tests.cs`)
   - Write test methods for each scenario
   - Tests will FAIL - that's expected (red phase)
 
3. **Implement to pass tests:**
   - Now write the actual feature code
   - Run tests: `dotnet test`
   - Iterate until all tests pass (green phase)
 
4. **Check coverage:**
   ```powershell
   dotnet test --collect:"XPlat Code Coverage"
   ```
 
### Success Criteria
- [ ] Tests written before implementation
- [ ] All tests pass (100% success rate)
- [ ] Coverage >80% line and branch
- [ ] Used realistic musical test data
 
### TDD Workflow
```
1. Write test (RED)
2. Run test - it fails ✗
3. Write minimal code (GREEN)
4. Run test - it passes ✓
5. Refactor (REFACTOR)
6. Repeat
```
 
---
 
## 🎮 Challenge 4: Security-First Development (25 minutes)
 
### Goal
Learn to build security in from the start, not bolt it on later.
 
### The Task
Add security patterns for user-generated content (songs, templates).
 
### Your Steps
 
**Part 1: Threat Modeling (8 min)**
 
1. **Ask Copilot about security:**
   - "What security risks exist for user song data?"
   - "What attacks should I protect against?"
   - "What validation is needed for musical data?"
   - "What authorization checks are required?"
 
2. **Review security guidelines:**
   - Read security sections in `.github/copilot-instructions.md`
   - Ask: "What security patterns does this project require?"
 
**Part 2: Document Security (10 min)**
 
1. **Add security documentation:**
   - Update `.github/copilot-instructions.md`
   - Document specific validation rules
   - Document authorization patterns
   - Include anti-patterns (what NOT to do)
 
**Part 3: Implement Security (7 min)**
 
1. **Write security tests first:**
   - Unauthorized access attempts
   - Invalid input validation
   - Malicious input handling
 
2. **Implement security:**
   - Input validation
   - Authorization checks
   - User ownership verification
 
3. **Verify:**
   ```powershell
   dotnet test --filter "Security"
   ```
 
### Success Criteria
- [ ] Identified security threats
- [ ] Documented security patterns
- [ ] Implemented validation BEFORE business logic
- [ ] Added authorization at entry points
- [ ] Created security-specific tests
- [ ] All security tests pass
 
### Security Checklist
- [ ] All inputs validated
- [ ] User authorization checks
- [ ] No SQL injection vulnerabilities
- [ ] No XSS vulnerabilities
- [ ] Error messages don't leak sensitive data
- [ ] Proper resource disposal
 
---
 
## 🎮 Challenge 5: Build Setlist Templates (20 minutes)
 
### Goal
Apply the complete workflow to build a production-ready feature.
 
### What You're Building
 
**Feature: Setlist Templates**
- Musicians create reusable templates (e.g., "Wedding Set", "Rock Bar Night")
- Templates contain song lists, duration estimates, categories
- Users can convert templates → actual setlists
- Private by default (bonus: public sharing)
 
**Minimum Viable Feature:**
- Template entity with basic properties
- CRUD service methods (Create, Read, Update, Delete)
- User authorization (can't access others' templates)
- Comprehensive tests (>80% coverage)
- Security validation
 
### Your Steps
 
**Phase 1: ASK - Strategy (7 min)**
 
1. **Have strategic conversation with Copilot:**
   - "How should I design a template system for setlists?"
   - "What's the difference between a template and actual setlist?"
   - "What entities and relationships do I need?"
   - "What security considerations exist for templates?"
   - "How should template → setlist conversion work?"
 
2. **Design your approach:**
   - Entity structure
   - Service methods
   - Security requirements
   - Testing strategy
 
**Phase 2: INSTRUCT - Document (6 min)**
 
1. **Add to `.github/copilot-instructions.md`:**
   - Template feature overview
   - Entity structure pattern
   - Service layer conventions
   - Security requirements
   - Testing expectations
 
2. **Include all five principles:**
   - Works, Secure, Scale, Maintainable, User Delight
 
**Phase 3: IMPLEMENT - Build (7 min)**
 
1. **Create tests first:**
   - Template CRUD operations
   - Authorization scenarios
   - Validation tests
   - Template → setlist conversion
 
2. **Implement the feature:**
   - Entity: `SetlistTemplate.cs`
   - Service: `SetlistTemplateService.cs`
   - Tests: `SetlistTemplateServiceTests.cs`
 
3. **Verify:**
   ```powershell
   dotnet build
   dotnet test
   ```
 
### Success Criteria
 
**Workflow:**
- [ ] Asked strategic questions (Phase 1)
- [ ] Documented patterns (Phase 2)
- [ ] Implemented with TDD (Phase 3)
 
**Core Principles:**
- [ ] **Works**: Feature is functional, tests pass
- [ ] **Secure**: Validation and authorization implemented
- [ ] **Scales**: Efficient queries, pagination considered
- [ ] **Maintainable**: Clear code, documented patterns
- [ ] **User Delight**: Solves real musician problem
 
**Technical:**
- [ ] All tests pass (100% success rate)
- [ ] Coverage >80%
- [ ] No build warnings
- [ ] Follows project conventions
 
---
 
## 📊 Team Presentations (15 minutes)
 
### What to Share (3 minutes per team)
 
**1. Your Feature (1 min)**
- Quick demo: What did you build?
- What design decisions did you make?
 
**2. Copilot Workflow (1 min)**
- How did Ask → Instruct → Implement help?
- Show your copilot-instructions.md contribution
- What surprised you about Copilot?
 
**3. Key Learning (1 min)**
- What will you do differently tomorrow?
- Which of the five principles was most valuable?
- What would you tell other developers?
 
---
 
## 🛠️ Quick Reference
 
### Copilot Shortcuts
- **Copilot Chat**: `Ctrl+Shift+I` (Windows) or `Cmd+Shift+I` (Mac)
- **Accept suggestion**: `Tab`
- **Next suggestion**: `Alt+]` or `Option+]`
- **Previous suggestion**: `Alt+[` or `Option+[`
- **Inline chat**: `Ctrl+I` or `Cmd+I`
 
### Useful Commands
```powershell
# Run all tests
dotnet test
 
# Run specific tests
dotnet test --filter "FullyQualifiedName~ClassName"
 
# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
 
# Build solution
dotnet build
 
# Restore packages
dotnet restore
```
 
### Key Files
- `.github/copilot-instructions.md` - Project patterns and standards
- `src/SetlistStudio.Core/Services/` - Service layer
- `src/SetlistStudio.Core/Entities/` - Domain entities
- `tests/SetlistStudio.Core.Tests/` - Unit tests
 
### Questions to Ask Copilot
- "What's the best approach for [feature]?"
- "What security concerns exist for [scenario]?"
- "How should I test [functionality]?"
- "What patterns does this codebase follow?"
- "How will this perform at scale?"
- "What edge cases should I consider?"
- "How can I make this more maintainable?"
 
---
 
## 🎯 Success Tips
 
### Do's ✅
- **Ask before coding** - Strategy first
- **Document decisions** - Help future you and your team
- **Test first** - TDD catches bugs early
- **Think security** - Build it in from the start
- **Use realistic data** - Real song names, valid BPMs
- **Help teammates** - Learn together
- **Ask questions** - Facilitators are here to help
 
### Don'ts ❌
- **Don't rush to code** - Strategy saves time
- **Don't skip documentation** - It helps everyone
- **Don't ignore security** - It's not optional
- **Don't work in isolation** - Collaborate with your team
- **Don't accept every suggestion** - Review Copilot's code
- **Don't skip tests** - They prove it works
 
---
 
## 📚 Resources
 
### During Hackathon
- **Facilitators**: Raise your hand for help
- **Copilot Chat**: Your AI pair programmer
- **Project Instructions**: `.github/copilot-instructions.md`
- **Existing Code**: Learn from patterns in the codebase
 
### After Hackathon
- Full hackathon plan (sent post-session)
- Bonus challenges for continued learning
- Recording and slides
- Community channel: [#copilot-users]
- Office hours: [Schedule TBD]
 
---
 
## 🏆 Evaluation (Optional Scoring)
 
### Five Core Principles (20 points each)
 
**🔧 Works (20 points)**
- Feature is functional
- All tests pass
- Coverage >80%
 
**🔒 Secure (20 points)**
- Input validation
- Authorization checks
- No vulnerabilities
 
**📈 Scales (20 points)**
- Efficient queries
- Pagination implemented
- Performance considered
 
**📚 Maintainable (20 points)**
- Clear, readable code
- Documented patterns
- Follows conventions
 
**✨ User Delight (20 points)**
- Solves real problem
- Professional UX
- Realistic scenarios
 
**Total: 100 points**
 
---
 
## 🎉 Next Steps
 
### Tomorrow
- Apply the workflow in your daily work
- Start conversations with Copilot before coding
- Update your team's copilot-instructions.md
 
### This Week
- Complete bonus challenges
- Share learnings with colleagues
- Review and refine your documentation
 
### Ongoing
- Join the Copilot community
- Contribute patterns to copilot-instructions.md
- Mentor others on the workflow
- Keep learning and experimenting
 
---
 
## 💬 Feedback
 
**Help us improve!** Please complete the post-session survey (link provided at end).
 
Your feedback helps us make future hackathons even better.
 
---
 
**Good luck! You've got this! 🚀**
 
*Remember: Great code isn't just code that works—it's code that's secure, scales, is maintainable, and delights users.*