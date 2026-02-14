
# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/URLBox.Presentation/URLBox.Presentation.csproj", "src/URLBox.Presentation/"]
COPY ["src/URLBox.Application/URLBox.Application.csproj", "src/URLBox.Application/"]
COPY ["src/URLBox.Domain/URLBox.Domain.csproj", "src/URLBox.Domain/"]
COPY ["src/URLBox.Infrastructure/URLBox.Infrastructure.csproj", "src/URLBox.Infrastructure/"]
RUN dotnet restore "./src/URLBox.Presentation/URLBox.Presentation.csproj"
COPY . .
WORKDIR "/src/src/URLBox.Presentation"
RUN dotnet build "./URLBox.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./URLBox.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "URLBox.Presentation.dll"]