FROM mcr.microsoft.com/dotnet/aspnet:7.0.11 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0.401 AS build
WORKDIR /src
COPY ["src/Booking.Admin/Booking.Admin.csproj", "src/Booking.Admin/"]
RUN dotnet restore "src/Booking.Admin/Booking.Admin.csproj"
COPY . .
WORKDIR "/src/src/Booking.Admin"
RUN dotnet build "Booking.Admin.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Booking.Admin.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Booking.Admin.dll"]
