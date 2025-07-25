FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY Chabloom.Accounts.Backend/Chabloom.Accounts.Backend.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY global.json ./
COPY Chabloom.Accounts.Backend/ ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT [ "./Chabloom.Accounts.Backend" ]
