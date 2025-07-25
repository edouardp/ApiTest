# TestContainers MySQL Integration for ApiTest

This document explains how to use the TestContainers MySQL integration that has been added to the ApiTest solution.

## Overview

The ApiTest solution now includes TestContainers support for MySQL database testing. This allows you to run integration tests against a real MySQL database without requiring a pre-installed MySQL server.

## What Was Added

### 1. TestSupport Project Updates
- **MySqlFixture.cs**: Central fixture for managing MySQL container lifecycle
- **init.sql**: Database initialization script with test data
- **MySqlIntegrationTests.cs**: Sample integration tests demonstrating MySQL operations
- **Updated packages**: Added TestContainers, MySQL, and Dapper packages

### 2. BDD Tests Project Updates
- **MySqlCollection.cs**: Collection definition for sharing MySQL fixture across BDD tests
- **DatabaseIntegrationTests.cs**: Integration tests demonstrating database operations
- **DatabaseStepDefinitions.cs**: BDD step definitions for database scenarios
- **Database.feature**: Sample feature file with database integration scenarios
- **Updated packages**: Added MySQL and Dapper packages

## How It Works

### MySQL Fixture
The `MySqlFixture` class automatically:
- Detects if MySQL is running locally on port 3306
- If local MySQL is available, uses it directly
- If not, starts a MySQL container using TestContainers
- Initializes the database using the `init.sql` script
- Provides a connection string for tests to use

### Database Initialization
The `init.sql` script creates:
- `testdb` database
- `jobs` table with sample job data (4 records)
- `users` table with sample user data (4 records)

### Test Integration
Tests use the xUnit Collection Fixture pattern to share the MySQL instance across multiple test classes, improving performance and reducing resource usage.

## Usage Examples

### Basic Integration Test
```csharp
[Collection("MySqlCollection")]
public class MyDatabaseTests
{
    private readonly MySqlFixture _fixture;

    public MyDatabaseTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TestDatabaseOperation()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        var jobs = await connection.QueryAsync<Job>("SELECT * FROM jobs");
        Assert.NotEmpty(jobs);
    }
}
```

### BDD Step Definition
```csharp
[Given(@"the database contains a job with title ""(.*)""")]
public async Task GivenTheDatabaseContainsAJobWithTitle(string jobTitle)
{
    await using var connection = new MySqlConnection(_fixture.ConnectionString);
    await connection.OpenAsync();
    
    // Insert or verify job exists
    // ... implementation
}
```

### Feature File
```gherkin
Feature: Database Integration
    Scenario: Query jobs from database
        When I query the database for jobs
        Then the queried jobs should contain a job with title "Software Engineer"
```

## Running Tests

### Prerequisites
- Docker or Podman installed and running
- .NET 9.0 SDK

### Local MySQL (Optional)
If you have MySQL running locally on port 3306, the fixture will use it instead of starting a container. Make sure to:
1. Create the `testdb` database
2. Run the `init.sql` script to set up test data

### Running Tests
```bash
# Run all tests
dotnet test

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run BDD tests
dotnet test TestApi.BddTests

# Run TestSupport tests
dotnet test TestSupport
```

## Configuration

### Environment Variables
- `TESTCONTAINERS_RYUK_DISABLED=true`: Disables ResourceReaper (useful for Podman)

### Connection Strings
- **Container**: Automatically generated by TestContainers
- **Local**: `Server=localhost;Port=3306;Database=testdb;Uid=root;Pwd=;`

## Test Data

### Jobs Table
- Software Engineer at TechCorp
- Data Analyst at DataCorp  
- DevOps Engineer at CloudCorp
- Product Manager at StartupCorp

### Users Table
- John Doe (john.doe@example.com)
- Jane Smith (jane.smith@example.com)
- TestContainers User (testcontainers@example.com)
- API Test User (apitest@example.com)

## Best Practices

### 1. Test Isolation
- Use transactions for tests that modify data
- Clean up test data in `[AfterScenario]` or test cleanup methods
- Use unique identifiers (GUIDs) for test data

### 2. Performance
- Share the MySQL fixture across test classes using Collection Fixtures
- Avoid starting/stopping containers for each test
- Use the same database instance for related tests

### 3. Debugging
- Check Docker/Podman logs if containers fail to start
- Verify port 3306 availability
- Ensure init.sql file is copied to output directory

## Troubleshooting

### Common Issues

1. **Container fails to start**
   - Check Docker/Podman is running
   - Verify port 3306 is not in use
   - Check firewall settings

2. **init.sql not found**
   - Ensure the file is marked as Content with CopyToOutputDirectory
   - Check file paths in MySqlFixture.GetInitSqlPath()

3. **Connection refused**
   - Wait for container to fully start
   - Check connection string format
   - Verify database name matches init.sql

4. **Tests are slow**
   - Ensure you're using Collection Fixtures
   - Check if multiple containers are being started
   - Consider using local MySQL for development

## Integration with Existing Tests

The TestContainers integration is designed to work alongside your existing API tests and BDD scenarios. You can:

1. **Extend existing step definitions** to include database verification
2. **Add database setup/teardown** to existing scenarios
3. **Create hybrid tests** that test both API endpoints and database state
4. **Use database state** to set up test conditions for API tests

## Next Steps

1. **Extend the init.sql** script with more test data as needed
2. **Add more database step definitions** for common operations
3. **Create database-specific feature files** for complex scenarios
4. **Integrate with API tests** to verify end-to-end functionality
5. **Add performance tests** using the database fixture
