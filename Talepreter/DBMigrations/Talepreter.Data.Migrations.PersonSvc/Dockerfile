FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.19 AS build-env
WORKDIR /app

# Copy everything
COPY . .

# Build and publish a release
WORKDIR /app/DBMigrations/Talepreter.Data.Migrations.PersonSvc
RUN dotnet restore "Talepreter.Data.Migrations.PersonSvc.csproj"
RUN dotnet build "Talepreter.Data.Migrations.PersonSvc.csproj" -c Release -o /app/build
RUN dotnet publish "Talepreter.Data.Migrations.PersonSvc.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine3.19
WORKDIR /app

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build-env /app/publish .
ENTRYPOINT ["dotnet", "Talepreter.Data.Migrations.PersonSvc.dll"]
