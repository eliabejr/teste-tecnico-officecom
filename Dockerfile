FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/BCBGames.Domain/BCBGames.Domain.csproj", "src/BCBGames.Domain/"]
COPY ["src/BCBGames.Application/BCBGames.Application.csproj", "src/BCBGames.Application/"]
COPY ["src/BCBGames.Infrastructure/BCBGames.Infrastructure.csproj", "src/BCBGames.Infrastructure/"]
COPY ["src/BCBGames.API/BCBGames.API.csproj", "src/BCBGames.API/"]

RUN dotnet restore "src/BCBGames.API/BCBGames.API.csproj"

COPY . .
WORKDIR "/src/src/BCBGames.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BCBGames.API.dll"]
