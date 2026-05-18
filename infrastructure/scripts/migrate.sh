#!/bin/bash
# Production migration script.
# Runs in the CI/CD pipeline. Production must not use MigrateAsync().

set -e

echo "Running migrations..."

dotnet ef database update \
  --project src/Modules/TicketGate.Identity \
  --startup-project src/TicketGate.API \
  --context IdentityDbContext \
  --no-build

dotnet ef database update \
  --project src/Modules/TicketGate.Event \
  --startup-project src/TicketGate.API \
  --context EventDbContext \
  --no-build

dotnet ef database update \
  --project src/Modules/TicketGate.Booking \
  --startup-project src/TicketGate.API \
  --context BookingDbContext \
  --no-build

dotnet ef database update \
  --project src/Modules/TicketGate.Payment \
  --startup-project src/TicketGate.API \
  --context PaymentDbContext \
  --no-build

echo "Migrations completed."
