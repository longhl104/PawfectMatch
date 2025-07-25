# Use the official .NET 9 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Matcher/Longhl104.Matcher/Longhl104.Matcher.csproj", "Matcher/Longhl104.Matcher/"]
COPY ["Shared/Longhl104.PawfectMatch/Longhl104.PawfectMatch.csproj", "Shared/Longhl104.PawfectMatch/"]

RUN dotnet restore "Matcher/Longhl104.Matcher/Longhl104.Matcher.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Matcher/Longhl104.Matcher"
RUN dotnet build "Longhl104.Matcher.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Longhl104.Matcher.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Longhl104.Matcher.dll"]
