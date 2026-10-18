FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Project files first, so the restore layer is cached until a dependency changes.
COPY ["ZaphkielBot/ZaphkielBot.csproj", "ZaphkielBot/"]
COPY ["ZaphkielCore/ZaphkielCore.csproj", "ZaphkielCore/"]
COPY ["ZaphkielData/ZaphkielData.csproj", "ZaphkielData/"]
COPY ["ZaphkielShared/ZaphkielShared.csproj", "ZaphkielShared/"]
RUN dotnet restore "ZaphkielBot/ZaphkielBot.csproj"
COPY . .
WORKDIR "/src/ZaphkielBot"
RUN dotnet build "./ZaphkielBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ZaphkielBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
