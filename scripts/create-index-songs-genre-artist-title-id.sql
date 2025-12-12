-- Creates a composite index to speed up queries filtering by UserId and Genre,
-- ordered by Artist, Title, and tie-broken by Id.
-- Suitable for PostgreSQL. Adjust syntax for other DBMS.

-- Safe to run multiple times; IF NOT EXISTS guards duplicate creation.
CREATE INDEX IF NOT EXISTS IX_Songs_UserId_Genre_Artist_Title_Id
ON "Songs" ("UserId", "Genre", "Artist", "Title", "Id");

-- Optional: create a covering index for common projection fields to avoid lookups
-- CREATE INDEX IF NOT EXISTS IX_Songs_UserId_Genre_Artist_Title_Id_Includes
-- ON "Songs" ("UserId", "Genre", "Artist", "Title", "Id")
-- INCLUDE ("Bpm", "MusicalKey", "DurationSeconds", "Tags");
