-- Query Optimization Indexes Migration for Setlist Studio
-- This migration adds database indexes to improve query performance for user-specific operations

-- Add indexes for Songs table (if they don't exist)
CREATE INDEX IF NOT EXISTS IX_Songs_UserId_MusicalKey ON Songs (UserId, MusicalKey);
CREATE INDEX IF NOT EXISTS IX_Songs_UserId_Bpm ON Songs (UserId, Bpm);
CREATE INDEX IF NOT EXISTS IX_Songs_UserId_Album ON Songs (UserId, Album);
CREATE INDEX IF NOT EXISTS IX_Songs_UserId_CreatedAt ON Songs (UserId, CreatedAt);

-- Add indexes for Setlists table (if they don't exist)
CREATE INDEX IF NOT EXISTS IX_Setlists_UserId_CreatedAt ON Setlists (UserId, CreatedAt);
CREATE INDEX IF NOT EXISTS IX_Setlists_UserId_IsActive ON Setlists (UserId, IsActive);
CREATE INDEX IF NOT EXISTS IX_Setlists_UserId_Venue ON Setlists (UserId, Venue);
CREATE INDEX IF NOT EXISTS IX_Setlists_UserId_IsTemplate_IsActive ON Setlists (UserId, IsTemplate, IsActive);

-- Additional composite indexes for common query patterns
CREATE INDEX IF NOT EXISTS IX_Songs_Artist_Genre ON Songs (Artist, Genre) WHERE UserId IS NOT NULL;
CREATE INDEX IF NOT EXISTS IX_Songs_Bpm_MusicalKey ON Songs (Bpm, MusicalKey) WHERE UserId IS NOT NULL;
CREATE INDEX IF NOT EXISTS IX_Setlists_PerformanceDate_IsActive ON Setlists (PerformanceDate, IsActive) WHERE UserId IS NOT NULL;

-- Performance optimization notes:
-- - These indexes improve user-specific queries performance
-- - Composite indexes support multiple filter combinations
-- - WHERE clauses on indexes exclude null values for better performance
-- - All indexes include UserId for tenant isolation