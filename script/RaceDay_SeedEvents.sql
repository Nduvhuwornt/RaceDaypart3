/* =========================================================================
   RaceDay - sample events for the running application
   Adds upcoming events and categories (dated relative to today) so the
   Participant home page has something to show.

   BEFORE RUNNING:
   1. Start the API once so it creates the database and tables.
   2. Register an Organiser in the MVC app (the events are attached to the
      first Organiser account found).
   3. Change the database name below if your connection string uses another one.
   Safe to re-run: events/categories that already exist are skipped.
   ========================================================================= */
USE RaceDayDB;
GO

SET NOCOUNT ON;

DECLARE @OrganiserId INT =
(
    SELECT TOP 1 u.UserId
    FROM dbo.Users u
    JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE r.RoleName = 'Organiser'
    ORDER BY u.UserId
);

IF @OrganiserId IS NULL
BEGIN
    RAISERROR('No Organiser found. Register an Organiser in the app first, then run this script again.', 16, 1);
    RETURN;
END

DECLARE @Today DATETIME = CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME);

-- ---------- Events ----------
INSERT INTO dbo.Events (OrganiserId, Name, Description, EventDate, Location, DistanceKm, EventType, BannerImageUrl, CreatedAt)
SELECT @OrganiserId, v.Name, v.Description, v.EventDate, v.Location, v.DistanceKm, v.EventType, NULL, GETUTCDATE()
FROM (VALUES
    ('Benoni Lake Fun Run',
     'A friendly community run around Benoni Lake and the surrounding parkland. All fitness levels welcome.',
     DATEADD(HOUR, 7, DATEADD(DAY, 10, @Today)), 'Benoni, Gauteng', 10.0, 'Run'),
    ('Stellenbosch Vineyard Walk',
     'A relaxed family walk through the Stellenbosch vineyards with views of the Cape Winelands.',
     DATEADD(HOUR, 8, DATEADD(DAY, 17, @Today)), 'Stellenbosch, Western Cape', 5.0, 'Walk'),
    ('Pretoria Charity Cycle Tour',
     'A scenic road cycling event raising funds for local schools.',
     DATEADD(HOUR, 6, DATEADD(DAY, 24, @Today)), 'Pretoria, Gauteng', 42.0, 'Cycle'),
    ('Soweto Community Half Marathon',
     'A half marathon through the streets of Soweto with live music and crowd support along the route.',
     DATEADD(HOUR, 6, DATEADD(DAY, 45, @Today)), 'Soweto, Gauteng', 21.1, 'Run'),
    ('Durban Beachfront Ride',
     'A flat, fast cycle along the Durban beachfront promenade.',
     DATEADD(HOUR, 7, DATEADD(DAY, 60, @Today)), 'Durban, KwaZulu-Natal', 30.0, 'Cycle')
) AS v(Name, Description, EventDate, Location, DistanceKm, EventType)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Events e WHERE e.Name = v.Name);

-- ---------- Categories ----------
INSERT INTO dbo.Categories (EventId, Name, MinAge, MaxAge, DistanceKm)
SELECT e.EventId, c.Name, c.MinAge, c.MaxAge, c.DistanceKm
FROM dbo.Events e
JOIN (VALUES
    ('Benoni Lake Fun Run', 'Under 20', 10, 19, 10.0),
    ('Benoni Lake Fun Run', 'Senior', 20, 59, 10.0),
    ('Benoni Lake Fun Run', 'Veteran (60+)', 60, 120, 10.0),
    ('Stellenbosch Vineyard Walk', 'Family (all ages)', NULL, NULL, 5.0),
    ('Pretoria Charity Cycle Tour', '42km Road', NULL, NULL, 42.0),
    ('Pretoria Charity Cycle Tour', '21km Half', NULL, NULL, 21.0),
    ('Soweto Community Half Marathon', 'Open 21.1km', 18, 120, 21.1),
    ('Soweto Community Half Marathon', '10km Fun Run', NULL, NULL, 10.0),
    ('Durban Beachfront Ride', '30km Road', NULL, NULL, 30.0),
    ('Durban Beachfront Ride', '15km Social', NULL, NULL, 15.0)
) AS c(EventName, Name, MinAge, MaxAge, DistanceKm) ON c.EventName = e.Name
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories x WHERE x.EventId = e.EventId AND x.Name = c.Name);

SELECT e.EventId, e.Name, e.EventDate, e.EventType, COUNT(c.CategoryId) AS Categories
FROM dbo.Events e
LEFT JOIN dbo.Categories c ON c.EventId = e.EventId
GROUP BY e.EventId, e.Name, e.EventDate, e.EventType
ORDER BY e.EventDate;
GO
