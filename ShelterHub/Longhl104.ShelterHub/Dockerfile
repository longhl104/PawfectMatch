# Use the official .NET 9 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["ShelterHub/Longhl104.ShelterHub/Longhl104.ShelterHub.csproj", "ShelterHub/Longhl104.ShelterHub/"]
COPY ["Shared/Longhl104.PawfectMatch/Longhl104.PawfectMatch.csproj", "Shared/Longhl104.PawfectMatch/"]

RUN dotnet restore "ShelterHub/Longhl104.ShelterHub/Longhl104.ShelterHub.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/ShelterHub/Longhl104.ShelterHub"
RUN dotnet build "Longhl104.ShelterHub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Longhl104.ShelterHub.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Longhl104.ShelterHub.dll"]
