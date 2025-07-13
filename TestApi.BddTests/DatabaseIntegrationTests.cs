using Dapper;
using MySql.Data.MySqlClient;
using TestSupport;

namespace TestApi.BddTests;

/// <summary>
/// Demonstrates using the shared MySQL fixture in the BDD Tests project.
/// This shows how the TestSupport project enables sharing test infrastructure across projects.
/// </summary>
[Collection("MySqlCollection")]
public class DatabaseIntegrationTests
{
    private readonly MySqlFixture _fixture;

    public DatabaseIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SharedMySqlFixture_ShouldProvideWorkingConnection()
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
    public async Task SharedMySqlFixture_ShouldSupportJobOperations()
    {
        // Arrange
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        
        var testJobTitle = $"Test Job {Guid.NewGuid():N}";
        var testCompany = $"Test Company {Guid.NewGuid():N}";

        // Act - Insert
        var insertedId = await connection.QuerySingleAsync<int>(
            "INSERT INTO jobs (title, description, company, location, salary) VALUES (@title, @description, @company, @location, @salary); SELECT LAST_INSERT_ID();",
            new { 
                title = testJobTitle, 
                description = "Test job description", 
                company = testCompany, 
                location = "Test Location", 
                salary = 75000.00m 
            });

        // Act - Retrieve
        var retrievedJob = await connection.QuerySingleOrDefaultAsync<Job>(
            "SELECT id, title, company, location FROM jobs WHERE id = @id",
            new { id = insertedId });

        // Act - Cleanup
        await connection.ExecuteAsync("DELETE FROM jobs WHERE id = @id", new { id = insertedId });

        // Assert
        Assert.True(insertedId > 0);
        Assert.NotNull(retrievedJob);
        Assert.Equal(insertedId, retrievedJob.Id);
        Assert.Equal(testJobTitle, retrievedJob.Title);
        Assert.Equal(testCompany, retrievedJob.Company);
    }

    [Fact]
    public async Task SharedMySqlFixture_ShouldSupportUserOperations()
    {
        // Arrange
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
}

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
