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

## Template Pattern Guidelines

**Setlist Templates** enable musicians to create reusable blueprints for recurring performance types (weddings, corporate events, tribute shows) and quickly generate event-specific setlists with customized details.

### Template vs. Performance Setlists

**Template Setlists (Reusable Blueprints):**
- `IsTemplate = true` - Marks as reusable template
- `IsActive = false` - Not for actual performance
- No performance-specific details (venue, date typically null)
- Purpose: Reusable blueprint for similar event types
- Examples: "Wedding Ceremony Template", "Corporate Event Template", "Jazz Club Standard Set"

**Performance Setlists (Event-Specific):**
- `IsTemplate = false` - Marks as actual performance
- `IsActive = true` - Ready for performance use
- Event-specific details: venue, date, custom notes
- Created from templates OR built from scratch
- Examples: "Smith Wedding - June 15, 2025", "ABC Corp Holiday Party"

### Creating Setlists from Templates

#### ✅ **Works**: Pattern Implementation

**Method Signature:**
```csharp
Task<Setlist?> CreateFromTemplateAsync(
    int templateId,
    string userId,
    string name,
    DateTime? performanceDate = null,
    string? venue = null,
    string? performanceNotes = null);
```

**Behavior:**
1. **Validates** template exists, user owns it, and `IsTemplate = true`
2. **Creates** new setlist with `IsTemplate = false`, `IsActive = true`
3. **Copies** all songs with complete metadata preservation:
   - Position order maintained
   - Custom BPM/keys preserved
   - Transition notes copied
   - Performance notes copied
   - IsEncore/IsOptional flags preserved
4. **Inherits** template properties:
   - Description (event context)
   - ExpectedDurationMinutes (timing estimate)
5. **Customizes** performance details:
   - Name (event-specific)
   - Venue (location)
   - PerformanceDate (when)
   - PerformanceNotes (special instructions, overrides template notes if provided)

**Returns:**
- New performance setlist if successful
- `null` if template not found, unauthorized, or IsTemplate=false

#### 🔒 **Secure**: Security Requirements

**Authorization Checks (MANDATORY):**
```csharp
// ALWAYS verify user ownership BEFORE checking template status
var template = await _context.Setlists
    .FirstOrDefaultAsync(sl => 
        sl.Id == templateId && 
        sl.UserId == userId &&      // ← Authorization check
        sl.IsTemplate == true);     // ← Template validation

if (template == null)
{
    _logger.LogWarning("Unauthorized or invalid template access");
    return null;  // Don't reveal whether template exists
}
```

**Input Validation (CRITICAL):**
- **Name**: Required, 1-200 characters, sanitized for XSS
- **UserId**: Required, not empty/whitespace
- **Venue**: Optional, max 200 characters if provided
- **PerformanceNotes**: Optional, max 2000 characters if provided
- **PerformanceDate**: Optional, no restriction on past/future dates

**Security Patterns:**
- ✅ User-scoped query: Always filter by `userId`
- ✅ Return `null` for unauthorized (don't throw exceptions)
- ✅ Log security events: Unauthorized access attempts
- ✅ Sanitize all string inputs: Prevent XSS and SQL injection
- ✅ No information disclosure: Don't reveal if template exists to unauthorized users

**Example Secure Usage:**
```csharp
// Validate inputs first
if (string.IsNullOrWhiteSpace(name))
    throw new ArgumentException("Name required");

if (name.Length > 200)
    throw new ArgumentException("Name too long");

// Query with authorization
var template = await _context.Setlists
    .Include(sl => sl.SetlistSongs)
    .FirstOrDefaultAsync(sl => 
        sl.Id == templateId && 
        sl.UserId == userId && 
        sl.IsTemplate);

// Return null if not found/unauthorized
if (template == null) return null;
```

#### 📈 **Scales**: Performance Considerations

**Performance Characteristics:**

| Template Size | Query Time | Insert Time | Total Time | Status |
|---------------|------------|-------------|------------|--------|
| **10 songs** | ~50ms | ~20ms | ~70ms | ✅ Excellent |
| **30 songs** | ~100ms | ~50ms | ~150ms | ✅ Good |
| **50 songs** | ~150ms | ~100ms | ~250ms | ✅ Acceptable |
| **100 songs** | ~250ms | ~200ms | ~450ms | ⚠️ At API limit (500ms) |
| **200+ songs** | ~500ms+ | ~400ms+ | ~900ms+ | ❌ Exceeds 500ms target |

**Optimization Strategies:**

**1. Remove Unnecessary Eager Loading:**
```csharp
// OPTIMIZED: Don't load Song entity details (not needed for copying)
var template = await _context.Setlists
    .Include(sl => sl.SetlistSongs)  // Only load SetlistSong (has SongId)
    // .ThenInclude(ss => ss.Song)   // ← REMOVE: Not needed!
    .FirstOrDefaultAsync(sl => sl.Id == templateId && sl.UserId == userId && sl.IsTemplate);
```

**Impact**: 30-50% faster query, 40-60% less memory

**2. Add Composite Index (CRITICAL):**
```csharp
// In DbContext.OnModelCreating
modelBuilder.Entity<Setlist>()
    .HasIndex(s => new { s.UserId, s.IsTemplate })
    .HasDatabaseName("IX_Setlists_UserId_IsTemplate");
```

**Impact**: 5-10x faster template lookups

**3. Batch Processing for Large Templates (100+ songs):**
```csharp
if (template.SetlistSongs.Count > 100)
{
    // Process in batches of 50 songs
    var batches = template.SetlistSongs
        .OrderBy(ss => ss.Position)
        .Select((song, index) => new { song, index })
        .GroupBy(x => x.index / 50);
    
    foreach (var batch in batches)
    {
        var batchSongs = batch.Select(x => CreateSetlistSong(x.song, newSetlistId));
        _context.SetlistSongs.AddRange(batchSongs);
        await _context.SaveChangesAsync();
    }
}
```

**4. Caching Frequently Used Templates:**
```csharp
// Cache popular templates for 1 hour
var cacheKey = $"template:{templateId}";
var cachedTemplate = await _cache.GetAsync<Setlist>(cacheKey);

if (cachedTemplate == null)
{
    cachedTemplate = await _context.Setlists
        .Include(sl => sl.SetlistSongs)
        .FirstOrDefaultAsync(sl => sl.Id == templateId);
    
    await _cache.SetAsync(cacheKey, cachedTemplate, TimeSpan.FromHours(1));
}
```

**Scalability Limits:**
- ✅ **Works well**: Up to 50 songs per template (95% of use cases)
- ⚠️ **Monitor**: 50-100 songs (approaches 500ms API limit)
- ❌ **Optimize required**: 100+ songs (batch processing recommended)

**Concurrent Usage:**
- 100 concurrent requests: No issues with proper connection pooling
- Database writes use bulk `AddRange` for efficiency
- Cache invalidation ensures consistency after creation

#### 📚 **Maintainable**: Code Patterns & Conventions

**Service Layer Pattern:**
```csharp
public async Task<Setlist?> CreateFromTemplateAsync(
    int templateId,
    string userId,
    string name,
    DateTime? performanceDate = null,
    string? venue = null,
    string? performanceNotes = null)
{
    // 1. Input validation
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Setlist name cannot be null or empty", nameof(name));
    
    if (string.IsNullOrWhiteSpace(userId))
        throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
    
    try
    {
        // 2. Authorization & template validation
        var template = await _context.Setlists
            .Include(sl => sl.SetlistSongs)
            .FirstOrDefaultAsync(sl => 
                sl.Id == templateId && 
                sl.UserId == userId && 
                sl.IsTemplate);
        
        if (template == null)
        {
            _logger.LogWarning(
                "Template {TemplateId} not found, not a template, or unauthorized for user {UserId}", 
                templateId, userId);
            return null;
        }
        
        // 3. Additional validation
        if (name.Length > 200)
            throw new ArgumentException("Setlist name cannot exceed 200 characters", nameof(name));
        
        // 4. Create performance setlist from template
        var newSetlist = new Setlist
        {
            Name = name,
            Description = template.Description,
            Venue = venue,
            PerformanceDate = performanceDate,
            ExpectedDurationMinutes = template.ExpectedDurationMinutes,
            IsTemplate = false,      // NOT a template
            IsActive = true,         // Ready for performance
            PerformanceNotes = performanceNotes ?? template.PerformanceNotes,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        // 5. Validate new setlist
        var validationErrors = ValidateSetlist(newSetlist);
        if (validationErrors.Any())
        {
            var errorMessage = string.Join(", ", validationErrors);
            throw new ArgumentException($"Validation failed: {errorMessage}");
        }
        
        // 6. Persist new setlist
        _context.Setlists.Add(newSetlist);
        await _context.SaveChangesAsync();
        
        // 7. Copy all songs from template
        await CopySetlistSongsAsync(template, newSetlist);
        
        // 8. Invalidate user cache
        await _cacheService.InvalidateUserCacheAsync(userId);
        
        // 9. Log success
        _logger.LogInformation(
            "Created setlist {SetlistId} '{Name}' from template {TemplateId} for user {UserId}", 
            newSetlist.Id, name, templateId, userId);
        
        return newSetlist;
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, 
            "Database error creating setlist from template {TemplateId} for user {UserId}", 
            templateId, userId);
        throw;
    }
    catch (ArgumentException ex)
    {
        _logger.LogError(ex, 
            "Invalid argument creating setlist from template {TemplateId} for user {UserId}", 
            templateId, userId);
        throw;
    }
}

// Helper method for song copying (reusable)
private async Task CopySetlistSongsAsync(Setlist sourceSetlist, Setlist newSetlist)
{
    var newSetlistSongs = sourceSetlist.SetlistSongs
        .OrderBy(ss => ss.Position)
        .Select(sourceSong => new SetlistSong
        {
            SetlistId = newSetlist.Id,
            SongId = sourceSong.SongId,
            Position = sourceSong.Position,
            TransitionNotes = sourceSong.TransitionNotes,
            PerformanceNotes = sourceSong.PerformanceNotes,
            IsEncore = sourceSong.IsEncore,
            IsOptional = sourceSong.IsOptional,
            CustomBpm = sourceSong.CustomBpm,
            CustomKey = sourceSong.CustomKey,
            CreatedAt = DateTime.UtcNow
        });
    
    _context.SetlistSongs.AddRange(newSetlistSongs);
    await _context.SaveChangesAsync();
}
```

**Testing Pattern:**
```csharp
[Fact]
public async Task CreateFromTemplateAsync_ShouldCreateActiveSetlist_WhenTemplateIsValid()
{
    // Arrange
    var userId = "user-123";
    var template = new Setlist
    {
        Name = "Wedding Template",
        Description = "Standard wedding ceremony setlist",
        IsTemplate = true,
        IsActive = false,
        UserId = userId,
        CreatedAt = DateTime.UtcNow
    };
    _context.Setlists.Add(template);
    await _context.SaveChangesAsync();
    
    // Act
    var result = await _service.CreateFromTemplateAsync(
        template.Id,
        userId,
        "Smith Wedding - June 2025",
        new DateTime(2025, 6, 15, 19, 0, 0),
        "Grand Ballroom",
        "First dance: 'Unchained Melody'");
    
    // Assert
    result.Should().NotBeNull();
    result!.Name.Should().Be("Smith Wedding - June 2025");
    result.Venue.Should().Be("Grand Ballroom");
    result.PerformanceDate.Should().Be(new DateTime(2025, 6, 15, 19, 0, 0));
    result.IsTemplate.Should().BeFalse();
    result.IsActive.Should().BeTrue();
    result.Description.Should().Be(template.Description);
}
```

**Naming Conventions:**
- Method: `CreateFromTemplateAsync` (clear intent)
- Parameters: Explicit names (no abbreviations)
- Returns: `Task<Setlist?>` (null for not found/unauthorized)
- Test: `MethodName_Scenario_ExpectedResult` pattern

#### ✨ **User Delight**: Business Value

**Problem Solved:**
Musicians perform similar types of shows repeatedly (weddings, corporate events, tribute nights). Creating setlists from scratch each time is:
- **Time-consuming**: 15-30 minutes per setlist
- **Error-prone**: Forgetting key songs or transitions
- **Inconsistent**: Different song order for similar events

**Solution Value:**

**1. Time Savings:**
- Template creation: ~30 minutes (one-time investment)
- Creating from template: ~2 minutes (vs. 30 minutes manual)
- **ROI**: After 2 uses, template saves 28 minutes per event
- **Annual savings**: Wedding band doing 50 weddings = ~23 hours saved

**2. Consistency & Quality:**
- Proven song order and transitions
- No forgotten songs or awkward gaps
- Professional presentation to clients
- Confidence in performance flow

**3. Customization Flexibility:**
- Keep template structure (what works)
- Customize event details (venue, date, special requests)
- Override performance notes per gig
- Maintain brand consistency across similar events

**4. Organization & Clarity:**
- Clear separation: Planning (templates) vs. Execution (performances)
- Easy to find: "Wedding Template" vs. "Smith Wedding - June 15"
- Template library: Build expertise over time
- Quick onboarding: New band members see proven setlists

**Real-World Scenarios:**

**Wedding Musicians:**
```csharp
// Create once: Wedding Ceremony Template (processional, recessional, etc.)
var ceremonyPerformance = await CreateFromTemplateAsync(
    weddingCeremonyTemplateId,
    userId,
    "Johnson Wedding Ceremony",
    new DateTime(2025, 7, 20, 14, 0, 0),
    "Riverside Chapel",
    "Bride entrance: 'Canon in D' - slow tempo");
```

**Corporate Event Bands:**
```csharp
// Template: "Corporate Event - Upbeat Mix"
var holidayParty = await CreateFromTemplateAsync(
    corporateTemplateId,
    userId,
    "TechCorp Holiday Party 2025",
    new DateTime(2025, 12, 15, 19, 0, 0),
    "Convention Center Ballroom",
    "Client request: No slow songs during dinner");
```

**Jazz Clubs:**
```csharp
// Template: "Jazz Standards - Friday Night"
var fridayShow = await CreateFromTemplateAsync(
    jazzStandardsTemplateId,
    userId,
    "Blue Note - Friday Dec 20",
    new DateTime(2025, 12, 20, 21, 0, 0),
    "Blue Note Jazz Club",
    "Special: Holiday jazz medley for second set");
```

**Customer Delight Metrics:**
- **Efficiency**: 90% time reduction (30 min → 2 min)
- **Confidence**: Musicians trust proven setlist structure
- **Professionalism**: Consistent quality across similar events
- **Flexibility**: Easy customization for event-specific needs
- **Scalability**: Build template library over career

**User Experience Flow:**
1. **Discover**: Musician identifies recurring event type (weddings)
2. **Create Template**: Build perfect setlist once (~30 min)
3. **Reuse Template**: Create event-specific setlist (~2 min)
4. **Customize**: Add venue, date, special requests
5. **Perform**: Confident in proven song order and flow
6. **Iterate**: Update template based on audience feedback

**Business Impact:**
- **More gigs**: Faster preparation = capacity for more bookings
- **Higher quality**: Consistent excellence across performances
- **Easier scaling**: Onboard new musicians with proven templates
- **Professional image**: Organized, reliable, client-ready

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

## Detailed Security Implementation Guide

### Musical Data Validation Rules

**Setlist Studio enforces specific validation rules for musical data to ensure data integrity and prevent malicious inputs.**

#### **Song Entity Validation**

```csharp
// CORRECT: Comprehensive song validation
private IEnumerable<string> ValidateSong(Song song)
{
    var errors = new List<string>();
    
    // Title validation
    if (string.IsNullOrWhiteSpace(song.Title))
        errors.Add("Title is required");
    else if (song.Title.Length > 200)
        errors.Add("Title cannot exceed 200 characters");
    
    // Artist validation
    if (string.IsNullOrWhiteSpace(song.Artist))
        errors.Add("Artist is required");
    else if (song.Artist.Length > 200)
        errors.Add("Artist cannot exceed 200 characters");
    
    // BPM validation (realistic musical tempo range)
    if (song.Bpm.HasValue && (song.Bpm.Value < 40 || song.Bpm.Value > 250))
        errors.Add("BPM must be between 40 and 250");
    
    // Duration validation (1 second to 1 hour)
    if (song.DurationSeconds.HasValue && 
        (song.DurationSeconds.Value < 1 || song.DurationSeconds.Value > 3600))
        errors.Add("Duration must be between 1 and 3600 seconds");
    
    // Difficulty validation (1-5 scale)
    if (song.Difficulty.HasValue && 
        (song.Difficulty.Value < 1 || song.Difficulty.Value > 5))
        errors.Add("Difficulty must be between 1 and 5");
    
    // Optional field length limits
    if (!string.IsNullOrWhiteSpace(song.Album) && song.Album.Length > 200)
        errors.Add("Album cannot exceed 200 characters");
    
    if (!string.IsNullOrWhiteSpace(song.Genre) && song.Genre.Length > 100)
        errors.Add("Genre cannot exceed 100 characters");
    
    if (!string.IsNullOrWhiteSpace(song.Key) && song.Key.Length > 10)
        errors.Add("Musical key cannot exceed 10 characters");
    
    if (!string.IsNullOrWhiteSpace(song.Notes) && song.Notes.Length > 2000)
        errors.Add("Notes cannot exceed 2000 characters");
    
    return errors;
}
```

#### **Musical Key Validation**

```csharp
// Valid musical keys for validation
private static readonly HashSet<string> ValidKeys = new()
{
    // Major keys
    "C", "C#", "Db", "D", "D#", "Eb", "E", "F", "F#", "Gb", 
    "G", "G#", "Ab", "A", "A#", "Bb", "B",
    
    // Minor keys
    "Cm", "C#m", "Dbm", "Dm", "D#m", "Ebm", "Em", "Fm", "F#m", 
    "Gbm", "Gm", "G#m", "Abm", "Am", "A#m", "Bbm", "Bm"
};

// Validation attribute
[AttributeUsage(AttributeTargets.Property)]
public class ValidMusicalKeyAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success; // Allow null/empty for optional fields
        
        var key = value.ToString();
        if (!ValidKeys.Contains(key))
            return new ValidationResult($"'{key}' is not a valid musical key");
        
        return ValidationResult.Success;
    }
}
```

#### **BPM (Tempo) Validation**

```csharp
// Custom validation attribute for realistic BPM ranges
[AttributeUsage(AttributeTargets.Property)]
public class SafeBpmAttribute : ValidationAttribute
{
    private readonly int _minBpm;
    private readonly int _maxBpm;
    
    public SafeBpmAttribute(int minBpm = 40, int maxBpm = 250)
    {
        _minBpm = minBpm;
        _maxBpm = maxBpm;
        ErrorMessage = $"BPM must be between {_minBpm} and {_maxBpm}";
    }
    
    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        if (value == null) return ValidationResult.Success; // Optional field
        
        if (value is int bpm)
        {
            if (bpm >= _minBpm && bpm <= _maxBpm)
                return ValidationResult.Success;
            
            return new ValidationResult(ErrorMessage);
        }
        
        return new ValidationResult("BPM must be a valid integer");
    }
}

// Usage in entity
public class Song
{
    [SafeBpm(40, 250)]
    public int? Bpm { get; set; }
}
```

---

### Authorization Patterns & Implementation

**Setlist Studio implements defense-in-depth authorization with multiple layers of security.**

#### **Pattern 1: User-Scoped Queries (PRIMARY DEFENSE)**

**ALWAYS filter by UserId FIRST in all database queries:**

```csharp
// ✅ CORRECT: User-scoped query (MANDATORY PATTERN)
public async Task<List<Song>> GetSongsAsync(string userId, string? searchTerm = null)
{
    var query = _context.Songs
        .Where(s => s.UserId == userId)  // ← ALWAYS FIRST: User ownership
        .AsQueryable();
    
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(s => 
            s.Title.Contains(searchTerm) || 
            s.Artist.Contains(searchTerm));
    }
    
    return await query.ToListAsync();
}

// ❌ INCORRECT: No user scoping (SECURITY VULNERABILITY)
public async Task<List<Song>> GetSongsAsync(string? searchTerm = null)
{
    // SECURITY FLAW: Returns ALL users' songs!
    return await _context.Songs
        .Where(s => s.Title.Contains(searchTerm))
        .ToListAsync();
}
```

#### **Pattern 2: Ownership Verification (SECONDARY DEFENSE)**

**Always verify ownership before operations on specific entities:**

```csharp
// ✅ CORRECT: Ownership verification in query
public async Task<Song?> GetSongByIdAsync(int songId, string userId)
{
    return await _context.Songs
        .FirstOrDefaultAsync(s => 
            s.Id == songId &&           // ← Entity ID
            s.UserId == userId);        // ← Ownership verification
}

// ✅ CORRECT: Ownership check before update
public async Task<Song?> UpdateSongAsync(int songId, string userId, Song updatedSong)
{
    // Verify ownership before allowing modification
    var existingSong = await _context.Songs
        .FirstOrDefaultAsync(s => s.Id == songId && s.UserId == userId);
    
    if (existingSong == null)
    {
        _logger.LogWarning("Unauthorized update attempt: Song {SongId} by user {UserId}",
            songId, SecureLoggingHelper.SanitizeUserId(userId));
        return null; // Don't reveal if song exists
    }
    
    // Update properties
    existingSong.Title = updatedSong.Title;
    existingSong.Artist = updatedSong.Artist;
    // ... other properties
    
    await _context.SaveChangesAsync();
    return existingSong;
}

// ❌ INCORRECT: Missing ownership verification
public async Task<Song?> UpdateSongAsync(int songId, Song updatedSong)
{
    // SECURITY FLAW: Can update ANY user's song!
    var song = await _context.Songs.FindAsync(songId);
    if (song != null)
    {
        song.Title = updatedSong.Title;
        await _context.SaveChangesAsync();
    }
    return song;
}
```

#### **Pattern 3: Secure Delete Operations**

```csharp
// ✅ CORRECT: Secure delete with ownership verification and audit trail
public async Task<bool> DeleteSongAsync(int songId, string userId)
{
    // Verify ownership before deletion
    var song = await _context.Songs
        .FirstOrDefaultAsync(s => s.Id == songId && s.UserId == userId);
    
    if (song == null)
    {
        _logger.LogWarning("Unauthorized delete attempt: Song {SongId} by user {UserId}",
            songId, SecureLoggingHelper.SanitizeUserId(userId));
        return false;
    }
    
    // Capture deleted values for audit trail BEFORE removing
    await _auditLogService.LogDeleteAsync(
        userId, 
        "Song", 
        songId.ToString(), 
        new { song.Title, song.Artist });
    
    _context.Songs.Remove(song);
    await _context.SaveChangesAsync();
    
    // Invalidate user cache
    await _cacheService.InvalidateUserCacheAsync(userId);
    
    _logger.LogInformation("Song {SongId} deleted by user {UserId}",
        songId, SecureLoggingHelper.SanitizeUserId(userId));
    
    return true;
}

// ❌ INCORRECT: No ownership check, no audit trail
public async Task<bool> DeleteSongAsync(int songId)
{
    // SECURITY FLAW: Can delete ANY user's song!
    var song = await _context.Songs.FindAsync(songId);
    if (song != null)
    {
        _context.Songs.Remove(song);
        await _context.SaveChangesAsync();
        return true;
    }
    return false;
}
```

#### **Pattern 4: Controller Authorization**

```csharp
// ✅ CORRECT: Controller with user identity verification
[Authorize] // Require authentication
[ApiController]
[Route("api/[controller]")]
public class SongsController : ControllerBase
{
    private readonly ISongService _songService;
    
    [HttpGet]
    public async Task<ActionResult<List<Song>>> GetSongs([FromQuery] string? search = null)
    {
        // Extract user identity from authenticated context
        var userId = User.Identity?.Name;
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unauthenticated access attempt to GetSongs");
            return Unauthorized();
        }
        
        var songs = await _songService.GetSongsAsync(userId, search);
        return Ok(songs);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSong(int id)
    {
        var userId = User.Identity?.Name;
        
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var deleted = await _songService.DeleteSongAsync(id, userId);
        
        if (!deleted)
        {
            // Don't reveal if song exists - return 404 for both cases
            return NotFound();
        }
        
        return NoContent();
    }
}

// ❌ INCORRECT: No authentication, missing user context
[ApiController]
[Route("api/[controller]")]
public class SongsController : ControllerBase
{
    // SECURITY FLAW: No [Authorize] attribute!
    [HttpGet]
    public async Task<ActionResult<List<Song>>> GetSongs()
    {
        // SECURITY FLAW: No user filtering - exposes all songs!
        var songs = await _songService.GetAllSongsAsync();
        return Ok(songs);
    }
}
```

---

### Security Anti-Patterns (WHAT NOT TO DO)

**These patterns represent CRITICAL SECURITY VULNERABILITIES. Never implement these in your code.**

#### **❌ Anti-Pattern 1: SQL Injection Vulnerability**

```csharp
// ❌ NEVER DO THIS: String concatenation with user input
public async Task<List<Song>> SearchSongs(string searchTerm)
{
    // CRITICAL VULNERABILITY: SQL Injection attack vector!
    var query = $"SELECT * FROM Songs WHERE Title = '{searchTerm}'";
    // Attacker input: "'; DROP TABLE Songs; --"
    // Results in: SELECT * FROM Songs WHERE Title = ''; DROP TABLE Songs; --'
    
    return await _context.Songs.FromSqlRaw(query).ToListAsync();
}

// ✅ CORRECT: Use parameterized LINQ queries
public async Task<List<Song>> SearchSongs(string searchTerm, string userId)
{
    return await _context.Songs
        .Where(s => s.UserId == userId)
        .Where(s => s.Title.Contains(searchTerm)) // Parameterized, safe
        .ToListAsync();
}
```

#### **❌ Anti-Pattern 2: Authorization Bypass**

```csharp
// ❌ NEVER DO THIS: Trusting client-provided user ID
[HttpGet("{songId}")]
public async Task<ActionResult<Song>> GetSong(int songId, [FromQuery] string userId)
{
    // CRITICAL VULNERABILITY: Client controls userId parameter!
    // Attacker can specify ANY userId and access other users' songs
    var song = await _context.Songs
        .FirstOrDefaultAsync(s => s.Id == songId && s.UserId == userId);
    return Ok(song);
}

// ✅ CORRECT: Extract user ID from authenticated context
[HttpGet("{songId}")]
[Authorize]
public async Task<ActionResult<Song>> GetSong(int songId)
{
    // User ID from authentication token (server-side, trusted)
    var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();
    
    var song = await _context.Songs
        .FirstOrDefaultAsync(s => s.Id == songId && s.UserId == userId);
    
    if (song == null)
        return NotFound(); // Don't reveal if song exists
    
    return Ok(song);
}
```

#### **❌ Anti-Pattern 3: Information Disclosure**

```csharp
// ❌ NEVER DO THIS: Exposing sensitive error details
[HttpPost]
public async Task<IActionResult> CreateSong([FromBody] Song song)
{
    try
    {
        await _songService.CreateSongAsync(song);
        return Ok();
    }
    catch (Exception ex)
    {
        // SECURITY FLAW: Exposes internal implementation details!
        // Reveals: connection strings, stack traces, file paths
        return BadRequest(new { 
            error = ex.Message, 
            stackTrace = ex.StackTrace,
            innerException = ex.InnerException?.Message 
        });
    }
}

// ✅ CORRECT: Secure error handling
[HttpPost]
[Authorize]
public async Task<IActionResult> CreateSong([FromBody] Song song)
{
    var userId = User.Identity?.Name;
    if (string.IsNullOrEmpty(userId))
        return Unauthorized();
    
    try
    {
        var created = await _songService.CreateSongAsync(song, userId);
        return CreatedAtAction(nameof(GetSong), new { id = created.Id }, created);
    }
    catch (ValidationException ex)
    {
        // Safe: Only exposes validation errors (expected user-facing info)
        return BadRequest(new { errors = ex.Errors });
    }
    catch (Exception ex)
    {
        // Log detailed error server-side
        _logger.LogError(ex, "Song creation failed for user {UserId}", 
            SecureLoggingHelper.SanitizeUserId(userId));
        
        // Return generic error to client
        return Problem("An error occurred while creating the song");
    }
}
```

#### **❌ Anti-Pattern 4: Logging Sensitive Data**

```csharp
// ❌ NEVER DO THIS: Logging PII and sensitive data
public async Task<bool> AuthenticateUser(string email, string password)
{
    // SECURITY FLAW: Logs password in plain text!
    _logger.LogInformation("Login attempt for {Email} with password {Password}", 
        email, password);
    
    // SECURITY FLAW: Logs user's email address (PII)
    var user = await _userService.GetByEmailAsync(email);
    
    if (user != null && VerifyPassword(password, user.PasswordHash))
    {
        _logger.LogInformation("Successful login for {Email}", email);
        return true;
    }
    
    return false;
}

// ✅ CORRECT: Secure logging without sensitive data
public async Task<bool> AuthenticateUser(string email, string password)
{
    // Use sanitized user ID, never log passwords or emails
    var user = await _userService.GetByEmailAsync(email);
    
    if (user != null && VerifyPassword(password, user.PasswordHash))
    {
        _logger.LogInformation("Successful authentication for user {UserId}",
            SecureLoggingHelper.SanitizeUserId(user.Id));
        return true;
    }
    
    // Don't reveal if user exists - generic message
    _logger.LogWarning("Failed authentication attempt");
    return false;
}
```

#### **❌ Anti-Pattern 5: Hardcoded Secrets**

```csharp
// ❌ NEVER DO THIS: Hardcoded credentials in source code
public class EmailService
{
    // CRITICAL VULNERABILITY: Credentials committed to version control!
    private const string SmtpUsername = "admin@setliststudio.com";
    private const string SmtpPassword = "MySecretPassword123!";
    private const string ApiKey = "sk_live_1234567890abcdef";
    
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // These secrets are now in Git history forever!
        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(SmtpUsername, SmtpPassword)
        };
        // ...
    }
}

// ✅ CORRECT: Load secrets from secure configuration
public class EmailService
{
    private readonly IConfiguration _configuration;
    
    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Load from environment variables or Azure Key Vault
        var smtpUsername = _configuration["Email:SmtpUsername"];
        var smtpPassword = _configuration["Email:SmtpPassword"];
        
        // Validate secrets are not placeholder values
        if (smtpUsername?.Contains("YOUR_") == true || 
            smtpPassword?.Contains("YOUR_") == true)
        {
            throw new InvalidOperationException(
                "Email configuration not properly set. Update secrets in environment variables.");
        }
        
        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(smtpUsername, smtpPassword)
        };
        // ...
    }
}
```

#### **❌ Anti-Pattern 6: Missing Input Validation**

```csharp
// ❌ NEVER DO THIS: No validation on user inputs
[HttpPost]
public async Task<IActionResult> CreateSong([FromBody] Song song)
{
    // SECURITY FLAW: No validation - accepts any input!
    // Allows: XSS scripts, SQL injection attempts, invalid data
    await _context.Songs.AddAsync(song);
    await _context.SaveChangesAsync();
    return Ok(song);
}

// ✅ CORRECT: Comprehensive input validation
[HttpPost]
[Authorize]
public async Task<IActionResult> CreateSong([FromBody] Song song)
{
    var userId = User.Identity?.Name;
    if (string.IsNullOrEmpty(userId))
        return Unauthorized();
    
    // Validate input
    if (string.IsNullOrWhiteSpace(song.Title))
        return BadRequest("Title is required");
    
    if (song.Title.Length > 200)
        return BadRequest("Title cannot exceed 200 characters");
    
    if (string.IsNullOrWhiteSpace(song.Artist))
        return BadRequest("Artist is required");
    
    if (song.Bpm.HasValue && (song.Bpm < 40 || song.Bpm > 250))
        return BadRequest("BPM must be between 40 and 250");
    
    // Sanitize input to prevent XSS
    song.Title = SecureLoggingHelper.SanitizeMessage(song.Title);
    song.Artist = SecureLoggingHelper.SanitizeMessage(song.Artist);
    
    // Assign authenticated user
    song.UserId = userId;
    song.CreatedAt = DateTime.UtcNow;
    
    await _context.Songs.AddAsync(song);
    await _context.SaveChangesAsync();
    
    return CreatedAtAction(nameof(GetSong), new { id = song.Id }, song);
}
```

---

### Security Testing Requirements

**All security implementations must have corresponding security tests:**

#### **Test Pattern 1: Authorization Tests**

```csharp
[Fact]
public async Task GetSongByIdAsync_ShouldReturnNull_WhenUserDoesNotOwnSong()
{
    // Arrange
    var user1Id = "user-123";
    var user2Id = "user-456";
    
    var song = new Song
    {
        Title = "Bohemian Rhapsody",
        Artist = "Queen",
        UserId = user1Id // Owned by user1
    };
    _context.Songs.Add(song);
    await _context.SaveChangesAsync();
    
    // Act - user2 tries to access user1's song
    var result = await _service.GetSongByIdAsync(song.Id, user2Id);
    
    // Assert
    result.Should().BeNull("users cannot access other users' songs");
}
```

#### **Test Pattern 2: Input Validation Tests**

```csharp
[Theory]
[InlineData(39, "BPM below minimum (40)")]
[InlineData(251, "BPM above maximum (250)")]
public async Task CreateSongAsync_ShouldRejectInvalidBpm(int invalidBpm, string reason)
{
    // Arrange
    var userId = "user-123";
    var song = new Song
    {
        Title = "Test Song",
        Artist = "Test Artist",
        Bpm = invalidBpm,
        UserId = userId
    };
    
    // Act & Assert
    await Assert.ThrowsAsync<ValidationException>(
        async () => await _service.CreateSongAsync(song, userId));
}

[Fact]
public async Task CreateSongAsync_ShouldRejectXssAttempt()
{
    // Arrange
    var userId = "user-123";
    var song = new Song
    {
        Title = "<script>alert('XSS')</script>",
        Artist = "Test Artist",
        UserId = userId
    };
    
    // Act & Assert
    await Assert.ThrowsAsync<ValidationException>(
        async () => await _service.CreateSongAsync(song, userId));
}
```

#### **Test Pattern 3: Secure Logging Tests**

```csharp
[Fact]
public async Task GetSongByIdAsync_ShouldSanitizeUserIdInLogs()
{
    // Arrange
    var userId = "user@example.com"; // Email address (PII)
    var song = new Song
    {
        Title = "Test Song",
        Artist = "Test Artist",
        UserId = userId
    };
    _context.Songs.Add(song);
    await _context.SaveChangesAsync();
    
    // Act
    await _service.GetSongByIdAsync(song.Id, userId);
    
    // Assert - verify logs don't contain raw email
    var logEntries = _loggerFactory.GetLogEntries();
    logEntries.Should().NotContain(entry => entry.Message.Contains(userId),
        "raw user email should not appear in logs");
    
    logEntries.Should().Contain(entry => 
        entry.Message.Contains(SecureLoggingHelper.SanitizeUserId(userId)),
        "sanitized user ID should appear in logs");
}
```

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