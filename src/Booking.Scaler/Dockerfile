FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Booking.Scaler/Booking.Scaler.csproj", "src/Booking.Scaler/"]
RUN dotnet restore "src/Booking.Scaler/Booking.Scaler.csproj"
COPY . .
WORKDIR "/src/src/Booking.Scaler"
RUN dotnet build "Booking.Scaler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Booking.Scaler.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Booking.Scaler.dll"]
