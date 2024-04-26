These database commands should be executed from the solution root folder.

### new migration: 
- `dotnet ef migrations add Initial --context CoreDbContext --output-dir .\Infrastructure\EntityFramework\Migrations --project .\src\Core\Core.csproj --startup-project .\src\Presentation.Web.Server\Presentation.Web.Server.csproj`

### update database: 
- `dotnet ef database update --project .\src\Core\Core.csproj --startup-project .\src\Presentation.Web.Server\Presentation.Web.Server.csproj`