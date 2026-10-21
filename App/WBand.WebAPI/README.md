# Web API

The API uses EF Core and PostgreSQL. `DataContext` is scoped to the request or
message handler. Each of its ten entities has an `IEntityTypeConfiguration<T>`
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

Songs support metadata history, soft deletion and restoration for 30 days.
Song content is a matrix: `SongSection` rows are shared by every `Partition`
column. `SongContentCell` stores the text and independent revision at each
intersection. Missing cells mean empty text with revision zero. Names and order
belong to the axes; renaming a section updates it across all partitions.

- `GET /song-api/songs/{songId}/content` returns `sections`, `partitions` and `cells`.
- `GET/POST /song-api/songs/{songId}/sections` and
  `PUT/DELETE /song-api/sections/{sectionId}` manage common rows.
- `GET/POST /song-api/songs/{songId}/partitions` and
  `PUT/DELETE /song-api/partitions/{partitionId}` manage columns.
- Axis creation accepts `name` and `order`; updates also take `expectedRevision`.
- `PUT /song-api/songs/{songId}/sections/{sectionId}/partitions/{partitionId}`
  accepts `content` and `expectedRevision` (zero for the first write).
  Stale writes return HTTP 409 with `song_cell_version_conflict`.
- `PUT /song-api/songs/{songId}/partitions/{partitionId}/content` accepts
  `cells: [{ sectionId, content, expectedRevision }]`. It saves all supplied
  sections in one transaction. All revisions and section references are checked
  before writing; conflicts return HTTP 409 with `song_partition_content_conflict`
  and the conflicting `sectionIds`. Other partitions and omitted sections remain
  unchanged. The single-cell endpoint is used for saving an individual section.
- Composite foreign keys prevent cells from combining axes of different songs.
  Deleting a row or column cascades only to its cells, retaining the opposite axis.
- Writes publish `SongSectionsChanged`, `SongPartitionsChanged` or
  `SongContentChanged` through Wolverine after committing, using the exchanges
  `song-sections-changed`, `song-partitions-changed` and `song-content-changed`.

The `SongPartitions` and `SongContentMatrix` migrations upgrade the original
lyrics/chords fields to cells while preserving original shared section IDs,
names, order, text, revisions and attribution. Databases that already applied the
flat-partition migration are also supported: known migration prefixes restore
section rows and standard columns; custom partitions retain their text in a
shared `Песня` row. Downgrading flattens each column with section headings.
Metadata history remains separate. Bracketed chords remain in stored content;
highlighted display omits the brackets and has no background.

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
