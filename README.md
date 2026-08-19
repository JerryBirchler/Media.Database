# Media.Database

A .NET 10 database abstraction library for media management, providing repositories and models for file and word indexing with support for both SQL (PostgreSQL) and NoSQL (Cassandra/ScyllaDB) databases.

## Overview

Media.Database is part of the Media suite of libraries, offering a robust data access layer for managing media files, metadata, and word indexing. The library implements the repository pattern and provides abstractions for working with different database backends.

## Features

- **Multi-Database Support**: Works with both SQL (PostgreSQL via Npgsql) and NoSQL (Cassandra/ScyllaDB) databases
- **Repository Pattern**: Clean abstractions for data access through `IFileRepository` and `IWordRepository`
- **Schema Management**: Dynamic schema handling with caching for optimal performance
- **Kafka Integration**: Built-in models for Kafka messaging and event streaming
- **Metadata Management**: Comprehensive metadata tracking for media files
- **Word Indexing**: Advanced word origin tracking and file associations
- **Query Builder**: Custom query builders for both SQL and NoSQL operations

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
│   ├── BaseRepository.cs           # Base repository implementation
│   ├── FileRepository.cs           # File data access
│   ├── WordRepository.cs           # Word data access
│   ├── Queries/
│   │   ├── QueryFiles.cs           # File query builder
│   │   └── QueryWords.cs           # Word query builder
│   └── Schemas/
│       ├── BaseSchema.cs           # Schema abstraction
│       ├── ColumnsSql.cs           # SQL schema definitions
│       ├── ColumnsNoSql.cs         # NoSQL schema definitions
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

Configure the database connection in your application settings:

```csharp
// For PostgreSQL
services.AddScoped<IFileRepository, FileRepository>();
services.AddScoped<IWordRepository, WordRepository>();

// Configure connection strings
services.Configure<LocalMachineSettings>(configuration.GetSection("LocalMachineSettings"));
services.Configure<ScyllaSettings>(configuration.GetSection("ScyllaSettings"));
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

The library supports dynamic schema detection and handles both SQL and NoSQL databases through a unified abstraction:

- **SQL Mode**: Uses PostgreSQL with structured tables and relationships
- **NoSQL Mode**: Uses Cassandra/ScyllaDB with denormalized document structure

Schema information is cached for performance using `BaseSchemaCache`.

## Kafka Integration

Built-in support for Kafka messaging:

- **KafkaSettings**: Configure Kafka brokers and topics
- **KafkaRecord**: Message structure
- **KafkaRecordWrapper**: Serialization wrapper
- **KafkaProducerActions**: Enumeration of producer operations

## Dependencies

This project depends on:

- **Media.Common**: Shared utilities, helpers, and base classes
  - BaseStartup for application initialization
  - Logging extensions
  - Docker port translation
  - Common models (LocalMachineSettings, ScyllaSettings, SerilogSettings)

## Testing

Run the test suite:

```bash
dotnet test tests/Media.Database.Tests/Media.Database.Tests.csproj
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
