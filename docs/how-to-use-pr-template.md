# How to Use Setlist Studio's Enhanced PR Template & Code Review Standards

## 🎯 Quick Start Guide

### When You're Ready to Submit a PR

1. **Pre-Submission Validation**
   ```bash
   # Run these commands before creating your PR:
   dotnet test                                    # Must achieve 100% success
   dotnet test --collect:"XPlat Code Coverage"    # Check 80%+ coverage
   dotnet build --verbosity normal               # Verify zero warnings
   .\scripts\run-codeql-security.ps1            # Security analysis
   ```

2. **Create Pull Request**
   - GitHub will automatically load the enhanced PR template
   - Work through each section methodically
   - Don't skip any checkboxes - they're all mandatory

---

## 📋 Step-by-Step PR Template Usage

### Section 1: **Description & Type**
```markdown
### 📋 Description
Add song BPM validation with range checking (40-250 BPM) and user-friendly error messages for musicians.

### 🎯 Type of Change
- [x] ✨ New feature (non-breaking change which adds functionality)
- [ ] 🔒 Security enhancement
```

### Section 2: **Musical Context** 🎼
```markdown
### 🎼 Musical Context (if applicable)
- [x] Improves setlist creation process
- [x] Adds realistic musical data/validation
- [ ] Supports mobile/backstage usage
- [ ] Enhances live performance workflow

*Explanation: Validates BPM ranges match real-world music (40-250) preventing invalid entries like 999 BPM*
```

### Section 3: **Quality Checklist** ✅
**Critical - All boxes must be checked:**

#### Security & Testing (MANDATORY)
```markdown
- [x] All tests pass (`dotnet test`) - **100% success rate required**
- [x] Code coverage ≥80% for new code (line AND branch coverage)
- [x] **CodeQL security analysis passes with zero high/critical issues**
- [x] No hardcoded secrets or credentials
- [x] Input validation added for user inputs
- [x] Authorization checks verify user ownership
- [x] Security headers and rate limiting implemented where applicable
```

#### Code Quality & Standards
```markdown
- [x] Follows existing code patterns and naming conventions
- [x] Uses musician-friendly terminology (`SafeBpmAttribute`, `MusicalKey`, etc.)
- [x] Includes XML documentation for public APIs
- [x] Realistic musical data in tests/examples
- [x] **Zero build warnings** in main and test projects
- [x] CodeQL best practices followed (null safety, LINQ usage, resource disposal)
```

#### Maintainability & Business Continuity
```markdown
- [x] Code facilitates easy team handover with clear business purpose
- [x] Features clearly serve documented musician workflows
- [x] Technical decisions include business justification
- [x] New developers can understand changes within context
- [x] Dependencies prioritize long-term stability over cutting-edge features
- [x] Breaking changes documented with migration path
```

### Section 4: **NEW - Maintainability Assessment** 🔧

#### Team Handover Readiness
```markdown
- [x] New team member could understand this change within 30 minutes
- [x] Business context is clear from code and comments
- [x] No single points of failure or undocumented complexity introduced

*Explanation: Added SafeBpmAttribute with clear validation logic and authentic musical ranges*
```

#### Performance & Scalability Impact
```markdown
- [x] API endpoints respond within 500ms under normal load
- [x] Database queries complete within 100ms
- [x] Memory usage patterns considered and optimized
- [x] Scalability impact assessed for growth scenarios

*Note: Validation happens client-side first, minimal server impact*
```

#### Long-term Sustainability
```markdown
- [x] Dependencies have active communities and LTS support
- [x] Technology choices align with business continuity goals
- [x] Migration strategies considered for future growth
- [x] Monitoring and observability maintained or improved

*Uses standard .NET validation attributes - stable, well-supported technology*
```

---

## 🔍 For Code Reviewers

### Using the Code Review Standards

When reviewing a PR, follow our **three-tier review process**:

#### 1. **Security Review (BLOCKING)** 🛡️
```bash
# Check CodeQL analysis results
# Look for security-analysis.sarif with empty results array
# Verify input validation patterns
# Confirm authorization checks present
```

**Review Checklist:**
- [ ] CodeQL security analysis shows zero high/critical issues
- [ ] All user inputs have validation attributes
- [ ] Authorization verifies user ownership
- [ ] No hardcoded secrets or credentials
- [ ] Security headers configured appropriately

#### 2. **Technical Excellence Review** 🎯
```bash
# Verify test results and coverage
# Check for build warnings
# Validate performance impact
# Review code patterns and quality
```

**Review Checklist:**
- [ ] All tests pass (100% success rate)
- [ ] Code coverage ≥80% line and branch
- [ ] Zero build warnings
- [ ] Performance standards met (<500ms API, <100ms DB)
- [ ] CodeQL best practices followed

#### 3. **Maintainability Assessment** 🔄
```bash
# Evaluate team handover readiness
# Check business alignment
# Assess long-term sustainability
# Review documentation quality
```

**Review Checklist:**
- [ ] Code is self-documenting with clear business purpose
- [ ] Features serve documented musician workflows
- [ ] Technology choices prioritize stability
- [ ] Decision rationale documented

---

## 🎵 Example PR Scenarios

### Scenario 1: Adding BPM Validation
```markdown
## 🎵 Setlist Studio Pull Request

### 📋 Description
Add SafeBpmAttribute validation for song BPM values with authentic musical ranges (40-250).

### 🎯 Type of Change
- [x] ✨ New feature

### 🎼 Musical Context
- [x] Adds realistic musical data/validation
*Prevents invalid BPM entries like 999 - uses authentic ranges from ballads (60-80) to fast songs (170+)*

### ✅ Quality Checklist
*All boxes checked with evidence...*

### 🔧 Maintainability Assessment
#### Team Handover Readiness
- [x] Business context clear: Musicians need authentic BPM validation
- [x] Implementation simple: Standard .NET validation attribute pattern
- [x] No complexity added: Follows existing validation conventions
```

### Scenario 2: Security Enhancement
```markdown
### 📋 Description
Implement rate limiting for authentication endpoints (5 requests/minute) to prevent brute force attacks.

### 🎯 Type of Change
- [x] 🔒 Security enhancement

### 🛡️ Security Review Required
- [x] This PR modifies authentication/authorization logic
- [x] This PR adds new security controls

### 🔧 Maintainability Assessment
#### Long-term Sustainability
- [x] Uses Microsoft.AspNetCore.RateLimiting (LTS supported)
- [x] Standard security pattern - well documented
- [x] Monitoring integrated for security events
```

---

## 🎯 Common Gotchas & Tips

### ❌ **DON'T Do This:**
```markdown
- [ ] All tests pass
*"Tests mostly pass, just one flaky test"* ❌

- [ ] CodeQL analysis passes
*"Only 2 medium security issues"* ❌

- [ ] Uses realistic musical data
*"BPM validation allows 1-9999 range"* ❌
```

### ✅ **DO This Instead:**
```markdown
- [x] All tests pass (`dotnet test`)
*"All 4000 tests pass with 0 failures - evidence: test output attached"* ✅

- [x] **CodeQL security analysis passes with zero high/critical issues**
*"security-analysis.sarif shows empty results array"* ✅

- [x] Realistic musical data in tests/examples
*"BPM validation uses 40-250 range matching real music from ballads to speed metal"* ✅
```

### 💡 **Pro Tips:**
1. **Fill out sections as you code** - don't wait until the end
2. **Include evidence** - link to test results, coverage reports, CodeQL output
3. **Be specific about musical context** - explain how features serve real musician needs
4. **Document trade-offs** - explain technical decisions from business perspective

---

## 🚀 Integration with Development Workflow

### Before You Start Coding
1. Read [CONTRIBUTING.md](CONTRIBUTING.md) for complete setup
2. Review [CODE_REVIEW_STANDARDS.md](.github/CODE_REVIEW_STANDARDS.md) for requirements
3. Check existing patterns in the codebase

### During Development
1. Follow security-first approach (validation, authorization, CodeQL)
2. Write tests that achieve 80%+ coverage
3. Use musician-friendly terminology and realistic data
4. Document complex business logic

### Before Creating PR
```bash
# Complete pre-submission checklist
dotnet test                                    # 100% success required
dotnet test --collect:"XPlat Code Coverage"    # 80%+ coverage
dotnet build --verbosity normal               # Zero warnings
.\scripts\run-codeql-security.ps1            # Zero security issues

# Prepare PR evidence
# - Test output showing 100% success
# - Coverage report showing 80%+
# - CodeQL results showing zero security issues
# - Performance testing results if applicable
```

### After Submitting PR
1. **Respond to Review Feedback Promptly**
2. **Address All Required Changes** - don't skip any reviewer requests
3. **Update Documentation** if changes affect setup or usage
4. **Celebrate Success** 🎉 when merged!

---

This enhanced PR template and review process ensures every contribution to Setlist Studio maintains our high standards for security, quality, maintainability, and musician-focused excellence! 🎸