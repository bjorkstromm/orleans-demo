FROM mcr.microsoft.com/dotnet/aspnet:7.0.11 AS base
WORKDIR /app
EXPOSE 11111
EXPOSE 30000
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0.401 AS build
WORKDIR /src
COPY ["src/Booking.Silo/Booking.Silo.csproj", "src/Booking.Silo/"]
COPY ["src/Booking.Abstractions/Booking.Abstractions.csproj", "src/Booking.Abstractions/"]
RUN dotnet restore "src/Booking.Silo/Booking.Silo.csproj"
COPY . .
WORKDIR "/src/src/Booking.Silo"
RUN dotnet build "Booking.Silo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Booking.Silo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Booking.Silo.dll"]
