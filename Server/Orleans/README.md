# AbilityKit Orleans Server

This folder contains the Orleans server, TCP gateway, room/battle grains, and Shooter smoke automation used by local development and integration validation.

## Projects

- `AbilityKit.Orleans.Contracts`: Grain interfaces, DTOs, and shared result status codes.
- `AbilityKit.Orleans.Grains`: Room, battle, and gameplay grain implementations.
- `AbilityKit.Orleans.Gateway`: HTTP/TCP gateway and room operation error mapping.
- `AbilityKit.Orleans.Host`: Standalone silo host.
- `AbilityKit.Orleans.ShooterSmoke`: Self-hosted Shooter TCP Gateway E2E smoke runner.
- `AbilityKit.Orleans.Grains.Tests`: Grain/runtime adapter tests.
- `AbilityKit.Orleans.Gateway.Tests`: Gateway error mapping tests.

## Run

- Build and run `AbilityKit.Orleans.Host` for standalone local server development.

## Shooter Smoke

The Shooter smoke runner self-hosts an Orleans silo and TCP gateway, then validates guest login, room creation/readiness, battle start, snapshot push, local input submission, stale snapshot rejection, late join projection, reconnect projection, and battle cleanup.

```powershell
.\tools\run_shooter_smoke.ps1 -Configuration Debug -TcpPort 41001
```

```cmd
tools\run_shooter_smoke.bat -Configuration Debug -TcpPort 41001
```

Use `-NoBuild` when the smoke project has already been built. Use `-TcpPort` to avoid local port conflicts in parallel runs.

## Validation

```cmd
dotnet test src\AbilityKit.Orleans.Grains.Tests\AbilityKit.Orleans.Grains.Tests.csproj --filter ShooterBattleRuntimeAdapterTests
```

```cmd
dotnet test src\AbilityKit.Orleans.Gateway.Tests\AbilityKit.Orleans.Gateway.Tests.csproj
```
