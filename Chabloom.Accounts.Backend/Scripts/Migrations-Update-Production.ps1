$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet ef database update --context AccountsDbContext
dotnet ef database update --context ConfigurationDbContext
dotnet ef database update --context PersistedGrantDbContext