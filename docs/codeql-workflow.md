# CodeQL Analysis Workflow in VS Code

## Quick Start Guide

### 1. Create Database
- Open Command Palette (`Ctrl+Shift+P`)
- Run: "CodeQL: Create Database from Folder"
- Select your project root folder
- Choose "csharp" as the language

### 2. Run Queries
- Open Command Palette (`Ctrl+Shift+P`)
- Run: "CodeQL: Run Query on Database"
- Select a query or query suite
- Choose your database

### 3. Common Security Queries for C# Projects

#### A. Security and Quality Suite
```
codeql/csharp-queries:codeql-suites/csharp-security-and-quality.qls
```

#### B. Specific Security Queries
- `codeql/csharp-queries:Security/CWE/CWE-079/XSS.ql` - Cross-site scripting
- `codeql/csharp-queries:Security/CWE/CWE-089/SqlInjection.ql` - SQL injection
- `codeql/csharp-queries:Security/CWE/CWE-022/TaintedPath.ql` - Path traversal
- `codeql/csharp-queries:Security/CWE/CWE-078/CommandInjection.ql` - Command injection
- `codeql/csharp-queries:Security/CWE/CWE-352/MissingCsrfTokenValidation.ql` - CSRF protection

### 4. VS Code CodeQL Features

#### View Results
- Results appear in the "CodeQL Query Results" panel
- Click on results to jump to source code
- Use filters to focus on specific severity levels

#### Custom Queries
- Create `.ql` files in your workspace
- Use CodeQL syntax highlighting and IntelliSense
- Run custom queries against your database

#### Query History
- View previous query results
- Compare results between runs
- Export results to various formats

### 5. Analyzing Security Issues

#### Common Patterns to Look For:
1. **Input Validation**: User input not properly validated
2. **SQL Injection**: Dynamic SQL without parameterization
3. **XSS**: User data rendered without encoding
4. **Path Traversal**: File paths from user input
5. **Insecure Deserialization**: Unsafe object deserialization
6. **Weak Cryptography**: Use of deprecated crypto algorithms

#### Interpreting Results:
- **Red (High)**: Critical security vulnerabilities
- **Orange (Medium)**: Important security issues
- **Yellow (Low)**: Code quality and minor security concerns

### 6. Integration with GitHub
- Results sync with GitHub Security tab
- Pull request integration shows new issues
- SARIF export for external tools