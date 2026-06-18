# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["POT_System_ASPNET.csproj", "./"]
RUN dotnet restore "POT_System_ASPNET.csproj"

# Copy all source files and publish the app
COPY . .
RUN dotnet publish "POT_System_ASPNET.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 8080 (default in .NET 8 ASP.NET images)
EXPOSE 8080
ENTRYPOINT ["dotnet", "POT_System_ASPNET.dll"]
