# Security Recommendations Todo List

## Critical Priority (Fix Immediately)

### ✅ 1. Implement Security Headers Middleware
**Priority: CRITICAL** - ✅ **COMPLETED**
- [x] Add security headers middleware in Program.cs to protect against MIME sniffing, clickjacking, XSS attacks
- [x] Configure X-Content-Type-Options, X-Frame-Options, X-XSS-Protection headers
- [x] Implement Content-Security-Policy and Strict-Transport-Security headers
- **Files modified**: `src/SetlistStudio.Web/Program.cs`
- **Completed on**: October 15, 2025

### ✅ 2. Configure Rate Limiting for API Endpoints
**Priority: CRITICAL**
- [x] Implement rate limiting using Microsoft.AspNetCore.RateLimiting
- [x] Configure different limits for API endpoints (100/min), authentication (5/min)
- [x] Apply to all controllers and endpoints
- **Files to modify**: `src/SetlistStudio.Web/Program.cs`, Controllers

### ✅ 3. Add CSRF Protection with Anti-Forgery Tokens
**Priority: CRITICAL** - ✅ **COMPLETED**
- [x] Configure anti-forgery tokens in Program.cs with secure cookie settings
- [x] Implement custom header name and __Host- prefixed cookies
- [x] Add comprehensive test coverage (10 tests) for CSRF protection
- [x] Fix test compatibility issues with HTTPS-only cookie policies
- **Files modified**: `src/SetlistStudio.Web/Program.cs`, `tests/SetlistStudio.Tests/Web/ProgramTests.cs`, `tests/SetlistStudio.Tests/Pages/Pages__HostTests.cs`
- **Completed on**: October 15, 2025

### ✅ 4. Strengthen Password Policy Requirements
**Priority: CRITICAL**
- [ ] Update Identity configuration in Program.cs
- [ ] Require minimum 12 characters, mixed case, numbers, and special characters
- [ ] Remove 'demo purposes' relaxed settings and implement proper password security
- **Files to modify**: `src/SetlistStudio.Web/Program.cs`

### ✅ 5. Fix Production Configuration Security
**Priority: CRITICAL**
- [ ] Update appsettings.Production.json to restrict AllowedHosts
- [ ] Enforce HTTPS-only endpoints, remove HTTP configuration
- [ ] Add HTTPS redirection middleware in Program.cs
- **Files to modify**: `src/SetlistStudio.Web/appsettings.Production.json`, `Program.cs`

## High Priority

### ✅ 6. Implement Comprehensive Input Validation
**Priority: HIGH**
- [ ] Add input validation and sanitization across all controllers and services
- [ ] Create validation attributes for musical keys, BPM ranges, and user inputs
- [ ] Prevent XSS and injection attacks
- **Files to modify**: Controllers, Services, Validation attributes

### ✅ 7. Add Resource-Based Authorization Checks
**Priority: HIGH**
- [ ] Implement user ownership validation in SongService and SetlistService
- [ ] Ensure all data access methods verify the requesting user owns the resources
- [ ] Prevent unauthorized data access
- **Files to modify**: `src/SetlistStudio.Infrastructure/Services/`

### ✅ 8. Configure Secure Logging with Data Filtering
**Priority: HIGH**
- [ ] Update Serilog configuration to filter sensitive data from logs
- [ ] Implement secure logging utilities that automatically sanitize passwords, tokens, and personal information
- **Files to modify**: `src/SetlistStudio.Web/Program.cs`, Logging configuration

### ✅ 9. Add Security Event Logging
**Priority: HIGH**
- [ ] Implement logging for security events including failed login attempts
- [ ] Log authorization failures, suspicious activities, and authentication errors
- [ ] Configure appropriate detail levels
- **Files to modify**: Authentication handlers, Services

## Medium Priority (Security Hardening)

### ✅ 10. Configure CORS Policy with Domain Restrictions
**Priority: MEDIUM**
- [ ] Replace AllowedHosts '*' with specific trusted domains
- [ ] Configure CORS middleware in Program.cs with restrictive policy
- [ ] Never use wildcards in production
- **Files to modify**: `src/SetlistStudio.Web/Program.cs`, appsettings

### ✅ 11. Enhance Session Security Configuration
**Priority: MEDIUM**
- [ ] Configure secure cookies with HttpOnly, Secure, and SameSite attributes
- [ ] Use __Host- prefix for cookie names, set appropriate session timeouts (2 hours max)
- [ ] Enable sliding expiration
- **Files to modify**: `src/SetlistStudio.Web/Program.cs`

### ✅ 12. Implement Account Lockout Security
**Priority: MEDIUM**
- [ ] Verify and enhance Identity lockout configuration
- [ ] Ensure 5 failed attempts trigger 5-minute lockout
- [ ] Implement progressive lockout for repeated failures, add lockout logging
- **Files to modify**: `src/SetlistStudio.Web/Program.cs`

### ✅ 13. Add Input Validation Attributes and Regex
**Priority: MEDIUM**
- [ ] Create custom validation attributes for musical keys (C, C#, Db, etc.)
- [ ] Implement BPM ranges (40-250), and other domain-specific inputs
- [ ] Implement regex patterns for structured data validation
- **Files to modify**: New validation attributes, Entity models

### ✅ 14. Implement Audit Logging for Data Changes
**Priority: MEDIUM**
- [ ] Add audit trails for all data modifications including user tracking, timestamps, and change details
- [ ] Log create, update, delete operations on songs and setlists with user context
- **Files to modify**: Services, DbContext

### ✅ 15. Add Dependency Vulnerability Scanning
**Priority: MEDIUM**
- [ ] Integrate security scanning tools in CI/CD pipeline
- [ ] Configure automated vulnerability scanning for NuGet packages
- [ ] Update dependency management processes
- **Files to modify**: `.github/workflows/`, CI/CD configuration

### ✅ 16. Enhance Container Security with Distroless Images
**Priority: MEDIUM**
- [ ] Evaluate migration to distroless container images to reduce attack surface
- [ ] Review and minimize container permissions and exposed ports in Dockerfile
- **Files to modify**: `Dockerfile`

### ✅ 17. Create Security Testing Suite
**Priority: MEDIUM**
- [ ] Develop comprehensive security tests including authentication bypass attempts
- [ ] Add authorization checks, input validation tests, and OWASP Top 10 vulnerability tests
- **Files to modify**: Test projects

### ✅ 18. Configure Security Monitoring and Alerting
**Priority: LOW**
- [ ] Implement security monitoring for unusual patterns, failed authentication attempts, and potential attacks
- [ ] Set up alerting for security events and threshold breaches
- **Files to modify**: Monitoring configuration, Logging setup

### ✅ 19. Validate OAuth Provider Security Configuration
**Priority: LOW**
- [ ] Review OAuth callback URLs, scope configurations, and ensure secure redirect URI validation
- [ ] Verify proper token handling and storage practices for external authentication
- **Files to modify**: `src/SetlistStudio.Web/Program.cs`, OAuth configuration

### ✅ 20. Create Security Incident Response Plan
**Priority: LOW**
- [ ] Develop incident response procedures for security breaches
- [ ] Create vulnerability disclosure process, and emergency security patch deployment procedures
- **Files to modify**: Documentation, Process documents

## Progress Tracking

- **Total Items**: 20
- **Completed**: 1
- **In Progress**: 0
- **Not Started**: 19

## Notes

- Items 1-5 are **CRITICAL** and should be addressed before any production deployment
- Items 6-9 are **HIGH PRIORITY** and should be completed after critical items
- Items 10-20 provide comprehensive security hardening

## References

- [OWASP Top 10 2021](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)