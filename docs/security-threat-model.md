# Security Threat Model for Setlist Studio

## Executive Summary

This document identifies security threats for user-generated content (songs, setlists, templates) and defines security patterns to protect against attacks.

---

## 🚨 Identified Security Threats

### 1. **SQL Injection (HIGH RISK)**
**Attack Vector**: Malicious input in search queries, filters, song titles
**Impact**: Database compromise, data theft, data destruction
**Example**: `'; DROP TABLE Songs--` in search field
**Mitigation**: Use Entity Framework LINQ queries exclusively, never concatenate SQL

### 2. **Cross-Site Scripting (XSS) (HIGH RISK)**
**Attack Vector**: Malicious JavaScript in song titles, artist names, notes
**Impact**: Session hijacking, credential theft, malicious redirects
**Example**: `<script>alert('xss')</script>` in song title
**Mitigation**: Input validation with regex patterns, output encoding

### 3. **Horizontal Privilege Escalation (CRITICAL)**
**Attack Vector**: Users accessing other users' songs/setlists via direct object references
**Impact**: Unauthorized data access, privacy breach
**Example**: User A accessing `/api/songs/123` owned by User B
**Mitigation**: Always filter by userId in database queries

### 4. **Mass Assignment (MEDIUM RISK)**
**Attack Vector**: Unauthorized modification of protected fields (UserId, CreatedAt, Id)
**Impact**: Data integrity violation, privilege escalation
**Example**: POST request with `{"UserId": "admin-user"}` to create song
**Mitigation**: Use DTOs/request models, never bind directly to entities

### 5. **Information Disclosure (MEDIUM RISK)**
**Attack Vector**: Error messages revealing system internals, stack traces
**Impact**: System architecture exposure, aids other attacks
**Example**: Exposing database connection strings in error messages
**Mitigation**: Generic error messages, secure logging

### 6. **Denial of Service (MEDIUM RISK)**
**Attack Vector**: Large inputs, excessive requests, resource exhaustion
**Impact**: Service unavailability, performance degradation
**Example**: 10,000-character song titles, 1000 requests/second
**Mitigation**: Input length limits, rate limiting, pagination

### 7. **CSRF Attacks (MEDIUM RISK)**
**Attack Vector**: State-changing operations without proper token validation
**Impact**: Unauthorized actions performed on behalf of authenticated users
**Example**: Malicious site triggers DELETE request to `/api/songs/123`
**Mitigation**: Anti-forgery tokens on all POST/PUT/DELETE operations

### 8. **Insecure Direct Object Reference (HIGH RISK)**
**Attack Vector**: Accessing resources via predictable IDs without authorization
**Impact**: Unauthorized data access
**Example**: Iterating through `/api/songs/1`, `/api/songs/2`, etc.
**Mitigation**: Authorization checks at every endpoint

---

## 🛡️ Required Security Patterns

### Pattern 1: Input Validation for Musical Data

#### **BPM Validation**
```csharp
[SafeBpm(40, 250)]
public int Bpm { get; set; }
```
- **Range**: 40-250 (realistic musical range)
- **Prevents**: DoS attacks via integer overflow, unrealistic data

#### **Musical Key Validation**
```csharp
[MusicalKey]
public string? Key { get; set; }
```
- **Whitelist**: 33 valid keys (C, C#, Db, D, etc.)
- **Prevents**: SQL injection, XSS, invalid data

#### **String Field Validation**
```csharp
[SafeString(MaxLength = 200, AllowSpecialCharacters = true)]
public string Title { get; set; }
```
- **Checks**: Length, XSS patterns, SQL injection patterns
- **Allows**: Musical notation (apostrophes, ampersands, hyphens)

### Pattern 2: Authorization Checks

#### **User Ownership Verification**
```csharp
// ALWAYS filter by userId FIRST
var query = _context.Songs.Where(s => s.UserId == userId);

// Verify ownership before operations
var userId = SecureUserContext.GetSanitizedUserId(User);
var song = await _songService.GetByIdAsync(songId, userId);
if (song == null)
    return NotFound(new { error = "Song not found or access denied" });
```

#### **Entry Point Authorization**
```csharp
[Authorize]  // REQUIRED on all endpoints
[EnableRateLimiting("ApiPolicy")]  // Rate limiting
[ValidateAntiForgeryToken]  // For state-changing operations
public async Task<IActionResult> CreateSong([FromBody] CreateSongRequest request)
```

### Pattern 3: Secure Error Handling

```csharp
try
{
    // Business logic
}
catch (UnauthorizedAccessException)
{
    _logger.LogWarning("Unauthorized access attempt by user {UserId}", sanitizedUserId);
    return Forbid();  // Don't reveal why
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed for user {UserId}", sanitizedUserId);
    return Problem("An error occurred processing your request");  // Generic message
}
```

---

## ❌ Security Anti-Patterns (What NOT to Do)

### **1. Trusting Client-Side Validation Only**
```csharp
// ❌ WRONG: No server-side validation
// Client: <input type="number" min="40" max="250" required />
public int Bpm { get; set; }  // Bypassable!
```

### **2. Missing Authorization Checks**
```csharp
// ❌ WRONG: No ownership verification
[HttpGet("{id}")]
public async Task<IActionResult> GetSong(int id)
{
    var song = await _context.Songs.FindAsync(id);  // Any user's song!
    return Ok(song);
}
```

### **3. SQL String Concatenation**
```csharp
// ❌ WRONG: SQL injection vulnerability
var genre = request.Genre;
var sql = $"SELECT * FROM Songs WHERE Genre = '{genre}'";  // DANGEROUS!
```

### **4. Exposing Internal Details**
```csharp
// ❌ WRONG: Information disclosure
catch (Exception ex)
{
    return BadRequest(ex.Message);  // Exposes stack trace!
}
```

### **5. Different Error Messages for Enumeration**
```csharp
// ❌ WRONG: User enumeration
if (user == null)
    return NotFound("User not found");  // Different message!
if (!ValidPassword(password))
    return Unauthorized("Invalid password");  // Confirms user exists!
```

---

## ✅ Security Validation Checklist

Before deploying any feature:

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

## 🧪 Security Testing Requirements

### **Test Categories**

1. **Malicious Input Tests**: XSS, SQL injection, command injection
2. **Authorization Tests**: Unauthorized access, horizontal privilege escalation
3. **Validation Tests**: Invalid BPM, invalid keys, oversized inputs
4. **Rate Limiting Tests**: Excessive requests, burst traffic
5. **CSRF Tests**: State-changing operations without tokens

### **Example Security Test**
```csharp
[Theory]
[InlineData("<script>alert('xss')</script>")]
[InlineData("'; DROP TABLE Songs--")]
[InlineData("javascript:alert('xss')")]
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

---

## 📊 Security Risk Matrix

| Threat | Likelihood | Impact | Priority | Mitigation Status |
|--------|-----------|--------|----------|-------------------|
| SQL Injection | High | Critical | P0 | ✅ Implemented |
| XSS | High | High | P0 | ✅ Implemented |
| Horizontal Privilege Escalation | High | Critical | P0 | ✅ Implemented |
| Mass Assignment | Medium | Medium | P1 | ✅ Implemented |
| Information Disclosure | Medium | Medium | P1 | ✅ Implemented |
| DoS | Low | Medium | P2 | ✅ Implemented |
| CSRF | Medium | High | P1 | ✅ Implemented |
| IDOR | High | High | P0 | ✅ Implemented |

---

## 🔄 Continuous Security

1. **Regular CodeQL Scans**: Every PR must pass security analysis
2. **Dependency Updates**: Weekly security patch reviews
3. **Penetration Testing**: Quarterly security audits
4. **Security Training**: Monthly security awareness for team
5. **Incident Response**: Documented process for security issues

---

## 📚 References

- OWASP Top 10: https://owasp.org/www-project-top-ten/
- .NET Security Best Practices: https://docs.microsoft.com/en-us/aspnet/core/security/
- CodeQL Security Queries: https://codeql.github.com/codeql-query-help/csharp/
