# Use the official .NET 9 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Identity/Longhl104.Identity/Longhl104.Identity.csproj", "Identity/Longhl104.Identity/"]
COPY ["Shared/Longhl104.PawfectMatch/Longhl104.PawfectMatch.csproj", "Shared/Longhl104.PawfectMatch/"]

RUN dotnet restore "Identity/Longhl104.Identity/Longhl104.Identity.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Identity/Longhl104.Identity"
RUN dotnet build "Longhl104.Identity.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Longhl104.Identity.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Longhl104.Identity.dll"]
