FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["04_Async_04_Docker/04_Async_04_Docker.csproj", "04_Async_04_Docker/"]
RUN dotnet restore "04_Async_04_Docker/04_Async_04_Docker.csproj"
COPY . .
WORKDIR "/src/04_Async_04_Docker"
RUN dotnet build "./04_Async_04_Docker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./04_Async_04_Docker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "04_Async_04_Docker.dll"]
