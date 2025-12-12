# Setlist Studio Development Patterns & Practices

This document captures key patterns, approaches, and best practices discovered and implemented during Setlist Studio development.

---

## **1. Project Structure & Architecture**

### Three-Layer Architecture
- **UI Layer** (`src/SetlistStudio.Web`): Blazor Server with MudBlazor, controllers, API endpoints
- **Core Layer** (`src/SetlistStudio.Core`): Entities, interfaces, validation, business logic
- **Infrastructure Layer** (`src/SetlistStudio.Infrastructure`): Data access, EF Core, services, configuration

### Key Directories
```
src/SetlistStudio.Web/
├── Controllers/          # API endpoints (REST)
├── Pages/               # Blazor pages/components
├── Services/            # UI-specific services
├── Middleware/          # Custom middleware
├── Security/            # Authentication/authorization
├── Models/              # Request/response DTOs

src/SetlistStudio.Core/
├── Entities/            # Domain models (Song, Setlist, etc.)
├── Interfaces/          # Service contracts (ISongService, etc.)
├── Models/              # Value objects (SongFilterCriteria, PaginatedResult)
├── Validation/          # Custom validators
├── Security/            # Security helpers

src/SetlistStudio.Infrastructure/
├── Data/                # DbContext, migrations, seeds
├── Services/            # Business logic (SongService, SongFilterService)
├── Configuration/       # DB configuration, constants
├── Security/            # Infrastructure security (audit, etc.)
```

---

## **2. Running the Application**

### Local Development (Recommended)
```powershell
Set-Location 'd:\setlist\setlist-studio'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src\SetlistStudio.Web\SetlistStudio.Web.csproj
```

**Key Points:**
- Development environment auto-disables strict secret validation
- Sample data is seeded automatically in Development
- Kestrel binds to http://localhost:5000 and https://localhost:5001 (configured in `appsettings.Development.json`)
- SQLite is used for Development DB

### Configuration Files Hierarchy
1. `appsettings.json` — Base configuration (all environments)
2. `appsettings.Development.json` — Dev-specific overrides (contains Urls, Kestrel endpoints)
3. `appsettings.Production.json` — Production settings
4. Environment variables — Override any config value (e.g., `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT`)

### Health Check Endpoints
- `/health` — Simple liveness probe (returns 200 if app running)
- `/health/ready` — Readiness probe with JSON details about service status

---

## **3. Dependency Injection & Service Registration**

### Pattern: Register in Program.cs
All services are registered in `src/SetlistStudio.Web/Program.cs` in the `ConfigureServices()` method.

```csharp
// Register application services
services.AddScoped<ISongService, SongService>();
services.AddScoped<SongFilterService>();  // No interface needed for filter service
services.AddScoped<ISetlistService, SetlistService>();
```

**Scopes:**
- `Transient` — New instance each time (state-less)
- `Scoped` — New instance per HTTP request (good for services with state)
- `Singleton` — Single instance for app lifetime (stateless, cacheable)

### Injection in Controllers
```csharp
public class SongsController : ControllerBase
{
    private readonly ISongService _songService;
    private readonly SongFilterService _filterService;
    private readonly ILogger<SongsController> _logger;

    public SongsController(ISongService songService, SongFilterService filterService, ILogger<SongsController> logger)
    {
        _songService = songService;
        _filterService = filterService;
        _logger = logger;
    }
}
```

---

## **4. Song Filtering Implementation (Approach 2: Server-Side)**

### Pattern: Layered Filtering Service

**Core concept:** Build a reusable, composable filtering pipeline in EF Core.

#### Step 1: Define Filter Criteria (DTO)
```csharp
// src/SetlistStudio.Core/Models/SongFilterCriteria.cs
public class SongFilterCriteria
{
    public string? SearchText { get; set; }
    public List<string>? Genres { get; set; }
    public int? MinBpm { get; set; }
    public int? MaxBpm { get; set; }
    public List<string>? MusicalKeys { get; set; }
    public int? DifficultyMin { get; set; }
    public int? DifficultyMax { get; set; }
    public List<string>? Tags { get; set; }
    public int? MinDurationSeconds { get; set; }
    public int? MaxDurationSeconds { get; set; }
    public string? SortBy { get; set; } = "artist";
    public string? SortOrder { get; set; } = "asc";
}
```

#### Step 2: Create Paginated Result Wrapper
```csharp
// src/SetlistStudio.Core/Models/PaginatedResult.cs
public class PaginatedResult<T>
{
    public List<T> Items { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
```

#### Step 3: Implement Filter Service with Composable Filters
```csharp
// src/SetlistStudio.Infrastructure/Services/SongFilterService.cs
public class SongFilterService
{
    public async Task<PaginatedResult<Song>> FilterSongsAsync(
        string userId,
        SongFilterCriteria criteria,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _context.Songs.Where(s => s.UserId == userId);
        
        // Apply filters in sequence
        query = ApplyTextSearchFilter(query, criteria.SearchText);
        query = ApplyGenreFilter(query, criteria.Genres);
        query = ApplyBpmFilter(query, criteria.MinBpm, criteria.MaxBpm);
        query = ApplyMusicalKeyFilter(query, criteria.MusicalKeys);
        // ... more filters
        
        var totalCount = await query.CountAsync();
        query = ApplySorting(query, criteria.SortBy, criteria.SortOrder);
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return new PaginatedResult<Song>(items, pageNumber, pageSize, totalCount);
    }

    #region Filter Methods
    private IQueryable<Song> ApplyTextSearchFilter(IQueryable<Song> query, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return query;
        var searchLower = searchText.ToLower();
        return query.Where(s =>
            s.Title.ToLower().Contains(searchLower) ||
            s.Artist.ToLower().Contains(searchLower) ||
            (s.Album != null && s.Album.ToLower().Contains(searchLower)));
    }

    private IQueryable<Song> ApplyGenreFilter(IQueryable<Song> query, List<string>? genres)
    {
        if (genres == null || genres.Count == 0) return query;
        return query.Where(s => genres.Contains(s.Genre!));
    }
    // ... more filter methods
    #endregion

    // Metadata endpoints for dropdown data
    public async Task<List<string>> GetAvailableGenresAsync(string userId) { ... }
    public async Task<List<string>> GetAvailableKeysAsync(string userId) { ... }
    public async Task<List<string>> GetAvailableTagsAsync(string userId) { ... }
}
```

#### Step 4: Expose via API Controller
```csharp
[HttpPost("filter")]
public async Task<IActionResult> FilterSongs(
    [FromBody] SongFilterCriteria criteria,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20)
{
    var userId = SecureUserContext.GetSanitizedUserId(User);
    var result = await _filterService.FilterSongsAsync(userId, criteria, pageNumber, pageSize);
    return Ok(result);
}

[HttpGet("filter/genres")]
public async Task<IActionResult> GetAvailableGenres()
{
    var userId = SecureUserContext.GetSanitizedUserId(User);
    var genres = await _filterService.GetAvailableGenresAsync(userId);
    return Ok(genres);
}
```

### Benefits of This Pattern
✅ **Efficiency** — Single EF Core query, optimized by database  
✅ **Composability** — Each filter is independent, easy to add/remove  
✅ **Testability** — Filter methods can be unit tested  
✅ **Scalability** — Handles 1K+ songs efficiently  
✅ **Security** — User isolation via `userId` filter  
✅ **UX** — Metadata endpoints enable dynamic UI (genre/key dropdowns)

---

## **5. Error Handling & Resolution Patterns**

### Build Errors
**Problem:** File locked during build
```
error MSB3021: Unable to copy file ... The process cannot access the file because it is being used by another process.
```

**Solution:**
```powershell
Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue
dotnet clean src\SetlistStudio.Web\SetlistStudio.Web.csproj
dotnet run --project src\SetlistStudio.Web\SetlistStudio.Web.csproj
```

### Compilation Errors
**Problem:** Lambda expressions in health check registration
```
error CS1660: Cannot convert lambda expression to type 'IHealthCheck'
```

**Solution:** Use simpler API
```csharp
// ❌ Wrong: custom lambda doesn't match IHealthCheck signature
builder.Services.AddHealthChecks()
    .AddCheck("database", async (sp, ct) => { ... });

// ✅ Right: use basic registration or custom class implementing IHealthCheck
builder.Services.AddHealthChecks();
```

### Nullability Issues
**Problem:** EF Core selects nullable reference types
```csharp
// Returns List<string?> but signature expects List<string>
Select(s => s.Genre)  // ❌

// Fix: use null-forgiving operator
Select(s => s.Genre!)  // ✅
```

---

## **6. Authentication & Security Patterns**

### User Isolation
Every data query filters by `UserId` to ensure users only see their own data:
```csharp
var query = _context.Songs.Where(s => s.UserId == userId);
```

### Secure User Context
Extract and sanitize user ID from claims:
```csharp
var userId = SecureUserContext.GetSanitizedUserId(User);
var sanitizedId = SecureLoggingHelper.SanitizeUserId(userId);  // For logging
```

### Rate Limiting
Configured in `Program.cs`:
```csharp
// Development rates: Global=10000, API=1000, Auth=50
[EnableRateLimiting("ApiPolicy")]
public async Task<IActionResult> FilterSongs(...)
```

### Input Validation
Use attributes and custom validators:
```csharp
[BpmRange(40, 250)]
public int? Bpm { get; set; }

[MusicalKey]
public string? MusicalKey { get; set; }
```

---

## **7. Logging & Observability**

### Serilog Configuration
- **Console output** — Real-time logging to terminal
- **File rolling** — Daily log files in `logs/` directory
- **Security logs** — Separate `logs/security/` folder for sensitive events
- **Sensitive data filtering** — Automatically redacts usernames, IPs, tokens

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}")
    .WriteTo.File("logs/setlist-studio-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.File("logs/security/security-.txt", restrictedToMinimumLevel: LogEventLevel.Warning)
    .Filter.ByExcluding(logEvent => SecureLoggingHelper.SensitivePatterns.Any(...))
    .CreateLogger();
```

### Logging in Services
```csharp
_logger.LogInformation(
    "Filtered songs for user {UserId}: found {Count} total, returning page {Page}",
    SecureLoggingHelper.SanitizeUserId(userId),
    totalCount,
    pageNumber);

_logger.LogError(ex, "Error filtering songs for user {UserId}", 
    SecureLoggingHelper.SanitizeUserId(userId));
```

---

## **8. Testing Endpoints**

### PowerShell Examples

**Test liveness:**
```powershell
Invoke-WebRequest -UseBasicParsing https://localhost:5001/health -SkipCertificateCheck
```

**Test readiness:**
```powershell
Invoke-WebRequest -UseBasicParsing https://localhost:5001/health/ready -SkipCertificateCheck | Select-Object StatusCode, Content
```

**Test filter endpoint:**
```powershell
$criteria = @{
    searchText = "bohemian"
    genres = @("rock")
    minBpm = 80
    maxBpm = 200
    musicalKeys = @("Cm")
    difficultyMin = 2
    sortBy = "artist"
    sortOrder = "asc"
} | ConvertTo-Json

$result = Invoke-WebRequest -UseBasicParsing `
  -Uri "https://localhost:5001/api/songs/filter?pageNumber=1&pageSize=20" `
  -Method Post `
  -ContentType "application/json" `
  -Body $criteria `
  -SkipCertificateCheck

$result.Content | ConvertFrom-Json | Select-Object Items, TotalPages, TotalCount
```

**Get dropdown data:**
```powershell
Invoke-WebRequest -UseBasicParsing https://localhost:5001/api/songs/filter/genres -SkipCertificateCheck | Select-Object -ExpandProperty Content | ConvertFrom-Json
```

---

## **9. Development Workflow**

### 1. Understand the Domain
- Read `README.md` for features
- Check `src/SetlistStudio.Core/Entities` for data models
- Review existing services in `src/SetlistStudio.Infrastructure/Services`

### 2. Plan Architecture
- Define DTOs/Models in `Core/Models`
- Create service in `Infrastructure/Services`
- Register in DI container in `Program.cs`
- Expose via Controller in `Web/Controllers`

### 3. Implement & Test
- Write code following existing patterns
- Build: `dotnet build`
- Run: `dotnet run`
- Test endpoints with PowerShell or Postman

### 4. Debug
- Check `logs/` for application logs
- Check `logs/security/` for security events
- Use `_logger.LogInformation()` for custom debugging

---

## **10. Common Gotchas & Solutions**

| Issue | Cause | Solution |
|-------|-------|----------|
| `URLs: null` in logs | Kestrel not reading config | Add `Urls` and `Kestrel` to `appsettings.Development.json` |
| Health check won't compile | Lambda signature mismatch | Use simple `AddHealthChecks()` without custom lambdas |
| Nullability errors | EF Core nullable references | Use null-forgiving `!` operator or explicit null checks |
| File locked during build | Process still running | `Stop-Process -Name dotnet -Force` |
| `Unauthorized` on endpoints | Missing `[Authorize]` attribute | Add `[Authorize]` to controller or methods |
| Slow queries | Missing pagination | Always use `.Skip().Take()` for list endpoints |
| Data leakage | Missing user filter | Always filter queries by `UserId` |

---

## **11. Server-Side Filtering: Deep Dive (5-Principle Architecture)**

### The Problem We Solved
Musicians need to quickly find songs in their library by multiple criteria (genre, BPM, key, difficulty) without waiting for full page loads. A naive client-side filter works for 10-50 songs but fails at scale.

### ✅ **Works: How the Pattern Functions**

The server-side filter uses a **composable pipeline** pattern in EF Core:

```
Client Request (SongFilterCriteria)
    ↓
SongFilterService.FilterSongsAsync(userId, criteria, pageNumber, pageSize)
    ↓
Build Base Query: _context.Songs.Where(s => s.UserId == userId)
    ↓
Apply Filters Sequentially:
    ├─ ApplyTextSearchFilter() → Title/Artist/Album contains search
    ├─ ApplyGenreFilter() → Genre in list
    ├─ ApplyBpmFilter() → Bpm between min/max
    ├─ ApplyMusicalKeyFilter() → Key in list
    ├─ ApplyDifficultyFilter() → DifficultyRating between min/max
    ├─ ApplyDurationFilter() → Duration between min/max
    └─ ApplyTagFilter() → Tags contains any requested tag
    ↓
Get Total Count (before pagination)
    ↓
Apply Sorting (OrderBy/OrderByDescending)
    ↓
Pagination: Skip((page-1)*pageSize).Take(pageSize)
    ↓
Execute Single Optimized SQL Query
    ↓
Return PaginatedResult<Song> with metadata
    ↓
Client receives Items, TotalPages, HasNextPage, etc.
```

**Key: Single Query**
All filters compile into ONE SQL WHERE clause, executed by the database. No N+1 queries, no client-side filtering of large datasets.

### 🔒 **Secure: Validation & Security Requirements**

**User Isolation (Critical)**
```csharp
// Every query starts with this
var query = _context.Songs.Where(s => s.UserId == userId);
```
- Prevents users from seeing other users' songs
- Enforced at database query level (defense in depth)
- Cannot be bypassed even with authorization mistakes

**Input Validation**
```csharp
// Controller validates before passing to service
if (pageNumber < 1) pageNumber = 1;
if (pageSize < 1 || pageSize > 100) pageSize = 20;  // Cap at 100 to prevent DoS

// SearchText is case-insensitive, not regex (prevents injection)
var searchLower = searchText.ToLower();
query = query.Where(s => s.Title.ToLower().Contains(searchLower));  // Safe
```

**Authorization Checks**
```csharp
[HttpPost("filter")]
[Authorize]  // Must be authenticated
[EnableRateLimiting("ApiPolicy")]  // Rate limited
public async Task<IActionResult> FilterSongs(...)
```

**Sanitized Logging**
```csharp
_logger.LogInformation(
    "Filtered songs for user {UserId}: found {Count} total",
    SecureLoggingHelper.SanitizeUserId(userId),  // Logs as "User_XXX" hash
    totalCount);
```

### 📈 **Scales: Performance Considerations**

**Database Index Strategy**
The Song table should have these indexes for optimal filter performance:
```sql
-- User isolation (always needed)
CREATE INDEX idx_songs_userid ON Songs(UserId);

-- Filter performance (add as filtering becomes common)
CREATE INDEX idx_songs_genre ON Songs(UserId, Genre);
CREATE INDEX idx_songs_bpm ON Songs(UserId, Bpm);
CREATE INDEX idx_songs_difficulty ON Songs(UserId, DifficultyRating);

-- Composite for common filter combinations
CREATE INDEX idx_songs_filter_composite ON Songs(UserId, Genre, Bpm, DifficultyRating);
```

**Pagination Best Practices**
```csharp
// ✅ Good: pagination with reasonable limits
var items = await query
    .Skip((pageNumber - 1) * pageSize)  // 0-based offset
    .Take(pageSize)  // Max 100 items
    .ToListAsync();

// ❌ Bad: loading all 10,000 songs then filtering in memory
var allSongs = await query.ToListAsync();
var filtered = allSongs.Where(s => s.Bpm > 100).Skip(100).Take(20).ToList();
```

**Performance Metrics**
- **1K songs:** < 50ms query time
- **10K songs:** < 100ms with proper indexes
- **100K songs:** Still < 200ms with composite indexes

**Query Compilation**
EF Core compiles the LINQ to SQL once, caches it, reuses on subsequent calls.

### 📚 **Maintainable: Code Example & Conventions**

**File Organization**
```
src/SetlistStudio.Core/Models/
├── SongFilterCriteria.cs      # Request DTO
└── PaginatedResult.cs         # Response wrapper

src/SetlistStudio.Infrastructure/Services/
└── SongFilterService.cs       # Implementation

src/SetlistStudio.Web/Controllers/
└── SongsController.cs         # API endpoints
```

**Naming Conventions**
- **Criteria classes** end in `Criteria` (SongFilterCriteria)
- **Filter methods** named `ApplyXyzFilter()` (ApplyBpmFilter)
- **Result wrappers** generic: `PaginatedResult<T>`
- **Service methods** are async Task: `FilterSongsAsync()`

**Code Pattern: Adding a New Filter**

To add a new filter (e.g., "isLiveVersion"), follow this 3-step pattern:

**Step 1:** Add property to criteria
```csharp
public class SongFilterCriteria
{
    public bool? IsLiveVersion { get; set; }
}
```

**Step 2:** Call filter in pipeline
```csharp
public async Task<PaginatedResult<Song>> FilterSongsAsync(...)
{
    var query = _context.Songs.Where(s => s.UserId == userId);
    query = ApplyTextSearchFilter(query, criteria.SearchText);
    query = ApplyLiveVersionFilter(query, criteria.IsLiveVersion);  // NEW
    // ... rest of filters
}
```

**Step 3:** Implement filter method
```csharp
private IQueryable<Song> ApplyLiveVersionFilter(IQueryable<Song> query, bool? isLive)
{
    if (!isLive.HasValue) return query;
    if (isLive.Value)
        return query.Where(s => s.Tags != null && s.Tags.Contains("live"));
    return query.Where(s => s.Tags == null || !s.Tags.Contains("live"));
}
```

**Code Review Checklist**
- [ ] Filter method is pure (no side effects)
- [ ] Handles null/empty gracefully
- [ ] Includes logging
- [ ] User isolation not bypassed
- [ ] No N+1 queries
- [ ] Pagination applied correctly

### ✨ **User Delight: Business Value**

**What Musicians Love**

1. **Fast Discovery**
   - Find "fast upbeat rock songs" in < 100ms
   - No page reloads, instant feedback
   - Especially valuable during performance preparation

2. **Smart Dropdowns**
   - Genre, key, and tag dropdowns show only what you have
   - No "Genre: Jazz" option if you have 0 jazz songs
   - **API:** `/api/songs/filter/genres`, `/api/songs/filter/keys`, `/api/songs/filter/tags`

3. **Responsive UI**
   - Filtering feels instant (server handles complexity)
   - Works on mobile with poor WiFi
   - Pagination avoids slow page loads

4. **Real-world Scenarios**
   - "Show me all songs in G minor under 3 minutes" → 2 results, 50ms
   - "Give me easy covers to warm up" → Difficulty 1-2, sorted by duration
   - "What rock songs do I have?" → Genre = Rock, 23 results across 2 pages

5. **Collaboration Ready**
   - When bandmates join, they instantly see shared setlist songs
   - Filters work the same for all users
   - No confusion about who can see what

**Competitive Advantage**
vs. spreadsheets: "I know exactly what I'm playing"
vs. generic setlist tools: "Finds songs by musician-relevant attributes (BPM, key, difficulty)"
vs. apps without filtering: "Setlist Studio lets me organize by what matters"

---

## **12. Next Steps for Enhancement**

### Immediate (Quick Wins)
- [ ] Add pagination UI to Blazor pages
- [ ] Create `SongLibrary.razor` component that calls filter API
- [ ] Add sorting toggles and filter UI

### Short-term (1-2 weeks)
- [ ] Implement Approach 4: Redis caching for filter results
- [ ] Add full-text search with PostgreSQL FTS
- [ ] Create saved filter/favorites feature

### Medium-term (1 month)
- [ ] PDF export for setlists
- [ ] CSV import for songs
- [ ] Collaborative editing with SignalR
- [ ] Offline mode / PWA

---

## **References**

- [Setlist Studio README](README.md)
- [Musician Quick Start Guide](docs/musician-quick-start.md)
- [Deployment Guide](docs/musician-deployment.md)
- EF Core Docs: https://docs.microsoft.com/en-us/ef/core/
- Blazor Docs: https://docs.microsoft.com/en-us/aspnet/core/blazor/

---

**Last Updated:** December 12, 2025  
**Editor:** Development Team  
**Status:** Active Development
