## 🎵 Setlist Studio Pull Request

### 📋 Description
<!-- Brief description of changes and why they're needed -->

### 🎯 Type of Change
- [ ] 🐛 Bug fix (non-breaking change which fixes an issue)
- [ ] ✨ New feature (non-breaking change which adds functionality)
- [ ] 💥 Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] 📚 Documentation update
- [ ] 🔒 Security enhancement
- [ ] 🎵 Musical workflow improvement

### 🎼 Musical Context (if applicable)
<!-- How does this change improve the musician experience? -->
- [ ] Improves live performance workflow
- [ ] Enhances setlist creation process
- [ ] Adds realistic musical data/validation
- [ ] Supports mobile/backstage usage

### ✅ Quality Checklist

#### Security & Testing (MANDATORY)
- [ ] All tests pass (`dotnet test`) - **100% success rate required**
- [ ] Code coverage ≥80% for new code (line AND branch coverage)
- [ ] **CodeQL security analysis passes with zero high/critical issues**
- [ ] No hardcoded secrets or credentials
- [ ] Input validation added for user inputs
- [ ] Authorization checks verify user ownership
- [ ] Security headers and rate limiting implemented where applicable

#### Code Quality & Standards
- [ ] Follows existing code patterns and naming conventions
- [ ] Uses musician-friendly terminology (`SetlistSong`, `PerformanceNotes`, etc.)
- [ ] Includes XML documentation for public APIs
- [ ] Realistic musical data in tests/examples
- [ ] **Zero build warnings** in main and test projects
- [ ] CodeQL best practices followed (null safety, LINQ usage, resource disposal)

#### Maintainability & Business Continuity
- [ ] Code facilitates easy team handover with clear business purpose
- [ ] Features clearly serve documented musician workflows
- [ ] Technical decisions include business justification
- [ ] New developers can understand changes within context
- [ ] Dependencies prioritize long-term stability over cutting-edge features
- [ ] Breaking changes documented with migration path

#### Documentation & Knowledge Transfer
- [ ] README updated if needed
- [ ] Decision records added for architectural changes
- [ ] Code is self-documenting with musician-focused terminology
- [ ] Complex business logic includes explanatory comments

### 🧪 Testing Details
<!-- Describe the tests you ran to verify your changes -->

### 📱 Mobile/Performance Testing
<!-- For UI changes: tested on mobile devices or performance scenarios? -->

### 🔧 Maintainability Assessment

#### Team Handover Readiness
- [ ] New team member could understand this change within 30 minutes
- [ ] Business context is clear from code and comments
- [ ] No single points of failure or undocumented complexity introduced

#### Performance & Scalability Impact
- [ ] API endpoints respond within 500ms under normal load
- [ ] Database queries complete within 100ms
- [ ] Memory usage patterns considered and optimized
- [ ] Scalability impact assessed for growth scenarios

#### Long-term Sustainability
- [ ] Dependencies have active communities and LTS support
- [ ] Technology choices align with business continuity goals
- [ ] Migration strategies considered for future growth
- [ ] Monitoring and observability maintained or improved

### 🔗 Related Issues
<!-- Link any related GitHub issues -->
Closes #

### 🎵 Screenshots/Demo
<!-- Add screenshots or demo GIFs for UI changes -->

---

### 🛡️ Security Review Required
- [ ] This PR contains security-sensitive changes
- [ ] This PR modifies authentication/authorization logic
- [ ] This PR changes data validation or sanitization
- [ ] This PR adds new API endpoints

---

**Ready for Review**: Please ensure all checkboxes are complete before requesting review.

*Every line of code should contribute to creating a reliable, secure, and delightful experience for artists sharing their music with the world.* 🎸