# Configuration Migration Guide

## Summary of Changes

This refactoring removes static `BaseStartup` dependencies and replaces them with the IOptions<T> pattern for proper dependency injection.

## New Configuration Classes

### PostgresOptions
- **Location**: `Media.Database/Configuration/PostgresOptions.cs`
- **Section**: `ConnectionStrings`
- **Properties**: `PostgresConnection` (string)

### ScyllaOptions
- **Location**: `Media.Database/Configuration/ScyllaOptions.cs`
- **Section**: `ScyllaDB`
- **Properties**:
  - `ContactPoints` (List<string>)
  - `ExternalContactPoints` (List<string>)
  - `Port` (int)
  - `Keyspace` (string)
  - `MaxBatchsize` (int, default: 100)

## Updated Providers

### PostgresConnectionProvider
- Now accepts `IOptions<PostgresOptions>` in constructor
- No longer depends on `BaseStartup.PostgresConnectionString`

### ScyllaSessionProvider
- Now accepts `IOptions<ScyllaOptions>` and `ILogger<ScyllaSessionProvider>` in constructor
- **Includes all session building and healing logic from BaseStartup**:
  - Session initialization on construction (preserves startup health check)
  - Semaphore-based session healing with 2-second timeout
  - Address translation for Docker environments
  - Session attachment to current request context
- No longer depends on `BaseStartup.ScyllaSession`, `BaseStartup.ScyllaSettings`, or static session methods

### FileRepository
- Now accepts `IOptions<ScyllaOptions>` in constructor
- Uses `scyllaOptions.Value.MaxBatchsize` instead of `BaseStartup.ScyllaSettings?.MaxBatchsize`

## Startup/DI Registration Example

```csharp
// In your Program.cs or Startup.cs

// Configure options from configuration
builder.Services.Configure<PostgresOptions>(
	builder.Configuration.GetSection(PostgresOptions.SectionName));

builder.Services.Configure<ScyllaOptions>(
	builder.Configuration.GetSection(ScyllaOptions.SectionName));

// Register providers
builder.Services.AddSingleton<IPostgresConnectionProvider, PostgresConnectionProvider>();
builder.Services.AddSingleton<IScyllaSessionProvider, ScyllaSessionProvider>(); // Session initialized on construction

// Register repositories
builder.Services.AddScoped<Func<IUnitOfWork>>(sp => 
	() => new UnitOfWork(sp.GetRequiredService<IPostgresConnectionProvider>()));

builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IWordRepository, WordRepository>();

// Register background task queue
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<BackgroundTaskService>();
```

## Configuration File (appsettings.json)

```json
{
  "ConnectionStrings": {
	"PostgresConnection": "Host=localhost;Port=5432;Database=media_organizer;Username=your_user;Password=your_password"
  },
  "ScyllaDB": {
	"ContactPoints": ["127.0.0.1"],
	"ExternalContactPoints": ["172.18.0.2", "172.18.0.3", "172.18.0.4"],
	"Port": 9042,
	"Keyspace": "media_organizer",
	"MaxBatchsize": 100
  }
}
```

## Session Logic Preservation

The session building and healing logic from `BaseStartup` has been **fully replicated** in `ScyllaSessionProvider`:

1. **Health Check on Construction**: `CheckHealth()` is called in the constructor, preserving the startup behavior
2. **Session Building**: `BuildClusterSession()` uses the same Cluster.Builder pattern with address translation
3. **Address Translation**: Internal `ScyllaAddressTranslator` class replicates `DockerPortTranslator` logic
4. **Session Healing**: `HealSessionAsync()` includes the same semaphore locking, timeout handling, and session recycling
5. **Session Attachment**: Request-scoped session tracking via `AsyncLocal<string>`

## Removed Static Dependencies

- ❌ `BaseStartup.PostgresConnectionString`
- ❌ `BaseStartup.ScyllaSession`
- ❌ `BaseStartup.ScyllaSettings`
- ❌ `BaseStartup.GetCurrentRequestSessionId()`
- ❌ `BaseStartup.AttachScyllaToCurrentRequest()`
- ❌ `BaseStartup.HealScyllaSessionAsync()`
- ❌ `BaseStartup.CheckScyllaHealth()`

## Benefits

✅ **Testability**: All dependencies are injected and mockable  
✅ **No Code Duplication**: Session logic exists once in `ScyllaSessionProvider`  
✅ **Startup Behavior Preserved**: Health check runs on provider construction  
✅ **Configuration**: Proper IOptions<T> pattern throughout  
✅ **Thread Safety**: Semaphore-based session healing maintained  
✅ **Logging**: Proper ILogger<T> usage instead of static Log calls
