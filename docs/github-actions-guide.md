# GitHub Actions Guide for Setlist Studio 🚀

This document explains how to use and understand the GitHub Actions CI/CD pipeline for Setlist Studio.

## 📊 Status Badge

The status badge in the README shows the current state of the main branch:

- ✅ **Green (Passing)**: All tests pass, build successful
- ❌ **Red (Failing)**: Tests failed or build errors  
- 🟡 **Yellow (In Progress)**: Currently running
- ❓ **Gray (No Status)**: No recent runs

## 🔄 Automated Workflows

### 1. CI/CD Pipeline (`ci.yml`)

**Triggers:**
- Push to `main` branch
- Pull request to `main` branch  
- Manual trigger via Actions tab

**What it does:**
1. **Build & Test** 🏗️
   - Restores NuGet packages with caching
   - Builds the solution in Release mode
   - Runs all unit tests
   - Generates test reports and code coverage
   - Creates comprehensive test summaries

2. **Security Scan** 🔒
   - Scans for vulnerable NuGet packages
   - Reports security issues

3. **Docker Build** 🐳
   - Builds Docker image
   - Tests the containerized app
   - Caches layers for faster builds

4. **Deploy Preview** 🚀 (PR only)
   - Creates preview environment
   - Adds deployment link to PR

**Permissions & Security:**
The workflow has been configured with minimal required permissions:
- `contents: read` - Access repository code
- `checks: write` - Create check runs for test results
- `pull-requests: write` - Comment on PRs with results
- `actions: read` - Access workflow information

### 2. Dependabot Management

**Auto-approve** (`dependabot-auto-approve.yml`):
- Automatically approves minor/patch updates
- Adds detailed comments with update info
- Labels safe updates for auto-merge

**Auto-merge** (`auto-merge.yml`):
- Merges approved PRs with `auto-merge` label
- Ensures all checks pass before merging
- Works with both manual and Dependabot PRs

**Dependabot Config** (`dependabot.yml`):
- Weekly updates for NuGet, Docker, GitHub Actions
- Automatically assigns to project maintainers
- Ignores major version updates for stability

### 3. Cleanup (`cleanup.yml`)

- Runs weekly to delete old workflow runs
- Keeps 30 days of history minimum
- Maintains at least 10 runs per workflow

## 📋 How to View Results

### 1. Actions Tab

Visit: [https://github.com/eugenecp/setlist-studio/actions](https://github.com/eugenecp/setlist-studio/actions)

**What you'll see:**
- List of all workflow runs
- Status of each run (✅❌🟡)
- Duration and timestamps
- Triggered by information

### 2. Individual Workflow Run

Click on any workflow run to see:

**Summary Page:**
- Overall status
- Job breakdown with timing
- Artifacts available for download

**Job Details:**
- Step-by-step execution
- Console output for each step
- Error messages and stack traces

**Artifacts:**
- Test results (`.trx` files)
- Code coverage reports
- Security audit logs

### 3. Pull Request Integration

On each PR you'll see:

**Status Checks:**
- ✅ Build and test status
- 📊 Code coverage changes
- 🔒 Security scan results

**Comments:**
- Detailed test results
- Code coverage summary
- Deployment preview links

## 🛠️ Manual Triggers

### Run CI/CD Pipeline Manually

1. Go to [Actions tab](https://github.com/eugenecp/setlist-studio/actions)
2. Click "CI/CD Pipeline" in the left sidebar
3. Click "Run workflow" button
4. Select your branch
5. Click green "Run workflow" button

### Trigger Other Workflows

Most workflows support manual triggering:
- **Cleanup**: Clean old workflow runs
- **Dependabot workflows**: Re-run dependency checks

## 📊 Interpreting Results

### ✅ Success Indicators

- **Green checkmarks**: All tests pass
- **Build artifacts**: Successful compilation
- **Coverage reports**: Code quality metrics
- **Security scans**: No vulnerabilities found

### ❌ Failure Indicators

- **Red X marks**: Failed steps
- **Build errors**: Compilation issues
- **Test failures**: Unit test problems
- **Security alerts**: Vulnerable dependencies

### 📈 Performance Metrics

Monitor these key metrics:

- **Build Time**: Should stay under 5 minutes
- **Test Coverage**: Aim for 80%+ coverage
- **Security**: Zero high/critical vulnerabilities
- **Dependencies**: Keep packages up-to-date

## 🔧 Troubleshooting

### Common Issues

**Build Failures:**
```bash
# Local testing before push
dotnet build SetlistStudio.sln --configuration Release
dotnet test SetlistStudio.sln --configuration Release
```

**Test Failures:**
- Check test output in Actions tab
- Download test artifacts for detailed analysis
- Run tests locally to debug

**Security Alerts:**
- Review Dependabot PRs for updates
- Check security scan artifacts
- Update vulnerable packages

**Docker Issues:**
- Verify Dockerfile builds locally
- Check container health endpoint
- Review Docker build logs

**Permission Errors:**
If you see "Resource not accessible by integration" errors:
- ✅ **Fixed**: Workflow now includes proper permissions
- 📋 **Alternative**: Test results appear in workflow summary
- 💬 **PR Comments**: Basic summaries added to pull requests
- 🔍 **Troubleshooting**: Download artifacts for detailed analysis

**Test Reporter Issues:**
The workflow uses multiple approaches for test reporting:
- Primary: Built-in GitHub Actions summary
- Secondary: External test reporter (when permissions allow)
- Fallback: Downloadable artifacts with detailed results

### Getting Help

1. **Check workflow logs** first
2. **Review error messages** in failed steps
3. **Download artifacts** for detailed analysis
4. **Run locally** to reproduce issues
5. **Check permissions** if integration errors occur
6. **Open an issue** if problems persist

## 🎯 Best Practices

### For Contributors

1. **Always run tests locally** before pushing
2. **Keep PRs small** for faster CI runs
3. **Write meaningful commit messages**
4. **Add tests for new features**
5. **Check CI status** before requesting reviews

### For Maintainers

1. **Monitor workflow performance** regularly
2. **Keep dependencies updated** via Dependabot
3. **Review security scans** weekly
4. **Optimize build times** when possible
5. **Document pipeline changes**

## 📚 Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET GitHub Actions](https://docs.microsoft.com/en-us/dotnet/devops/github-actions-overview)
- [Dependabot Configuration](https://docs.github.com/en/code-security/dependabot)
- [Docker Actions](https://docs.docker.com/ci-cd/github-actions/)

---

*This pipeline ensures code quality, security, and reliability for Setlist Studio. Every change is automatically tested to maintain the high standards musicians expect from their tools.* 🎵