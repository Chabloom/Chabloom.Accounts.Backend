dotnet ef migrations add ApplicationDbMigration1 --context ApplicationDbContext -o Data/Migrations/ApplicationDb
dotnet ef migrations add ConfigurationDbMigration1 --context ConfigurationDbContext -o Data/Migrations/ConfigurationDb
dotnet ef migrations add PersistedGrantDbMigration1 --context PersistedGrantDbContext -o Data/Migrations/PersistedGrantDb