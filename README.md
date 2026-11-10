# Empire POS

Clean Visual Studio source package for the Empire POS solution.

## Requirements

- Windows 10/11
- Visual Studio 2022 with **ASP.NET and web development**
- .NET 8 SDK
- SQL Server Express (`.\SQLEXPRESS`)
- DYMO Connect Desktop for label printing

## Run locally

1. Open `EmpirePOS.sln` in Visual Studio.
2. Restore NuGet packages.
3. Confirm the SQL Server name in both local `appsettings.json` files.
4. Configure **Multiple startup projects** and start `Empire.API` before `Empire.Web`.
5. The API applies the included Entity Framework migrations and seeds a new database.

Initial local login for a newly created database:

- Username: `superadmin`
- Password: `Admin@123`

Change the password immediately after the first login.

## GitHub safety

The included `.gitignore` excludes build output, IDE files, local settings, databases,
logs, backups, deployment copies, uploaded files, archives, and runtime keys.

The local `appsettings.json` files are included in the ZIP so the project can run, but
they are ignored by Git. Commit `appsettings.example.json` instead. Never commit a real
database, customer data, JWT secret, printer credentials, or production configuration.

