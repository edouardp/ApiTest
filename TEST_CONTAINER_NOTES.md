# TestContainers MySQL Implementation Notes

## Overview
The TestContainers project demonstrates a comprehensive approach to using TestContainers with MySQL for integration testing. The implementation provides flexibility to use either TestContainers-managed MySQL instances or local MySQL instances.

## Project Structure

### Solution Components
- **TestSupport**: Shared library containing MySQL fixture and utilities
- **TestContainers**: Core test project with MySQL integration tests
- **TestContainers.Api**: Sample API project
- **TestContainers.Api.Tests**: API integration tests using the shared MySQL fixture

## Key Implementation Details

### 1. TestSupport Project (`TestSupport.csproj`)
**Purpose**: Shared library for test infrastructure

**Key NuGet Packages**:
- `Testcontainers` (4.6.0) - Core TestContainers functionality
- `Testcontainers.MySql` (4.6.0) - MySQL-specific TestContainers support
- `MySql.Data` (9.3.0) - MySQL .NET connector
- `Dapper` (2.1.66) - Lightweight ORM for database operations
- `xunit` (2.9.3) - Testing framework
- `xunit.abstractions` (2.0.3) - xUnit abstractions

### 2. MySqlFixture Class (`MySqlFixture.cs`)
**Purpose**: Central fixture for managing MySQL container lifecycle

**Key Features**:
- Implements `IAsyncLifetime` for proper async setup/teardown
- Supports both TestContainers and local MySQL instances
- Automatic port detection to determine which mode to use
- Configurable database initialization via SQL scripts
- Resource cleanup and container management

**Configuration**:
```csharp
// Disables ResourceReaper for Podman compatibility
Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");

// MySQL container configuration
mySqlContainer = new MySqlBuilder()
    .WithDatabase("testdb")
    .WithUsername("testuser")
    .WithPassword("testpass")
    .WithBindMount(GetInitSqlPath(), "/docker-entrypoint-initdb.d/init.sql")
    .WithAutoRemove(true)
    .Build();
```

**Smart Path Resolution**: The fixture automatically locates `init.sql` files in multiple possible locations:
- Current directory
- Base directory
- Relative paths for different project structures

### 3. Database Initialization (`init.sql`)
**Purpose**: Sets up test database schema and seed data

**Contents**:
- Creates `testdb` database
- Creates `users` table with auto-increment ID and timestamp
- Inserts 3 test users for consistent test data

### 4. Collection Fixtures
**Purpose**: Share MySQL fixture across multiple test classes

**Implementation Pattern**:
```csharp
[CollectionDefinition("MySqlCollection")]
public class MySqlCollection : ICollectionFixture<MySqlFixture>;
```

**Usage in Tests**:
```csharp
[Collection("MySqlCollection")]
public class MySqlTestContainerTests(MySqlFixture fixture, ITestOutputHelper output)
```

**Important**: Each test assembly needs its own collection definition due to xUnit requirements.

### 5. Test Implementation Patterns

**Connection Management**:
```csharp
await using var connection = new MySqlConnection(fixture.ConnectionString);
await connection.OpenAsync();
```

**Database Operations with Dapper**:
```csharp
// Query operations
var users = await connection.QueryAsync<User>("SELECT id, name, email FROM users");

// Single record queries
var user = await connection.QuerySingleOrDefaultAsync<User>(
    "SELECT id, name, email FROM users WHERE name = @name", 
    new { name = "John Doe" });

// Insert with return ID
var insertedId = await connection.QuerySingleAsync<int>(
    "INSERT INTO users (name, email) VALUES (@name, @email); SELECT LAST_INSERT_ID();",
    new { name = testUserName, email = testUserEmail });
```

### 6. Project Configuration

**Test Project Settings**:
- Target Framework: `net9.0`
- `ImplicitUsings` enabled
- `Nullable` enabled
- `TreatWarningsAsErrors` for code quality

**Content Files**:
```xml
<ItemGroup>
    <Content Include="init.sql">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
</ItemGroup>
```

### 7. Cross-Project Usage

**TestSupport as Shared Library**:
- Contains reusable MySQL fixture
- Referenced by multiple test projects
- Enables consistent database testing across solution

**API Integration Tests**:
- Uses same MySQL fixture as unit tests
- Demonstrates database operations in API context
- Shows cleanup patterns for test isolation

## Best Practices Observed

### 1. Flexibility
- Supports both containerized and local MySQL instances
- Automatic detection of available MySQL services
- Graceful fallback mechanisms

### 2. Resource Management
- Proper async disposal patterns
- Container auto-removal
- Connection lifecycle management

### 3. Test Isolation
- Each test uses the same base data from `init.sql`
- Cleanup operations for tests that modify data
- Shared fixture reduces setup overhead

### 4. Configuration Management
- Environment variable configuration for TestContainers
- Flexible path resolution for initialization scripts
- Parameterized database connection settings

### 5. Error Handling
- Robust file path resolution
- Port availability checking
- Exception handling for container operations

## Integration Points

### 1. xUnit Integration
- Collection fixtures for shared resources
- Async lifetime management
- Test output helper integration

### 2. ASP.NET Core Integration
- `Microsoft.AspNetCore.Mvc.Testing` for API tests
- Integration with dependency injection
- Configuration override capabilities

### 3. Database Integration
- Dapper for lightweight ORM operations
- MySQL.Data for direct database connectivity
- SQL script execution for initialization

## Performance Considerations

### 1. Container Lifecycle
- Single container per test collection
- Reuse across multiple test classes
- Proper cleanup to prevent resource leaks

### 2. Database Operations
- Connection pooling through fixture
- Efficient query patterns with Dapper
- Minimal data setup for faster test execution

### 3. Local Development
- Port detection for local MySQL usage
- Reduced container overhead when local instance available
- Consistent behavior across environments
