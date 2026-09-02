# Media.Database

A .NET 10 database abstraction library for media management, providing repositories and models for file and word indexing with support for both SQL (PostgreSQL) and CQL (Cassandra/ScyllaDB) databases.

## Overview

Media.Database is part of the Media suite of libraries, offering a robust data access layer for managing media files, metadata, and word indexing. The library implements the repository pattern and provides abstractions for working with different database backends.

## Features

- **Multi-Database Support**: Works with both SQL (PostgreSQL via Npgsql) and CQL (Cassandra/ScyllaDB) databases
- **Repository Pattern**: Clean abstractions for data access through `IFileRepository` and `IWordRepository`
- **Schema Management**: Dynamic schema handling with caching for optimal performance
- **Kafka Integration**: Built-in models for Kafka messaging and event streaming
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
│   ├── KafkaSettings.cs            # Kafka configuration
│   ├── KafkaRecord.cs              # Kafka message models
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

### IFileRepository
- `CreateFileAsync(CreateFileRequest request)` - Create a new file entry
- `UpdateFileAsync(UpdateFileRequest request)` - Update existing file
- `GetFileAsync(Guid fileId)` - Retrieve file by ID
- `DeleteFileAsync(Guid fileId)` - Remove file entry

### IWordRepository
- `UpsertWordAsync(UpsertWordRequest request)` - Insert or update word
- `DeleteWordAsync(DeleteWordRequest request)` - Remove word entry
- `GetWordFilesAsync(string word)` - Find files containing word

## Database Schema

The library supports dynamic schema detection and handles both SQL and CQL databases through a unified abstraction:

- **SQL Mode**: Uses PostgreSQL with structured tables and relationships
- **CQL Mode**: Uses Cassandra/ScyllaDB with denormalized document structure

Schema information is cached for performance using `BaseSchemaCache`.

## Kafka Integration

Built-in support for Kafka messaging:

- **KafkaSettings**: Configure Kafka brokers and topics
- **KafkaRecord**: Message structure
- **KafkaRecordWrapper**: Serialization wrapper
- **KafkaProducerActions**: Enumeration of producer operations

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
