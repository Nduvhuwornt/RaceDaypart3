# RaceDay - PROG6212 Programming 2B PoE

## System Description
RaceDay is a web-based event management system for the South African road running, walking and cycling
community. Organisers create and manage events, categories and results. Participants browse events,
enter them by choosing a category, and track their personal results.

## Roles
- **Organiser** - creates, edits and deletes their own events; manages event categories; views all enrolments
  for their events; captures finish times and positions.
- **Participant** - registers, browses events, enrols in an event by selecting a category, views their own
  enrolments and personal results.

## Repository Layout
```
README.md
RaceDay.sln
docker-compose.yml         Runs SQL Server + API + MVC with one command
.env.example               Settings template (copy to .env)
docs/                      Part 1 - ERD, endpoint plan (PDF + MD), SQL script
script/                    Copy of the SQL script (RaceDay_Database.sql)
src/RaceDay.Api/           Part 2/3 - ASP.NET Core Web API (EF Core, Swagger, session auth, Azure Blob Storage)
src/RaceDay.Mvc/           Part 3 - ASP.NET Core MVC web app (consumes the API) + Dockerfile
tests/RaceDay.Tests/       xUnit integration tests for the API
.github/workflows/         validate-docs.yml (Part 1) and ci.yml (build, test, Docker build)
```

## Part 1 - Planning (see /docs)
`RaceDay_ERD.png/.pdf`, `API_Endpoint_Plan.pdf/.md`, `RaceDay_Database.sql`.
Run the SQL script in SSMS on a clean SQL Server instance to create `RaceDayDB` with seed data.

## Part 2 - RESTful API

### Requirements
- .NET 8 SDK
- SQL Server (LocalDB, SQL Express or full SQL Server)
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

### Setup and run
1. Check the connection string in `src/RaceDay.Api/appsettings.json` (`ConnectionStrings:DefaultConnection`).
2. Create the first migration (Code-First) from the repository root:
   ```
   dotnet ef migrations add InitialCreate --project src/RaceDay.Api
   ```
3. Run the API (it applies migrations on start-up):
   ```
   dotnet run --project src/RaceDay.Api
   ```
4. Swagger opens at `http://localhost:5238/swagger` (set in `Properties/launchSettings.json`).

### Using Swagger
1. `POST /api/auth/register` - register a Participant, or an Organiser (needs the `organiserCode`
   from `appsettings.json`, key `Registration:OrganiserCode`).
2. `POST /api/auth/login` - the session cookie is stored by the browser.
3. Try the other endpoints. A `401` means not logged in; a `403` means the wrong role.

### Authentication and roles
- Passwords are hashed with PBKDF2 (SHA-256, random salt) and never stored in plain text.
- On login the server stores the user's ID and role in a server-side session; every protected endpoint checks it
  (`SessionAuthorize` attribute): `401` when logged out, `403` for the wrong role.
- Organisers can only change events, categories, enrolments and results that belong to their own events.
- Registering as an Organiser requires a secret organiser code, so users cannot pick the Organiser role freely.

### Unit tests
```
dotnet test RaceDay.sln
```
Tests boot the real API in memory with an isolated EF Core in-memory database and cover registration and login,
event management by Organisers, role rejection (401/403), enrolments and results.

### Differences from the Part 1 plan
Explained at the bottom of `docs/API_Endpoint_Plan.pdf`: organiser registration code, a few extra failure codes
(400/403/409), event list filters, and new enrolments being `Confirmed`. The code-first schema matches the ERD;
the SQL script's `DEFAULT GETDATE()` values are set in application code instead.

## Part 3 - MVC Web App, Azure Blob Storage and Docker

### What was built
- **MVC app (`src/RaceDay.Mvc`)** - talks to the API only (no database access, no business logic, no Azure access).
  The API's session cookie is kept in the MVC session and attached to every API call, so the API still decides
  who the user is and what they may do. Role-based navigation, protected pages (redirect to login), logout clears the session.
- **Organiser pages** - dashboard (events, enrolments per event, upcoming dates), create/edit/delete events with banner upload,
  category management, enrolments per event, results entry.
- **Participant pages** - home with filters (search and event type), event details with categories, enter an event by category,
  My Events with colour-coded status, My Results (finish time and position, e.g. "47th out of 312 finishers"), profile with picture upload.
- **Azure Blob Storage (in the API)** - `POST /api/events/{id}/banner` and `POST /api/users/me/profile-picture`.
  Files are validated (type, size, file signature), stored in Azure, and the public URL is saved in the database and shown in the MVC app.
- **Docker** - `src/RaceDay.Mvc/Dockerfile` (multi-stage build, non-root user) plus a Dockerfile for the API and a `docker-compose.yml`.

### Set up Azure Blob Storage
1. Azure Portal > **Storage accounts** > Create (Standard, LRS).
2. Storage account > **Settings > Configuration** > set **Allow Blob anonymous access** to **Enabled** and save.
3. **Security + networking > Access keys** > copy a **Connection string**.
4. Give it to the API (never commit it):
   - Visual Studio: right-click **RaceDay.Api** > **Manage User Secrets** and add
     `{ "AzureBlobStorage": { "ConnectionString": "<your connection string>" } }`
   - Docker: put `AZURE_STORAGE_CONNECTION_STRING=<your connection string>` in `.env`.
5. The `raceday-images` container is created automatically on the first upload (public read access for blobs).

### Run locally (Visual Studio 2026)
1. Set up the API as in Part 2 (connection string, migration).
2. Right-click the solution > **Configure Startup Projects** > **Multiple startup projects** > set **RaceDay.Api** and **RaceDay.Mvc** to **Start**.
3. Press F5. API: `http://localhost:5238/swagger`, MVC app: `http://localhost:5240`.
4. Register an Organiser (code from `Registration:OrganiserCode` in `src/RaceDay.Api/appsettings.json`) and a Participant.

### Run with Docker
Everything with one command (SQL Server, API and MVC):
```
docker compose up --build
```
Then open **http://localhost:8081** (MVC) and **http://localhost:5238/swagger** (API). Optional: copy `.env.example` to `.env` first to set the SQL password and Azure connection string.

Only the MVC container (API already running on your machine at port 5238):
```
docker build -t raceday-mvc ./src/RaceDay.Mvc
docker run --rm -p 8081:8080 -e Api__BaseUrl=http://host.docker.internal:5238 raceday-mvc
```

### Screenshots
- [ ] Add screenshots of the Organiser dashboard, event page with banner, and My Results.
- [ ] <img width="1895" height="903" alt="organiser dashboard" src="https://github.com/user-attachments/assets/409ed796-a716-4074-95a6-362c73904170" />
<img width="1902" height="915" alt="SWAGGER" src="https://github.com/user-attachments/assets/260847e3-8319-4235-95b9-0c8b9f88eb21" />
<img width="1903" height="912" alt="LOGIN PAGE" src="https://github.com/user-attachments/assets/6c537ef4-fd16-483c-9bc2-e39c2dfe8574" />
<img width="1903" height="912" alt="LOGIN PAGE" src="https://github.com/user-attachments/assets/ee08f235-1aa0-4f43-82b6-8d552cb13134" />


## CI/CD
- `ci.yml` restores, builds the API and MVC projects, runs all unit tests, then builds both Docker images on every push.
- `validate-docs.yml` checks the `/docs` folder and required files exist.
- [ ] Add screenshot of the green build here.

## Video
- [ ] Add the unlisted YouTube link here (Part 3: demo the full application, explain the MVC structure, Blob Storage integration and Docker setup; your own voice, no AI voiceover).

## AI Disclosure
- [ ] State briefly which parts were drafted with AI assistance (planning documents, API code scaffold, tests) and
  what you reviewed, changed and tested yourself.
