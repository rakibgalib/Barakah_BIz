FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY BarakahPlatform.sln .
COPY src/Shared/Barakah.SharedKernel/Barakah.SharedKernel.csproj src/Shared/Barakah.SharedKernel/
COPY src/Shared/Barakah.Persistence/Barakah.Persistence.csproj src/Shared/Barakah.Persistence/
COPY src/Shared/Barakah.TenantContext/Barakah.TenantContext.csproj src/Shared/Barakah.TenantContext/
COPY src/Shared/Barakah.EventBus/Barakah.EventBus.csproj src/Shared/Barakah.EventBus/
COPY src/Modules/OrderService/Barakah.OrderService.csproj src/Modules/OrderService/
RUN dotnet restore src/Modules/OrderService/Barakah.OrderService.csproj

COPY src/Shared/ src/Shared/
COPY src/Modules/OrderService/ src/Modules/OrderService/
RUN dotnet publish src/Modules/OrderService/Barakah.OrderService.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 5006
ENTRYPOINT ["dotnet", "Barakah.OrderService.dll"]
