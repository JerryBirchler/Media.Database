# Media.Database

A .NET 10 database abstraction library for media management, providing repositories and models for file and word indexing with support for both SQL (PostgreSQL) and CQL (Cassandra/ScyllaDB) databases.

## Overview

Media.Database is part of the Media suite of libraries, offering a robust data access layer for managing media files, metadata, and word indexing. The library implements the repository pattern and provides abstractions for working with different database backends.

## Features

- **Multi-Database Support**: Works with both SQL (PostgreSQL via Npgsql) and CQL (Cassandra/ScyllaDB) databases
- **Repository Pattern**: Clean abstractions for data access through `IFileRepository` and `IWordRepository`
- **Schema Management**: Dynamic schema handling with caching for optimal performance
- **Metadata Management**: Comprehensive metadata tracking for media files
- **Word Indexing**: Advanced word origin tracking and file associations
- **Query Builder**: Custom query builders for both SQL and CQL operations
- **Fluent Logging**: Every repository log write is caller-aware via Media.Common's Fluent Logging API — no hand-typed `{ClassName}`/`{MethodName}` prefixes to drift out of sync with the code around them

## Technologies

- **.NET 10** - Latest .NET framework
- **Npgsql** - PostgreSQL database driver
- **CassandraCSharpDriver** - Cassandra/ScyllaDB support
- **Serilog** - Structured logging
- **Media.Common** - Shared utilities and helpers

## Project Structure

```
Media.Database/
├── Constants.cs                     # Global constants
├── Helpers/
│   ├── BaseSchemaCache.cs          # Schema caching mechanism
│   └── ExtensionMethods.cs         # Utility extensions
├── Models/
│   ├── Files.cs                    # File entity model
│   ├── Words.cs                    # Word entity model
│   ├── Metadata.cs                 # Metadata model
│   └── ...                         # Additional models and requests
├── Repositories/
│   ├── BaseRepository.cs           # Scylla session access shared by repositories that need it
│   ├── ISqlQueryExecutor.cs        # Mockable seam for hand-written SQL execution
│   ├── SqlQueryExecutor.cs         # The only class that opens a real Npgsql connection
│   ├── FileRepository.cs           # File data access
│   ├── WordRepository.cs           # Word data access
│   ├── Queries/
│   │   ├── QueryFiles.cs           # File query builder
│   │   └── QueryWords.cs           # Word query builder
│   └── Schemas/
│       ├── BaseSchema.cs           # Schema abstraction
│       ├── ColumnsSql.cs           # SQL schema definitions
│       ├── ColumnsCql.cs         # CQL schema definitions
│       └── ...                     # Additional schema components
└── tests/
	└── Media.Database.Tests/        # Unit tests
```

## Design Patterns in Use

Named here so the code can be read with intent rather than reverse-engineered. Each entry says
where the pattern lives and what problem it solves *in this codebase* -- not what a textbook says
it does.

### Compile-time schema binding (the one that shapes everything else)

`Repositories/Schemas/` -- `Tables`, `TablesSql`, `TablesCql`, `ColumnsSql`, `OrdinalsSql`,
`ParameterNames`, all deriving from `BaseSchema<TParent, TChild>`.

Every table, column, ordinal and parameter name is a `static readonly string` whose value is
derived from its own member name, and each registry validates against a parent registry. A
mistyped column is therefore a **build error**, not a runtime one -- which is the entire design
intent. The trade is deliberate: this is harder to unit test than a dynamic mapper, and that cost
is accepted because a compile error beats a test that might not exist.

This is also why there is no ORM here. An ORM would move identifier correctness back to runtime
and put generated SQL between the author and the query plan.

### Symmetric ports over two stores

`ISqlQueryExecutor` (PostgreSQL/Npgsql) and `ICqlQueryExecutor` (Scylla/Cassandra) expose
deliberately isomorphic surfaces:

```
QuerySingleAsync<T>(string sql, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader,T>)
QuerySingleAsync<T>(string cql, Action<Dictionary<string,object>>,  Func<Row,T>)
```

Same names, same arity, same shape; only the store-specific parameter bag and reader differ.
Learning one store teaches the other. Preserving this symmetry is a hard constraint -- it is the
reason micro-ORMs that cover only ADO.NET have been evaluated and declined.

### Query objects with co-located mappers

`Repositories/Queries/QueryXxx.cs` holds the SQL/CQL text for an aggregate alongside the
extension-method mappers that materialise its rows (`reader.ToFile()`, `row.ToPerson()`). Query
text stays readable and greppable, mapping stays next to the shape it maps, and repositories stay
thin.

### Repository and Unit of Work

`IFileRepository`, `IRegistrationRepository`, `IPersonRepository` and friends wrap the executors
per aggregate. `IUnitOfWork` carries a transaction across several writes where one must not land
without the others -- device registration being the clearest case.

### Set-once writes for permanent assignments

`SetOwningPersonIfUnsetAsync`, `SetGroupShellIdIfUnsetAsync`, `PromoteIfUnpromotedAsync`,
`RevokeIfActiveAsync`.

Each carries its permanence in the `WHERE` clause (`... AND Column IS NULL`, `... AND IsActive =
true`) rather than in a service-layer check, so the guarantee holds under concurrency and the
operation is idempotent: a second call changes nothing and returns nothing.

### Deactivation over deletion

`IsActive` flags plus `RevokedOn`/`UpdatedOn` stamps instead of `DELETE`, with unique partial
indexes (`WHERE IsActive = true`) enforcing "one active row per key". History survives, so it
stays possible to answer what was true at a past moment -- which matters for credentials and
encryption keys specifically.

### Envelope encryption

`GroupEncryptionKey` stores a wrapped Data Encryption Key; the customer's key is the wrapping key
and is never persisted. Rotating the outer key re-wraps a small value instead of re-encrypting
bulk data. Separate DEKs per `EncryptionDataCategory` keep blast radius contained.

### Polyglot persistence with identify-then-hydrate

Paged reads identify rows in PostgreSQL and hydrate them from Scylla. PostgreSQL owns
relationships and ordering; Scylla owns read throughput. CDC handlers
(`Repositories/Cdc/*CdcSyncHandler.cs`) keep the Scylla side current from Postgres's own change
stream rather than dual writes.

## Getting Started

### Installation

Add a reference to this library in your project:

```xml
<ProjectReference Include="Media.Database\Media.Database.csproj" />
```

### Configuration

Configure settings using `IOptions` pattern with validation:

```csharp
using Media.Common.Helpers;
using Media.Common.Providers;

// In Program.cs or Startup.cs
var configuration = BaseStartup.GetConfiguration("Development");

// Configure settings with validation (PostgresSettings, LocalMachineSettings, ScyllaSettings)
BaseStartup.ConfigureSettings(services);

// Register providers
services.AddSingleton<IPostgresConnectionProvider, PostgresConnectionProvider>();
services.AddSingleton<IScyllaSessionProvider, ScyllaSessionProvider>();

// Register the SQL executor (the only class that opens a real Npgsql connection;
// repositories depend on ISqlQueryExecutor so they stay unit-testable)
services.AddScoped<ISqlQueryExecutor, SqlQueryExecutor>();

// Register repositories
services.AddScoped<IFileRepository, FileRepository>();
services.AddScoped<IWordRepository, WordRepository>();

// Optional: Check ScyllaDB health on startup
BaseStartup.CheckScyllaHealth(services);
```

Configuration file (`appsettings.json`):

```json
{
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Database=media;Username=user;Password=pass"
  },
  "LocalMachineSettings": {
    "UploadDirectory": "C:\\uploads"
  },
  "ScyllaDB": {
    "ContactPoints": ["localhost"],
    "Port": 9042,
    "Keyspace": "media_keyspace"
  }
}
```

### Basic Usage

#### Working with Files

```csharp
public class MyService
{
	private readonly IFileRepository _fileRepository;

	public MyService(IFileRepository fileRepository)
	{
		_fileRepository = fileRepository;
	}

	public async Task CreateFileAsync(CreateFileRequest request)
	{
		await _fileRepository.CreateFileAsync(request);
	}

	public async Task<Files?> GetFileAsync(Guid fileId)
	{
		return await _fileRepository.GetFileAsync(fileId);
	}
}
```

#### Working with Words

```csharp
public class WordIndexService
{
	private readonly IWordRepository _wordRepository;

	public WordIndexService(IWordRepository wordRepository)
	{
		_wordRepository = wordRepository;
	}

	public async Task UpsertWordAsync(UpsertWordRequest request)
	{
		await _wordRepository.UpsertWordAsync(request);
	}

	public async Task<IEnumerable<ViewWordFiles>> SearchWordsAsync(string searchTerm)
	{
		return await _wordRepository.GetWordFilesAsync(searchTerm);
	}
}
```

## Repository Interfaces

Thirteen repositories, one per aggregate. Every method documented below was verified against the
interface rather than remembered -- an earlier revision of this file listed four `IFileRepository`
methods that do not exist.

### IFileRepository
- `Upsert` / `Update` / `Delete` -- write a file and its metadata
- `GetById` / `GetByIds` -- read one or many, hydrating from Scylla where available
- `GetCurrentBySourceMachineId` -- the current version of a path for a device
- `GetCurrentPagesBySourceMachineId` / `GetCurrentPageIdentifiersBySourceMachineId` -- keyset
  pagination, identifiers first so hydration can be batched
- `GetHistoryPagesBySourceMachineId` / `DeleteHistoryBySourceMachineId` -- prior versions of a path

### IWordRepository
- `GetByUuid` -- a single indexed word
- `GetFilePageIdentifiers` / `GetByIds` -- identify-then-hydrate paging over word-to-file links
- `GetWordsByFileId` -- the inverse traversal

### The rest

`IRegistrationRepository` (devices and their OTP lifecycle), `IPersonRepository`,
`IPersonSourceMachineRepository`, `IGroupRepository`, `IGroupPersonRepository`,
`IGroupSourceMachineRepository`, `IGroupShellRepository`, `IGroupEncryptionKeyRepository`,
`IGroupUuidOrchestrationRepository`, `ISourceMachineKeyRepository` and
`ICanBeEncryptedFieldsRepository`.

## Database Schema

Identifiers are **bound at compile time**, not detected at runtime. `Repositories/Schemas/` is the
single source of truth for every table, column, ordinal and parameter name, and a mistyped
identifier fails the build. See [Design Patterns in Use](#design-patterns-in-use).

> An earlier revision of this file described "dynamic schema detection", which is the opposite of
> how this library works and of why it was built.

Two stores, each owning what it is good at:

- **PostgreSQL** -- relationships, ordering, uniqueness and transactional writes. Tables include
  `SourceMachineRegistrations`, `Registrations`, `Persons`, `PersonsSourceMachines`, `Groups`,
  `GroupsPersons`, `GroupsSourceMachines`, `Files`, `Words`, `WordFiles`, plus the
  encryption-related `GroupShell`, `GroupEncryptionKeys` and the device-credential table
  `SourceMachineKeys`.
- **Scylla/Cassandra** -- read throughput for hydration, plus tables it owns outright such as
  `can_be_encrypted_fields` and `group_uuid_orchestration`.

Paged reads identify rows in PostgreSQL and hydrate them from Scylla; CDC sync handlers keep the
Scylla side current from Postgres's own change stream rather than by dual writes.

`BaseSchemaCache` memoises the resolved identifier sets so the registries cost nothing per call.

## Logging

Every log write in `FileRepository` and `WordRepository` goes through **Fluent Logging**, a small
chainable API on `ILogger<T>` provided by
[Media.Common](https://github.com/JerryBirchler/Media.Common). It stamps each entry with the
calling class and method automatically, so there's no hand-typed prefix to fall out of sync with
the code around it:

```csharp
_logger.WithCaller().LogError(ex, "GetById failed for WordId: [{Id}]", id);
// class: [WordRepository] method: [GetById] GetById failed for WordId: [42]
```

Within a single method, `WithCaller()` is captured once and reused across every log call in that
method (success path, multiple `catch` blocks, loop iterations) rather than re-derived per call.

Repository constructors write a standardized "class initializing" entry the same way, via a single
`logger.LogInitializing()` call in the `_logger` field initializer — replacing what used to be a
one-off lambda duplicated in every repository.

See `Media.Common/Helpers/Fluent/README.md` in the
[Media.Common repo](https://github.com/JerryBirchler/Media.Common) for the full design: the four
reserved tokens, configurable templates, and distributed-tracing integration.

## Dependencies

This project depends on:

- **Media.Common**: Shared utilities, helpers, and base classes
  - `IOptions<>` configuration pattern
  - Database providers (IPostgresConnectionProvider, IScyllaSessionProvider)
  - Settings validators (PostgresSettingsValidator, LocalMachineSettingsValidator)
  - BaseStartup for application initialization
  - Fluent Logging (`WithCaller`/`LogInitializing`)
  - Docker port translation
  - Common models (LocalMachineSettings, PostgresSettings, ScyllaSettings, SerilogSettings)

## Testing

The test suite uses **NUnit**, **AutoFixture**, **Moq**, and **Shouldly** for comprehensive unit testing coverage.

### Running Tests

Run the complete test suite:

```bash
dotnet test tests/Media.Database.Tests/Media.Database.Tests.csproj
```

Run specific test classes:

```bash
dotnet test --filter "FullyQualifiedName~Media.Database.Tests.Repositories.WordRepositoryTests"
```

### Test Coverage

The test suite includes:

- **Repository Tests**: Constructor validation, interface implementation, method availability
- **Model Tests**: Property assignment, deconstruction, AutoFixture compatibility
- **Schema Tests**: Column definitions, field caching, query builders
- **Validator Tests**: Settings validation for PostgresSettings and LocalMachineSettings
  - Null/empty/whitespace checks
  - Path qualification validation
  - Platform-specific path handling
- **Provider Tests**: Connection string retrieval, options injection
- **Request Mapping Tests**: Word request mapping and change detection

### Test Technologies

- **NUnit 3.13.3** - Test framework
- **AutoFixture 4.18.1** - Auto-mocking and test data generation
- **Moq 4.18.4** - Mocking framework
- **Shouldly 4.0.0** - Assertion library

### Example Test

```csharp
using Moq;
using NUnit.Framework;
using Shouldly;

[Test]
public async Task GetById_Should_ReturnWord_When_ExecutorFindsMatch()
{
    // Arrange - ISqlQueryExecutor is the only seam repositories depend on for Postgres access,
    // so no real connection is ever opened in a unit test.
    var expected = new Words { Id = 1, Word = "example", Origin = WordOrigin.Name, IsProperName = false, CameFromFileId = Guid.NewGuid() };
    var sqlExecutorMock = new Mock<ISqlQueryExecutor>();
    sqlExecutorMock
        .Setup(e => e.QuerySingleAsync(QueryWords.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Words>>()))
        .ReturnsAsync(expected);
    var repository = new WordRepository(sqlExecutorMock.Object, Mock.Of<ILogger<WordRepository>>(), new LoggingLevelSwitch());

    // Act
    var result = await repository.GetById(1);

    // Assert
    result.ShouldBe(expected);
}
```

## Contributing

1. Clone the repository
2. Create a feature branch
3. Make your changes
4. Run tests to ensure everything works
5. Submit a pull request

## License

This is a private project. Please contact the repository owner for licensing information.

## Related Projects

- **Media.Common**: [https://github.com/JerryBirchler/Media.Common](https://github.com/JerryBirchler/Media.Common)

## Contact

For questions or issues, please contact the repository owner or create an issue on GitHub.
