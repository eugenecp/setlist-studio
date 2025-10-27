# Code Review Standards for Setlist Studio 🎵

## Overview

This document establishes comprehensive code review standards for Setlist Studio, ensuring every contribution maintains our high standards for security, maintainability, performance, and user experience. Our code review process is designed to facilitate seamless team handovers and sustainable business growth while serving the real-world needs of musicians.

## 🎯 Core Review Principles

### 1. **Security First - Zero Tolerance**
- **MANDATORY**: Zero high/critical CodeQL security issues
- **Input Validation**: All user inputs must be validated and sanitized
- **Authorization**: Every data access must verify user ownership
- **Secrets Management**: No hardcoded credentials or sensitive data

### 2. **Maintainability & Business Continuity**
- **Team Handover**: Code must enable smooth knowledge transfer
- **Business Context**: Technical decisions include business justification
- **Long-term Sustainability**: Technology choices prioritize stability
- **Musician-Focused**: Features clearly serve documented musician workflows

### 3. **Technical Excellence**
- **100% Test Success**: Zero failing tests allowed
- **80%+ Coverage**: Line AND branch coverage for new code
- **Zero Build Warnings**: Clean builds in main and test projects
- **Performance Standards**: <500ms API responses, <100ms DB queries

---

## 📋 Review Checklist

### 🔒 Security Review (MANDATORY - BLOCKING)

#### CodeQL Analysis
- [ ] **CodeQL security analysis passes** with zero high/critical issues
  - Run: `codeql database analyze codeql-database --output=security-analysis.sarif codeql/csharp-security-extended.qls`
  - Verify: Empty results array in SARIF file
- [ ] **Distinguish security vs quality findings** - Focus on security-specific results
- [ ] **Address all security vulnerabilities** before approving

#### Input Validation & Authorization
- [ ] **All user inputs validated** with appropriate regex patterns and length limits
- [ ] **Authorization checks present** for all data access operations
- [ ] **User ownership verification** implemented for resource access
- [ ] **Parameterized queries only** - no string concatenation with user input
- [ ] **Error messages secure** - no sensitive information leakage

#### Security Configuration
- [ ] **Security headers configured** (X-Content-Type-Options, X-Frame-Options, etc.)
- [ ] **Rate limiting applied** to API endpoints (100/min API, 5/min Auth)
- [ ] **CSRF protection enabled** for state-changing operations
- [ ] **Secrets in environment variables** or Key Vault (never hardcoded)
- [ ] **HTTPS enforced** in production configurations

### 🧪 Testing & Quality Review (MANDATORY)

#### Test Coverage & Success
- [ ] **100% test success rate** - zero failing tests allowed
- [ ] **80%+ line coverage** for new code files
- [ ] **80%+ branch coverage** for new code files
- [ ] **Test file naming compliance** - `{SourceClass}Tests.cs` or `{SourceClass}AdvancedTests.cs`
- [ ] **Realistic test data** using authentic musical examples

#### Code Quality Standards
- [ ] **Zero build warnings** in main and test projects
- [ ] **CodeQL best practices followed** (null safety, LINQ usage, resource disposal)
- [ ] **XML documentation** added for public APIs
- [ ] **Musician-friendly terminology** used consistently
- [ ] **Existing patterns followed** for consistency

### 🎼 Maintainability Review

#### Team Handover Assessment
- [ ] **Clear business purpose** - new developers can understand intent within 30 minutes
- [ ] **Self-documenting code** with musician-focused variable and method names
- [ ] **Decision documentation** for complex business logic
- [ ] **No single points of failure** introduced without documentation

#### Business Continuity Impact
- [ ] **Features serve musician workflows** with clear traceability to user stories
- [ ] **Technology choices sustainable** - prioritize LTS versions and stable dependencies
- [ ] **Migration strategies considered** for future growth scenarios
- [ ] **Dependency risk assessed** - active communities and long-term support

#### Knowledge Transfer Quality
- [ ] **Onboarding impact minimal** - changes don't significantly increase complexity
- [ ] **Documentation updated** including README, decision records, migration guides
- [ ] **Breaking changes documented** with clear migration paths
- [ ] **Context preservation** - business rationale included in comments

### 🚀 Performance & Scalability Review

#### Performance Standards
- [ ] **API response times <500ms** under normal load conditions
- [ ] **Database queries <100ms** completion time
- [ ] **Memory usage optimized** with proper resource disposal
- [ ] **Async/await used consistently** for I/O operations

#### Scalability Considerations
- [ ] **Database indexes appropriate** for user-specific queries
- [ ] **Pagination implemented** for large datasets
- [ ] **Caching strategy considered** for expensive operations
- [ ] **Connection pooling configured** for high-concurrency scenarios

#### Resource Management
- [ ] **IDisposable objects properly disposed** using `using` statements
- [ ] **Memory leaks prevented** with proper cleanup patterns
- [ ] **Background processing considered** for heavy operations
- [ ] **Load testing impact assessed** for new features

---

## 🔍 Review Process

### 1. **Pre-Review Validation**

**Author Responsibilities:**
- [ ] All quality checklist items completed
- [ ] Local CodeQL security analysis passed
- [ ] All tests passing with adequate coverage
- [ ] Security validation checklist completed
- [ ] Performance impact assessed

**Reviewer Preparation:**
- [ ] Review related issues and business context
- [ ] Understand feature requirements and user stories
- [ ] Check CI/CD pipeline status and quality gates
- [ ] Review automated security and quality scans

### 2. **Security-First Review**

**Critical Security Assessment:**
1. **CodeQL Analysis**: Verify zero high/critical security issues
2. **Input Validation**: Check all user input handling
3. **Authorization**: Verify proper access controls
4. **Data Protection**: Ensure no sensitive data exposure
5. **Configuration Security**: Review security headers and settings

**Security Review Outcomes:**
- **BLOCK**: Any high/critical security issues found
- **REQUEST CHANGES**: Medium security issues or missing validation
- **APPROVE**: All security requirements met

### 3. **Technical Excellence Review**

**Code Quality Assessment:**
1. **Test Coverage**: Verify 80%+ line and branch coverage
2. **Build Quality**: Confirm zero warnings
3. **Performance**: Check response times and resource usage
4. **Patterns**: Ensure consistency with existing codebase
5. **Documentation**: Validate XML docs and comments

### 4. **Maintainability Assessment**

**Business Continuity Review:**
1. **Team Handover**: Assess knowledge transfer readiness
2. **Business Alignment**: Verify features serve musician needs
3. **Sustainability**: Check dependency and technology choices
4. **Documentation**: Review decision records and guides

**Long-term Impact:**
1. **Complexity Assessment**: Avoid unnecessary complexity
2. **Migration Planning**: Consider future scalability needs
3. **Onboarding Impact**: Minimize learning curve for new developers
4. **Knowledge Distribution**: Prevent single points of failure

---

## 🎵 Musical Context & User Experience

### Musician-Centric Review
- [ ] **Realistic musical data** used in examples and tests
- [ ] **Performance workflow alignment** - features support live performance needs
- [ ] **Mobile/backstage optimization** for actual usage scenarios
- [ ] **Terminology accuracy** - uses authentic music industry terms

### User Experience Standards
- [ ] **Intuitive navigation** matching musician mental models
- [ ] **Fast data entry** workflows for quick setlist modifications
- [ ] **Professional presentation** suitable for venue/collaborator sharing
- [ ] **Offline capabilities** for critical performance features

---

## 📊 Quality Metrics & Gates

### Technical Health Indicators (BLOCKING)
- **Build Success Rate**: >99% successful CI/CD runs
- **Test Success**: 100% of tests must pass - zero tolerance for failures
- **Code Coverage**: ≥80% line AND branch coverage maintained
- **Security Posture**: Zero high/critical security vulnerabilities
- **Build Quality**: Zero build warnings in main and test projects

### Performance Requirements (BLOCKING)
- **API Response Times**: <500ms for all endpoints under normal load
- **Database Query Performance**: <100ms for user data queries
- **Page Load Times**: <2 seconds for Blazor Server pages
- **Memory Usage**: <4MB per concurrent user connection

### Business Continuity Metrics (ADVISORY)
- **Onboarding Time**: New developer productivity within 2-3 days
- **Feature Delivery Consistency**: Maintain development velocity
- **Documentation Currency**: All guides work with current codebase
- **Dependency Health**: Regular security updates and deprecation monitoring

---

## 🚦 Review Decision Guidelines

### ✅ **APPROVE** Criteria
- All security requirements met (zero high/critical issues)
- All tests passing with adequate coverage
- Zero build warnings
- Performance standards met
- Maintainability standards satisfied
- Clear business value and musician focus

### 🔄 **REQUEST CHANGES** Criteria
- Security issues present (any severity)
- Test failures or inadequate coverage
- Build warnings present
- Performance standards not met
- Maintainability concerns identified
- Missing documentation or context

### ❌ **BLOCK** Criteria
- High/critical security vulnerabilities
- Systematic test failures
- Introduces breaking changes without migration path
- Violates core architectural principles
- Significantly increases complexity without business justification

---

## 🛠️ Tools & Automation

### Required Tools
- **CodeQL**: Static security analysis (`codeql database analyze`)
- **Coverlet**: Code coverage analysis (`dotnet test --collect:"XPlat Code Coverage"`)
- **SonarQube**: Additional code quality analysis
- **Dependabot**: Dependency security updates

### Automated Checks
- **GitHub Actions**: CI/CD pipeline with quality gates
- **Security Scanning**: Automatic vulnerability detection
- **Code Coverage**: Automated coverage reporting
- **Performance Testing**: Basic performance validation

### Manual Review Areas
- **Business Logic**: Complex musical workflow implementations
- **Security Architecture**: Authentication and authorization patterns
- **User Experience**: Mobile/performance scenario validation
- **Maintainability**: Team handover and knowledge transfer assessment

---

## 📚 Resources & References

### Internal Documentation
- [Copilot Instructions](/.github/copilot-instructions.md) - Comprehensive development guidelines
- [Security Standards](/#security-standards--guidelines) - Detailed security requirements
- [Testing Framework](/#testing-framework) - Test organization and coverage standards
- [Performance Standards](/#performance--scalability) - Response time and resource requirements

### External Standards
- [OWASP Top 10](https://owasp.org/www-project-top-ten/) - Security vulnerability categories
- [Microsoft Security Development Lifecycle](https://www.microsoft.com/en-us/securityengineering/sdl/) - Secure development practices
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Architectural principles
- [CodeQL Documentation](https://codeql.github.com/docs/) - Static analysis best practices

---

## 🎸 Review Culture & Philosophy

### Collaborative Excellence
- **Constructive Feedback**: Focus on code improvement, not personal criticism
- **Learning Opportunities**: Share knowledge and best practices
- **Business Value**: Always connect technical decisions to musician needs
- **Continuous Improvement**: Regular retrospectives on review process effectiveness

### Quality Mindset
- **Security First**: Never compromise on security requirements
- **Musician Focus**: Every feature should clearly serve musical workflows
- **Long-term Thinking**: Consider maintainability and business continuity
- **Professional Standards**: Code should reflect the quality musicians expect

---

*Remember: We're building a tool that musicians will rely on for their performances. Every review should ensure we're creating a reliable, secure, and delightful experience for artists sharing their music with the world.* 🎵