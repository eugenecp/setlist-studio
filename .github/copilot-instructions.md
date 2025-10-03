# Copilot Instructions for Setlist Studio

## Table of Contents
- [Project Overview](#project-overview)
- [Development Standards](#development-standards)
- [Testing Guidelines](#testing-guidelines)
- [Code Quality & Coverage](#code-quality--coverage)
- [Development Workflow](#development-workflow)
- [Sample Data & Examples](#sample-data--examples)
- [Copilot Prompt Examples](#copilot-prompt-examples)
- [Getting Started](#getting-started)

---

## Project Overview

**Setlist Studio** is a comprehensive music management application designed to help musicians organize and plan their performances. The app enables users to:

- **Manage Artists and Songs**: Add and organize musical artists and their songs with detailed metadata
- **Build Dynamic Setlists**: Create performance setlists with song order, transitions, BPM, and musical keys
- **Schedule Performances**: Plan and manage upcoming shows and events

### Target Audience

- **Developers**: Software engineers building, maintaining, and enhancing the Setlist Studio application
- **Musicians**: Artists, bands, and performers who need a reliable tool to organize their music and plan their shows

### Technology Stack

- **.NET 8**: The latest long-term support version of .NET for robust application development
- **Blazor Server**: Interactive web UI with real-time updates
- **Entity Framework Core**: Database ORM with SQLite/SQL Server support
- **ASP.NET Core Identity**: Authentication and authorization
- **MudBlazor**: Material Design component library
- **xUnit**: Testing framework with comprehensive test coverage
- **GitHub Actions**: Automated CI/CD pipelines

---

## Development Standards

### Core Principles

When working with Setlist Studio, adhere to these five core principles:

#### 1. Reliability 🛡️
Every feature must work consistently and predictably with comprehensive testing and graceful error handling.

#### 2. Scalability 📈
The application must handle growth in songs, setlists, users, and performance data as the user base expands.

#### 3. Security 🔒
Protect user data and maintain system integrity through robust security practices.

#### 4. Maintainability 🔧
Keep the codebase organized, well-documented, and easy to understand for current and future developers.

#### 5. Delight ✨
Create an enjoyable user experience with realistic, relatable content and smooth interactions.

### Code Quality Requirements

- **Error Handling**: Implement proper error handling with user-friendly error messages
- **Database Integrity**: Ensure database transactions are atomic and consistent
- **Input Validation**: Validate all user inputs on both client and server sides
- **Security**: Never store secrets in source code; use environment variables
- **Documentation**: Write clear, self-documenting code with meaningful names
- **Performance**: Design efficient database queries and implement caching where appropriate

---

## Testing Guidelines

### Coverage Standards

Setlist Studio maintains **minimum 90% code coverage requirements** for both line and branch coverage at file and project levels.

**Quality Metrics Requirements:**
- ✅ **Line Coverage**: Each file must achieve at least 90% line coverage
- ✅ **Branch Coverage**: Each file must achieve at least 90% branch coverage
- ✅ **Project Coverage**: Overall project must maintain at least 90% line and branch coverage
- ✅ **CRAP Score**: All methods must maintain passing CRAP scores
- ✅ **Cyclomatic Complexity**: All methods must maintain passing complexity metrics

### Test Framework Requirements

- **xUnit**: Primary testing framework for all unit and integration tests
- **Moq**: For creating mocks and stubs of dependencies
- **FluentAssertions**: For readable, expressive test assertions
- **Bunit**: For Blazor component testing

### Test File Organization

Setlist Studio follows a strategic test organization approach that separates core functionality tests from specialized coverage and edge case tests.

#### Test File Structure

- **Base Test Files** (e.g., `SetlistServiceTests.cs`): Core functionality and primary business logic scenarios
- **Advanced Test Files** (e.g., `SetlistServiceAdvancedTests.cs`): Edge cases, error conditions, validation boundaries
- **Specialized Test Files** (e.g., `ProgramAdvancedTests.cs`): Environment-specific configurations, startup logic

#### Naming Conventions

**Required Naming Pattern:**
- **Source File**: `{ClassName}.cs` → **Test File**: `{ClassName}Tests.cs`
- **Advanced Tests**: `{SourceClass}AdvancedTests.cs`
- **Razor Component**: `{ComponentName}.razor` → **Test File**: `{ComponentName}Tests.cs`

#### When to Create Advanced Test Files

- **File Size**: When base test files exceed ~1,400 lines
- **Different Purposes**: When tests target specific coverage gaps rather than core business logic
- **Specialized Testing**: Error handling, validation boundaries, configuration scenarios
- **Coverage Targeting**: Tests specifically to reach 90%+ line and branch coverage

#### Advanced Test Content Guidelines

- **Validation Boundaries**: Test min/max values, field length limits, required field validation
- **Edge Cases**: Null inputs, empty strings, special characters, Unicode handling
- **Error Conditions**: Database failures, network issues, invalid configurations
- **Authentication Scenarios**: Missing credentials, invalid tokens, authorization failures
- **Configuration Testing**: Environment-specific settings, database provider selection
- **Performance Edge Cases**: Large datasets, concurrent operations, resource limits

### Test Organization Best Practices

- **Maintainability**: Keep individual test files under 1,500 lines for easy navigation
- **Single Responsibility**: Each test file focuses on one primary concern
- **Clear Separation**: Base tests cover happy paths; advanced tests cover edge cases
- **Consistent Naming**: Use descriptive test method names: `MethodName_Scenario_ExpectedResult`
- **Documentation**: Include comprehensive XML documentation for advanced test files

---

## Code Quality & Coverage

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

1. **Identify Gaps**: Use coverage reports to find files below 90% line/branch coverage
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

---

## Development Workflow

### Version Control
- **Git-based workflow**: Feature branches with pull request reviews
- **Branch naming**: `feature/[issue-number]-[short-description]`
- **Commit messages**: Clear, descriptive messages following conventional commits

### CI/CD Pipeline
- **GitHub Actions**: Automated building, testing, and deployment
- **Quality Gates**: All tests must pass with 90%+ coverage before merge
- **Code Review**: All changes require peer review and approval

### Test Execution Strategy
- **Unit Tests**: Fast, isolated tests for individual components
- **Integration Tests**: Database and service integration scenarios
- **Component Tests**: Blazor component rendering and interaction tests
- **Advanced Tests**: Edge cases, error conditions, and coverage gaps

---

## Sample Data & Examples

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

## Copilot Prompt Examples

### Testing & Quality Assurance

```
"Write comprehensive unit tests for the setlist creation endpoint, including validation edge cases"

"Create SongServiceAdvancedTests.cs file with edge case testing for null inputs, Unicode characters, and validation boundaries"

"Generate ProgramAdvancedTests.cs file targeting authentication configuration scenarios and database provider selection logic"

"Write advanced tests for SetlistService focusing on position adjustment edge cases and error handling scenarios"

"Create comprehensive test file targeting specific coverage gaps identified in the coverage report"

"Write validation boundary tests covering minimum/maximum values, field length limits, and required field validation"

"Generate authentication scenario tests for missing credentials, invalid tokens, and authorization failures"

"Create performance edge case tests for large datasets, concurrent operations, and resource exhaustion scenarios"

"Analyze current code coverage and identify classes/methods missing tests to reach 90% line and branch coverage"

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

## Getting Started

When contributing to Setlist Studio:

1. **Read the codebase**: Familiarize yourself with existing patterns and conventions
2. **Follow the principles**: Keep reliability, scalability, security, maintainability, and delight in mind
3. **Match tests to source files**: Every test file must correspond to exactly one source code file using the `{SourceClass}Tests.cs` naming pattern
4. **Use realistic examples**: When creating tests or documentation, use authentic musical data
5. **Test thoroughly**: Ensure your code works correctly and handles edge cases with 90%+ line and branch coverage
6. **Organize tests strategically**: 
   - Add core functionality tests to base test files (e.g., `SetlistServiceTests.cs`)
   - Create advanced test files for edge cases, error handling, and coverage gaps when base files exceed ~1,400 lines
   - Use the `{SourceClass}AdvancedTests.cs` naming pattern for specialized testing scenarios
7. **Target coverage gaps**: Use coverage reports to identify areas needing additional testing and create focused advanced test suites
8. **Document your work**: Add clear comments and update documentation as needed

### Quick Start Checklist

- [ ] Clone repository and set up development environment
- [ ] Run `dotnet test` to ensure all tests pass
- [ ] Generate coverage report to understand current coverage status
- [ ] Review existing code patterns and test organization
- [ ] Create feature branch following naming conventions
- [ ] Write tests first (TDD approach recommended)
- [ ] Ensure 90%+ line and branch coverage for new code
- [ ] Submit pull request with clear description and test evidence

---

**Remember**: We're building a tool that musicians will rely on for their performances. Every line of code should contribute to creating a reliable, secure, and delightful experience for artists sharing their music with the world. 🎵