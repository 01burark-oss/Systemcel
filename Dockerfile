# syntax=docker/dockerfile:1

FROM node:22-alpine AS web-build
WORKDIR /src/Systemcel.Web
COPY Systemcel.Web/package*.json ./
RUN npm ci
COPY Systemcel.Web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src
COPY global.json ./
COPY NuGet.Config ./
COPY Directory.Build.props ./
COPY CashTracker.Core/CashTracker.Core.csproj CashTracker.Core/
COPY CashTracker.Infrastructure/CashTracker.Infrastructure.csproj CashTracker.Infrastructure/
COPY Systemcel.Api/Systemcel.Api.csproj Systemcel.Api/
RUN dotnet restore Systemcel.Api/Systemcel.Api.csproj
COPY CashTracker.Core/ CashTracker.Core/
COPY CashTracker.Infrastructure/ CashTracker.Infrastructure/
COPY Systemcel.Api/ Systemcel.Api/
RUN dotnet publish Systemcel.Api/Systemcel.Api.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=api-build /app/publish ./
COPY --from=web-build /src/Systemcel.Web/dist ./wwwroot
EXPOSE 8080
ENTRYPOINT ["dotnet", "Systemcel.Api.dll"]
