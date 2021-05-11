dotnet ef migrations add AccountsDbMigration1 --context AccountsDbContext -o Data/Migrations/AccountsDb
dotnet ef migrations add ConfigurationDbMigration1 --context ConfigurationDbContext -o Data/Migrations/ConfigurationDb
dotnet ef migrations add PersistedGrantDbMigration1 --context PersistedGrantDbContext -o Data/Migrations/PersistedGrantDb