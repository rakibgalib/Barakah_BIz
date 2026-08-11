FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY BarakahPlatform.sln .
COPY src/Shared/Barakah.SharedKernel/Barakah.SharedKernel.csproj src/Shared/Barakah.SharedKernel/
COPY src/Shared/Barakah.Persistence/Barakah.Persistence.csproj src/Shared/Barakah.Persistence/
COPY src/Shared/Barakah.TenantContext/Barakah.TenantContext.csproj src/Shared/Barakah.TenantContext/
COPY src/Modules/CatalogService/Barakah.CatalogService.csproj src/Modules/CatalogService/
RUN dotnet restore src/Modules/CatalogService/Barakah.CatalogService.csproj

COPY src/Shared/ src/Shared/
COPY src/Modules/CatalogService/ src/Modules/CatalogService/
RUN dotnet publish src/Modules/CatalogService/Barakah.CatalogService.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 5004
ENTRYPOINT ["dotnet", "Barakah.CatalogService.dll"]
