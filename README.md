# WBand

## Description

The project is a web application that allows users to create and manage their own bands. The application is built using the latest web technologies and is designed to be user-friendly and easy to navigate.

## Local development

Run the application from the repository root with `aspire start --non-interactive`.
The AppHost is `App/WBand.AppHost/WBand.AppHost.csproj`.

### Database migrations

WebAPI uses EF Core with PostgreSQL. Restore the EF CLI and create a migration:

```sh
dotnet tool restore
bash scripts/add-migration.sh AddUserProfile
```

EF migrations in `App/WBand.WebAPI/Data/Migrations` run at API startup.
See [WebAPI persistence documentation](App/WBand.WebAPI/README.md) for transaction,
event publication and existing database baseline details.

### Infrastructure stuck in Unknown

If Docker is running but all container resources remain `Unknown`, inspect DCP logs:

```sh
DCP_DIAGNOSTICS_LOG_LEVEL=info aspire start --non-interactive
```

On Linux, the diagnostic logs are in `/tmp/dcp/logs`. With Aspire CLI 13.6.1
and Docker 29.8.2, DCP can repeatedly report `Could not determine whether
container volume exists` for a missing volume instead of creating it. Create
each missing volume named in that error with `docker volume create <name>`.
The explicit WBand volumes are:

```sh
docker volume create wband-postgres
docker volume create wband-rabbitmq
docker volume create wband-redis
docker volume create wband-rustfs
```

Keycloak uses an Aspire-generated volume name; use the exact name from the DCP
error. These commands preserve existing volumes. DCP retries automatically;
verify recovery with `aspire wait postgres --non-interactive` and `aspire describe`.
