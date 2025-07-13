using Dapper;
using MySql.Data.MySqlClient;

namespace TestSupport;

/// <summary>
/// Integration tests for MySQL database operations using TestContainers.
/// These tests demonstrate the MySQL fixture functionality.
/// </summary>
[Collection("MySqlCollection")]
public class MySqlIntegrationTests
{
    private readonly MySqlFixture _fixture;

    public MySqlIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TestDatabaseConnection_ShouldReturnTestData()
    {
        // Arrange & Act
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        var jobs = await connection.QueryAsync<Job>("SELECT id, title, company, location FROM jobs");
        var jobList = jobs.ToList();
        
        // Assert
        Assert.NotEmpty(jobList);
        Assert.Equal(4, jobList.Count);
        Assert.Contains(jobList, j => j.Title == "Software Engineer");
        Assert.Contains(jobList, j => j.Company == "TechCorp");
    }

    [Fact]
    public async Task TestUserData_ShouldReturnInitialUsers()
    {
        // Arrange & Act
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        var users = await connection.QueryAsync<User>("SELECT id, name, email FROM users");
        var userList = users.ToList();
        
        // Assert
        Assert.NotEmpty(userList);
        Assert.Equal(4, userList.Count);
        Assert.Contains(userList, u => u.Name == "John Doe");
        Assert.Contains(userList, u => u.Email == "testcontainers@example.com");
    }

    [Fact]
    public async Task TestJobOperations_ShouldSupportCRUD()
    {
        // Arrange
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        var testJobTitle = $"Test Job {Guid.NewGuid():N}";
        var testCompany = $"Test Company {Guid.NewGuid():N}";

        try
        {
            // Act - Create
            var insertedId = await connection.QuerySingleAsync<int>(
                "INSERT INTO jobs (title, description, company, location, salary) VALUES (@title, @description, @company, @location, @salary); SELECT LAST_INSERT_ID();",
                new { 
                    title = testJobTitle, 
                    description = "Test job description", 
                    company = testCompany, 
                    location = "Test Location", 
                    salary = 75000.00m 
                });

            // Act - Read
            var retrievedJob = await connection.QuerySingleOrDefaultAsync<Job>(
                "SELECT id, title, company, location FROM jobs WHERE id = @id",
                new { id = insertedId });

            // Act - Update
            await connection.ExecuteAsync(
                "UPDATE jobs SET title = @newTitle WHERE id = @id",
                new { newTitle = $"{testJobTitle} Updated", id = insertedId });

            var updatedJob = await connection.QuerySingleOrDefaultAsync<Job>(
                "SELECT id, title, company, location FROM jobs WHERE id = @id",
                new { id = insertedId });

            // Assert
            Assert.True(insertedId > 0);
            Assert.NotNull(retrievedJob);
            Assert.Equal(insertedId, retrievedJob.Id);
            Assert.Equal(testJobTitle, retrievedJob.Title);
            Assert.Equal(testCompany, retrievedJob.Company);
            
            Assert.NotNull(updatedJob);
            Assert.Equal($"{testJobTitle} Updated", updatedJob.Title);
        }
        finally
        {
            // Cleanup
            await connection.ExecuteAsync("DELETE FROM jobs WHERE company = @company", new { company = testCompany });
        }
    }
}

/// <summary>
/// Collection definition for MySQL tests in the TestSupport project.
/// </summary>
[CollectionDefinition("MySqlCollection")]
public class MySqlCollection : ICollectionFixture<MySqlFixture>;

/// <summary>
/// Represents a job entity from the MySQL database.
/// </summary>
public class Job
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Represents a user entity from the MySQL database.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
