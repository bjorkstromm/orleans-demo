FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 11111
EXPOSE 30000

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Booking.Dashboard/Booking.Dashboard.csproj", "src/Booking.Dashboard/"]
RUN dotnet restore "src/Booking.Dashboard/Booking.Dashboard.csproj"
COPY . .
WORKDIR "/src/src/Booking.Dashboard"
RUN dotnet build "Booking.Dashboard.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Booking.Dashboard.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Booking.Dashboard.dll"]
