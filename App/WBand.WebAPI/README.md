# Web API

The API uses EF Core and PostgreSQL. `DataContext` is scoped to the request or
message handler. Each of its eight entities has an `IEntityTypeConfiguration<T>`
in `Data/Configurations`, including explicit snake_case column names, keys,
indexes and relationships. Membership and ownership reference `users.keycloak_id`.
Computed song deletion properties are excluded from persistence.

Mutations use explicit EF Core transactions. Band row locks serialize membership
and invitation changes. Song writers acquire band and song locks in that order;
registration/login uses a PostgreSQL advisory lock for the Keycloak subject.
Reads use EF Core no-tracking queries so a reread after acquiring a lock observes
current database values. Parameterized SQL for projections and locks runs through
EF Core on the same connection and transaction.

Outgoing events are published directly through Wolverine **after the database
transaction commits**. RabbitMQ uses inline delivery with publisher confirmations.
There is no persistent outgoing queue or background dispatcher. A publication
failure does not roll back committed data, and events are not recovered after a
process crash. Incoming Keycloak messages still use Wolverine retry handling;
duplicate registration and older login events do not change the saved profile.

HTTP paths and JSON contracts are unchanged. Songs support metadata history,
soft deletion and restoration for 30 days; text sections have independent revisions.

EF Core migrations in `Data/Migrations` run at API startup and are recorded in
`__EFMigrationsHistory`. Install the repository tool and add a migration with:

```sh
dotnet tool restore
bash scripts/add-migration.sh DescribeChange
```

The initial migration targets an empty database. An existing database created by
the previous SQL migrations needs a reviewed schema comparison and EF migration
baseline before startup; existing data and legacy outgoing messages are not
imported or deleted automatically.

Run integration tests with Docker available:

```sh
dotnet test Tests/WBand.WebAPI.Tests/WBand.WebAPI.Tests.csproj
```
