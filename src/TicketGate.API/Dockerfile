FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/TicketGate.API/TicketGate.API.csproj", "src/TicketGate.API/"]
COPY ["src/TicketGate.Core/TicketGate.Core.csproj", "src/TicketGate.Core/"]
COPY ["src/Modules/TicketGate.Identity/TicketGate.Identity.csproj", "src/Modules/TicketGate.Identity/"]
COPY ["src/Modules/TicketGate.Event/TicketGate.Event.csproj", "src/Modules/TicketGate.Event/"]
COPY ["src/Modules/TicketGate.Booking/TicketGate.Booking.csproj", "src/Modules/TicketGate.Booking/"]
COPY ["src/Modules/TicketGate.Payment/TicketGate.Payment.csproj", "src/Modules/TicketGate.Payment/"]
COPY ["src/Modules/TicketGate.Notification/TicketGate.Notification.csproj", "src/Modules/TicketGate.Notification/"]
RUN dotnet restore "src/TicketGate.API/TicketGate.API.csproj"
COPY . .
RUN dotnet build "src/TicketGate.API/TicketGate.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/TicketGate.API/TicketGate.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicketGate.API.dll"]
