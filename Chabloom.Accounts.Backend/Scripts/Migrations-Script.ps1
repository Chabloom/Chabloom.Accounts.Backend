dotnet ef migrations script -i --context AccountsDbContext -o Scripts/AccountsDb.sql
dotnet ef migrations script -i --context ConfigurationDbContext -o Scripts/ConfigurationDb.sql
dotnet ef migrations script -i --context PersistedGrantDbContext -o Scripts/PersistedGrantDb.sql