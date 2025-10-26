# Security Policy

## Supported Versions

We take security seriously and maintain security updates for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| main    | :white_check_mark: |
| 1.x.x   | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability in Setlist Studio, please report it responsibly:

### How to Report

1. **DO NOT** create a public GitHub issue for security vulnerabilities
2. Send an email to [security@setliststudio.com] with details about the vulnerability
3. Include steps to reproduce the issue if possible
4. Allow up to 48 hours for an initial response

### What to Include

Please include the following information in your report:

- Description of the vulnerability
- Steps to reproduce the issue  
- Potential impact of the vulnerability
- Any suggested fixes (if available)
- Your contact information

### Response Timeline

- **Initial Response**: Within 48 hours
- **Assessment**: Within 1 week
- **Fix Development**: Timeline depends on severity
- **Disclosure**: Coordinated disclosure after fix is available

## Security Measures

### Automated Security

- **Daily Security Scans**: Automated vulnerability scanning
- **Dependency Updates**: Automated dependency security updates via Dependabot
- **Static Analysis**: CodeQL and Semgrep for code security analysis
- **Container Scanning**: Docker image vulnerability scanning with Trivy
- **Secret Detection**: Automated scanning for exposed secrets

### Development Security

- **Secure Coding Guidelines**: Comprehensive security requirements in development workflow
- **Security Code Review**: All changes require security review
- **Input Validation**: All user inputs are validated and sanitized
- **Authentication**: OAuth-based authentication with secure session management
- **Authorization**: Resource-based authorization ensuring users can only access their data
- **HTTPS Enforcement**: All communication over HTTPS in production
- **Security Headers**: Comprehensive security headers for XSS, CSRF, and clickjacking protection

### Infrastructure Security

- **Container Security**: Non-root user, minimal attack surface
- **Database Security**: Parameterized queries, encrypted connections
- **Secrets Management**: No hardcoded secrets, environment variables and Key Vault
- **Rate Limiting**: API rate limiting to prevent abuse
- **Audit Logging**: Security events are logged and monitored

## Security Best Practices for Contributors

### Code Security

1. **Never commit secrets** - Use environment variables or secure vaults
2. **Validate all inputs** - Sanitize and validate user data
3. **Use parameterized queries** - Prevent SQL injection attacks
4. **Implement proper authorization** - Verify user permissions
5. **Follow secure coding guidelines** - See `.github/copilot-instructions.md`

### Dependencies

1. **Keep dependencies updated** - Regularly update packages
2. **Review new dependencies** - Evaluate security posture of new packages
3. **Monitor vulnerability alerts** - Address security advisories promptly

### Testing

1. **Security testing** - Include security test cases
2. **Edge case testing** - Test with malicious inputs
3. **Authentication testing** - Verify auth flows work correctly
4. **Test dependency isolation** - Test dependencies with vulnerabilities are isolated from production

### Test Dependency Security

Test dependencies may include packages with known vulnerabilities that are acceptable in test-only scenarios:

1. **Testcontainers.PostgreSql** - Contains PostgreSQL container image CVEs that are suppressed because:
   - Used only in isolated test containers, not production
   - PostgreSQL container runs temporarily during tests and is destroyed afterward  
   - Latest stable version (4.8.1) is maintained
   - CVEs are for PostgreSQL server, not the Testcontainers library
   - Tests are currently skipped by default and run manually when needed

2. **Suppression Review** - Test dependency suppressions are reviewed quarterly and updated when:
   - Newer secure versions become available
   - Alternative testing approaches are identified
   - Test dependencies are no longer needed

## Security Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Container Security Best Practices](https://sysdig.com/blog/dockerfile-best-practices/)

## Security Contact

For general security questions or concerns:
- Email: security@setliststudio.com
- Security Officer: Eugene CP (@eugenecp)

## Acknowledgments

We appreciate the security research community and responsible disclosure of vulnerabilities. Contributors who report valid security issues may be acknowledged in our security hall of fame (with their permission).

---

*This security policy is reviewed and updated regularly to ensure it reflects our current security practices.*