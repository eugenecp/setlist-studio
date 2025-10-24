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
- **Maintainability**: Clean code, clear documentation, consistent patterns
- **User Experience**: Realistic musical data, smooth interactions
- **Code Quality**: Zero build warnings in main and test projects

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

**Remember**: We're building a tool that musicians will rely on for their performances. Every line of code should contribute to creating a reliable, **secure**, and delightful experience for artists sharing their music with the world.