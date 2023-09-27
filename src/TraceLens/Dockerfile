FROM mcr.microsoft.com/dotnet/aspnet:8.0.0-preview.4 AS base
WORKDIR /app
EXPOSE 5001
EXPOSE 4317

FROM docker.io/rogeralsing/tracelens:amd64 AS build
WORKDIR /app

FROM base AS final
ENV ASPNETCORE_URLS=http://*:5001;http://*:4317
WORKDIR /app
COPY --from=build /app/ .
ENTRYPOINT ["dotnet", "TraceLens.Server.dll"]