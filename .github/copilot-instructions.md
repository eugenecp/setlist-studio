# Copilot Instructions for Setlist Studio

## Quick Reference

### Essential Rules
- **Test Naming**: `{SourceClass}Tests.cs` (base) or `{SourceClass}AdvancedTests.cs` (advanced only)
- **Coverage Target**: 80%+ line and branch coverage
- **Security Analysis**: All code must pass CodeQL security scans with zero high/critical issues
- **Architecture**: Clean Architecture (Core/Infrastructure/Web)
- **Framework**: .NET 8 + Blazor Server + MudBlazor + xUnit

### CRITICAL: Test File Creation Workflow
1. **Check existing**: Use `file_search` for `{SourceClass}Tests.cs`
2. **Enhance first**: Add to base test file before creating new ones
3. **Strict naming**: Only `Tests.cs` or `AdvancedTests.cs` suffixes
4. **No custom names**: Never use "FocusedTests", "CoverageTests", etc.

---

## Project Architecture

**Setlist Studio** is a music management application for musicians to organize performances and manage their repertoire.

### Core Features
- **Song Management**: Artists, songs, metadata (BPM, keys, genres)
- **Setlist Creation**: Performance planning with song order and transitions
- **User Authentication**: Secure multi-user access with OAuth providers

### Architecture Layers
- **SetlistStudio.Core**: Domain entities, interfaces, business logic
- **SetlistStudio.Infrastructure**: Data access, Entity Framework, services
- **SetlistStudio.Web**: Blazor Server UI, controllers, authentication

### Technology Stack
- **.NET 8**: Framework with modern C# features
- **Blazor Server**: Real-time interactive web UI
- **Entity Framework Core**: ORM with SQLite/SQL Server
- **ASP.NET Core Identity**: Authentication with OAuth (Google, Microsoft, Facebook)
- **MudBlazor**: Material Design component library
- **xUnit + FluentAssertions + Bunit**: Testing framework
- **CodeQL**: Static application security testing (SAST) for vulnerability detection
- **Docker**: Containerization for deployment
- **GitHub Actions**: CI/CD pipeline

### Quality Standards
- **Reliability**: Comprehensive testing with graceful error handling
- **Scalability**: Efficient queries, pagination, caching for growth
- **Security**: OAuth authentication, input validation, no hardcoded secrets
- **Maintainability**: Clean code, clear documentation, consistent patterns, business continuity focus
- **User Experience**: Realistic musical data, smooth interactions
- **Code Quality**: Zero build warnings in main and test projects

### Customer Delight Assessment Framework

**When reviewing this project, always evaluate from a customer and stakeholder perspective — not as a developer.**

Assess the project for customer delight by focusing on the experience and value it provides to end users, not the technical implementation.

#### Customer Delight Evaluation Criteria

**User Problem & Value Proposition:**
- Does the product clearly solve a real user or business problem?
- Is the solution aligned with actual musician workflows and performance needs?
- Does it address genuine pain points in setlist management and music organization?

**User Experience Quality:**
- Is it intuitive, easy to use, and visually pleasing?
- Are there moments of delight — small touches that make it feel polished or special?
- Does it feel reliable, responsive, and professional?
- Would end users feel confident and satisfied using it daily?

**Communication & Clarity:**
- Does it communicate its purpose and value clearly to non-technical users?
- Are features discoverable and self-explanatory to musicians?
- Is the interface terminology familiar to music industry professionals?

**Friction Analysis:**
- Are there any friction points, confusing steps, or unmet expectations?
- Does the workflow match how musicians naturally organize and perform music?
- Are there barriers to adoption or daily usage?

#### Assessment Deliverables

When evaluating customer delight, always provide:

1. **User Experience Summary**: A concise overview of the overall user experience quality
2. **Delight Strengths**: Specific features, interactions, or design elements that contribute to customer satisfaction
3. **Improvement Opportunities**: Concrete areas where user satisfaction could be enhanced
4. **Customer Delight Rating**: A 1-10 rating of how much delight this product likely brings to its users
5. **Usage Confidence**: Assessment of whether users would feel confident using this product in professional settings

#### Focus Areas for Customer Assessment

**Performance Context**: Always consider real-world usage scenarios:
- Musicians using the app backstage before performances
- Quick setlist adjustments during sound checks
- Collaborative planning with band members
- Professional presentation to venue coordinators and sound engineers

**User Journey Evaluation**: Assess the complete user experience:
- First-time setup and onboarding experience
- Daily workflow efficiency and ease of use
- Error handling and recovery scenarios
- Mobile and tablet usage in performance environments

**Professional Standards**: Evaluate against industry expectations:
- Does it meet the reliability standards musicians expect for professional tools?
- Is the interface polished enough for client-facing scenarios?
- Does it integrate well with existing musician workflows and tools?

**Remember**: Focus on how it feels to use, not how it's coded. The best technical implementation means nothing if users don't find it delightful, reliable, and valuable for their creative work.

### Maintainability & Business Continuity Standards
- **Team Handover Readiness**: All code must facilitate smooth knowledge transfer to new developers
- **Business Alignment**: Features must clearly serve musician workflows and creative processes
- **Documentation Quality**: Technical decisions must be explained from business impact perspective
- **Dependency Management**: Technology choices prioritize long-term sustainability over cutting-edge features
- **Onboarding Efficiency**: New team members should be productive within days, not months
- **Creative Industry Focus**: All development decisions must consider real-world music performance needs

---

## Testing Framework

### Coverage Standards

Setlist Studio maintains **100% test success rate requirement** with minimum 80% code coverage for both line and branch coverage at file and project levels.

**PRIORITY: Individual File Coverage First**
- **Target each file to 80%+ line AND branch coverage before moving to the next file**
- **Focus on files closest to 80% threshold first** (e.g., 75%+ files get priority)
- **Complete one file at a time** rather than spreading effort across multiple files
- **Use file-specific coverage analysis** to identify exact uncovered lines and branches
- **Create targeted tests** for specific line coverage gaps and branch conditions
- **Verify both line and branch coverage targets are met before proceeding**

**Quality Metrics Requirements:**
- **Test Success Rate**: **100% of all tests must pass** - zero tolerance for failing tests
- **Build Quality**: **Zero build warnings** in main and test projects - clean builds required
- **Security Analysis**: **Zero high/critical CodeQL security issues** - all security vulnerabilities must be resolved
- **Individual File Coverage**: **Each file must achieve at least 80% line AND branch coverage before moving to next file**
- **Line Coverage**: Each file must achieve at least 80% line coverage
- **Branch Coverage**: Each file must achieve at least 80% branch coverage
- **Project Coverage**: Overall project must maintain at least 80% line and branch coverage
- **CRAP Score**: All methods must maintain passing CRAP scores
- **Cyclomatic Complexity**: All methods must maintain passing complexity metrics
- **Test Reliability**: All tests must be deterministic and pass consistently

### Test Framework Requirements

- **xUnit**: Primary testing framework for all unit and integration tests
- **Moq**: For creating mocks and stubs of dependencies
- **FluentAssertions**: For readable, expressive test assertions
- **Bunit**: For Blazor component testing

### Test File Organization

Setlist Studio follows a strategic test organization approach that separates core functionality tests from specialized coverage and edge case tests.

#### MANDATORY TEST FILE NAMING CONVENTIONS

**STRICT ENFORCEMENT REQUIRED - NO EXCEPTIONS:**

1. **ALWAYS check if base test file exists FIRST**
2. **NEVER create custom-named test files** (e.g., "FocusedTests", "CoverageTests", "SpecializedTests")  
3. **FOLLOW EXACT naming pattern** - one source class = one test class

#### Test File Structure

- **Base Test Files** (e.g., `SetlistServiceTests.cs`): Core functionality and primary business logic scenarios
- **Advanced Test Files** (e.g., `SetlistServiceAdvancedTests.cs`): Edge cases, error conditions, validation boundaries
- **Specialized Test Files** (e.g., `ProgramAdvancedTests.cs`): Environment-specific configurations, startup logic

#### Naming Conventions

**REQUIRED NAMING PATTERN - NEVER DEVIATE:**
- **Source File**: `{ClassName}.cs` → **Test File**: `{ClassName}Tests.cs`
- **Advanced Tests**: `{SourceClass}AdvancedTests.cs`
- **Razor Component**: `{ComponentName}.razor` → **Test File**: `{ComponentName}Tests.cs`

**CORRECT Examples:**
- `MainLayout.razor` → `MainLayoutTests.cs`
- `SetlistService.cs` → `SetlistServiceTests.cs`
- `Program.cs` → `ProgramTests.cs`

**PROHIBITED Examples:**
- `MainLayoutFocusedTests.cs` ← WRONG
- `MainLayoutCoverageTests.cs` ← WRONG
- `SetlistServiceUnitTests.cs` ← WRONG
- `ProgramConfigurationTests.cs` ← WRONG

#### Test File Creation Workflow - MANDATORY STEPS

**STEP 1: Always Check Base Test File First**
```bash
# Use file_search tool in VS Code or:
# Linux/Mac: find . -name "{ClassName}Tests.cs"  
# Windows: Get-ChildItem -Recurse -Name "*{ClassName}Tests.cs"
# VS Code: Use file_search tool with "{ClassName}Tests.cs"
```

**STEP 2: Determine Appropriate Action**
- **If base test exists**: Enhance existing `{ClassName}Tests.cs` with core functionality tests
- **If base test missing**: Create `{ClassName}Tests.cs` for core functionality FIRST
- **If base test >1,400 lines**: ONLY THEN create `{ClassName}AdvancedTests.cs`

#### When to Create Advanced Test Files

**STRICT CRITERIA - ALL MUST BE MET:**
- **File Size**: Base test files exceed ~1,400 lines
- **Different Purposes**: Tests target specific coverage gaps rather than core business logic
- **Specialized Testing**: Error handling, validation boundaries, configuration scenarios
- **Coverage Targeting**: Tests specifically to reach 80%+ line and branch coverage
- **Base Tests Complete**: Core functionality is fully tested in base test file

#### Advanced Test Content Guidelines

- **Validation Boundaries**: Test min/max values, field length limits, required field validation
- **Edge Cases**: Null inputs, empty strings, special characters, Unicode handling
- **Error Conditions**: Database failures, network issues, invalid configurations
- **Authentication Scenarios**: Missing credentials, invalid tokens, authorization failures
- **Configuration Testing**: Environment-specific settings, database provider selection
- **Performance Edge Cases**: Large datasets, concurrent operations, resource limits
- **Edge Cases**: Null inputs, empty strings, special characters, Unicode handling
- **Error Conditions**: Database failures, network issues, invalid configurations
- **Authentication Scenarios**: Missing credentials, invalid tokens, authorization failures
- **Configuration Testing**: Environment-specific settings, database provider selection
- **Performance Edge Cases**: Large datasets, concurrent operations, resource limits

### Test Organization Best Practices

**ENFORCEMENT RULES:**
- **Naming Compliance**: NO custom test file names allowed - follow exact patterns only
- **File Size Limits**: Base test files under 1,500 lines; create advanced tests when exceeded
- **Single Responsibility**: Each test file focuses on ONE source class only
- **Check Before Create**: Always verify base test file exists before creating any test
- **Maintainability**: Keep individual test files under 1,500 lines for easy navigation
- **Clear Separation**: Base tests cover happy paths; advanced tests cover edge cases
- **Consistent Naming**: Use descriptive test method names: `MethodName_Scenario_ExpectedResult`
- **Documentation**: Include comprehensive XML documentation for advanced test files

### Test File Validation Checklist

**Before creating ANY test file, verify:**
- [ ] Checked if `{SourceClass}Tests.cs` exists using `file_search` tool
- [ ] Using exact naming pattern: `{SourceClass}Tests.cs` or `{SourceClass}AdvancedTests.cs`
- [ ] NOT using custom names like "FocusedTests", "CoverageTests", "SpecializedTests"
- [ ] Base test file exists and is >1,400 lines (if creating advanced tests)
- [ ] Tests target single source class/component only
- [ ] Following test organization hierarchy (base → advanced)

**VALIDATION EXAMPLE:**
```bash
# 1. Check existing: file_search for "MainLayoutTests.cs"
# 2. If found: Enhance MainLayoutTests.cs with new tests
# 3. If not found: Create MainLayoutTests.cs (not MainLayoutFocusedTests.cs)
# 4. Advanced tests: Only if MainLayoutTests.cs >1,400 lines → MainLayoutAdvancedTests.cs
```

---

## Coverage Standards

### Running Coverage Analysis

```bash
# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory:./TestResults/[TestRun]

# Generate HTML coverage report
reportgenerator -reports:"./TestResults/[TestRun]/*/coverage.cobertura.xml" -targetdir:"./CoverageReport/[TestRun]" -reporttypes:Html

# Open coverage report in browser
# Navigate to ./CoverageReport/[TestRun]/index.html
```

### Coverage Analysis Commands

```bash
# Quick coverage check for current changes
dotnet test --collect:"XPlat Code Coverage" --results-directory:./TestResults/QuickCheck

# Full coverage analysis with detailed reporting
dotnet test --collect:"XPlat Code Coverage" --results-directory:./TestResults/FullAnalysis
reportgenerator -reports:"./TestResults/FullAnalysis/*/coverage.cobertura.xml" -targetdir:"./CoverageReport/FullAnalysis" -reporttypes:Html

# Coverage comparison between branches
reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:"./CoverageReport/Comparison" -reporttypes:Html -historydirectory:"./CoverageReport/History"
```

### Coverage Improvement Methodology

1. **Identify Gaps**: Use coverage reports to find files below 80% line/branch coverage
2. **Analyze Uncovered Code**: Determine if gaps are in core logic (add to base tests) or edge cases (create advanced tests)
3. **Strategic Testing**: Create targeted advanced tests for authentication, validation, error handling
4. **Validate Impact**: Run coverage analysis after adding advanced tests to measure improvement
5. **Maintain Quality**: Ensure all new tests follow naming conventions and documentation standards

### Coverage Report Structure

- **CoverageReport/**: Root directory for all coverage analysis reports
  - **[TestRun]/**: Timestamped or named subdirectories for different test runs
    - **index.html**: Main coverage report with summary and detailed breakdowns
    - **[Assembly]_[Class].html**: Detailed line-by-line coverage for specific classes

### Quality Metrics Analysis

- **CRAP Score**: Change Risk Anti-Patterns score combining complexity and coverage
  - Target: Keep CRAP score low by maintaining high test coverage on complex methods
- **Cyclomatic Complexity**: Measures code complexity through decision points
  - Target: Break down methods with high complexity or ensure comprehensive testing

### CodeQL Static Security Analysis

**CodeQL is MANDATORY for all code contributions** - it performs static application security testing (SAST) to identify vulnerabilities before they reach production.

#### CodeQL Analysis Configurations

Setlist Studio uses **two different CodeQL analysis configurations** aligned with GitHub Actions security.yaml:

**1. Security-Focused Analysis (Local Development)**
- **Query Suite**: `codeql/csharp-security-extended.qls` (68 security queries)
- **Purpose**: Critical security vulnerability detection
- **Target**: **Zero high/critical security issues** (blocking)
- **Use**: Pre-commit validation, security-focused development

**2. Comprehensive Quality Analysis (GitHub Actions)**
- **Query Suite**: `security-and-quality` (170 comprehensive queries)  
- **Purpose**: Security + code quality + best practices
- **Configuration**: `.github/codeql/codeql-config.yml`
- **Results**: Security issues + warnings + recommendations
- **Use**: CI/CD pipeline, comprehensive code review

#### Running CodeQL Analysis Locally

**OPTION 1: Use Provided Scripts (Recommended)**

**Security-Focused Analysis:**
```powershell
# Quick security validation (68 security queries)
.\scripts\run-codeql-security.ps1

# With clean database rebuild
.\scripts\run-codeql-security.ps1 -CleanDatabase
```

**Comprehensive Analysis (Matches GitHub Actions Exactly):**
```powershell
# Full analysis matching GitHub Actions security.yml
.\scripts\run-codeql-comprehensive.ps1

# With clean database and open results
.\scripts\run-codeql-comprehensive.ps1 -CleanDatabase -OpenResults
```

**OPTION 2: Manual Commands (Advanced)**

**Security-Only Analysis:**
```powershell
# Create CodeQL database (matches GitHub Actions build)
codeql database create codeql-database --language=csharp --command="dotnet build SetlistStudio.sln --configuration Release --no-restore" --source-root=.

# Run security-focused analysis
codeql database analyze codeql-database --format=sarif-latest --output=security-analysis.sarif codeql/csharp-security-extended.qls --download

# Check results (should be zero for security compliance)
$results = (Get-Content security-analysis.sarif | ConvertFrom-Json).runs[0].results
Write-Host "Security issues found: $($results.Count)"
```

**Comprehensive Analysis (GitHub Actions Match):**
```powershell
# Run full analysis with local config (mirrors GitHub Actions)
codeql database analyze codeql-database --format=sarif-latest --output=github-analysis.sarif --config-file=.codeql/codeql-config.yml codeql/csharp-queries:codeql-suites/csharp-security-and-quality.qls --download

# Categorize findings by severity (matches GitHub Actions analysis)
$sarif = Get-Content github-analysis.sarif | ConvertFrom-Json; $results = $sarif.runs[0].results; $rules = $sarif.runs[0].tool.driver.rules; $findings = @{}; foreach($result in $results) { $rule = $rules | Where-Object { $_.id -eq $result.ruleId }; $severity = $rule.properties.'problem.severity'; if($findings.ContainsKey($severity)) { $findings[$severity]++ } else { $findings[$severity] = 1 } }; $findings.GetEnumerator() | Sort-Object Value -Descending
```

#### Local CodeQL Configuration

**Configuration Files:**
- **`.codeql/codeql-config.yml`**: Local configuration mirroring GitHub Actions exactly
- **`.codeql/config.env`**: Environment variables for local development
- **`scripts/run-codeql-local.ps1`**: Main analysis script with full configurability
- **`scripts/run-codeql-security.ps1`**: Quick security-focused analysis
- **`scripts/run-codeql-comprehensive.ps1`**: Full analysis matching GitHub Actions

**Local vs GitHub Actions Alignment:**
- **Same Query Suites**: Both use `security-and-quality` for comprehensive analysis
- **Same Build Commands**: Both use `dotnet build SetlistStudio.sln --configuration Release --no-restore`
- **Same Path Exclusions**: Tests, build artifacts, coverage reports excluded
- **Same Output Format**: SARIF with structured findings categorization
- **Same Configuration File**: `.codeql/codeql-config.yml` mirrors `.github/codeql/codeql-config.yml`

#### CodeQL Automated Analysis

CodeQL analysis runs automatically via GitHub Actions (.github/workflows/security.yml):
- **All pull requests** to main branch
- **Push to main branch** (for baseline maintenance)  
- **Daily scheduled scans** (2 AM UTC)
- **Manual workflow dispatch** (ad-hoc security audits)

#### CodeQL Security Standards

**ZERO TOLERANCE POLICY FOR SECURITY ISSUES:**
- **Critical security issues**: Must be fixed before merge - **no exceptions**
- **High security issues**: Must be fixed before merge - **no exceptions**
- **Medium security issues**: Should be fixed or justified with suppression

**CODE QUALITY FINDINGS (Non-blocking):**
- **Warnings**: Code quality issues, potential bugs (162 typical findings)
- **Recommendations**: Best practice suggestions (68 typical findings)
- **Notes**: Minor improvements and optimizations

**CRITICAL DISTINCTION**: 
- **Security findings** = Blocking (must fix)
- **Quality findings** = Non-blocking (continuous improvement)

#### CodeQL Results Interpretation

**Understanding GitHub Actions Results:**

When GitHub Actions reports "67 new alerts" (22 warnings + 45 notes), this includes:
- **Security vulnerabilities** (if any) - **BLOCKING**
- **Code quality warnings** - Non-blocking  
- **Best practice recommendations** - Non-blocking

**Local vs GitHub Analysis Comparison:**
- **Local Security Analysis**: `0 results` = No security vulnerabilities ✅
- **GitHub Comprehensive Analysis**: `230 results` = Security + quality findings
- **Discrepancy is Expected**: Different query scopes, not a security concern

**ALWAYS CHECK GitHub Security Tab** for actual security findings rather than relying on workflow summaries.

#### CodeQL Issue Resolution

**For Security Issues (Critical/High):**
1. **Immediate action**: Treat as security failure regardless of other scan results
2. **Root cause analysis**: Understand the vulnerability and potential impact  
3. **Secure implementation**: Fix underlying security flaw, don't just suppress
4. **Validation testing**: Ensure fix resolves issue without breaking functionality
5. **Re-verification**: Run security-focused CodeQL to confirm resolution
6. **Documentation**: Explain security improvements in commit messages

**For Quality Issues (Warnings/Recommendations):**
1. **Assess impact**: Determine if issue affects maintainability or performance
2. **Prioritize fix**: Address based on code quality improvement value
3. **Batch improvements**: Group similar quality fixes in dedicated PRs
4. **Document rationale**: Explain quality improvements in commit messages

#### CodeQL Configuration Files

**Local Security Configuration:**
- Uses default security-extended query suite
- Focuses on OWASP Top 10 and CWE security categories
- Excludes test files and build artifacts

**GitHub Actions Configuration (.github/codeql/codeql-config.yml):**
```yaml
queries:
  - uses: security-and-quality
paths-ignore:
  - "tests/**"
  - "**/bin/**"
  - "**/obj/**"  
  - "TestResults/**"
  - "CoverageReport/**"
paths:
  - "src/**"
  - "*.cs"
  - "*.cshtml"
  - "*.razor"
```

#### CodeQL Best Practices

**To minimize security findings:**
- **Input validation**: Always validate and sanitize user inputs
- **Parameterized queries**: Never concatenate user input into SQL strings
- **Secure defaults**: Use secure configurations and libraries
- **Error handling**: Don't expose sensitive information in error messages
- **Access control**: Implement proper authorization checks
- **Secrets management**: Never hardcode credentials or API keys

**To minimize quality findings:**
- **Resource disposal**: Use `using` statements for IDisposable objects
- **Performance optimization**: Avoid string concatenation in loops
- **API modernization**: Replace obsolete method calls
- **Documentation**: Add XML documentation for public APIs
- **Code simplification**: Reduce complexity and nested conditions

#### CodeQL Suppression Guidelines

**Security Issue Suppressions (Rare):**
- Only suppress **confirmed false positives** after thorough security review
- Require security team approval for high/critical suppression
- Document detailed justification with security impact analysis
- Regular review of all security suppressions

**Quality Issue Suppressions (Selective):**
- Suppress when fixing would reduce code readability or maintainability
- Document business justification for suppression
- Consider suppression for generated code or third-party integrations
- Review suppressions during major refactoring efforts

#### Common CodeQL Issues in .NET Applications

**Security Issues (Must Fix):**
- **SQL Injection**: Use Entity Framework LINQ queries instead of raw SQL
- **XSS Vulnerabilities**: Always encode output, validate inputs
- **Path Traversal**: Validate file paths, use safe file operations
- **Information Disclosure**: Sanitize error messages and logs
- **Authentication Bypass**: Implement proper authorization checks
- **Cryptographic Issues**: Use strong algorithms and proper key management

**Quality Issues (Continuous Improvement):**
- **Resource Management**: Dispose IDisposable objects properly
- **Performance**: Optimize string operations and LINQ usage
- **API Usage**: Update obsolete method calls and improve error handling
- **Code Structure**: Simplify complex conditions and reduce nesting
- **Documentation**: Add XML comments for public APIs

---

## Development Workflow

### Version Control
- **Git-based workflow**: Feature branches with pull request reviews
- **Branch naming**: `feature/[issue-number]-[short-description]`
- **Commit messages**: Clear, descriptive messages following conventional commits

### CI/CD Pipeline
- **GitHub Actions**: Automated building, testing, and deployment
- **Quality Gates**: **100% test success rate**, **zero build warnings**, **zero high/critical CodeQL issues**, and 80%+ coverage required before merge
- **Performance Requirements**: API endpoints must respond within 500ms, database queries within 100ms under normal load
- **CodeQL Analysis**: Mandatory static security analysis on all pull requests - **CodeQL findings override general security summaries**
- **CodeQL Code Generation Compliance**: All generated code must pass CodeQL static analysis without high/critical security vulnerabilities
- **CodeQL Best Practices**: Generated code must follow CodeQL quality recommendations (null safety, LINQ usage, resource disposal)
- **Code Review**: All changes require peer review and approval
- **Zero Tolerance**: No failing tests, build warnings, or high/critical security issues allowed in any branch or pull request
- **Security Priority**: CodeQL high/critical issues constitute security failures regardless of other scan status indicators

### Test Execution Strategy
- **Unit Tests**: Fast, isolated tests for individual components (must pass 100%)
- **Integration Tests**: Database and service integration scenarios (must pass 100%)
- **Component Tests**: Blazor component rendering and interaction tests (must pass 100%)
- **Advanced Tests**: Edge cases, error conditions, and coverage gaps (must pass 100%)
- **Test Reliability**: All tests must be deterministic and consistently passing

### Performance Monitoring

**Performance Benchmarks (Must Meet)**:
- **API Response Times**: <500ms for all endpoints under normal load
- **Database Query Times**: <100ms for user data queries
- **Page Load Times**: <2 seconds for Blazor Server pages
- **Memory Usage**: <4MB per concurrent user connection
- **Database File Size**: Monitor SQLite files >50MB for migration planning

**Performance Testing Commands**:
```bash
# Run performance benchmarks
dotnet run --project tests/SetlistStudio.PerformanceTests

# Monitor database query performance
dotnet ef dbcontext optimize --startup-project src/SetlistStudio.Web

# Generate performance report
./scripts/run-performance-tests.ps1

# Check for N+1 query problems
dotnet trace collect --providers Microsoft-EntityFrameworkCore
```

**Scalability Thresholds**:
- **SQLite Limits**: 100 concurrent users, 50MB database size
- **Blazor Server**: 200 connections per instance, 2-4MB per connection
- **Memory Cache**: Monitor growth patterns, implement Redis at 1GB+

### Common Commands
```bash
# Run all tests (must achieve 100% success rate)
dotnet test

# Run tests with coverage (must achieve 100% success with 80%+ coverage)
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class (verify 100% success for targeted testing)
dotnet test --filter "FullyQualifiedName~SetlistServiceTests"

# Run tests and generate coverage report (validate 100% success + coverage)
./scripts/run-tests-clean.ps1
```

---

## Maintainability & Business Continuity Framework

**Setlist Studio prioritizes long-term maintainability and seamless team handovers to ensure business continuity and sustainable growth.**

### 🎯 Core Maintainability Principles

#### **1. Team Handover Excellence**
- **Knowledge Transfer Priority**: All code and documentation must enable smooth transitions to new team members
- **Onboarding Efficiency**: New developers should be productive within 2-3 days, not weeks
- **Self-Documenting Code**: Business logic should be clear to both technical and non-technical stakeholders
- **Decision Documentation**: Technical choices must include business justification and impact analysis

#### **2. Business Alignment Focus**
- **Creative Workflow Clarity**: Every feature must clearly serve real musician needs and performance workflows
- **User Story Traceability**: Code should directly trace back to specific musician pain points or creative processes
- **Performance-First Design**: All technical decisions consider live performance scenarios and backstage environments
- **Industry Standards**: Musical data models and terminology reflect actual industry practices

#### **3. Sustainable Technology Strategy**
- **Long-term Viability**: Technology choices prioritize stability and community support over cutting-edge features
- **Dependency Management**: Minimize external dependencies; prefer established, well-maintained libraries
- **Migration Readiness**: Architecture supports evolution (SQLite → PostgreSQL, single-instance → load-balanced)
- **Version Stability**: Use LTS versions (.NET 8 LTS) for predictable support lifecycle

### 📋 Maintainability Assessment Criteria

#### **Organization & Clarity**
- [ ] **Clear Project Purpose**: README immediately explains what Setlist Studio does and who it serves
- [ ] **Logical Structure**: File and folder organization follows industry standards and is intuitive
- [ ] **Naming Conventions**: All identifiers (classes, methods, variables) use musician-friendly terminology
- [ ] **Documentation Hierarchy**: Information is layered from business overview to technical implementation details

#### **Ease of Handover** 
- [ ] **Quick Start Guide**: New developers can run the application locally within 30 minutes
- [ ] **Development Workflow**: Clear steps from clone to productive contribution
- [ ] **Business Context**: Technical documentation includes "why" decisions were made, not just "how"
- [ ] **Deployment Documentation**: Multiple deployment scenarios documented with troubleshooting guides

#### **Business Continuity & Sustainability**
- [ ] **Scalability Roadmap**: Clear growth path from small bands to large music organizations
- [ ] **Technology Longevity**: Dependencies have active communities and long-term support commitments
- [ ] **Performance Benchmarks**: Measurable criteria for user experience quality (response times, uptime)
- [ ] **Migration Strategies**: Documented paths for database, hosting, and technology upgrades

#### **Collaboration & Governance**
- [ ] **CI/CD Maturity**: Automated testing, security scanning, and deployment processes
- [ ] **Code Review Standards**: Pull request templates enforce quality and maintainability checks
- [ ] **Security Governance**: Regular security updates and vulnerability management processes
- [ ] **Documentation Maintenance**: Regular review and update cycles for all documentation

### 🎼 Creative Industry Alignment Standards

#### **Musical Workflow Integration**
- **Realistic Data Models**: BPM ranges (40-250), standard key signatures, authentic genre classifications
- **Performance Context**: Features designed for actual performance scenarios (low light, quick access, reliability)
- **Collaborative Features**: Support for band members, sound engineers, and venue coordinators
- **Mobile-First Design**: Optimized for tablets and phones used backstage and during performances

#### **User Experience for Musicians**
- **Intuitive Navigation**: Interface matches how musicians think about and organize their music
- **Offline Capability**: Critical features work without internet connection during performances
- **Fast Data Entry**: Efficient workflows for adding songs, creating setlists, and making quick changes
- **Professional Presentation**: Export formats suitable for sharing with venues, sound engineers, and collaborators

### 🔄 Maintainability Review Process

#### **Regular Assessment (Monthly)**
1. **Documentation Currency**: Verify all setup guides work with current codebase
2. **Dependency Health**: Check for security updates and deprecated packages
3. **Performance Benchmarks**: Validate response times and scalability metrics
4. **User Feedback Integration**: Review musician feedback for usability improvements

#### **Quarterly Business Alignment Review**
1. **Feature-to-Workflow Mapping**: Ensure all features serve documented musician needs
2. **Technology Sustainability**: Assess dependency roadmaps and migration needs
3. **Onboarding Metrics**: Measure new developer time-to-productivity
4. **Team Knowledge Distribution**: Identify single points of failure in project knowledge

#### **Annual Strategic Assessment**
1. **Technology Roadmap**: Plan major upgrades and architectural evolution
2. **Business Model Alignment**: Ensure technical architecture supports business growth
3. **Competition Analysis**: Compare maintainability against industry best practices
4. **Succession Planning**: Validate project can survive team changes and organizational shifts

### 🛡️ Maintainability Risk Management

#### **Common Risk Mitigation Strategies**
- **Over-Engineering Risk**: Regular reviews to ensure complexity serves business value
- **Technology Lock-in**: Maintain abstraction layers for major dependencies
- **Knowledge Concentration**: Rotate code review assignments and pair programming
- **Documentation Drift**: Automated checks for outdated documentation and broken links

#### **Team Transition Checklist**
- [ ] **Environment Setup**: New team can deploy development environment independently
- [ ] **Business Context**: Product vision and user stories are clearly documented
- [ ] **Technical Architecture**: Decision records explain why specific technologies were chosen
- [ ] **Deployment Process**: Production deployment can be executed by new team members
- [ ] **Monitoring & Support**: Operational procedures for ongoing maintenance are documented

### 📈 Maintainability Success Metrics

#### **Technical Health Indicators**
- **Build Success Rate**: >99% successful CI/CD runs
- **Test Coverage**: >80% line and branch coverage maintained
- **Security Posture**: Zero high/critical security vulnerabilities
- **Performance Standards**: API response times <500ms, page loads <2 seconds

#### **Business Continuity Metrics**
- **Onboarding Time**: New developer productivity within 2-3 days
- **Feature Delivery**: Consistent development velocity over time
- **User Satisfaction**: Positive feedback on ease of use from musicians
- **Deployment Reliability**: Zero-downtime deployments and quick rollback capability

---

## Setlist Templates Feature

### Overview

**Setlist Templates** are reusable blueprints that allow musicians to quickly create setlists for recurring performance types (weddings, bar gigs, concerts). Templates contain suggested song lists and metadata but no performance-specific details.

### Business Value

**User Problem**: Musicians perform similar types of shows repeatedly (weddings, bar nights, corporate events) and manually recreate setlists each time.

**Solution**: Reusable templates that can be converted to actual setlists with one action, then customized for specific performances.

**Musician Workflow**:
1. Create template "Wedding Ceremony" with romantic songs
2. When booked for wedding, convert template → setlist
3. Add performance date, venue, make minor adjustments
4. Perform and track history

### Entity Structure

```csharp
public class SetlistTemplate
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    [SafeString(MaxLength = 200, AllowEmpty = false)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    [SafeString(MaxLength = 1000, AllowEmpty = true)]
    public string? Description { get; set; }
    
    [StringLength(100)]
    [SafeString(MaxLength = 100, AllowEmpty = true)]
    public string? Category { get; set; } // "Wedding", "Rock Bar", "Jazz Club"
    
    public int? EstimatedDurationMinutes { get; set; }
    
    public bool IsPublic { get; set; } = false; // Future: template sharing
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<SetlistTemplateSong> TemplateSongs { get; set; } = new List<SetlistTemplateSong>();
}

public class SetlistTemplateSong
{
    public int Id { get; set; }
    public int SetlistTemplateId { get; set; }
    public int SongId { get; set; }
    public int Position { get; set; } // Order in template
    
    // Navigation properties
    public SetlistTemplate Template { get; set; } = null!;
    public Song Song { get; set; } = null!;
}
```

### Service Layer Pattern

```csharp
public interface ISetlistTemplateService
{
    // CRUD Operations
    Task<SetlistTemplate> CreateTemplateAsync(SetlistTemplate template, string userId);
    Task<(IEnumerable<SetlistTemplate> Templates, int TotalCount)> GetTemplatesAsync(
        string userId, 
        string? category = null,
        int pageNumber = 1, 
        int pageSize = 20);
    Task<SetlistTemplate?> GetTemplateByIdAsync(int templateId, string userId);
    Task<SetlistTemplate?> UpdateTemplateAsync(int templateId, SetlistTemplate updatedTemplate, string userId);
    Task<bool> DeleteTemplateAsync(int templateId, string userId);
    
    // Template Operations
    Task<SetlistTemplate?> AddSongToTemplateAsync(int templateId, int songId, int position, string userId);
    Task<bool> RemoveSongFromTemplateAsync(int templateId, int songId, string userId);
    Task<SetlistTemplate?> ReorderTemplateSongsAsync(int templateId, List<int> songIds, string userId);
    
    // Conversion
    Task<Setlist> ConvertTemplateToSetlistAsync(int templateId, string setlistName, DateTime? performanceDate, string userId);
    
    // Discovery
    Task<IEnumerable<string>> GetCategoriesAsync(string userId);
}
```

### Security Requirements

#### Authorization Pattern

**CRITICAL**: Every template operation MUST verify user ownership.

```csharp
// ✅ CORRECT: Service layer
public async Task<SetlistTemplate?> GetTemplateByIdAsync(int templateId, string userId)
{
    return await _context.SetlistTemplates
        .Include(t => t.TemplateSongs)
        .ThenInclude(ts => ts.Song)
        .Where(t => t.Id == templateId && t.UserId == userId) // ALWAYS filter by userId
        .FirstOrDefaultAsync();
}

// ❌ WRONG: No user ownership check
var template = await _context.SetlistTemplates.FindAsync(templateId); // Any user's template!
```

#### Input Validation

```csharp
[Required]
[StringLength(200)]
[SafeString(MaxLength = 200, AllowEmpty = false)]
public string Name { get; set; }

[StringLength(1000)]
[SafeString(MaxLength = 1000, AllowEmpty = true)]
public string? Description { get; set; }

// Category validation - whitelist common template types
private static readonly HashSet<string> ValidCategories = new(StringComparer.OrdinalIgnoreCase)
{
    "Wedding", "Corporate Event", "Bar Gig", "Concert", 
    "Church Service", "Jazz Club", "Rock Show", "Cover Set",
    "Original Music", "Practice Session", "Recording Session", "Other"
};
```

### Template → Setlist Conversion Pattern

```csharp
public async Task<Setlist> ConvertTemplateToSetlistAsync(
    int templateId, 
    string setlistName, 
    DateTime? performanceDate, 
    string userId)
{
    // 1. Load template with authorization check
    var template = await GetTemplateByIdAsync(templateId, userId);
    if (template == null)
        throw new UnauthorizedAccessException("Template not found or access denied");
    
    // 2. Create new setlist from template
    var setlist = new Setlist
    {
        Name = setlistName,
        PerformanceDate = performanceDate,
        UserId = userId,
        CreatedAt = DateTime.UtcNow
    };
    
    await _context.Setlists.AddAsync(setlist);
    await _context.SaveChangesAsync();
    
    // 3. Copy songs from template to setlist
    foreach (var templateSong in template.TemplateSongs.OrderBy(ts => ts.Position))
    {
        var setlistSong = new SetlistSong
        {
            SetlistId = setlist.Id,
            SongId = templateSong.SongId,
            Position = templateSong.Position
        };
        await _context.SetlistSongs.AddAsync(setlistSong);
    }
    
    await _context.SaveChangesAsync();
    
    // 4. Return fully loaded setlist
    return await _setlistService.GetByIdAsync(setlist.Id, userId) 
        ?? throw new InvalidOperationException("Failed to load created setlist");
}
```

### Testing Strategy

**Test Coverage Requirements: >80% line and branch coverage**

#### Test Categories

1. **CRUD Operations** (20 tests)
   - Create valid templates
   - Read templates with pagination
   - Update template properties
   - Delete templates
   - User ownership enforcement

2. **Song Management** (15 tests)
   - Add songs to template
   - Remove songs from template
   - Reorder template songs
   - Position validation
   - Duplicate song handling

3. **Template Conversion** (10 tests)
   - Convert template to setlist
   - Preserve song order
   - Set performance metadata
   - User ownership transfer
   - Empty template handling

4. **Security Tests** (12 tests)
   - Authorization violations (accessing other users' templates)
   - Input validation (XSS, SQL injection, oversized inputs)
   - Malicious category names
   - CSRF protection on state changes

5. **Pagination & Performance** (8 tests)
   - Large template collections
   - Category filtering
   - Search functionality
   - Query optimization

#### Sample Test Structure

```csharp
[Fact]
public async Task CreateTemplateAsync_WithValidData_CreatesTemplate()
{
    // Arrange
    var template = new SetlistTemplate
    {
        Name = "Wedding Ceremony Set",
        Description = "Romantic songs for ceremony entrance and signing",
        Category = "Wedding",
        EstimatedDurationMinutes = 45,
        UserId = TestUserId
    };
    
    // Act
    var result = await _service.CreateTemplateAsync(template, TestUserId);
    
    // Assert
    result.Should().NotBeNull();
    result.Id.Should().BeGreaterThan(0);
    result.Name.Should().Be("Wedding Ceremony Set");
    result.UserId.Should().Be(TestUserId);
    result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
}

[Fact]
public async Task GetTemplateByIdAsync_WithOtherUsersTemplate_ReturnsNull()
{
    // Arrange: Create template for User A
    var templateUserA = await _service.CreateTemplateAsync(
        new SetlistTemplate { Name = "User A Template", UserId = "userA-123" },
        "userA-123");
    
    // Act: User B tries to access User A's template
    var result = await _service.GetTemplateByIdAsync(templateUserA.Id, "userB-456");
    
    // Assert: Access denied
    result.Should().BeNull();
}
```

### Performance Considerations

**Database Indexes:**
```csharp
// OnModelCreating
builder.Entity<SetlistTemplate>(entity =>
{
    entity.HasIndex(t => t.UserId);
    entity.HasIndex(t => new { t.UserId, t.Category });
    entity.HasIndex(t => new { t.UserId, t.Name });
});

builder.Entity<SetlistTemplateSong>(entity =>
{
    entity.HasIndex(ts => new { ts.SetlistTemplateId, ts.Position });
});
```

**Pagination:**
- Default page size: 20 templates
- Maximum page size: 100 templates
- Consistent ordering: OrderBy CreatedAt DESC

**Caching Strategy:**
- Cache category list per user (5-minute TTL)
- Invalidate cache on template create/update/delete

### User Delight Patterns

**Example Template Categories for Musicians:**
- **"Wedding Ceremony"**: Romantic ballads, instrumental pieces
- **"Rock Bar Night"**: High-energy covers, crowd favorites
- **"Jazz Club Set"**: Standards, improvisational showcases
- **"Corporate Event"**: Professional background music, crowd-pleasers
- **"Church Service"**: Worship songs, hymns, spiritual music
- **"Practice Session"**: Technical exercises, new material rehearsal

**Template Naming Conventions:**
- Clear, descriptive names: "Friday Night Bar Set" not "Template1"
- Include context: "Summer Wedding Reception" not "Wedding"
- Duration indicators: "45-Minute Coffee Shop Set"

### Example Usage Scenarios

**Scenario 1: Wedding Musician**
```
1. Create template "Wedding Ceremony" with 8 romantic songs
2. Create template "Wedding Reception" with 25 dance songs
3. When booked: Convert "Wedding Ceremony" → "Smith Wedding 6/15"
4. Add venue "Riverside Gardens", set time "2:00 PM"
5. Customize: Add requested song "All of Me" by John Legend
```

**Scenario 2: Cover Band**
```
1. Create template "Rock Bar Night" with 40 classic rock covers
2. Every Friday gig: Convert template → new setlist
3. Rotate songs based on venue, add new covers monthly
4. Track performance history: which songs work best
```

---

## Sample Data Guidelines

Use realistic musical data in all examples, tests, and documentation:

### Song Examples
- **Classic Rock**: "Sweet Child O' Mine" by Guns N' Roses (BPM: 125, Key: D)
- **Pop**: "Billie Jean" by Michael Jackson (BPM: 117, Key: F#m)
- **Jazz**: "Take Five" by Dave Brubeck (BPM: 176, Key: Bb)
- **Blues**: "The Thrill Is Gone" by B.B. King (BPM: 98, Key: Bm)

### BPM Ranges
- **Ballads**: 60-80 BPM
- **Medium Tempo**: 90-120 BPM  
- **Up-tempo**: 130-160 BPM
- **Fast Songs**: 170+ BPM

### Common Keys
- **Guitar-friendly**: E, A, D, G, C
- **Vocal-friendly**: F, Bb, Eb, Ab
- **Minor keys**: Am, Em, Bm, F#m, Cm

---

## CodeQL Code Generation Standards

**MANDATORY: All generated code must pass CodeQL static analysis with zero high/critical security issues.**

### Code Generation Requirements

When generating any code (classes, methods, controllers, services, tests), **ALWAYS** ensure:

#### 1. **Security-First Code Generation**
- **Input Validation**: Every user input must be validated and sanitized
- **Parameterized Queries**: Never concatenate user input into SQL strings - use Entity Framework LINQ exclusively
- **Authorization Checks**: Every data access operation must verify user ownership
- **Error Handling**: Never expose sensitive information in error messages or logs
- **Resource Management**: Always use `using` statements for IDisposable objects

#### 2. **Null Safety and Type Safety**
- **Explicit Null Handling**: Use null-conditional operators (`?.`) and null-forgiving operators (`!`) appropriately
- **Avoid `default()` Casts**: Use explicit nullable casts like `(HttpContext?)null` instead of `default(HttpContext)`
- **Null Checks**: Add proper null checks before accessing potentially null variables
- **Non-nullable References**: Leverage C# nullable reference types to prevent null reference exceptions

#### 3. **LINQ and Performance Best Practices**
- **Use LINQ Methods**: Replace foreach loops with appropriate LINQ methods (`.Select()`, `.Where()`, `.Any()`)
- **Avoid Unnecessary Variables**: Don't create variables that are assigned but never used
- **Efficient Queries**: Use `FirstOrDefaultAsync()` instead of `Where().FirstAsync()` when appropriate
- **Resource Optimization**: Avoid string concatenation in loops, use `StringBuilder` or string interpolation

#### 4. **Authentication and Authorization Patterns**
```csharp
// CORRECT: Always validate user ownership
var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();
var userResource = await _service.GetByUserIdAsync(userId, resourceId);
if (userResource == null) throw new ForbiddenException();

// INCORRECT: Direct access without validation
var resource = await _service.GetByIdAsync(resourceId); // Missing ownership check
```

#### 5. **Input Validation Patterns**
```csharp
// CORRECT: Comprehensive validation
[SafeBpm(40, 250)]
public int Bpm { get; set; }

if (string.IsNullOrWhiteSpace(userInput) || userInput.Length > 500)
    throw new ValidationException("Invalid input");

var sanitized = SecureLoggingHelper.SanitizeMessage(userInput);

// INCORRECT: No validation
public int Bpm { get; set; } // Missing validation attribute
var query = $"SELECT * FROM Songs WHERE Name = '{userInput}'"; // SQL injection risk
```

#### 6. **Error Handling Patterns**
```csharp
// CORRECT: Secure error handling
try 
{
    // Operation
}
catch (UnauthorizedAccessException)
{
    _logger.LogWarning("Unauthorized access attempt by user {UserId}", userId);
    return Forbid();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed for user {UserId}", userId);
    return Problem("An error occurred processing your request");
}

// INCORRECT: Information leakage
catch (Exception ex)
{
    return BadRequest(ex.Message); // Exposes internal details
}
```

#### 7. **Resource Management Patterns**
```csharp
// CORRECT: Proper disposal
using var scope = _serviceProvider.CreateScope();
await using var stream = File.OpenRead(path);

// CORRECT: Explicit disposal in tests
_mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

// INCORRECT: Resource leaks
var stream = File.OpenRead(path); // Missing using statement
```

### CodeQL Quality Standards

#### **Common CodeQL Issues to Avoid:**

1. **Dereferenced variable may be null**
   - Use null-conditional operators: `user?.Name`
   - Add null-forgiving operators after null checks: `result!.Message`
   - Validate parameters: `name ?? throw new ArgumentNullException(nameof(name))`

2. **Useless assignment to local variable**
   - Remove unused variables
   - Use discard pattern `_` only when appropriate
   - Assign and use variables in the same scope

3. **Useless upcast**
   - Use explicit nullable casts: `(Type?)null` instead of `default(Type)`
   - Let compiler handle implicit conversions

4. **Missed opportunity to use LINQ**
   - Replace `foreach` + `Add()` with `.Select()`
   - Replace `foreach` + `if` with `.Where()`
   - Use `Any()` instead of `Count() > 0`

### Pre-Generation Checklist

**Before generating any code, ensure:**
- [ ] Input validation is implemented for all user inputs
- [ ] Authorization checks verify user ownership of resources
- [ ] Error handling doesn't leak sensitive information
- [ ] Resource disposal is handled with `using` statements
- [ ] Null safety is addressed with appropriate operators
- [ ] LINQ methods are used instead of manual loops where appropriate
- [ ] No hardcoded secrets or connection strings
- [ ] Logging doesn't expose sensitive data

### CodeQL Validation Commands

**Always validate generated code with:**
```bash
# Security-focused analysis (zero issues required)
codeql database analyze codeql-database --output=security-analysis.sarif codeql/csharp-security-extended.qls

# Quality analysis for comprehensive review
codeql database analyze codeql-database --output=quality-analysis.sarif codeql/csharp-queries:codeql-suites/csharp-security-and-quality.qls
```

---

## Copilot Prompts

### Testing & Quality Assurance

**ALWAYS follow these naming conventions - NO EXCEPTIONS:**

```
"Check if SetlistServiceTests.cs exists, then enhance it with comprehensive unit tests for the setlist creation endpoint"

"First verify SongServiceTests.cs exists, then create SongServiceAdvancedTests.cs ONLY IF base file exceeds 1,400 lines"

"Check if ProgramTests.cs exists first, then create ProgramAdvancedTests.cs for authentication configuration scenarios"

"Enhance existing SetlistServiceTests.cs with position adjustment tests, create SetlistServiceAdvancedTests.cs only if needed"

"First check if MainLayoutTests.cs exists, then enhance it with core functionality tests before considering advanced tests"

"Verify LoginTests.cs exists, then add authentication edge cases to the base file or create LoginAdvancedTests.cs if base file is too large"

"Always follow {SourceClass}Tests.cs naming pattern - NEVER create custom-named test files like 'FocusedTests' or 'CoverageTests'"

"Before creating any test file, use file_search to check if base test file exists, then enhance existing file or create properly named new file"

"Write validation boundary tests covering minimum/maximum values, field length limits, and required field validation"

"Generate authentication scenario tests for missing credentials, invalid tokens, and authorization failures"

"Create performance edge case tests for large datasets, concurrent operations, and resource exhaustion scenarios"

"Analyze current code coverage and identify classes/methods missing tests to reach 80% line and branch coverage"

"Generate coverage report in CoverageReport/NewFeature and analyze which classes need additional testing"
```

### Architecture & Scalability

```
"Optimize the query for fetching large setlists with song metadata using Entity Framework"

"Implement pagination for the artists endpoint to handle thousands of artists efficiently"

"Add caching layer for frequently accessed song and artist data"

"Redesign the setlist storage to support better performance with 10,000+ songs per user"

"Add database indexes for user-specific queries on Songs and Setlists tables to improve query performance"

"Implement distributed caching with Redis to support horizontal scaling across multiple server instances"

"Migrate from SQLite to PostgreSQL for better concurrent user support and write performance"

"Add connection pooling to handle 100+ concurrent database connections efficiently"

"Implement response caching for expensive operations like genre listings and artist aggregations"

"Design API endpoints to support bulk operations for better performance with large datasets"

"Add database query optimization with proper LINQ usage to minimize N+1 query problems"

"Implement background jobs for heavy operations like setlist calculations and data aggregations"

"Configure load balancing with sticky sessions to support Blazor Server horizontal scaling"

"Add performance monitoring and metrics collection for database query times and memory usage"
```

### Data Querying & Filtering Patterns

```
"Implement genre filtering with exact matching for structured fields - use database indexes efficiently"

"Apply query composition pattern: start with base query, conditionally add filters, then execute once"

"Use exact match (==) for structured fields (Genre, MusicalKey, DifficultyRating) to leverage database indexes"

"Use partial match (Contains) for free-text fields (Title, Artist, Album) where flexibility is needed"

"Implement controller-level input normalization for case handling without breaking index usage"

"Return pagination tuple (IEnumerable<T> data, int totalCount) with Count() before Skip/Take"

"Order results consistently (OrderBy Artist, ThenBy Title) before applying pagination"

"Filter by user ownership FIRST: query = _context.Songs.Where(s => s.UserId == userId)"

"Build query incrementally: if (!string.IsNullOrWhiteSpace(filter)) query = query.Where(...)"

"Execute query once: var totalCount = await query.CountAsync(); var results = await query.OrderBy(...).Skip(...).Take(...).ToListAsync()"

"Avoid case-insensitive queries in service layer (ToLower() breaks index usage) - normalize at controller level instead"

"Cache expensive aggregations (GetGenresAsync, GetArtistsAsync) using cache-aside pattern with per-user scope"

"Target <100ms for all database queries, <20ms for indexed exact-match filters"

"Use composite indexes for common filter patterns: (UserId, Genre), (UserId, Artist), (UserId, Title)"

"Implement pagination validation: page >= 1, pageSize between 1-100"

"Design filtering endpoints for musician workflows: genre dropdowns populated from distinct query results"

"Test filtering with realistic musical data: various genres (Rock, Jazz, Hip-Hop), special characters (R&B, K-Pop)"

"Validate filter inputs for security: check for SQL injection, XSS, and malicious content before querying"

"Create dropdown-friendly APIs: GetGenresAsync() returns distinct values for UI selection, filtering uses exact matches"

"Document filtering pattern: Structured fields → Exact match (fast), Free-text fields → Partial match (flexible)"
```

---

## Genre Filtering Pattern: Complete Implementation Guide

### Pattern Overview

**Genre filtering with pagination** is a fundamental feature for musicians to organize and access their song libraries efficiently. This pattern demonstrates how to implement high-performance, secure filtering that scales to thousands of songs while maintaining excellent user experience.

---

### ✅ **How It Works**

#### **Core Implementation Pattern**

```csharp
// SERVICE LAYER: SongService.cs
public async Task<(IEnumerable<Song> Songs, int TotalCount)> GetSongsAsync(
    string userId,
    string? searchTerm = null,
    string? genre = null,
    string? tags = null,
    int pageNumber = 1,
    int pageSize = 20)
{
    // 1. Start with user-scoped query
    var query = _context.Songs.Where(s => s.UserId == userId);

    // 2. Apply search filter (partial match for free-text)
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        var lowerSearch = searchTerm.ToLower();
        query = query.Where(s => 
            s.Title.ToLower().Contains(lowerSearch) ||
            s.Artist.ToLower().Contains(lowerSearch) ||
            (s.Album != null && s.Album.ToLower().Contains(lowerSearch)));
    }

    // 3. Apply genre filter (exact match for structured data)
    if (!string.IsNullOrWhiteSpace(genre))
    {
        query = query.Where(s => s.Genre == genre);  // ✅ Uses index
    }

    // 4. Apply tags filter
    if (!string.IsNullOrWhiteSpace(tags))
    {
        query = query.Where(s => s.Tags != null && s.Tags.Contains(tags));
    }

    // 5. Count total before pagination
    var totalCount = await query.CountAsync();

    // 6. Apply consistent ordering and pagination
    var songs = await query
        .OrderBy(s => s.Artist)
        .ThenBy(s => s.Title)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return (songs, totalCount);
}
```

```csharp
// CONTROLLER LAYER: SongsController.cs
[HttpGet("genre/{genre}")]
public async Task<IActionResult> GetSongsByGenre(
    [FromRoute] string genre,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    // 1. Input validation
    if (string.IsNullOrWhiteSpace(genre))
        return BadRequest(new { error = "Genre parameter is required" });

    // 2. Pagination validation
    if (page < 1)
        return BadRequest(new { error = "Page number must be greater than 0" });
    
    if (pageSize < 1 || pageSize > 100)
        return BadRequest(new { error = "Page size must be between 1 and 100" });

    // 3. Security validation
    if (ContainsMaliciousContent(genre))
        return BadRequest(new { error = "Invalid genre parameter" });

    // 4. Optional: Controller-level normalization
    var normalizedGenre = genre.Trim();  // Preserves index usage

    // 5. Call service with filters
    var (songs, totalCount) = await _songService.GetSongsAsync(
        userId: SecureUserContext.GetSanitizedUserId(User),
        genre: normalizedGenre,
        pageNumber: page,
        pageSize: pageSize);

    // 6. Calculate pagination metadata
    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

    // 7. Return structured response
    return Ok(new
    {
        songs,
        pagination = new
        {
            currentPage = page,
            pageSize,
            totalCount,
            totalPages,
            hasNextPage = page < totalPages,
            hasPreviousPage = page > 1
        },
        filter = new
        {
            genre = normalizedGenre
        }
    });
}
```

#### **Query Execution Flow**

1. **User Isolation** → Filter by `UserId` first (security + index)
2. **Conditional Filters** → Build query incrementally based on provided parameters
3. **Count Total** → Execute `CountAsync()` for pagination metadata
4. **Order Consistently** → Always `OrderBy(Artist).ThenBy(Title)` for predictable results
5. **Apply Pagination** → `Skip((page - 1) * pageSize).Take(pageSize)`
6. **Single Execution** → All filters applied in one database round-trip

---

### 🔒 **Security Requirements**

#### **1. Input Validation**

```csharp
// Validate genre parameter
if (string.IsNullOrWhiteSpace(genre))
    return BadRequest(new { error = "Genre parameter is required" });

// Validate pagination bounds
if (page < 1 || pageSize < 1 || pageSize > 100)
    return BadRequest(new { error = "Invalid pagination parameters" });
```

#### **2. Malicious Content Detection**

```csharp
private bool ContainsMaliciousContent(string input)
{
    if (string.IsNullOrEmpty(input)) return false;
    
    var maliciousPatterns = new[]
    {
        "<script", "javascript:", "onclick=", "onerror=",
        "--", ";", "/*", "*/", "xp_", "sp_", "DROP ", "DELETE ", "UPDATE "
    };
    
    return maliciousPatterns.Any(pattern => 
        input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
}
```

#### **3. User Authorization**

```csharp
// ALWAYS filter by userId at query start
var query = _context.Songs.Where(s => s.UserId == userId);

// Verify user ownership before any operations
var userId = SecureUserContext.GetSanitizedUserId(User);
```

#### **4. Secure Logging**

```csharp
// Sanitize all logged data
var sanitizedGenre = SecureLoggingHelper.SanitizeMessage(genre.Trim());
var sanitizedUserId = SecureLoggingHelper.SanitizeUserId(userId);

_logger.LogInformation(
    "Filtering songs by genre {Genre} for user {UserId}",
    sanitizedGenre, sanitizedUserId);
```

#### **5. SQL Injection Prevention**

```csharp
// ✅ CORRECT: Use Entity Framework LINQ (parameterized)
query = query.Where(s => s.Genre == genre);

// ❌ WRONG: Never concatenate user input
// var sql = $"SELECT * FROM Songs WHERE Genre = '{genre}'";  // DANGEROUS!
```

---

### 📈 **Performance & Scalability**

#### **Performance Targets**

- **API Response Time**: <500ms for all endpoints
- **Database Query Time**: <100ms for filtered queries
- **Genre Filter Query**: <20ms with proper index usage
- **Large Dataset**: Handle 1,000+ songs efficiently

#### **Index Strategy**

```csharp
// DbContext OnModelCreating
builder.Entity<Song>(entity =>
{
    // Composite indexes for filtering
    entity.HasIndex(s => s.UserId);                      // User isolation
    entity.HasIndex(s => new { s.UserId, s.Genre });     // ✅ Genre filtering
    entity.HasIndex(s => new { s.UserId, s.Artist });    // Artist filtering
    entity.HasIndex(s => new { s.UserId, s.Title });     // Title filtering
});
```

#### **Query Performance Comparison**

| Approach | Query Type | Index Usage | Performance (1000 songs) |
|----------|-----------|-------------|--------------------------|
| **Exact Match** | `s.Genre == "Rock"` | ✅ Uses `(UserId, Genre)` index | <20ms ✅ |
| **Case-Insensitive** | `s.Genre.ToLower() == "rock"` | ❌ Full table scan | 50-100ms ❌ |
| **Partial Match** | `s.Genre.Contains("Roc")` | ❌ Full table scan | 100-200ms ❌ |

#### **Caching Strategy**

```csharp
// Cache expensive aggregations
public async Task<IEnumerable<string>> GetGenresAsync(string userId)
{
    return await _cacheService.GetGenresAsync(userId, async () =>
    {
        // Only executes on cache miss
        return await _context.Songs
            .Where(s => s.UserId == userId && !string.IsNullOrEmpty(s.Genre))
            .Select(s => s.Genre!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();
    });
}

// Invalidate cache on mutations
await _cacheService.InvalidateUserCacheAsync(userId);
```

#### **Scalability Thresholds**

- **Current (SQLite)**: 10,000 songs per user, 100 concurrent users
- **Migration Trigger**: Database >50MB or >100 concurrent users
- **Future (PostgreSQL)**: 100,000+ songs, 1,000+ concurrent users
- **Upgrade Path**: Genre lookup table with foreign key relationships

---

### 📚 **Maintainability & Code Conventions**

#### **Pattern Consistency Rules**

**CRITICAL: Apply different strategies based on field type:**

```csharp
// Structured Fields → Exact Match (uses indexes)
if (!string.IsNullOrWhiteSpace(genre))
    query = query.Where(s => s.Genre == genre);                    // ✅ Fast

if (!string.IsNullOrWhiteSpace(musicalKey))
    query = query.Where(s => s.MusicalKey == musicalKey);          // ✅ Fast

if (difficultyRating.HasValue)
    query = query.Where(s => s.DifficultyRating == difficultyRating); // ✅ Fast

// Free-Text Fields → Partial Match (flexible search)
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    var lower = searchTerm.ToLower();
    query = query.Where(s => 
        s.Title.ToLower().Contains(lower) ||                       // Flexible
        s.Artist.ToLower().Contains(lower));                       // Flexible
}
```

#### **Why This Distinction Matters**

- **Structured fields** (Genre, MusicalKey) → Enumerated values from dropdowns → Exact match expected
- **Free-text fields** (Title, Artist) → User-typed search → Partial match needed

#### **Controller Normalization Pattern**

```csharp
// Handle case variations without breaking index usage
private string? NormalizeGenre(string? genre)
{
    if (string.IsNullOrWhiteSpace(genre)) return null;
    
    // Stored genres are Title Case: "Rock", "Jazz", "Hip-Hop"
    return CultureInfo.CurrentCulture.TextInfo
        .ToTitleCase(genre.Trim().ToLower());
}

// Use in controller:
var normalizedGenre = NormalizeGenre(genre);  // "rock" → "Rock"
var (songs, totalCount) = await _songService.GetSongsAsync(
    userId, 
    genre: normalizedGenre);  // Service receives exact value
```

#### **Testing Pattern**

```csharp
[Theory]
[InlineData("Rock")]
[InlineData("Jazz")]
[InlineData("Hip-Hop")]
[InlineData("R&B")]
[InlineData("Death Metal")]
public async Task GetSongsByGenre_WithVariousGenres_FiltersCorrectly(string genre)
{
    // Arrange
    var songs = CreateTestSongs(genre, 3);
    _mockSongService
        .Setup(x => x.GetSongsAsync(TestUserId, null, genre, null, 1, 20))
        .ReturnsAsync((songs, 3));

    // Act
    var result = await _controller.GetSongsByGenre(genre, page: 1, pageSize: 20);

    // Assert
    result.Should().BeOfType<OkObjectResult>();
    // Verify filtering behavior
}
```

#### **Edge Cases to Test**

- Empty/null genre parameters
- SQL injection attempts: `"Rock'; DROP TABLE Songs--"`
- XSS attempts: `"<script>alert('xss')</script>"`
- Invalid pagination: page <= 0, pageSize > 100
- Special characters: `"R&B"`, `"Hip-Hop"`, `"K-Pop"`
- No matching results
- Large datasets (1000+ songs)
- Last page with partial results

---

### ✨ **User Delight & Business Value**

#### **Musician Workflow Integration**

**Real-World Scenario:**
> *It's Tuesday night, 8 PM. A musician is planning Friday's setlist for a wedding reception. They need to find all their "Jazz" songs to create a sophisticated dinner music set.*

**User Experience Flow:**
1. **Open song library** → Page loads in <2 seconds
2. **Select "Jazz" from genre dropdown** → Populated from `GetGenresAsync()` with cached results
3. **Filter applied instantly** → Query returns in <20ms (indexed)
4. **See 47 Jazz songs** → Pagination shows "Page 1 of 3"
5. **Browse results alphabetically** → Ordered by Artist → Title (predictable)
6. **Add songs to setlist** → Build performance plan with confidence

**Why This Matters:**
- ✅ **Fast response** = Professional tool for time-sensitive planning
- ✅ **Predictable results** = "Jazz" returns only Jazz songs (no "Smooth Jazz Fusion" confusion)
- ✅ **Reliable pagination** = Can navigate large libraries efficiently
- ✅ **Dropdown UX** = No typos, no guessing genre names

#### **Business Value Metrics**

| Metric | Target | Achieved | Impact |
|--------|--------|----------|--------|
| **Query Performance** | <100ms | <20ms | 5x faster than target ✅ |
| **User Satisfaction** | "Fast enough" | "Instant" | Professional confidence ✅ |
| **Data Accuracy** | Correct results | Exact matching | Zero ambiguity ✅ |
| **Scalability** | 1,000 songs | 10,000 songs | Future-proof ✅ |

#### **Musician-Centric Design Decisions**

**Why Exact Match?**
- Musicians distinguish between "Rock", "Hard Rock", "Alternative Rock" for setlist planning
- Different genres = different performance energy = different audience expectations
- Exact matching respects musical categories as professional tools

**Why Dropdown + Caching?**
- Musicians work backstage with unreliable internet
- Cached genre list loads instantly from memory
- Dropdown prevents typos and ensures valid selections

**Why <20ms Performance?**
- Musicians make quick decisions during rehearsals
- Fast filtering feels "instant" = professional tool
- Slow filtering feels "sluggish" = amateur app

#### **Competitive Advantage**

**Compared to Alternative Approaches:**

| Feature | Setlist Studio | Competitor A | Competitor B |
|---------|---------------|--------------|--------------|
| **Genre Filtering** | Exact match | Fuzzy search | Text search |
| **Query Speed** | <20ms | ~200ms | ~500ms |
| **Result Accuracy** | 100% | ~80% | ~60% |
| **Professional Use** | ✅ Reliable | ⚠️ Inconsistent | ❌ Too slow |

#### **Real Musician Feedback** (Hypothetical)

> *"Finally! An app that understands Jazz ≠ Smooth Jazz. When I filter by genre, I get EXACTLY what I need for that vibe. No guessing, no scrolling through wrong results."*
> 
> — Professional Guitarist, Wedding Band

> *"The instant filtering is a game-changer. I can switch between Rock and Country setlists backstage in seconds. That's the speed I need when planning last-minute."*
> 
> — Acoustic Duo Leader

---

### 🎯 **Implementation Checklist**

When implementing genre filtering (or similar structured field filtering):

- [ ] **Service Layer**: Use exact match (`==`) for structured fields
- [ ] **Controller Layer**: Validate inputs (required, pagination, malicious content)
- [ ] **Database Layer**: Create composite index `(UserId, Genre)`
- [ ] **Security**: Verify user ownership in query, sanitize logs
- [ ] **Performance**: Target <100ms query time, <20ms with index
- [ ] **Caching**: Cache `GetGenresAsync()` results per user
- [ ] **Pagination**: Return tuple `(data, totalCount)`, validate bounds
- [ ] **Testing**: Cover happy path, edge cases, security scenarios
- [ ] **Documentation**: Explain why exact match chosen (performance + UX)
- [ ] **UX Design**: Implement dropdown populated from cached distinct genres

---

### 📖 **Further Reading**

- **Query Composition Pattern**: See `SongService.GetSongsAsync()` implementation
- **Index Strategy**: See `SetlistStudioDbContext.OnModelCreating()`
- **Security Validation**: See `SongsController.GetSongsByGenre()` validation
- **Cache-Aside Pattern**: See `QueryCacheService` implementation
- **Test Examples**: See `SongsControllerGenreFilterTests.cs` for comprehensive examples

---

### Security & Validation

```
"Add input validation for BPM values to ensure they're between 40 and 250"

"Implement authorization checks to ensure users can only access their own setlists"

"Create validation rules for musical keys to only accept valid key signatures (C, C#, Db, etc.)"

"Add data sanitization for artist names and song titles to prevent XSS attacks"

"Implement comprehensive input validation with regex patterns for musical keys and numeric ranges"

"Add anti-forgery token validation to all state-changing API endpoints"

"Generate code that passes CodeQL static analysis with zero high/critical security issues"

"Use null-conditional operators and null-forgiving operators appropriately to prevent null reference exceptions"

"Replace default() casts with explicit nullable casts like (HttpContext?)null to avoid useless upcast warnings"

"Implement proper resource disposal with using statements for all IDisposable objects"

"Use LINQ methods instead of foreach loops where appropriate - replace foreach + Add() with .Select()"

"Avoid creating variables that are assigned but never used - remove unnecessary variable assignments"

"Validate all user inputs and use parameterized queries exclusively to prevent SQL injection"

"Configure secure session cookies with HttpOnly, Secure, and SameSite attributes"

"Implement rate limiting on API endpoints to prevent DoS attacks (100 requests per minute per user)"

"Add security headers: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, CSP"

"Validate and sanitize all user inputs to prevent SQL injection and XSS attacks"

"Implement resource-based authorization to ensure users can only access their own data"

"Use parameterized queries exclusively - never concatenate user input into SQL strings"

"Add logging for security events: failed logins, suspicious activities, authorization failures"

"Implement secure password policy: 12+ characters, mixed case, numbers, special characters"

"Configure HTTPS redirection and HSTS headers for all production environments"

"Use environment variables or Azure Key Vault for all secrets - never hardcode credentials"

"Implement proper error handling that doesn't leak sensitive information to end users"

"Add audit trails for all data modifications with user tracking and timestamps"

"Configure CORS policy to only allow specific trusted domains, never use wildcards"

"Run security-focused CodeQL analysis locally before submitting PR: codeql database analyze codeql-database --output=security-analysis.sarif codeql/csharp-security-extended.qls"

"Analyze CodeQL findings and implement proper fixes rather than just suppressing alerts"

"Run comprehensive CodeQL analysis to match GitHub Actions: codeql database analyze codeql-database --output=github-analysis.sarif codeql/csharp-queries:codeql-suites/csharp-security-and-quality.qls"

"Validate that CodeQL security analysis shows zero results in security-analysis.sarif file"

"Distinguish between security findings (blocking) and quality findings (non-blocking) in CodeQL results"

"Address CodeQL SQL injection findings by using Entity Framework LINQ queries exclusively"

"Fix CodeQL XSS vulnerabilities by implementing proper input validation and output sanitization"

"Resolve CodeQL authentication bypass issues by adding proper authorization checks to all endpoints"

"Apply CodeQL cryptographic recommendations: use strong algorithms, proper key management, secure defaults"

"Understand that GitHub Actions may report 200+ quality findings while security analysis shows 0 vulnerabilities"

"Focus on security-specific CodeQL results rather than comprehensive quality analysis for security validation"

"Always verify security analysis results locally before relying on GitHub Actions comprehensive reports"

"Never merge code with CodeQL high/critical issues regardless of overall security scan status"
```

### Performance & Optimization

```
"Analyze database queries for N+1 problems and optimize with proper Include() statements"

"Add response caching to expensive operations like genre aggregations and artist listings"

"Implement query result caching for frequently accessed data using IMemoryCache or distributed cache"

"Optimize Entity Framework queries to avoid loading unnecessary navigation properties"

"Add database indexes for common query patterns: UserId, Artist, Genre, PerformanceDate"

"Implement pagination efficiently with Skip/Take and proper ordering to handle large datasets"

"Use asynchronous operations (async/await) consistently throughout the application for I/O operations"

"Add performance monitoring to track slow queries and API endpoint response times"

"Implement bulk operations for inserting/updating multiple songs or setlist items"

"Cache expensive calculations like setlist duration and song counts using computed properties"

"Add database connection pooling configuration for high-concurrency scenarios"

"Optimize JSON serialization by excluding unnecessary properties and using JsonIgnore attributes"

"Implement lazy loading patterns for large collections that aren't always needed"

"Add compression middleware for API responses to reduce bandwidth usage"

"Monitor memory usage patterns and implement proper disposal of database contexts and resources"

"Use compiled queries for frequently executed database operations to improve performance"

"Implement read-through caching patterns for user-specific data like song libraries and setlists"

"Add performance benchmarks and load testing to validate scalability improvements"

"Configure appropriate timeout values for database operations and HTTP requests"

"Implement background processing for non-critical operations that don't need immediate response"
```

### User Experience & Content

```
"Generate Swagger API examples using realistic song data like 'Bohemian Rhapsody' by Queen (BPM: 72, Key: Bb)"

"Create seed data with a diverse mix of musical genres including rock, jazz, classical, and electronic music"

"Add sample setlists for different types of performances (wedding, concert, practice session)"

"Design user-friendly error messages that use musical terminology musicians will understand"
```

### Code Organization

```
"Refactor the Song and Setlist classes with clearer property names and comprehensive XML documentation"

"Organize the API controllers into logical folders and add consistent routing patterns"

"Create a comprehensive README with setup instructions and API documentation"

"Add inline comments explaining the complex setlist transition logic"
```

### Maintainability & Business Continuity

```
"Generate code that facilitates easy team handover with clear business purpose and musician-focused terminology"

"Add comprehensive XML documentation explaining business logic from musician workflow perspective, not just technical implementation"

"Create onboarding documentation that allows new developers to be productive within 2-3 days maximum"

"Design features that clearly trace back to specific musician pain points and real-world performance scenarios"

"Implement sustainable technology choices prioritizing long-term stability over cutting-edge features"

"Add decision records explaining why specific architectural patterns were chosen from business impact perspective"

"Create migration strategies for scalability growth: SQLite to PostgreSQL, single-instance to load-balanced deployment"

"Generate realistic musical data models that reflect actual industry practices and authentic performance workflows"

"Design mobile-first responsive interfaces optimized for backstage and live performance environments"

"Implement offline capabilities for critical features that musicians need during performances without internet connection"

"Add performance benchmarks and monitoring that align with real-world musician usage patterns and venue requirements"

"Create deployment documentation covering multiple scenarios from solo artists to large music organizations"

"Design intuitive navigation that matches how musicians naturally think about and organize their music"

"Implement professional export formats suitable for sharing with venues, sound engineers, and band collaborators"

"Generate user experience flows optimized for quick data entry and fast setlist modifications during rehearsals and shows"

"Add comprehensive troubleshooting guides that non-technical musicians can follow for common deployment issues"

"Create automated health checks and monitoring that musicians can understand and act upon without technical expertise"

"Design feature documentation that explains business value and creative workflow impact, not just technical functionality"

"Implement dependency management strategies that minimize risk of obsolescence and ensure long-term project sustainability"

"Create team transition checklists ensuring smooth knowledge transfer and business continuity across developer changes"
```

---

## Security Standards & Guidelines

### MANDATORY Security Requirements

Setlist Studio maintains **strict security standards** that must be followed for all code contributions. Security is not optional - it's a fundamental requirement.

#### Authentication & Authorization

**Password Security Requirements:**
- **Minimum 12 characters** with mixed case, numbers, and special characters
- **Account lockout** after 5 failed attempts for 5 minutes
- **No password hints** or recovery questions
- **Secure password reset** with time-limited tokens

**Session Management:**
- Configure secure cookies with HttpOnly, Secure, and SameSite attributes
- Use secure cookie names with __Host- prefix
- Set appropriate session timeouts (2 hours max)
- Enable sliding expiration for better UX

**Authorization Checks:**
- **ALWAYS verify user ownership** of resources (songs, setlists)
- **Use resource-based authorization** for entity access
- **Never trust client-side authorization** - validate server-side

#### Input Validation & Sanitization

**CRITICAL: All user inputs must be validated and sanitized**

- Validate all string inputs for length, format, and malicious content
- Use regex patterns for structured data (musical keys, BPM ranges)
- Sanitize inputs to prevent XSS and injection attacks
- Check for malicious patterns (script tags, javascript:, etc.)
- Return meaningful validation error messages
- Never trust client-side validation alone

#### Security Headers (MANDATORY)

**ALL responses must include security headers:**

- X-Content-Type-Options: nosniff (prevent MIME type sniffing)
- X-Frame-Options: DENY (prevent clickjacking)
- X-XSS-Protection: 1; mode=block (XSS protection)
- Referrer-Policy: strict-origin-when-cross-origin
- Content-Security-Policy with restrictive defaults
- Permissions-Policy to disable unnecessary browser features
- Strict-Transport-Security for HTTPS enforcement (production only)

#### Secrets Management (CRITICAL)

**NEVER commit secrets to version control:**

- Never hardcode connection strings, API keys, or passwords in code
- Use Configuration providers (appsettings.json, environment variables)
- Implement Azure Key Vault for production secret management
- Use placeholder values in configuration files (YOUR_CLIENT_ID format)
- Validate that secrets are not placeholder values before using them

#### Rate Limiting & DoS Protection

**REQUIRED: All API endpoints must have rate limiting:**

- Configure rate limiting using Microsoft.AspNetCore.RateLimiting
- Implement different limits for different endpoint types (API: 100/min, Auth: 5/min)
- Use appropriate rate limiting algorithms (FixedWindow, SlidingWindow, etc.)
- Apply rate limiting attributes to controllers and actions
- Configure queue processing and overflow handling

#### Secure Logging Practices

**NEVER log sensitive information:**

- Never log passwords, tokens, API keys, or personal data
- Use user IDs instead of email addresses or names in logs
- Sanitize all logged data to remove sensitive fields
- Implement secure logging utilities that automatically filter sensitive data
- Log security events (failed logins, suspicious activities) appropriately

#### Database Security

**ALWAYS use parameterized queries:**

- Never concatenate user input directly into SQL strings
- Use Entity Framework LINQ queries exclusively for data access
- Always include user ownership validation in data queries
- Implement resource-based authorization for entity access
- Use strongly-typed query parameters to prevent injection attacks

#### CSRF Protection

**ALL state-changing operations must include CSRF protection:**

- Configure anti-forgery tokens with secure settings
- Use secure cookie names with __Host- prefix
- Apply ValidateAntiForgeryToken attribute to state-changing endpoints
- Configure CSRF tokens for AJAX requests
- Use SameSite=Strict and Secure cookie policies

### Security Validation Checklist

**Before submitting any code, verify:**

- [ ] **CodeQL Security Analysis** passes with zero high/critical security issues
  - Run: `codeql database analyze codeql-database --output=security-analysis.sarif codeql/csharp-security-extended.qls`
  - Verify: Results array is empty in SARIF file
- [ ] **Input validation** implemented for all user inputs
- [ ] **Authorization checks** verify user ownership of resources  
- [ ] **Parameterized queries** used exclusively (no string concatenation)
- [ ] **Security headers** configured in middleware
- [ ] **Rate limiting** applied to all API endpoints
- [ ] **Secrets** stored in environment variables or Key Vault
- [ ] **Error messages** don't leak sensitive information
- [ ] **Logging** doesn't expose sensitive data
- [ ] **CSRF protection** enabled for state-changing operations
- [ ] **HTTPS** enforced in production configurations

**Note**: GitHub Actions may report 200+ "findings" from comprehensive quality analysis, but these are mostly code quality improvements, not security vulnerabilities. Focus on the security-specific analysis results.

### Security Code Review Guidelines

**All pull requests must pass security review:**

1. **CodeQL Analysis**: All PRs must pass CodeQL static security analysis with zero high/critical issues
2. **Automated Security Scans**: All PRs trigger comprehensive security vulnerability scans
3. **Manual Security Review**: Security-sensitive changes require manual review
4. **Threat Modeling**: New features require security impact assessment
5. **Penetration Testing**: Regular security testing of the application

### Security Incident Response

**If a security vulnerability is discovered:**

1. **Immediate Action**: Create private security issue (not public)
2. **Assessment**: Evaluate impact and severity
3. **Remediation**: Develop and test fix
4. **Deployment**: Emergency deployment if critical
5. **Communication**: Notify stakeholders appropriately
6. **Post-Mortem**: Review and improve security processes

---

## User-Generated Content Security Patterns

### Overview

User-generated content (songs, setlists, templates) requires **defense-in-depth** security with validation at EVERY layer. This section provides specific patterns for securing musical data.

### 🎯 Security Threat Model for User Data

**See**: `docs/security-threat-model.md` for comprehensive threat analysis

**Critical Threats:**
1. **SQL Injection** - Database compromise via malicious queries
2. **XSS Attacks** - JavaScript injection in song titles, notes
3. **Horizontal Privilege Escalation** - Unauthorized access to other users' data
4. **Mass Assignment** - Unauthorized field modification
5. **Information Disclosure** - Stack traces revealing system internals
6. **Denial of Service** - Resource exhaustion via large inputs
7. **CSRF** - Unauthorized state changes
8. **IDOR** - Predictable ID enumeration

### 🛡️ Required Validation Patterns for Musical Data

#### **1. BPM (Beats Per Minute) Validation**

**CRITICAL**: BPM must be realistic (40-250) to prevent DoS and ensure data quality.

```csharp
// Custom validation attribute
[SafeBpm(40, 250)]
public int Bpm { get; set; }

// Implementation
public class SafeBpmAttribute : ValidationAttribute
{
    public int MinBpm { get; set; } = 40;
    public int MaxBpm { get; set; } = 250;
    
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is int bpm)
        {
            if (bpm < MinBpm || bpm > MaxBpm)
                return new ValidationResult($"BPM must be between {MinBpm} and {MaxBpm}");
        }
        return ValidationResult.Success;
    }
}
```

**Why These Limits:**
- Minimum 40 BPM: Slowest ballads (e.g., "The Scientist" by Coldplay)
- Maximum 250 BPM: Fastest extreme metal/drum & bass
- Common ranges: Ballads (60-80), Pop/Rock (90-140), Metal (160-200)

#### **2. Musical Key Validation**

**CRITICAL**: Use whitelist of 33 valid Western musical keys.

```csharp
[MusicalKey]
public string? Key { get; set; }

// Implementation
public class MusicalKeyAttribute : ValidationAttribute
{
    private static readonly string[] ValidKeys = new[]
    {
        // Major keys (17)
        "C", "C#", "Db", "D", "D#", "Eb", "E", "F", 
        "F#", "Gb", "G", "G#", "Ab", "A", "A#", "Bb", "B",
        
        // Minor keys (16)
        "Cm", "C#m", "Dbm", "Dm", "D#m", "Ebm", "Em", "Fm",
        "F#m", "Gbm", "Gm", "G#m", "Abm", "Am", "A#m", "Bbm", "Bm"
    };
    
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success; // Allow null/empty
            
        var key = value.ToString()!.Trim();
        if (!ValidKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            return new ValidationResult($"Invalid musical key. Must be one of: {string.Join(", ", ValidKeys)}");
            
        return ValidationResult.Success;
    }
}
```

#### **3. String Field Validation (Titles, Artists, Notes)**

**CRITICAL**: Prevent XSS, SQL injection, command injection while allowing musical notation.

```csharp
[SafeString(MaxLength = 200, AllowSpecialCharacters = true)]
public string Title { get; set; } = string.Empty;

// Implementation detects:
// - XSS patterns: <script>, javascript:, onclick=, etc.
// - SQL injection: UNION SELECT, DROP TABLE, --, OR 1=1, etc.
// - Command injection: ;, |, &&, backticks, etc.
// - But ALLOWS musical notation: apostrophes, ampersands, hyphens
```

**Musical Characters to Allow:**
- ✅ Apostrophes: "Rock 'n' Roll", "Guns N' Roses"
- ✅ Ampersands: "Simon & Garfunkel", "R&B"
- ✅ Pipes: "Rock|Alternative" for genre classification
- ✅ Hyphens: "Hip-Hop", "K-Pop"
- ✅ Parentheses: "Remastered (2023 Edition)"

### 🔒 Authorization Patterns for User Data

#### **Pattern 1: User Ownership Verification (MANDATORY)**

**RULE**: Every database query MUST filter by `userId` FIRST.

```csharp
// ✅ CORRECT: Controller layer
[HttpGet("{id}")]
[Authorize]
public async Task<IActionResult> GetSong(int id)
{
    var userId = SecureUserContext.GetSanitizedUserId(User);
    var song = await _songService.GetByIdAsync(id, userId);
    
    if (song == null)
        return NotFound(new { error = "Song not found or access denied" });
    
    return Ok(song);
}

// ✅ CORRECT: Service layer
public async Task<Song?> GetByIdAsync(int songId, string userId)
{
    return await _context.Songs
        .Where(s => s.Id == songId && s.UserId == userId)  // ALWAYS filter by userId
        .FirstOrDefaultAsync();
}
```

**❌ ANTI-PATTERNS:**
```csharp
// WRONG: No user ownership check
var song = await _context.Songs.FindAsync(id);  // Any user's song!

// WRONG: Trusting client-provided userId
var song = await _songService.GetByIdAsync(id, request.UserId);  // Attacker controls!

// WRONG: Authorization after data retrieval
var song = await _context.Songs.FindAsync(id);  // Already accessed!
if (song.UserId != userId) return Forbid();  // Too late!
```

#### **Pattern 2: State-Changing Operations Protection**

**RULE**: All POST/PUT/DELETE operations require CSRF tokens AND authorization.

```csharp
[HttpPost]
[Authorize]
[ValidateAntiForgeryToken]  // CSRF protection
[EnableRateLimiting("ApiPolicy")]  // DoS protection
public async Task<IActionResult> CreateSong([FromBody] CreateSongRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    var userId = SecureUserContext.GetSanitizedUserId(User);
    
    // Create entity with server-side userId (never trust client)
    var song = new Song
    {
        Title = request.Title,
        Artist = request.Artist,
        Bpm = request.Bpm,
        UserId = userId  // Set server-side, not from request!
    };
    
    var created = await _songService.CreateSongAsync(song);
    return CreatedAtAction(nameof(GetSong), new { id = created.Id }, created);
}
```

### 🧪 Security Testing Requirements

**MANDATORY**: Every feature MUST include security tests.

#### **Test Category 1: Malicious Input Detection**

```csharp
[Theory]
[InlineData("<script>alert('xss')</script>")]
[InlineData("javascript:alert('xss')")]
[InlineData("<img src=x onerror='alert(1)'>")]
[InlineData("'; DROP TABLE Songs--")]
[InlineData("' OR '1'='1")]
[InlineData("UNION SELECT * FROM Users--")]
public async Task CreateSong_WithMaliciousInput_ReturnsValidationError(string maliciousTitle)
{
    var request = new CreateSongRequest
    {
        Title = maliciousTitle,
        Artist = "Test Artist",
        Bpm = 120
    };
    
    var result = await _controller.CreateSong(request);
    
    result.Should().BeOfType<BadRequestObjectResult>();
}
```

#### **Test Category 2: Authorization Violations**

```csharp
[Fact]
public async Task GetSong_WithOtherUsersSong_ReturnsNotFound()
{
    // Arrange: Create song for User A
    var userASong = await _songService.CreateSongAsync(new Song
    {
        Title = "User A Song",
        Artist = "Artist",
        Bpm = 120,
        UserId = "userA-123"
    });
    
    // Act: User B tries to access User A's song
    _mockHttpContextAccessor.Setup(x => x.HttpContext.User.Identity.Name)
        .Returns("userB-456");
    
    var result = await _controller.GetSong(userASong.Id);
    
    // Assert: Access denied
    result.Should().BeOfType<NotFoundObjectResult>();
}
```

#### **Test Category 3: Validation Boundaries**

```csharp
[Theory]
[InlineData(39)]   // Below minimum
[InlineData(251)]  // Above maximum
[InlineData(-10)]  // Negative
[InlineData(0)]    // Zero
public async Task CreateSong_WithInvalidBpm_ReturnsValidationError(int invalidBpm)
{
    var request = new CreateSongRequest
    {
        Title = "Test Song",
        Artist = "Test Artist",
        Bpm = invalidBpm
    };
    
    var result = await _controller.CreateSong(request);
    
    result.Should().BeOfType<BadRequestObjectResult>();
}
```

### 📋 Security Implementation Checklist

**Before submitting ANY code:**

- [ ] **Input Validation**: BPM (40-250), Musical Keys (whitelist), String lengths (<500)
- [ ] **XSS Prevention**: SafeString validation on all text fields
- [ ] **SQL Injection Prevention**: Entity Framework LINQ only, no string concatenation
- [ ] **Authorization**: User ownership verified in EVERY query
- [ ] **CSRF Protection**: [ValidateAntiForgeryToken] on POST/PUT/DELETE
- [ ] **Rate Limiting**: [EnableRateLimiting] on all endpoints
- [ ] **Error Handling**: Generic messages, no stack traces exposed
- [ ] **Secure Logging**: Sanitize userId, never log passwords/tokens
- [ ] **Security Tests**: Malicious input, authorization, validation boundaries
- [ ] **CodeQL Scan**: Zero high/critical security issues

### 🚫 Common Security Anti-Patterns

**NEVER do these:**

```csharp
// ❌ 1. Client-side validation only
// Client: <input type="number" min="40" max="250" />
public int Bpm { get; set; }  // Missing server validation!

// ❌ 2. SQL string concatenation
var sql = $"SELECT * FROM Songs WHERE Title = '{title}'";  // SQL injection!

// ❌ 3. Missing [Authorize] attribute
[HttpGet]  // Allows anonymous access!
public async Task<IActionResult> GetSongs()

// ❌ 4. Exposing internal errors
catch (Exception ex) {
    return BadRequest(ex.Message);  // Information disclosure!
}

// ❌ 5. No user ownership check
var song = await _context.Songs.FindAsync(id);  // Any user's song!

// ❌ 6. Trusting client data
var song = new Song { UserId = request.UserId };  // Attacker controls!

// ❌ 7. Different error messages (user enumeration)
if (user == null) return NotFound("User not found");
if (!ValidPassword()) return Unauthorized("Invalid password");  // Leaks user existence!
```

### 📚 Security Resources

- **Threat Model**: `docs/security-threat-model.md`
- **Security Enhancements**: `docs/security-enhancements.md`
- **CodeQL Workflow**: `docs/codeql-workflow.md`
- **OWASP Top 10**: https://owasp.org/www-project-top-ten/

---

## Detailed Security Implementation Guide

### Musical Data Validation Rules

#### **1. BPM (Beats Per Minute) Validation**

**Realistic Range**: 40-250 BPM covers all musical genres from ballads to extreme metal.

**Implementation Pattern:**
```csharp
[SafeBpm(40, 250)]
public int Bpm { get; set; }

// SafeBpmAttribute implementation
public class SafeBpmAttribute : ValidationAttribute
{
    public int MinBpm { get; set; } = 40;
    public int MaxBpm { get; set; } = 250;
    
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is int bpm)
        {
            if (bpm < MinBpm || bpm > MaxBpm)
                return new ValidationResult($"BPM must be between {MinBpm} and {MaxBpm}");
        }
        return ValidationResult.Success;
    }
}
```

**Why These Limits:**
- **Minimum 40 BPM**: Slowest ballads (e.g., "The Scientist" by Coldplay: 40-50 BPM)
- **Maximum 250 BPM**: Fastest extreme metal/drum & bass (rare but legitimate)
- **Common Ranges**: Ballads (60-80), Pop/Rock (90-140), Punk/Metal (160-200)

**Anti-Pattern Examples:**
```csharp
// ❌ WRONG: No validation (allows negative, unrealistic values)
public int Bpm { get; set; }

// ❌ WRONG: Too restrictive (excludes legitimate ballads and fast songs)
[Range(80, 160)]
public int Bpm { get; set; }

// ❌ WRONG: No upper limit (allows denial of service via integer overflow)
[Range(40, int.MaxValue)]
public int Bpm { get; set; }
```

---

#### **2. Musical Key Validation**

**Valid Keys**: 33 standard Western musical keys (17 major + 16 minor).

**Implementation Pattern:**
```csharp
[MusicalKey]
public string? Key { get; set; }

// MusicalKeyAttribute implementation
public class MusicalKeyAttribute : ValidationAttribute
{
    private static readonly string[] ValidKeys = new[]
    {
        // Major keys (17)
        "C", "C#", "Db", "D", "D#", "Eb", "E", "F", 
        "F#", "Gb", "G", "G#", "Ab", "A", "A#", "Bb", "B",
        
        // Minor keys (16)
        "Cm", "C#m", "Dbm", "Dm", "D#m", "Ebm", "Em", "Fm",
        "F#m", "Gbm", "Gm", "G#m", "Abm", "Am", "A#m", "Bbm", "Bm"
    };
    
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success; // Allow null/empty
            
        var key = value.ToString()!.Trim();
        if (!ValidKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            return new ValidationResult($"Invalid musical key. Must be one of: {string.Join(", ", ValidKeys)}");
            
        return ValidationResult.Success;
    }
}
```

**Why These Keys:**
- **Enharmonic Equivalents**: Both C# and Db are valid (same pitch, different notation)
- **Minor Keys**: Identified by lowercase 'm' suffix (e.g., "Am" for A minor)
- **No Exotic Modes**: Dorian, Phrygian, etc. should be stored as their parent keys

**Anti-Pattern Examples:**
```csharp
// ❌ WRONG: String validation without whitelist (allows SQL injection)
public string Key { get; set; }

// ❌ WRONG: Case-sensitive comparison (rejects valid "am" for "Am")
if (Key != "Am") throw new ValidationException();

// ❌ WRONG: Overly permissive (allows arbitrary strings)
[StringLength(10)]
public string Key { get; set; }

// ❌ WRONG: Missing enharmonic equivalents (rejects "Db" when only "C#" allowed)
private static readonly string[] ValidKeys = { "C", "C#", "D", ... }; // Missing Db, Eb, etc.
```

---

#### **3. Song Title & Artist Name Validation**

**Requirements**: Prevent XSS, SQL injection, command injection while allowing musical notation.

**Implementation Pattern:**
```csharp
[SafeString(MaxLength = 500, AllowMusicalKeys = true, AllowSpecialCharacters = true)]
public string Title { get; set; } = string.Empty;

[SafeString(MaxLength = 300, AllowMusicalKeys = true, AllowSpecialCharacters = true)]
public string Artist { get; set; } = string.Empty;

// SafeStringAttribute implementation
public class SafeStringAttribute : ValidationAttribute
{
    public int MaxLength { get; set; } = 1000;
    public bool AllowEmpty { get; set; } = false;
    public bool AllowMusicalKeys { get; set; } = false;
    public bool AllowNewlines { get; set; } = false;
    public bool AllowSpecialCharacters { get; set; } = false;
    
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return AllowEmpty ? ValidationResult.Success 
                : new ValidationResult("This field is required");
        }
        
        var input = value.ToString()!;
        
        // Length validation
        if (input.Length > MaxLength)
            return new ValidationResult($"Maximum length is {MaxLength} characters");
        
        // XSS patterns (always blocked)
        var xssPatterns = new[]
        {
            @"<script[^>]*>.*?</script>", @"javascript:", @"vbscript:",
            @"onload\s*=", @"onerror\s*=", @"onclick\s*=", @"onmouseover\s*=",
            @"<iframe", @"<object", @"<embed", @"<form", @"<input"
        };
        
        foreach (var pattern in xssPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                return new ValidationResult("Input contains potentially dangerous content");
        }
        
        // SQL injection patterns (always blocked)
        var sqlPatterns = new[]
        {
            @"(?:UNION\s+SELECT|DROP\s+TABLE|DELETE\s+FROM|INSERT\s+INTO)",
            @"(?:--|/\*|\*/)", @"(?:'\s*OR\s*'|""\s*OR\s*"")",
            @"(?:OR\s+1\s*=\s*1|OR\s+'1'\s*=\s*'1')"
        };
        
        foreach (var pattern in sqlPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                return new ValidationResult("Input contains potentially dangerous SQL content");
        }
        
        // Command injection patterns (conditional based on AllowSpecialCharacters)
        if (!AllowSpecialCharacters)
        {
            var commandPatterns = new[]
            {
                @"[;`$(){}[\]\\]", @"\.\./", @"(?:cmd|powershell|bash|sh)\s"
            };
            
            foreach (var pattern in commandPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                    return new ValidationResult("Input contains invalid characters");
            }
        }
        else
        {
            // Allow & and | for musical notation (e.g., "R&B", "Rock|Alternative")
            // but still block shell metacharacters
            if (Regex.IsMatch(input, @"[;`$(){}[\]\\]|\.\.\/|(?:cmd|powershell|bash|sh)\s", 
                RegexOptions.IgnoreCase))
                return new ValidationResult("Input contains invalid characters");
        }
        
        // Log injection patterns (conditional based on AllowNewlines)
        if (!AllowNewlines && input.Any(c => c < 32 && c != 9)) // Allow tab (9)
            return new ValidationResult("Input contains control characters");
        
        return ValidationResult.Success;
    }
}
```

**Musical Context Considerations:**
- **Allow apostrophes**: "Rock 'n' Roll", "Guns N' Roses"
- **Allow ampersands**: "Simon & Garfunkel", "R&B"
- **Allow pipes**: "Rock|Alternative" for genre classification
- **Allow dashes/hyphens**: "Hip-Hop", "K-Pop"
- **Allow parentheses in display**: "Remastered (2023 Edition)"

**Anti-Pattern Examples:**
```csharp
// ❌ WRONG: No validation (XSS, SQL injection vulnerable)
public string Title { get; set; }

// ❌ WRONG: Blocks legitimate musical characters
[RegularExpression(@"^[a-zA-Z0-9\s]+$")] // Rejects "Rock 'n' Roll", "R&B"
public string Artist { get; set; }

// ❌ WRONG: Only client-side validation (bypassable)
// Client: <input type="text" maxlength="500" required />
// Server: public string Title { get; set; } // No validation!

// ❌ WRONG: Strips characters instead of rejecting (data loss)
public string Title 
{ 
    get => _title; 
    set => _title = Regex.Replace(value, @"[<>]", ""); // Loses user intent
}
```

---

#### **4. Genre Validation**

**Approach**: Use enumeration or database whitelist for structured data.

**Implementation Pattern:**
```csharp
// Option 1: Enum-based (compile-time safety)
public enum Genre
{
    Rock, Pop, Jazz, Blues, Country, Folk, Classical,
    HipHop, Electronic, Metal, Punk, Alternative, Indie,
    RnB, Soul, Funk, Disco, Reggae, Latin, World,
    DeathMetal, HeavyMetal, ProgressiveRock
}

[Required]
public Genre Genre { get; set; }

// Option 2: Database whitelist (runtime flexibility)
[SafeString(MaxLength = 100)]
public string? Genre { get; set; }

// In service layer:
private static readonly HashSet<string> ValidGenres = new(StringComparer.OrdinalIgnoreCase)
{
    "Rock", "Pop", "Jazz", "Blues", "Country", "Folk", "Classical",
    "Hip-Hop", "Electronic", "Metal", "Punk", "Alternative", "Indie",
    "R&B", "Soul", "Funk", "Disco", "Reggae", "Latin", "World",
    "Death Metal", "Heavy Metal", "Progressive Rock"
};

public async Task<Song> CreateSongAsync(CreateSongRequest request, string userId)
{
    if (!string.IsNullOrWhiteSpace(request.Genre) && 
        !ValidGenres.Contains(request.Genre))
    {
        throw new ValidationException($"Invalid genre. Valid genres: {string.Join(", ", ValidGenres)}");
    }
    // ... create song
}
```

**Why Whitelist Validation:**
- **Exact Matching**: Ensures consistent genre classification
- **Performance**: Enables database index usage for filtering
- **User Experience**: Dropdown selection prevents typos
- **Caching**: Genre list can be cached for fast retrieval

**Anti-Pattern Examples:**
```csharp
// ❌ WRONG: Free-text genre (inconsistent data)
public string Genre { get; set; } // Allows "rock", "Rock", "ROCK", "Rokc"

// ❌ WRONG: No validation (allows SQL injection)
var genre = request.Genre;
var songs = await _db.ExecuteSqlRawAsync($"SELECT * FROM Songs WHERE Genre = '{genre}'");

// ❌ WRONG: Case-sensitive comparison (rejects valid inputs)
if (request.Genre != "Rock") throw new ValidationException();

// ❌ WRONG: Overly permissive regex (allows arbitrary strings)
[RegularExpression(@"^[a-zA-Z\s-]+$")] // Allows "DELETE FROM Songs"
public string Genre { get; set; }
```

---

### Authorization Patterns for Musical Data

#### **1. Resource-Based Authorization (User Ownership)**

**CRITICAL**: Every data access MUST verify user ownership.

**Implementation Pattern:**
```csharp
// ✅ CORRECT: SongsController.GetSong()
[HttpGet("{id}")]
[Authorize]
public async Task<IActionResult> GetSong(int id)
{
    // 1. Extract authenticated user ID
    var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();
    
    // 2. Retrieve resource with user ownership validation
    var song = await _songService.GetByIdAsync(id, userId);
    
    // 3. Verify ownership (service returns null if not owned by user)
    if (song == null)
        return NotFound(new { error = "Song not found or access denied" });
    
    return Ok(song);
}

// SongService.GetByIdAsync()
public async Task<Song?> GetByIdAsync(int songId, string userId)
{
    // ALWAYS filter by userId in query
    return await _context.Songs
        .Where(s => s.Id == songId && s.UserId == userId)
        .FirstOrDefaultAsync();
}
```

**Why This Matters:**
- **Prevents Horizontal Privilege Escalation**: User A cannot access User B's songs
- **Defense in Depth**: Authorization at controller AND service layer
- **Explicit Ownership**: Every query includes `UserId` filter

**Anti-Pattern Examples:**
```csharp
// ❌ WRONG: No user ownership check (horizontal privilege escalation)
[HttpGet("{id}")]
public async Task<IActionResult> GetSong(int id)
{
    var song = await _context.Songs.FindAsync(id); // Missing userId filter!
    return Ok(song);
}

// ❌ WRONG: Client-side authorization only
[HttpGet("{id}")]
public async Task<IActionResult> GetSong(int id, [FromQuery] string userId)
{
    // Never trust userId from query parameters!
    var song = await _context.Songs.FirstAsync(s => s.Id == id && s.UserId == userId);
    return Ok(song);
}

// ❌ WRONG: Authorization after data retrieval
[HttpGet("{id}")]
public async Task<IActionResult> GetSong(int id)
{
    var song = await _context.Songs.FindAsync(id); // Retrieves any user's song
    var userId = User.Identity?.Name;
    if (song.UserId != userId) return Forbid(); // Too late! Data already accessed
    return Ok(song);
}

// ❌ WRONG: Missing [Authorize] attribute (allows anonymous access)
[HttpGet("{id}")]
public async Task<IActionResult> GetSong(int id)
{
    // ... implementation
}
```

---

#### **2. Operation-Level Authorization (CRUD Operations)**

**Pattern**: Verify user can perform specific operation on specific resource.

**Implementation Pattern:**
```csharp
// ✅ CORRECT: SongsController.UpdateSong()
[HttpPut("{id}")]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateSong(int id, [FromBody] UpdateSongRequest request)
{
    var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();
    
    try
    {
        // Verify ownership before update
        var existingSong = await _songService.GetByIdAsync(id, userId);
        if (existingSong == null)
            return NotFound(new { error = "Song not found or access denied" });
        
        // Perform update with ownership validation
        var updated = await _songService.UpdateAsync(id, request, userId);
        return Ok(updated);
    }
    catch (UnauthorizedAccessException)
    {
        return Forbid();
    }
}

// ✅ CORRECT: SongsController.DeleteSong()
[HttpDelete("{id}")]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteSong(int id)
{
    var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();
    
    try
    {
        var deleted = await _songService.DeleteAsync(id, userId);
        if (!deleted)
            return NotFound(new { error = "Song not found or access denied" });
        
        return NoContent();
    }
    catch (UnauthorizedAccessException)
    {
        return Forbid();
    }
}

// SongService.DeleteAsync() with soft delete
public async Task<bool> DeleteAsync(int songId, string userId)
{
    var song = await _context.Songs
        .Where(s => s.Id == songId && s.UserId == userId)
        .FirstOrDefaultAsync();
    
    if (song == null)
        return false; // Not found or unauthorized
    
    // Soft delete (recommended for audit trail)
    song.IsDeleted = true;
    song.DeletedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    
    return true;
}
```

**Anti-Pattern Examples:**
```csharp
// ❌ WRONG: Missing CSRF protection on state-changing operation
[HttpDelete("{id}")]
[Authorize]
// Missing [ValidateAntiForgeryToken]!
public async Task<IActionResult> DeleteSong(int id)
{
    // Vulnerable to CSRF attacks
}

// ❌ WRONG: No ownership verification before delete
[HttpDelete("{id}")]
[Authorize]
public async Task<IActionResult> DeleteSong(int id)
{
    await _context.Songs.Where(s => s.Id == id).ExecuteDeleteAsync(); // Deletes ANY user's song!
    return NoContent();
}

// ❌ WRONG: Exposing whether resource exists for other users (information disclosure)
[HttpGet("{id}")]
[Authorize]
public async Task<IActionResult> GetSong(int id)
{
    var song = await _context.Songs.FindAsync(id);
    if (song == null)
        return NotFound("Song not found"); // Different message!
    
    var userId = User.Identity?.Name;
    if (song.UserId != userId)
        return Forbid("You don't own this song"); // Reveals song exists!
    
    return Ok(song);
}
// ✅ CORRECT: Same error message for both cases
return NotFound(new { error = "Song not found or access denied" });
```

---

#### **3. Setlist Collaboration Authorization**

**Future Feature**: Allow band members to collaborate on shared setlists.

**Implementation Pattern:**
```csharp
// SetlistCollaborator entity
public class SetlistCollaborator
{
    public int Id { get; set; }
    public int SetlistId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public CollaboratorRole Role { get; set; }
    public DateTime InvitedAt { get; set; }
}

public enum CollaboratorRole
{
    Owner,      // Full control
    Editor,     // Can modify setlist
    Viewer      // Read-only access
}

// Authorization service
public async Task<bool> CanAccessSetlistAsync(int setlistId, string userId, CollaboratorRole minimumRole)
{
    var collaborator = await _context.SetlistCollaborators
        .Where(c => c.SetlistId == setlistId && c.UserId == userId)
        .FirstOrDefaultAsync();
    
    if (collaborator == null)
        return false;
    
    return collaborator.Role >= minimumRole;
}

// SetlistsController usage
[HttpPut("{id}")]
[Authorize]
public async Task<IActionResult> UpdateSetlist(int id, [FromBody] UpdateSetlistRequest request)
{
    var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();
    
    // Check editor or owner permission
    var canEdit = await _authorizationService.CanAccessSetlistAsync(id, userId, CollaboratorRole.Editor);
    if (!canEdit)
        return Forbid();
    
    var updated = await _setlistService.UpdateAsync(id, request);
    return Ok(updated);
}
```

**Anti-Pattern Examples:**
```csharp
// ❌ WRONG: Overly permissive (any authenticated user can edit)
[HttpPut("{id}")]
[Authorize]
public async Task<IActionResult> UpdateSetlist(int id, [FromBody] UpdateSetlistRequest request)
{
    var setlist = await _context.Setlists.FindAsync(id); // No ownership check!
    // ... update
}

// ❌ WRONG: Hardcoded roles (inflexible, not scalable)
if (User.IsInRole("Admin") || User.IsInRole("BandLeader"))
{
    // ... allow access
}

// ❌ WRONG: Missing audit trail for shared access
public async Task AddCollaboratorAsync(int setlistId, string userId, CollaboratorRole role)
{
    await _context.SetlistCollaborators.AddAsync(new SetlistCollaborator
    {
        SetlistId = setlistId,
        UserId = userId,
        Role = role
        // Missing: InvitedBy, InvitedAt, AcceptedAt
    });
}
```

---

### Common Security Anti-Patterns (What NOT to Do)

#### **1. Input Validation Anti-Patterns**

**❌ Trusting Client-Side Validation Only:**
```csharp
// Client-side (HTML)
<input type="number" min="40" max="250" required />

// Server-side (WRONG - no validation!)
public int Bpm { get; set; }
```
**Why It's Wrong**: Client-side validation can be bypassed using browser dev tools or API calls.

---

**❌ Sanitizing Instead of Rejecting:**
```csharp
// WRONG: Data loss and potential bypass
public string Title
{
    get => _title;
    set => _title = value.Replace("<", "").Replace(">", ""); // Loses user intent
}
```
**Why It's Wrong**: Sanitization can be bypassed (e.g., `<<script>script>`) and loses user data.

---

**❌ Blacklist Instead of Whitelist:**
```csharp
// WRONG: Incomplete blacklist (easily bypassed)
if (input.Contains("<script>") || input.Contains("DROP TABLE"))
{
    throw new ValidationException();
}
// Bypass: "<SCRIPT>", "drop table", "dRoP tAbLe"
```
**Why It's Wrong**: Attackers find creative bypasses. Use whitelists for structured data.

---

#### **2. Authorization Anti-Patterns**

**❌ Missing [Authorize] Attribute:**
```csharp
// WRONG: Allows anonymous access
[HttpGet]
public async Task<IActionResult> GetUserSongs()
{
    var userId = User.Identity?.Name; // null for anonymous!
    var songs = await _songService.GetByUserIdAsync(userId);
    return Ok(songs);
}
```
**Why It's Wrong**: Sensitive endpoints accessible without authentication.

---

**❌ Trusting User-Provided IDs:**
```csharp
// WRONG: Horizontal privilege escalation
[HttpGet]
[Authorize]
public async Task<IActionResult> GetSongs([FromQuery] string userId)
{
    // Attacker can request any user's songs!
    var songs = await _songService.GetByUserIdAsync(userId);
    return Ok(songs);
}
```
**Why It's Wrong**: Users can access other users' data by changing query parameters.

---

**❌ Authorization After Data Retrieval:**
```csharp
// WRONG: Data accessed before authorization check
var song = await _context.Songs.FindAsync(id); // Retrieves from database
if (song.UserId != userId) return Forbid(); // Too late!
```
**Why It's Wrong**: Timing attacks, logging exposure, defense in depth violation.

---

#### **3. Database Security Anti-Patterns**

**❌ String Concatenation in Queries:**
```csharp
// WRONG: SQL injection vulnerability
var genre = request.Genre;
var query = $"SELECT * FROM Songs WHERE Genre = '{genre}'";
var songs = await _context.Database.ExecuteSqlRawAsync(query);
// Attack: genre = "'; DROP TABLE Songs--"
```
**Why It's Wrong**: Direct SQL injection leading to data breach or destruction.

---

**❌ Using ToLower() in Service Layer:**
```csharp
// WRONG: Breaks database index usage
var songs = await _context.Songs
    .Where(s => s.Genre.ToLower() == genre.ToLower()) // Full table scan!
    .ToListAsync();
```
**Why It's Wrong**: ToLower() prevents index usage, causing severe performance degradation.

---

**❌ N+1 Query Problem:**
```csharp
// WRONG: Multiple database round-trips
var setlists = await _context.Setlists.ToListAsync();
foreach (var setlist in setlists)
{
    setlist.Songs = await _context.Songs
        .Where(s => s.SetlistId == setlist.Id)
        .ToListAsync(); // N queries!
}
```
**Why It's Wrong**: Performance disaster with large datasets.

---

#### **4. Error Handling Anti-Patterns**

**❌ Exposing Stack Traces:**
```csharp
// WRONG: Information disclosure
catch (Exception ex)
{
    return BadRequest(ex.Message); // Exposes internal details!
}
```
**Why It's Wrong**: Reveals system architecture, file paths, technology stack to attackers.

---

**❌ Different Error Messages:**
```csharp
// WRONG: User enumeration vulnerability
var user = await _userManager.FindByEmailAsync(email);
if (user == null)
    return NotFound("User not found"); // Different message!
if (!await _userManager.CheckPasswordAsync(user, password))
    return Unauthorized("Invalid password"); // Confirms user exists!
```
**Why It's Wrong**: Attacker can enumerate valid usernames/emails.

---

**❌ Logging Sensitive Data:**
```csharp
// WRONG: PII in logs
_logger.LogInformation("User {Email} logged in with password {Password}", 
    user.Email, password);
```
**Why It's Wrong**: Exposes sensitive data in log files.

---

#### **5. Secrets Management Anti-Patterns**

**❌ Hardcoded Secrets:**
```csharp
// WRONG: Secret in source code
var connectionString = "Server=prod.db;User=admin;Password=P@ssw0rd123";
```
**Why It's Wrong**: Committed to version control, visible in plain text.

---

**❌ Secrets in Configuration Files:**
```json
// appsettings.json (WRONG)
{
  "ConnectionStrings": {
    "Default": "Server=prod;User=sa;Password=RealPassword123"
  }
}
```
**Why It's Wrong**: Configuration files often committed to repositories.

---

**❌ Using Placeholder Values in Production:**
```csharp
// WRONG: No validation
var clientId = _configuration["OAuth:Google:ClientId"];
// Uses placeholder "YOUR_CLIENT_ID" in production!
```
**Why It's Wrong**: Application fails silently with placeholder values.

---

### Security Testing Patterns

#### **1. Malicious Input Testing**

**Test XSS Attempts:**
```csharp
[Theory]
[InlineData("<script>alert('xss')</script>")]
[InlineData("javascript:alert('xss')")]
[InlineData("<img src=x onerror='alert(1)'>")]
[InlineData("';alert(String.fromCharCode(88,83,83))//")]
public async Task CreateSong_WithXSSPayload_ReturnsValidationError(string maliciousTitle)
{
    var request = new CreateSongRequest
    {
        Title = maliciousTitle,
        Artist = "Test Artist",
        Bpm = 120
    };
    
    var result = await _controller.CreateSong(request);
    
    result.Should().BeOfType<BadRequestObjectResult>();
}
```

---

**Test SQL Injection Attempts:**
```csharp
[Theory]
[InlineData("'; DROP TABLE Songs--")]
[InlineData("' OR '1'='1")]
[InlineData("'; DELETE FROM Songs WHERE '1'='1")]
[InlineData("UNION SELECT * FROM Users--")]
public async Task GetSongsByGenre_WithSQLInjection_ReturnsBadRequest(string maliciousGenre)
{
    var result = await _controller.GetSongsByGenre(maliciousGenre);
    
    result.Should().BeOfType<BadRequestObjectResult>();
    var response = (result as BadRequestObjectResult)?.Value as dynamic;
    response?.error.ToString().Should().Contain("Invalid genre");
}
```

---

**Test Command Injection Attempts:**
```csharp
[Theory]
[InlineData("Song Title; rm -rf /")]
[InlineData("Artist Name && cat /etc/passwd")]
[InlineData("Title | powershell -Command Get-Process")]
public async Task CreateSong_WithCommandInjection_ReturnsValidationError(string maliciousInput)
{
    var request = new CreateSongRequest
    {
        Title = maliciousInput,
        Artist = "Test",
        Bpm = 120
    };
    
    var result = await _controller.CreateSong(request);
    
    result.Should().BeOfType<BadRequestObjectResult>();
}
```

---

#### **2. Authorization Testing**

**Test Horizontal Privilege Escalation:**
```csharp
[Fact]
public async Task GetSong_WithDifferentUsersSong_ReturnsNotFound()
{
    // Arrange: Create song owned by User A
    var userASong = await _songService.CreateAsync(new CreateSongRequest
    {
        Title = "User A Song",
        Artist = "Artist",
        Bpm = 120
    }, userIdA);
    
    // Act: User B tries to access User A's song
    _mockHttpContextAccessor.Setup(x => x.HttpContext.User.Identity.Name)
        .Returns(userIdB);
    
    var result = await _controller.GetSong(userASong.Id);
    
    // Assert: Access denied
    result.Should().BeOfType<NotFoundObjectResult>();
}
```

---

**Test Missing Authorization:**
```csharp
[Fact]
public void SongsController_AllEndpoints_HaveAuthorizeAttribute()
{
    var controller = typeof(SongsController);
    var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());
    
    foreach (var method in methods)
    {
        var hasAuthorize = method.GetCustomAttribute<AuthorizeAttribute>() != null
            || controller.GetCustomAttribute<AuthorizeAttribute>() != null;
        
        hasAuthorize.Should().BeTrue(
            $"Method {method.Name} should have [Authorize] attribute");
    }
}
```

---

#### **3. Rate Limiting Testing**

**Test Rate Limit Enforcement:**
```csharp
[Fact]
public async Task CreateSong_ExceedsRateLimit_ReturnsTooManyRequests()
{
    // Arrange: Configure rate limiter for testing
    var requests = new List<Task<IActionResult>>();
    
    // Act: Make 101 requests (limit is 100/min)
    for (int i = 0; i < 101; i++)
    {
        requests.Add(_controller.CreateSong(new CreateSongRequest
        {
            Title = $"Song {i}",
            Artist = "Artist",
            Bpm = 120
        }));
    }
    
    var results = await Task.WhenAll(requests);
    
    // Assert: Last request should be rate limited
    results.Last().Should().BeOfType<StatusCodeResult>()
        .Which.StatusCode.Should().Be(429); // Too Many Requests
}
```

---

### Security Documentation Checklist

**Before deploying any feature:**

- [ ] **Input Validation**: All user inputs validated with whitelist/regex patterns
- [ ] **Authorization**: User ownership verified at service AND controller layers
- [ ] **Security Headers**: Configured in middleware (X-Frame-Options, CSP, etc.)
- [ ] **Rate Limiting**: Applied to all public endpoints
- [ ] **CSRF Protection**: [ValidateAntiForgeryToken] on state-changing operations
- [ ] **Error Handling**: No stack traces or sensitive data in error messages
- [ ] **Logging**: Sensitive data sanitized before logging
- [ ] **Secrets**: No hardcoded credentials, using environment variables/Key Vault
- [ ] **Database**: Only parameterized queries, user ownership filters
- [ ] **Security Tests**: XSS, SQL injection, authorization, rate limiting tested
- [ ] **CodeQL**: Zero high/critical security issues

---

## Quick Start Guide

When contributing to Setlist Studio:

1. **Read the codebase**: Familiarize yourself with existing patterns and conventions
2. **Follow the principles**: Keep reliability, scalability, **security**, maintainability, and delight in mind
3. **Security first**: Always implement security requirements (validation, authorization, secure headers, rate limiting) before adding functionality
4. **CodeQL compliance**: Generate code that passes CodeQL static analysis with zero high/critical security issues and follows best practices (null safety, LINQ usage, resource disposal)
5. **Match tests to source files**: Every test file must correspond to exactly one source code file using the `{SourceClass}Tests.cs` naming pattern
6. **Use realistic examples**: When creating tests or documentation, use authentic musical data
7. **Test thoroughly**: Ensure your code works correctly and handles edge cases with 80%+ line and branch coverage
8. **Organize tests strategically**: 
   - Add core functionality tests to base test files (e.g., `SetlistServiceTests.cs`)
   - Create advanced test files for edge cases, error handling, and coverage gaps when base files exceed ~1,400 lines
   - Use the `{SourceClass}AdvancedTests.cs` naming pattern for specialized testing scenarios
9. **Target coverage gaps**: Use coverage reports to identify areas needing additional testing and create focused advanced test suites
10. **Security validation**: Complete the security checklist before submitting any pull request
11. **Document your work**: Add clear comments and update documentation as needed

### Quick Start Checklist

**Development Setup:**
- [ ] Clone repository and set up development environment
- [ ] Run `dotnet test` to ensure **100% of tests pass** (zero failures allowed)
- [ ] Generate coverage report to understand current coverage status
- [ ] Review existing code patterns and test organization
- [ ] Create feature branch following naming conventions

**Security First Development:**
- [ ] Review security requirements in this document
- [ ] Implement input validation for all user inputs
- [ ] Add authorization checks for data access
- [ ] Configure security headers and rate limiting
- [ ] Use parameterized queries exclusively
- [ ] Store secrets in environment variables or Key Vault

**CodeQL Compliance:**
- [ ] Generate code using null-conditional operators (`?.`) and null-forgiving operators (`!`) appropriately
- [ ] Use explicit nullable casts like `(HttpContext?)null` instead of `default(HttpContext)`
- [ ] Implement proper resource disposal with `using` statements for IDisposable objects
- [ ] Replace foreach loops with LINQ methods (`.Select()`, `.Where()`, `.Any()`) where appropriate
- [ ] Avoid creating variables that are assigned but never used
- [ ] Ensure all user inputs are validated and use parameterized queries exclusively
- [ ] Run local CodeQL security analysis: `codeql database analyze codeql-database --output=security-analysis.sarif codeql/csharp-security-extended.qls`
- [ ] Verify zero high/critical security issues in CodeQL results

**Testing & Quality:**
- [ ] Write tests first (TDD approach recommended)
- [ ] Ensure 80%+ line and branch coverage for new code
- [ ] Include security test cases (authentication, authorization, validation)
- [ ] Test with malicious inputs and edge cases

**Performance & Scalability:**
- [ ] Ensure API endpoints respond within 500ms under normal load
- [ ] Optimize database queries to complete within 100ms
- [ ] Implement proper pagination for large datasets (already exists)
- [ ] Use async/await consistently for I/O operations
- [ ] Add appropriate database indexes for user-specific queries
- [ ] Monitor memory usage patterns and implement proper disposal
- [ ] Consider caching for expensive operations (genres, artist aggregations)
- [ ] Test with realistic data volumes (1000+ songs, 100+ setlists per user)

**Code Review Preparation:**
- [ ] Complete security validation checklist
- [ ] Run security scans and address any issues
- [ ] Ensure CodeQL analysis passes with zero high/critical issues
- [ ] Document security considerations in PR description
- [ ] Submit pull request with clear description and test evidence

---

## FINAL ENFORCEMENT REMINDER

**Every time you create or modify tests, you MUST:**

1. **CHECK FIRST**: Use `file_search` to verify if `{SourceClass}Tests.cs` exists
2. **ENHANCE EXISTING**: Add to base test file before creating new files
3. **FOLLOW NAMING**: Only use `{SourceClass}Tests.cs` or `{SourceClass}AdvancedTests.cs`
4. **NO CUSTOM NAMES**: Never create "FocusedTests", "CoverageTests", "SpecializedTests", etc.
5. **VALIDATE SIZE**: Create advanced tests only when base file exceeds 1,400 lines

**This is not optional - it's mandatory for all test file operations.**

---

## SECURITY ENFORCEMENT REMINDER

**Security is MANDATORY - not optional. Every contribution must:**

1. **PASS CODEQL SECURITY ANALYSIS**: All code must pass CodeQL security-focused analysis with zero high/critical issues
   - Run: `codeql database analyze codeql-database --output=security-analysis.sarif codeql/csharp-security-extended.qls`
   - Verify: Empty results array in SARIF file
2. **VALIDATE ALL INPUTS**: No user input is trusted without validation and sanitization
3. **AUTHORIZE ALL ACCESS**: Every data access must verify user ownership
4. **USE SECURE DEFAULTS**: Security headers, HTTPS, secure cookies are required
5. **PROTECT SECRETS**: Never hardcode credentials - use secure storage
6. **PREVENT ATTACKS**: Guard against XSS, CSRF, SQL injection, and DoS attacks
7. **LOG SECURELY**: Never log sensitive data, always sanitize log entries
8. **FAIL SECURELY**: Error messages must not leak sensitive information

**Note**: GitHub's comprehensive analysis may show hundreds of code quality findings while security analysis shows zero vulnerabilities. This is expected - focus on security-specific results for security compliance.

**Security violations will result in immediate pull request rejection.**

---

## CODEQL ENFORCEMENT REMINDER

**CodeQL compliance is MANDATORY - not optional. Every code contribution must:**

1. **PASS CODEQL SECURITY ANALYSIS**: All code must pass CodeQL security-focused analysis with zero high/critical issues
   - Run: `codeql database analyze codeql-database --output=security-analysis.sarif codeql/csharp-security-extended.qls`
   - Verify: Empty results array in SARIF file
2. **USE NULL SAFETY**: Implement proper null handling with `?.`, `!`, and explicit nullable casts
3. **OPTIMIZE WITH LINQ**: Replace foreach loops with LINQ methods where appropriate
4. **DISPOSE RESOURCES**: Use `using` statements for all IDisposable objects
5. **VALIDATE INPUTS**: All user inputs must be validated and sanitized
6. **AVOID USELESS ASSIGNMENTS**: Don't create variables that are assigned but never used
7. **FOLLOW PATTERNS**: Use established security and quality patterns consistently
8. **VERIFY LOCALLY**: Run CodeQL analysis before submitting pull requests

**Note**: GitHub Actions may report 200+ "findings" from comprehensive quality analysis, but these are mostly code quality improvements, not security vulnerabilities. Focus on the security-specific analysis results.

**CodeQL violations will result in immediate pull request rejection.**

---

## SCALABILITY CONSIDERATIONS

**Current System Limits & Growth Planning:**

### **Database Scalability**
- **SQLite Current Limit**: ~100 concurrent users, ~50MB database files
- **Migration Threshold**: Plan PostgreSQL migration when database >50MB or >100 concurrent users
- **Index Strategy**: All user-specific queries have appropriate indexes (UserId, Artist, Genre, PerformanceDate)
- **Query Performance**: All queries must complete within 100ms; optimize with proper Entity Framework usage

### **Application Scalability**
- **Blazor Server Limits**: ~200 concurrent connections per instance, 2-4MB memory per connection
- **Horizontal Scaling**: Implement Redis distributed caching and sticky sessions for load balancing
- **Memory Management**: Monitor memory usage patterns, implement proper resource disposal
- **Background Processing**: Use background jobs for heavy operations (calculations, aggregations)

### **Performance Monitoring**
- **API Response Times**: <500ms for all endpoints under normal load
- **Database Query Performance**: Monitor with Entity Framework logging and optimize N+1 problems
- **Memory Usage**: Track per-user memory consumption and implement distributed caching at 1GB+
- **Connection Limits**: Plan for database connection pooling when approaching 100+ concurrent users

### **Scaling Roadmap**
1. **Phase 1 (100-300 users)**: Optimize existing SQLite with indexes and caching
2. **Phase 2 (300-1000 users)**: Migrate to PostgreSQL with connection pooling
3. **Phase 3 (1000+ users)**: Implement Redis caching and load balancing
4. **Phase 4 (5000+ users)**: Add read replicas and horizontal scaling

---

**Remember**: We're building a tool that musicians will rely on for their performances. Every line of code should contribute to creating a reliable, **secure**, scalable, and delightful experience for artists sharing their music with the world.