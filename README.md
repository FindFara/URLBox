# URLBox

A simple .NET 8 app for storing and sharing URLs within teams.

- One shared team name and password.
- A shared teams.yaml file for team information, plus a named YAML file per team for its URLs and projects.
- Team members manage their own URLs and projects.
- Administrators manage teams and access any team's URLs.
- Edit URLs, copy/open links, filter using project tags, and sort URLs in the admin dashboard. Click multiple project tags to include URLs from any selected project; All clears the filter.
- No SQL Server, Entity Framework, database setup, or migrations.

## Run

Configure the initial admin team in `src/URLBox.Presentation/appsettings.json`, or use environment variables `AdminTeam__Name` and `AdminTeam__Password`.

```powershell
dotnet run --project src/URLBox.Presentation
```

Open the local address printed by the application. The admin team is created automatically on first startup. Its password is only set at creation; later password changes use Admin > Teams.

## Storage

By default, data lives in `src/URLBox.Presentation/App_Data/teams/`:

```text
teams.yaml     # Team names, password hashes, administrator status and session versions
admin.yaml     # Admin team projects and URLs
Profit.yaml    # Profit team projects and URLs
```

The URL files do not contain login credentials. Renaming a team also renames its URL file while preserving its links and project assignments. Team names may contain Unicode letters, numbers, spaces, underscores, and hyphens. `teams` and operating-system reserved filenames cannot be team names.

Set `Storage:Directory` (environment variable `Storage__Directory`) to choose another folder. Relative paths resolve from the web project's content root. The folder must be writable and outside `wwwroot`. Files are read from disk on each operation, so valid manual YAML changes become visible immediately. Invalid files produce an error and are not silently replaced.

Writes use a temporary file and atomic replacement. A folder lock prevents concurrent requests or app processes from overwriting each other's edits. Back up the whole folder, including `.keys`, which preserves login cookies across restarts. For Docker, mount a writable volume at `/app/App_Data` or configure a different storage directory.

The previous SQL database is no longer accessed or modified. Existing SQL records are not automatically imported into the YAML files.

## Sample data

With `SampleData:Enabled` set to `true` (the default), the first startup creates:

- The configured admin team with a URLBox project and repository link.
- A `Profit` team, shared password `profit1234`, with Documentation and API projects and three sample URLs.

Samples are only added when the team folder is empty; subsequent starts preserve edits and deletions. Set `SampleData__Enabled=false` for an empty initial setup. If the admin team is named Profit, the sample team is named Profit Sample instead. A ready-to-use example folder containing `teams.yaml`, `admin.yaml`, and `Profit.yaml` is included in `samples/teams`. Copy all three files into an empty storage folder to use it. Its sample logins are `admin` / `Admin#123` and `Profit` / `profit1234`.

Each project has a color stored alongside its name in the team's YAML file. Teams can choose colors for their own projects in the Projects dialog; admins can also set colors for any selected team in the dashboard. Existing projects automatically receive colors from a palette. The same colors appear on the filter tags and URL cards, with contrasting text; selected filters show a check mark. URL card content is centered.

Teams create and rename their own environment columns (such as Dev, Beta, QA, or Production) through Environments. New teams have no predefined environments. Admins can manage environments for the selected team too. Environment names are stored in the team's YAML file; old files automatically retain the environment names used by their URLs. Renaming updates associated URLs, and an environment containing URLs must be emptied before deletion.

## Checks

```powershell
dotnet build URLBox.sln
dotnet run --project tests/URLBox.Checks
node tests/project-filter.checks.cjs
node tests/environment-navigation.checks.cjs
```

Checks cover YAML round trips, restart persistence, sample seeding, team isolation, editing, validation, password changes, concurrent writers, safe filenames, and malformed-file protection.
