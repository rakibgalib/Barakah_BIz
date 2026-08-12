FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY BarakahPlatform.sln .
COPY src/Shared/Barakah.SharedKernel/Barakah.SharedKernel.csproj src/Shared/Barakah.SharedKernel/
COPY src/Shared/Barakah.Persistence/Barakah.Persistence.csproj src/Shared/Barakah.Persistence/
COPY src/Shared/Barakah.TenantContext/Barakah.TenantContext.csproj src/Shared/Barakah.TenantContext/
COPY src/Modules/RestaurantService/Barakah.RestaurantService.csproj src/Modules/RestaurantService/
RUN dotnet restore src/Modules/RestaurantService/Barakah.RestaurantService.csproj

COPY src/Shared/ src/Shared/
COPY src/Modules/RestaurantService/ src/Modules/RestaurantService/
RUN dotnet publish src/Modules/RestaurantService/Barakah.RestaurantService.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 5010
ENTRYPOINT ["dotnet", "Barakah.RestaurantService.dll"]
