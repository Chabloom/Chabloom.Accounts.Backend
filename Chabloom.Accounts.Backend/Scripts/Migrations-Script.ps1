dotnet ef migrations script -i --context ApplicationDbContext -o Scripts/ApplicationDb.sql
dotnet ef migrations script -i --context ConfigurationDbContext -o Scripts/ConfigurationDb.sql
dotnet ef migrations script -i --context PersistedGrantDbContext -o Scripts/PersistedGrantDb.sql