# Copilot Instructions for Setlist Studio

## Project Description

**Setlist Studio** is a comprehensive music management application designed to help musicians organize and plan their performances. The app enables users to:

- **Manage Artists and Songs**: Add and organize musical artists and their songs with detailed metadata
- **Build Dynamic Setlists**: Create performance setlists with song order, transitions, BPM, and musical keys
- **Schedule Performances**: Plan and manage upcoming shows and events

### Target Audience

This application serves two primary audiences:

- **Developers**: Software engineers building, maintaining, and enhancing the Setlist Studio application
- **Musicians**: Artists, bands, and performers who need a reliable tool to organize their music and plan their shows

## Tools and Setup

Setlist Studio is built using modern .NET technologies and follows industry best practices:

### Technology Stack
- **.NET 8**: The latest long-term support version of .NET for robust application development
- **RESTful APIs**: Well-designed endpoints for seamless data interaction
- **Authentication & Authorization**: Secure user management and access control
- **Comprehensive Logging**: Detailed application monitoring and debugging capabilities
- **Database Integration**: Persistent storage for artists, songs, setlists, and performance data
- **Automated Testing**: Unit, integration, and end-to-end testing suites

### Development Workflow
- **GitHub Actions**: Automated CI/CD pipelines for building, testing, and deployment
- **Version Control**: Git-based workflow with feature branches and pull request reviews

## Key Principles

When working with Setlist Studio, please adhere to these core principles:

### 1. Reliability 🛡️
Every feature must work consistently and predictably. All functionality should be thoroughly tested and handle edge cases gracefully.

**Guidelines:**
- Write comprehensive unit and integration tests for all new features
- Implement proper error handling and user-friendly error messages
- Ensure database transactions are atomic and consistent
- Test boundary conditions (empty setlists, maximum song limits, etc.)

#### Code Coverage Standards
Setlist Studio maintains **minimum 90% code coverage requirements for both line and branch coverage at the file and project levels** to ensure reliability and confidence in our codebase.

**Quality Metrics Requirements:**
- ✅ **Line Coverage**: Each individual file must achieve at least 90% line coverage
- ✅ **Branch Coverage**: Each individual file must achieve at least 90% branch coverage
- ✅ **Project Coverage**: Overall project must maintain at least 90% line and branch coverage
- ✅ **CRAP Score**: All methods must maintain passing CRAP scores (low complexity-to-coverage ratios)
- ✅ **Cyclomatic Complexity**: All methods must maintain passing cyclomatic complexity metrics

**Individual File Coverage Requirements:**
- **Each file must achieve at least 90% line coverage AND 90% branch coverage**
- New code must include tests that achieve 90%+ line and branch coverage for the modified files
- Pull requests should not reduce coverage below 90% for any existing file (line or branch)
- **All new files must achieve 90% line and branch coverage before merge**
- Focus on achieving high individual file coverage, not just overall project coverage

**Testing Framework Requirements:**
- **xUnit**: Primary testing framework for all unit and integration tests
- **Moq**: For creating mocks and stubs of dependencies
- **FluentAssertions**: For readable, expressive test assertions

**Coverage Guidelines:**
- All new code must include comprehensive tests covering normal cases, error scenarios, and edge cases
- **Each individual file must maintain at least 90% line coverage AND 90% branch coverage**
- **Project overall must maintain at least 90% line coverage AND 90% branch coverage**
- Tests must cover all conditional branches (if/else statements, try/catch blocks, switch cases)
- Each test should be small, focused, and clearly named describing what it tests
- Test names should follow the pattern: `MethodName_Scenario_ExpectedResult`
- Mock external dependencies to ensure tests are isolated and fast
- Use realistic musical data in test examples (songs, artists, BPMs, keys)
- **Focus on achieving high individual file coverage (line and branch), not just overall project coverage**

**Test Organization:**
- Group related tests in nested classes or separate test files
- Use descriptive test method names that explain the scenario being tested
- Include comments for complex test setups or assertions
- Ensure tests are deterministic and can run in any order

**Test File Naming Conventions:**
Test files must follow strict naming conventions that directly correspond to the source code files they are testing to ensure clarity and maintainability.

**Required Naming Pattern:**
- **Source File**: `{ClassName}.cs` → **Test File**: `{ClassName}Tests.cs`
- **Examples**:
  - `Song.cs` → `SongTests.cs`
  - `SetlistService.cs` → `SetlistServiceTests.cs`
  - `HealthController.cs` → `HealthControllerTests.cs`
  - `Program.cs` → `ProgramTests.cs`
  - `SetlistStudioDbContext.cs` → `SetlistStudioDbContextTests.cs`

**Test File Structure Requirements:**
- **One test file per source file**: Each source code file must have exactly one corresponding test file
- **ALL tests must match a source file**: No orphaned test files are allowed - every test file must correspond to an actual source code file
- **No generic or utility test files**: Avoid creating generic test files like `UtilityTests.cs` or `HelpersTests.cs` - instead, create specific test files for specific source classes
- **Mirror directory structure**: Test files must be organized in directories that mirror the source code structure
- **Consistent namespace mapping**: 
  - Source: `SetlistStudio.Core.Entities` → Test: `SetlistStudio.Tests.Entities`
  - Source: `SetlistStudio.Infrastructure.Services` → Test: `SetlistStudio.Tests.Services`
  - Source: `SetlistStudio.Web.Controllers` → Test: `SetlistStudio.Tests.Controllers`
- **Test class naming**: Test class names must match the pattern `{SourceClassName}Tests`

**Project Structure Mapping:**
```
src/SetlistStudio.Core/Entities/Song.cs → tests/SetlistStudio.Tests/Entities/SongTests.cs
src/SetlistStudio.Infrastructure/Services/SongService.cs → tests/SetlistStudio.Tests/Services/SongServiceTests.cs
src/SetlistStudio.Web/Controllers/HealthController.cs → tests/SetlistStudio.Tests/Controllers/HealthControllerTests.cs
src/SetlistStudio.Web/Program.cs → tests/SetlistStudio.Tests/Web/ProgramTests.cs
```

**Test-to-Source File Validation:**
To maintain test quality and ensure comprehensive coverage, all test files must have clear traceability to source code files.

**Validation Requirements:**
- **Every test file must correspond to exactly one source code file**
- **Every source code file should have exactly one corresponding test file** (except for auto-generated files)
- **Test file names must exactly match the source file naming pattern**: `{SourceClass}Tests.cs`
- **Test files without corresponding source files are prohibited** - they indicate either:
  - Missing source code that should be implemented
  - Incorrectly named test files that should be renamed
  - Test files that should be deleted or refactored

**Verification Process:**
- Before creating new test files, verify the corresponding source file exists
- When refactoring or renaming source files, update corresponding test file names
- During code reviews, validate that all new test files follow the 1:1 mapping requirement
- Use tools or scripts to validate test-to-source file mapping consistency

**Acceptable Exceptions:**
- Auto-generated files (e.g., migrations, scaffolded code) may not require test files
- Static program entry points may have specialized test approaches
- Configuration files and data files typically don't require dedicated test files

**Coverage Reporting and Analysis:**
Setlist Studio uses comprehensive coverage reporting to track and maintain code quality. All coverage reports are generated in the `CoverageReport` directory for easy analysis and review.

**Running Coverage Analysis:**
```bash
# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory:./TestResults/[TestRun]

# Generate HTML coverage report
reportgenerator -reports:"./TestResults/[TestRun]/*/coverage.cobertura.xml" -targetdir:"./CoverageReport/[TestRun]" -reporttypes:Html

# Open coverage report in browser
# Navigate to ./CoverageReport/[TestRun]/index.html
```

**Coverage Report Structure:**
- **CoverageReport/**: Root directory for all coverage analysis reports
  - **[TestRun]/**: Timestamped or named subdirectories for different test runs
    - **index.html**: Main coverage report with summary and detailed breakdowns
    - **[Assembly]_[Class].html**: Detailed line-by-line coverage for specific classes
    - **report.css**: Styling for coverage reports

**Coverage Analysis Guidelines:**
- Generate coverage reports for all major code changes and pull requests
- Review line-by-line coverage for newly added classes and methods
- **Identify and address any files with less than 90% individual line or branch coverage**
- Use coverage reports to find untested edge cases and error handling paths
- **Focus on achieving high individual file coverage (line and branch), not just overall project coverage**
- Document any intentionally excluded code with appropriate justification
- **Prioritize files below 90% line or branch coverage for immediate testing improvements**

**Coverage Report Interpretation:**
- **Green bars**: Well-covered code (>90% line and branch coverage)
- **Yellow bars**: Moderately covered code (70-90% coverage) - needs attention
- **Red bars**: Poorly covered code (<70% coverage) - requires immediate improvement
- **Risk Hotspots**: High complexity code with low coverage - prioritize for testing
- **Branch Coverage**: Measures all conditional paths (if/else, switch, try/catch) - must achieve 90%

**Quality Metrics Analysis:**
- **CRAP Score**: Change Risk Anti-Patterns score combining complexity and coverage
  - **Requirement**: All methods must maintain passing CRAP scores
  - Target: Keep CRAP score low by maintaining high test coverage on complex methods
  - Action: Break down methods with high CRAP scores or add comprehensive tests
- **Cyclomatic Complexity**: Measures code complexity through decision points
  - **Requirement**: All methods must maintain passing cyclomatic complexity metrics
  - Target: Break down methods with high complexity or ensure comprehensive testing
  - Action: Refactor complex methods into smaller, more testable units

**Example Coverage Analysis Commands:**
```bash
# Quick coverage check for current changes
dotnet test --collect:"XPlat Code Coverage" --results-directory:./TestResults/QuickCheck

# Full coverage analysis with detailed reporting
dotnet test --collect:"XPlat Code Coverage" --results-directory:./TestResults/FullAnalysis
reportgenerator -reports:"./TestResults/FullAnalysis/*/coverage.cobertura.xml" -targetdir:"./CoverageReport/FullAnalysis" -reporttypes:Html

# Coverage comparison between branches
reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:"./CoverageReport/Comparison" -reporttypes:Html -historydirectory:"./CoverageReport/History"
```

### 2. Scalability 📈
The application must handle growth in songs, setlists, users, and performance data as the user base expands.

**Guidelines:**
- Design efficient database queries and indexing strategies
- Implement pagination for large data sets
- Use caching where appropriate to reduce database load
- Structure code to support horizontal scaling when needed

### 3. Security 🔒
Protect user data and maintain system integrity through robust security practices.

**Guidelines:**
- Validate all user inputs on both client and server sides
- Never store secrets, API keys, or sensitive data in source code
- Use environment variables and secure configuration management
- Implement proper authentication and authorization checks
- Sanitize data to prevent injection attacks

### 4. Maintainability 🔧
Keep the codebase organized, well-documented, and easy to understand for current and future developers.

**Guidelines:**
- Follow consistent naming conventions and coding standards
- Write clear, self-documenting code with meaningful variable and method names
- Maintain up-to-date documentation for APIs and complex business logic
- Organize code into logical modules and maintain separation of concerns
- Keep dependencies up to date and minimize technical debt

### 5. Delight ✨
Create an enjoyable user experience with realistic, relatable content and smooth interactions.

**Guidelines:**
- Use realistic music examples in documentation, tests, and sample data
- Include diverse genres, artists, and musical styles in examples
- Provide helpful default values (e.g., common BPM ranges, popular keys)
- Create intuitive user interfaces and clear user feedback
- Use authentic musical terminology and metadata

## Example Prompts for GitHub Copilot

Use these example prompts to get the most out of GitHub Copilot while maintaining our key principles:

### Reliability Examples
```
"Write comprehensive unit tests for the setlist creation endpoint, including validation edge cases"

"Create integration tests for the artist and song relationship management"

"Add error handling for database connection failures in the performance scheduling service"

"Write tests that verify setlist ordering is maintained correctly when songs are added or removed"

"Analyze current code coverage and identify classes/methods missing tests to reach 90% line and branch coverage"

"Write xUnit tests using Moq and FluentAssertions for the SongService covering normal cases, null inputs, and exception scenarios"

"Create SongServiceTests.cs file with comprehensive tests for the SongService.cs class following the required naming convention"

"Generate SetlistStudioDbContextTests.cs file to test all methods in SetlistStudioDbContext.cs with 90% line and branch coverage"

"Write HealthControllerTests.cs file that matches the HealthController.cs structure and tests all endpoints"

"Create ProgramTests.cs file in tests/SetlistStudio.Tests/Web/ directory to test the Program.cs startup configuration"

"Generate test files following the naming pattern {ClassName}Tests.cs for all classes in the SetlistStudio.Core.Entities namespace"

"Verify that all test files correspond to existing source files and follow the 1:1 mapping requirement"

"Create test files for all source code files that are missing corresponding test files"

"Validate that each test file matches exactly one source code file using the {SourceClass}Tests.cs naming pattern"

"Review existing test files and ensure none are orphaned - all must correspond to actual source code files"

"Generate individual test files for each source class rather than creating combined or utility test files"

"Create test cases for all branches in the SetlistService AddSong method including duplicate song handling"

"Generate unit tests for entity validation covering valid data, invalid BPM ranges, and missing required fields"

"Write tests for all conditional logic in the authentication service including successful login, failed login, and account lockout scenarios"

"Generate a coverage report in CoverageReport/NewFeature and analyze which classes need additional testing"

"Run coverage analysis and create tests for all uncovered branches in the Program.cs startup configuration"

"Review the coverage report in CoverageReport/Latest and identify critical paths with less than 90% line or branch coverage"

"Create tests for all conditional branches to achieve 90% branch coverage on the UserService class"

"Analyze CRAP scores and cyclomatic complexity metrics to identify methods needing refactoring or additional tests"

"Write comprehensive tests to ensure all files maintain 90% line and branch coverage with passing CRAP scores"
```

### Scalability Examples
```
"Optimize the query for fetching large setlists with song metadata using Entity Framework"

"Implement pagination for the artists endpoint to handle thousands of artists efficiently"

"Redesign the setlist storage to support better performance with 10,000+ songs per user"

"Add caching layer for frequently accessed song and artist data"
```

### Security Examples
```
"Add input validation for BPM values to ensure they're between 40 and 250"

"Implement authorization checks to ensure users can only access their own setlists"

"Add data sanitization for artist names and song titles to prevent XSS attacks"

"Create validation rules for musical keys to only accept valid key signatures (C, C#, Db, etc.)"
```

### Maintainability Examples
```
"Refactor the Song and Setlist classes with clearer property names and comprehensive XML documentation"

"Organize the API controllers into logical folders and add consistent routing patterns"

"Create a comprehensive README with setup instructions and API documentation"

"Add inline comments explaining the complex setlist transition logic"
```

### Delight Examples
```
"Generate Swagger API examples using realistic song data like 'Bohemian Rhapsody' by Queen (BPM: 72, Key: Bb)"

"Create seed data with a diverse mix of musical genres including rock, jazz, classical, and electronic music"

"Add sample setlists for different types of performances (wedding, concert, practice session)"

"Design user-friendly error messages that use musical terminology musicians will understand"
```

## Sample Data Guidelines

When creating examples, tests, or documentation, use realistic musical data:

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

## Getting Started

When contributing to Setlist Studio:

1. **Read the codebase**: Familiarize yourself with existing patterns and conventions
2. **Follow the principles**: Keep reliability, scalability, security, maintainability, and delight in mind
3. **Match tests to source files**: Every test file must correspond to exactly one source code file using the `{SourceClass}Tests.cs` naming pattern
4. **Use realistic examples**: When creating tests or documentation, use authentic musical data
5. **Test thoroughly**: Ensure your code works correctly and handles edge cases with 90%+ line and branch coverage
6. **Document your work**: Add clear comments and update documentation as needed

Remember: We're building a tool that musicians will rely on for their performances. Every line of code should contribute to creating a reliable, secure, and delightful experience for artists sharing their music with the world.