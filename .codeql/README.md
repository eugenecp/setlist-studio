# CodeQL Local Configuration Guide

This directory contains the local CodeQL configuration that mirrors the GitHub Actions security workflow exactly.

## Quick Start

### Option 1: Use Provided Scripts (Recommended)

**Security-Focused Analysis (68 queries):**
```powershell
# Quick security validation  
.\scripts\run-codeql-security.ps1

# With clean database rebuild
.\scripts\run-codeql-security.ps1 -CleanDatabase
```

**Comprehensive Analysis (170 queries, matches GitHub Actions):**
```powershell
# Full analysis matching CI/CD pipeline
.\scripts\run-codeql-comprehensive.ps1

# With clean database and open results
.\scripts\run-codeql-comprehensive.ps1 -CleanDatabase -OpenResults
```

### Option 2: Custom Analysis

```powershell
# Custom analysis with specific parameters
.\scripts\run-codeql-local.ps1 -QuerySuite "security-and-quality" -CleanDatabase -OpenResults
```

## Configuration Files

### `.codeql/codeql-config.yml`
Local CodeQL configuration that mirrors `.github/codeql/codeql-config.yml` exactly:
- Same query suites (`security-and-quality`)
- Same path inclusions and exclusions
- Same query filters
- Same query packs

### `.codeql/config.env`
Environment variables for local development:
- Database and results paths
- Build configuration settings
- CodeQL execution parameters
- Output format specifications

## Script Overview

### `run-codeql-local.ps1` (Main Script)
Comprehensive CodeQL analysis with full configurability:
- **Parameters**: QuerySuite, OutputFormat, DatabasePath, CleanDatabase, OpenResults
- **Database Creation**: Matches GitHub Actions build command exactly
- **Results Processing**: Categorizes findings by severity
- **Output**: SARIF files and summary reports
- **Security vs Quality**: Distinguishes between blocking security issues and non-blocking quality improvements

### `run-codeql-security.ps1` (Security Focus)
Quick security validation for development:
- Uses `codeql/csharp-security-extended.qls` (68 security queries)
- Focuses on critical security vulnerabilities only
- Target: Zero security issues (blocking)
- Use case: Pre-commit validation

### `run-codeql-comprehensive.ps1` (Full Analysis)
Complete analysis matching GitHub Actions:
- Uses `security-and-quality` suite (170 queries)
- Includes security + code quality + best practices
- Results: Security issues + warnings + recommendations
- Use case: Full code review, matches CI/CD pipeline

## Alignment with GitHub Actions

### Build Commands
Both local and GitHub Actions use identical build commands:
```bash
dotnet build SetlistStudio.sln --configuration Release --no-restore
```

### Configuration Files
- **Local**: `.codeql/codeql-config.yml`
- **GitHub Actions**: `.github/codeql/codeql-config.yml`
- **Content**: Identical configuration (paths, queries, filters)

### Query Suites
- **Security-Focused**: `codeql/csharp-security-extended.qls` (68 queries)
- **Comprehensive**: `security-and-quality` (170 queries)
- **GitHub Actions Default**: `security-and-quality`

### Results Interpretation
Both local and GitHub Actions produce the same SARIF output with identical finding categorization:
- **Security Issues**: error/warning severity with security tags (blocking)
- **Quality Issues**: warnings and recommendations (non-blocking)
- **Expected Results**: 0 security issues, ~230 quality findings

## Output Structure

### Results Directory: `codeql-results/`
- **SARIF Files**: `codeql-analysis-{suite}-{timestamp}.sarif`
- **Summary Reports**: `summary-{timestamp}.md`
- **Categorized Findings**: Organized by severity (error, warning, recommendation, note)

### Summary Report Contents
- Configuration details (query suite, paths, commands)
- Results overview (total findings, security vs quality)
- Command used for reproducibility
- Next steps and recommendations

## Security vs Quality Distinction

### Security Issues (Blocking)
- **Criteria**: `error` or `warning` severity WITH `security` tag
- **Action Required**: Must be fixed before merge
- **Examples**: SQL injection, XSS, authentication bypass
- **Target**: Zero security issues

### Quality Issues (Non-blocking)
- **Criteria**: All other findings (warnings, recommendations, notes)
- **Action Required**: Continuous improvement, not blocking
- **Examples**: Performance optimization, code style, best practices
- **Expected**: ~162 warnings + ~68 recommendations

## Troubleshooting

### Common Issues

**Database Creation Fails:**
```powershell
# Clean and rebuild
dotnet clean SetlistStudio.sln
dotnet restore SetlistStudio.sln
.\scripts\run-codeql-local.ps1 -CleanDatabase
```

**Different Results from GitHub Actions:**
- Ensure using same query suite (`security-and-quality`)
- Verify configuration file alignment
- Check build command consistency
- Compare SARIF output structure

**Performance Issues:**
```powershell
# Use specific query suite for faster analysis
.\scripts\run-codeql-security.ps1  # Security only (faster)
```

### Expected Results Comparison

**Local Security Analysis:**
```
Query Suite: codeql/csharp-security-extended.qls
Expected Results: 0 security issues
Purpose: Development validation
```

**Local Comprehensive Analysis:**
```
Query Suite: security-and-quality  
Expected Results: ~230 total findings (162 warnings + 68 recommendations)
Purpose: Matches GitHub Actions exactly
```

**GitHub Actions Analysis:**
```
Query Suite: security-and-quality
Expected Results: Same as local comprehensive analysis
Purpose: CI/CD pipeline validation
```

## Best Practices

### Development Workflow
1. **Before Commit**: Run security-focused analysis (`.\scripts\run-codeql-security.ps1`)
2. **Before PR**: Run comprehensive analysis (`.\scripts\run-codeql-comprehensive.ps1`)
3. **Regular Review**: Address quality findings in dedicated improvement PRs
4. **Security Priority**: Always fix security issues immediately

### Performance Optimization
- Use security-focused analysis for frequent validation
- Run comprehensive analysis less frequently
- Clean database when switching between branches
- Use threading options for faster analysis

### Integration with VS Code
1. Install CodeQL extension
2. Open generated SARIF files in VS Code
3. Use "CodeQL: Run Query on Database" for custom queries
4. View results in "CodeQL Query Results" panel

## Validation Commands

### Verify Configuration Alignment
```powershell
# Compare local and GitHub Actions config
Compare-Object (Get-Content .codeql/codeql-config.yml) (Get-Content .github/codeql/codeql-config.yml)
```

### Test Analysis Pipeline
```powershell
# Full test of analysis pipeline
.\scripts\run-codeql-comprehensive.ps1 -CleanDatabase -OpenResults
```

### Validate Security Standards
```powershell
# Ensure zero security issues
.\scripts\run-codeql-security.ps1
# Check that results array is empty in output SARIF
```

This configuration ensures perfect alignment between local development and CI/CD security analysis.