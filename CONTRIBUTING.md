# Contributing to Setlist Studio 🎵

Welcome to Setlist Studio! We're building a tool that musicians rely on for their performances, so every contribution should help create a reliable, secure, and delightful experience for artists sharing their music with the world.

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- Visual Studio Code or Visual Studio 2022
- Docker (optional, for containerized development)
- CodeQL CLI (for security analysis)

### Setup
1. **Clone the repository**
   ```bash
   git clone https://github.com/eugenecp/setlist-studio.git
   cd setlist-studio
   ```

2. **Run tests to verify setup**
   ```bash
   dotnet test
   ```
   - **Requirement**: All tests must pass (100% success rate)

3. **Generate coverage report**
   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```
   - **Requirement**: Understand current coverage baseline

4. **Run security analysis**
   ```powershell
   .\scripts\run-codeql-security.ps1
   ```
   - **Requirement**: Zero high/critical security issues

## 🎯 Contribution Guidelines

### 1. **Security First Development**
Before writing any code, ensure you understand our security requirements:
- **Input Validation**: All user inputs must be validated and sanitized
- **Authorization**: Every data access must verify user ownership
- **CodeQL Compliance**: Zero tolerance for high/critical security issues
- **Secrets Management**: Never hardcode credentials or sensitive data

### 2. **Test Organization Standards**
Follow our strict test file naming conventions:
- **Base Tests**: `{SourceClass}Tests.cs` for core functionality
- **Advanced Tests**: `{SourceClass}AdvancedTests.cs` for edge cases (only when base file >1,400 lines)
- **NEVER use custom names** like "FocusedTests" or "CoverageTests"

### 3. **Quality Requirements**
Every contribution must meet these non-negotiable standards:
- **100% Test Success**: Zero failing tests allowed
- **80%+ Coverage**: Line AND branch coverage for new code
- **Zero Build Warnings**: Clean builds in main and test projects
- **Performance Standards**: <500ms API responses, <100ms DB queries

### 4. **Maintainability Focus**
Write code that facilitates easy team handover:
- **Clear Business Purpose**: Use musician-friendly terminology
- **Self-Documenting**: Code should be understandable within 30 minutes
- **Sustainable Technology**: Prioritize stability over cutting-edge features
- **Decision Documentation**: Include business justification for complex logic

## 📋 Development Process

### Branch Strategy
```bash
# Create feature branch from main
git checkout main
git pull origin main
git checkout -b feature/[issue-number]-[short-description]

# Example:
git checkout -b feature/123-song-key-validation
```

### Development Workflow
1. **Review Requirements**
   - Read related GitHub issues
   - Understand musician workflow impact
   - Check existing patterns and conventions

2. **Security-First Implementation**
   - Implement input validation first
   - Add authorization checks
   - Configure security headers where needed
   - Run CodeQL analysis: `.\scripts\run-codeql-security.ps1`

3. **Test-Driven Development**
   - Check if base test file exists: Use VS Code file search for `{SourceClass}Tests.cs`
   - Write tests first (TDD approach)
   - Ensure 80%+ line and branch coverage
   - Include security test cases

4. **Code Quality Validation**
   ```bash
   # Run all tests (must be 100% success)
   dotnet test
   
   # Generate coverage report
   dotnet test --collect:"XPlat Code Coverage"
   
   # Check for build warnings (must be zero)
   dotnet build --verbosity normal
   
   # Run security analysis (must pass)
   .\scripts\run-codeql-security.ps1
   ```

### Commit Standards
Use conventional commit format:
```bash
git commit -m "feat: add BPM validation for song creation

- Add SafeBpmAttribute for 40-250 range validation
- Include realistic musical examples in tests
- Ensure authorization checks for user ownership
- Zero CodeQL security issues verified

Closes #123"
```

## 🔍 Pull Request Process

### 1. **Pre-Submission Checklist**
Complete our [Pull Request Template](.github/PULL_REQUEST_TEMPLATE.md) including:
- [ ] All tests pass with 80%+ coverage
- [ ] CodeQL security analysis passes with zero high/critical issues
- [ ] Zero build warnings
- [ ] Security validation checklist completed
- [ ] Maintainability assessment completed

### 2. **Code Review Process**
All changes go through our [Code Review Standards](.github/CODE_REVIEW_STANDARDS.md):
- **Security Review**: Mandatory security validation (blocking)
- **Technical Review**: Code quality and performance assessment
- **Maintainability Review**: Business continuity and team handover assessment
- **Musical Context**: Workflow alignment and user experience validation

### 3. **Review Criteria**
- **APPROVE**: All standards met, clear business value
- **REQUEST CHANGES**: Issues identified that need resolution
- **BLOCK**: Security vulnerabilities or systematic failures

## 🎼 Musical Context Guidelines

### Use Realistic Musical Data
When creating examples, tests, or documentation, use authentic musical data:

**Song Examples:**
- "Sweet Child O' Mine" by Guns N' Roses (BPM: 125, Key: D)
- "Billie Jean" by Michael Jackson (BPM: 117, Key: F#m)
- "Take Five" by Dave Brubeck (BPM: 176, Key: Bb)
- "The Thrill Is Gone" by B.B. King (BPM: 98, Key: Bm)

**BPM Ranges:**
- Ballads: 60-80 BPM
- Medium Tempo: 90-120 BPM
- Up-tempo: 130-160 BPM
- Fast Songs: 170+ BPM

**Common Keys:**
- Guitar-friendly: E, A, D, G, C
- Vocal-friendly: F, Bb, Eb, Ab
- Minor keys: Am, Em, Bm, F#m, Cm

### Musician Workflow Focus
Every feature should clearly serve real musician needs:
- **Live Performance**: Consider backstage environments and quick access needs
- **Setlist Creation**: Support natural song organization and transition planning
- **Collaboration**: Enable sharing with band members, venues, and sound engineers
- **Mobile Usage**: Optimize for tablets and phones used during performances

## 🛡️ Security Requirements

### Mandatory Security Standards
- **CodeQL Analysis**: Zero high/critical security issues
- **Input Validation**: All user inputs validated with regex patterns
- **Authorization**: User ownership verification for all resources
- **Security Headers**: X-Content-Type-Options, X-Frame-Options, CSP
- **Rate Limiting**: API endpoints (100/min), Auth endpoints (5/min)
- **HTTPS**: Enforced in production with HSTS headers

### Security Testing
```bash
# Run security-focused analysis
.\scripts\run-codeql-security.ps1

# Verify zero security issues
# Check security-analysis.sarif for empty results array

# Run comprehensive analysis (optional)
.\scripts\run-codeql-comprehensive.ps1
```

### Common Security Patterns
```csharp
// CORRECT: Input validation and authorization
[SafeBpm(40, 250)]
public int Bpm { get; set; }

var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();
var userSong = await _service.GetByUserIdAsync(userId, songId);
if (userSong == null) throw new ForbiddenException();

// CORRECT: Secure error handling
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed for user {UserId}", userId);
    return Problem("An error occurred processing your request");
}
```

## 📊 Performance Standards

### Response Time Requirements
- **API Endpoints**: <500ms under normal load
- **Database Queries**: <100ms for user data queries
- **Page Load Times**: <2 seconds for Blazor Server pages
- **Memory Usage**: <4MB per concurrent user connection

### Performance Testing
```bash
# Run performance tests
dotnet run --project tests/SetlistStudio.PerformanceTests

# Monitor database performance
dotnet ef dbcontext optimize --startup-project src/SetlistStudio.Web

# Generate performance reports
.\scripts\run-performance-tests.ps1
```

## 🤝 Getting Help

### Resources
- **[Copilot Instructions](.github/copilot-instructions.md)**: Comprehensive development guidelines
- **[Code Review Standards](.github/CODE_REVIEW_STANDARDS.md)**: Detailed review requirements
- **[Security Documentation](SECURITY.md)**: Security policies and procedures
- **GitHub Issues**: Report bugs or request features

### Contact
- **GitHub Issues**: For bugs, feature requests, and technical discussions
- **Pull Request Reviews**: For code-specific feedback and guidance
- **Documentation Updates**: Submit PRs for any unclear or outdated information

## 🎵 Project Philosophy

### Our Mission
Setlist Studio helps musicians organize their performances and manage their repertoire. Every technical decision should serve this core purpose and enhance the creative process.

### Quality Standards
- **Reliability**: Musicians depend on our tool during live performances
- **Security**: Protect user data and prevent vulnerabilities
- **Maintainability**: Enable seamless team transitions and sustainable growth
- **User Experience**: Intuitive workflows that match how musicians think about their music

### Maintainability Principles
- **Team Handover Excellence**: All code enables smooth knowledge transfer
- **Business Alignment**: Features clearly serve musician workflows
- **Sustainable Technology**: Long-term stability over cutting-edge features
- **Documentation Quality**: Technical decisions include business justification

---

## 📝 Contribution Checklist

**Before submitting any contribution:**

### Development Setup
- [ ] Repository cloned and development environment working
- [ ] All tests pass (`dotnet test`) - 100% success required
- [ ] Coverage baseline understood
- [ ] CodeQL security analysis runs successfully

### Security Compliance
- [ ] Input validation implemented for all user inputs
- [ ] Authorization checks added for data access
- [ ] Security headers configured where applicable
- [ ] CodeQL analysis passes with zero high/critical issues
- [ ] No hardcoded secrets or credentials

### Code Quality
- [ ] Test file naming follows `{SourceClass}Tests.cs` convention
- [ ] 80%+ line and branch coverage achieved
- [ ] Zero build warnings in main and test projects
- [ ] Realistic musical data used in examples and tests
- [ ] XML documentation added for public APIs

### Maintainability
- [ ] Code uses musician-friendly terminology
- [ ] Business purpose is clear and self-documenting
- [ ] Complex logic includes explanatory comments
- [ ] Technology choices prioritize long-term sustainability
- [ ] Features clearly serve documented musician workflows

### Performance
- [ ] API endpoints respond within 500ms
- [ ] Database queries complete within 100ms
- [ ] Memory usage optimized with proper resource disposal
- [ ] Async/await used consistently for I/O operations

### Documentation
- [ ] README updated if needed
- [ ] Breaking changes documented with migration path
- [ ] Decision records added for architectural changes
- [ ] Pull request template completed thoroughly

---

**Thank you for contributing to Setlist Studio! Together, we're building a tool that helps musicians share their passion with the world.** 🎸