FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Booking.Web/Booking.Web.csproj", "src/Booking.Web/"]
COPY ["src/Booking.Abstractions/Booking.Abstractions.csproj", "src/Booking.Abstractions/"]
RUN dotnet restore "src/Booking.Web/Booking.Web.csproj"
COPY . .
WORKDIR "/src/src/Booking.Web"
RUN dotnet build "Booking.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Booking.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Booking.Web.dll"]
