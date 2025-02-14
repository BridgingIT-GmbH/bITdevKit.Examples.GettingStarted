These database commands should be executed from the solution root folder.

### new migration: 
- `dotnet ef migrations add Initial --context AppDbContext --output-dir .\EntityFramework\Migrations --project .\src\Infrastructure\Infrastructure.csproj --startup-project .\src\Presentation.Web.Server\Presentation.Web.Server.csproj`

### update database: 
- `dotnet ef database update --project .\src\Infrastructure\Infrastructure.csproj --startup-project .\src\Presentation.Web.Server\Presentation.Web.Server.csproj`