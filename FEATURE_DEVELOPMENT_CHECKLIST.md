# Feature Development Checklist — Five Principles

**MANDATORY: Use this checklist for every feature, regardless of size or complexity.**

This checklist ensures all features meet Setlist Studio's quality standards: Works, Secure, Scales, Maintainable, User Delight.

---

## Pre-Development: Feature Planning

- [ ] **Feature goal is clear**: What problem does it solve for musicians?
- [ ] **User workflow documented**: How will musicians actually use this feature?
- [ ] **Success metrics defined**: How will we know if this feature delights users?
- [ ] **Scope defined**: What's in scope? What's deferred to v2?

---

## ✅ **Works: Core Functionality**

### Implementation
- [ ] **Feature works as intended**: Happy path tested manually
- [ ] **API contract clear**: Request/response shapes documented with examples
- [ ] **Pagination implemented** (for list endpoints): Offset or keyset pagination with stable ordering
- [ ] **Filtering logic** (for searchable features): Server-side filtering, case-insensitive where appropriate
- [ ] **Error responses clear**: All error cases return meaningful HTTP status + message
- [ ] **Response metadata included**: Pagination info, counts, headers (`X-Total-Count` etc.)
- [ ] **Async/await used consistently**: All I/O operations are asynchronous

### Testing
- [ ] **Happy path tests**: Verify core functionality works correctly
- [ ] **Boundary tests**: Edge cases, empty results, single item, max limits
- [ ] **Error scenario tests**: Invalid inputs, missing data, service failures
- [ ] **Data consistency tests**: Verify correct data returned in correct order

---

## 🔒 **Secure: Security & Validation**

### Input Validation
- [ ] **All user inputs validated**: No null/empty strings where not allowed
- [ ] **Parameter clamping enforced**: Page sizes, limits, ranges have maximums
- [ ] **Whitespace trimmed**: Search terms, filters trimmed before use
- [ ] **Case-insensitive matching**: Genre/search filters handle case variations safely
- [ ] **SQL injection prevented**: Use parameterized LINQ queries, NEVER string concatenation
- [ ] **XSS prevention**: Input sanitized, output encoded

### Authorization & Authentication
- [ ] **[Authorize] attribute applied**: Endpoint requires authentication
- [ ] **User ownership verified**: Queries filtered by UserId; cross-user access impossible
- [ ] **Resource-based authorization**: Users can only access their own data
- [ ] **[EnableRateLimiting] applied**: Endpoint protected against DoS
- [ ] **[ValidateAntiForgeryToken] used**: POST/PUT/DELETE operations protected
- [ ] **[InputSanitization] applied**: Malicious input patterns rejected

### Logging & Monitoring
- [ ] **Sensitive data NOT logged**: Use `SecureLoggingHelper.Sanitize*`
- [ ] **User IDs sanitized**: Log `SanitizedUserId`, not bare user IDs
- [ ] **Audit logging integrated**: CREATE/UPDATE/DELETE operations logged via `IAuditLogService`
- [ ] **Error logging contextual**: Logs include enough context for debugging (but no secrets)
- [ ] **Security events tracked**: Failed auth, suspicious activity logged

### Data Protection
- [ ] **HTTPS enforced**: Secure cookie flags (`HttpOnly`, `Secure`, `SameSite`)
- [ ] **No hardcoded secrets**: All sensitive config from env vars or Key Vault
- [ ] **Secrets validated**: `SecretValidationService` checks placeholder values
- [ ] **Data encryption at rest**: Sensitive fields encrypted if needed
- [ ] **CORS policy tight**: No wildcard origins; only trusted domains allowed

---

## 📈 **Scales: Performance & Efficiency**

### Database Queries
- [ ] **AsNoTracking() used for reads**: Change-tracking disabled for read-only queries
- [ ] **Projection to DTOs**: Select specific columns, not full entities
- [ ] **N+1 problems avoided**: Use `Include()` strategically, not in loops
- [ ] **Indexes created**: Composite indexes for common filter + sort patterns
- [ ] **Query performance tested**: Queries complete within 100ms on typical data
- [ ] **Pagination limits enforced**: Max pageSize prevents huge result sets
- [ ] **Stable ordering with tie-breaker**: OrderBy + ThenBy ensures reproducible pages

### Caching & Optimization
- [ ] **Hot data cached**: Expensive queries (genres, artists) cached via `IQueryCacheService`
- [ ] **Cache invalidation logic**: Writes invalidate user-scoped caches
- [ ] **Memory efficiency**: No unnecessary object allocations or large intermediate collections
- [ ] **Async operations throughout**: Free up thread pool threads; don't block

### Scalability Roadmap
- [ ] **Current limits understood**: Know when this feature hits bottlenecks (100 users? 1M records?)
- [ ] **Growth strategy documented**: How to scale from SQLite to Postgres, single to multi-instance?
- [ ] **Performance monitoring hooks**: Logs include timing data or metrics for analysis

### Load Testing (optional for high-impact features)
- [ ] **Benchmark tests written**: Measure performance on realistic data volumes
- [ ] **Concurrency tested**: Multiple users accessing simultaneously behave correctly
- [ ] **Resource usage profiled**: Memory, CPU, DB connections under load

---

## 📚 **Maintainable: Code Quality & Documentation**

### Code Style & Organization
- [ ] **Naming conventions followed**: Class/method/variable names are clear and consistent
- [ ] **Single responsibility**: Methods do one thing well
- [ ] **DRY principle applied**: No duplicated logic across files
- [ ] **Error handling granular**: Catch specific exceptions, not generic `Exception`
- [ ] **Comments explain "why"**: Not "what" (code is self-explanatory)

### Testing & Coverage
- [ ] **Unit tests added**: Service/business logic tested in isolation
- [ ] **Integration tests added**: Controller/service interaction tested
- [ ] **80%+ line coverage**: Each file meets minimum coverage target
- [ ] **80%+ branch coverage**: All conditional paths tested
- [ ] **Test naming clear**: `{Method}_{Scenario}_{ExpectedResult}` pattern
- [ ] **Tests deterministic**: Pass consistently; no flaky random failures

### Documentation
- [ ] **XML doc comments**: Public methods/classes documented with `///<summary>`
- [ ] **Business logic explained**: Why does this algorithm exist? What problem does it solve?
- [ ] **API examples provided**: Swagger/OpenAPI examples show request/response
- [ ] **Error cases documented**: Explain when/why each error response occurs
- [ ] **Dependencies documented**: What services/infrastructure does this need?

### Code Review Readiness
- [ ] **PR description clear**: Why is this change needed? What does it do?
- [ ] **Test evidence included**: Reference test results, coverage metrics
- [ ] **Breaking changes noted**: Any API changes that affect existing code?
- [ ] **Migration path provided**: If data model changed, migration strategy documented
- [ ] **Security review checklist**: All security considerations addressed (see above)

---

## ✨ **User Delight: Musician Experience**

### Real-World Use Cases
- [ ] **Backstage workflow**: Works reliably with poor lighting, quick access
- [ ] **Performance context**: Tested in realistic musician scenarios (not just office)
- [ ] **Error recovery**: Graceful failure, not cryptic error messages
- [ ] **Offline capability**: Critical features work without internet (where applicable)
- [ ] **Mobile-friendly**: UI responsive on phones/tablets (if customer-facing)

### User Experience
- [ ] **Intuitive interaction**: Musician unfamiliar with app understands feature immediately
- [ ] **Terminology familiar**: Uses musical language, not programmer jargon
- [ ] **Fast response**: No noticeable lag; <500ms for common operations
- [ ] **Consistent with existing UI**: Matches patterns in Setlist Studio already
- [ ] **Confirmations for destructive actions**: Delete/overwrite actions need confirmation

### Customer Value
- [ ] **Solves real musician problem**: Not "nice to have"; addresses actual pain point
- [ ] **Measurable benefit**: Users can articulate why this feature helps them perform better
- [ ] **Professional quality**: Would musicians use this in paying gig (not just hobby)?
- [ ] **Small touches of delight**: One "wow" moment that makes experience memorable
- [ ] **Reduces friction**: Fewer steps, fewer clicks, fewer errors in common workflows

### Sample Data
- [ ] **Realistic music data**: Use authentic song titles, artists, BPMs, keys
- [ ] **Real genre examples**: Classic Rock, Jazz Standards, Folk, Blues, Funk, etc.
- [ ] **Realistic performance scenarios**: Wedding sets, concert tours, practice sessions
- [ ] **Diverse musicians**: Feature works for solo artists, bands, orchestras

---

## Pre-Submission: Quality Gate

### Build & Tests
- [ ] **Clean build**: Zero warnings, zero errors
- [ ] **All tests pass**: 100% test success rate (no failing tests)
- [ ] **Coverage targets met**: 80%+ line and branch coverage per file
- [ ] **No regressions**: Existing tests still pass

### Security Analysis
- [ ] **CodeQL security scan passes**: Zero high/critical security issues
- [ ] **Local CodeQL validation**: Run `scripts/run-codeql-security.ps1` locally, inspect results
- [ ] **Input validation comprehensive**: All user inputs validated before use
- [ ] **Authorization checks in place**: UserId filtering, [Authorize] attributes, resource ownership verified

### Performance Validation
- [ ] **No N+1 queries**: Entity Framework queries optimized
- [ ] **Response times acceptable**: <500ms for API, <2s for page loads
- [ ] **Database queries <100ms**: Query performance tested
- [ ] **Memory usage reasonable**: No memory leaks, no unbounded collections

### Documentation Complete
- [ ] **Feature documented in copilot-instructions.md**: Pattern and examples added (if reusable)
- [ ] **README or wiki updated**: High-level feature description for team
- [ ] **API documented**: Swagger/OpenAPI definitions accurate
- [ ] **Deployment notes**: Any special setup/migration steps documented

---

## Post-Submission: Monitoring & Feedback

### After Merge
- [ ] **Monitor error logs**: Watch for unexpected exceptions in production
- [ ] **Monitor performance metrics**: API response times, database query times
- [ ] **Gather user feedback**: Are musicians finding the feature delightful?
- [ ] **Track adoption metrics**: Are users actually using the feature?

### Iteration
- [ ] **Collect pain points**: What confuses users? What takes too long?
- [ ] **Prioritize improvements**: Which complaints will have biggest impact?
- [ ] **Schedule follow-up**: Plan v2 improvements based on real usage

---

## Example: Applying This Checklist

**Feature**: "Filter songs by BPM range"

✅ **Works**: 
- [ ] Implement `GetSongsByBpmRangeAsync(userId, minBpm, maxBpm, pageNumber, pageSize)`
- [ ] Return paginated results with stable ordering

🔒 **Secure**:
- [ ] Validate BPM range: minBpm >= 40, maxBpm <= 250
- [ ] Clamp pageSize to max 100
- [ ] Always filter by UserId first
- [ ] Add [Authorize], [EnableRateLimiting], [InputSanitization]

📈 **Scales**:
- [ ] Create index: `(UserId, Bpm, Artist, Title, Id)`
- [ ] Use AsNoTracking() for reads
- [ ] Cache genre list invalidation on song updates

📚 **Maintainable**:
- [ ] Add unit tests: happy path, boundary (40, 250), error cases
- [ ] Document API example in Swagger
- [ ] Add inline comments explaining Bpm validation logic

✨ **User Delight**:
- [ ] Musicians can quickly find uptempo songs for energetic setlists
- [ ] Clear error: "BPM must be between 40 and 250"
- [ ] Results ordered by artist for quick scanning

---

**Remember**: Every feature, every time. No shortcuts. The five principles compound over time to create a product musicians love and a codebase the team maintains with pride.
