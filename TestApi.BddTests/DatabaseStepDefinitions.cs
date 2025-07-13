using Dapper;
using MySql.Data.MySqlClient;
using Reqnroll;
using TestSupport;

namespace TestApi.BddTests;

/// <summary>
/// Step definitions for database-related BDD scenarios.
/// Uses the MySQL fixture to interact with the test database.
/// </summary>
[Binding]
public class DatabaseStepDefinitions
{
    private readonly MySqlFixture _fixture;
    private readonly Dictionary<string, object> _testData = new();

    public DatabaseStepDefinitions()
    {
        _fixture = Hooks.GetMySqlFixture();
    }

    [Given(@"the database contains a job with title ""(.*)""")]
    public async Task GivenTheDatabaseContainsAJobWithTitle(string jobTitle)
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var existingJob = await connection.QuerySingleOrDefaultAsync<Job>(
            "SELECT id, title, company, location FROM jobs WHERE title = @title",
            new { title = jobTitle });

        if (existingJob == null)
        {
            var jobId = await connection.QuerySingleAsync<int>(
                "INSERT INTO jobs (title, description, company, location, salary) VALUES (@title, @description, @company, @location, @salary); SELECT LAST_INSERT_ID();",
                new { 
                    title = jobTitle, 
                    description = "Test job description", 
                    company = "Test Company", 
                    location = "Test Location", 
                    salary = 75000.00m 
                });
            
            _testData[$"job_{jobTitle}_id"] = jobId;
        }
        else
        {
            _testData[$"job_{jobTitle}_id"] = existingJob.Id;
        }
    }

    [Given(@"the database contains a user with name ""(.*)""")]
    public async Task GivenTheDatabaseContainsAUserWithName(string userName)
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var existingUser = await connection.QuerySingleOrDefaultAsync<User>(
            "SELECT id, name, email FROM users WHERE name = @name",
            new { name = userName });

        if (existingUser == null)
        {
            var userId = await connection.QuerySingleAsync<int>(
                "INSERT INTO users (name, email) VALUES (@name, @email); SELECT LAST_INSERT_ID();",
                new { 
                    name = userName, 
                    email = $"{userName.Replace(" ", ".").ToLower()}@test.com"
                });
            
            _testData[$"user_{userName}_id"] = userId;
        }
        else
        {
            _testData[$"user_{userName}_id"] = existingUser.Id;
        }
    }

    [When(@"I query the database for jobs")]
    public async Task WhenIQueryTheDatabaseForJobs()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var jobs = await connection.QueryAsync<Job>("SELECT id, title, company, location FROM jobs");
        _testData["queried_jobs"] = jobs.ToList();
    }

    [When(@"I query the database for users")]
    public async Task WhenIQueryTheDatabaseForUsers()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var users = await connection.QueryAsync<User>("SELECT id, name, email FROM users");
        _testData["queried_users"] = users.ToList();
    }

    [When(@"I delete the job with title ""(.*)""")]
    public async Task WhenIDeleteTheJobWithTitle(string jobTitle)
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var deletedRows = await connection.ExecuteAsync(
            "DELETE FROM jobs WHERE title = @title",
            new { title = jobTitle });

        _testData["deleted_job_rows"] = deletedRows;
    }

    [Then(@"the database should contain (\d+) jobs")]
    public async Task ThenTheDatabaseShouldContainJobs(int expectedCount)
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var actualCount = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM jobs");
        
        if (actualCount != expectedCount)
        {
            throw new InvalidOperationException($"Expected {expectedCount} jobs in database, but found {actualCount}");
        }
    }

    [Then(@"the database should contain (\d+) users")]
    public async Task ThenTheDatabaseShouldContainUsers(int expectedCount)
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var actualCount = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        
        if (actualCount != expectedCount)
        {
            throw new InvalidOperationException($"Expected {expectedCount} users in database, but found {actualCount}");
        }
    }

    [Then(@"the queried jobs should contain a job with title ""(.*)""")]
    public void ThenTheQueriedJobsShouldContainAJobWithTitle(string jobTitle)
    {
        if (!_testData.TryGetValue("queried_jobs", out var jobsObj) || jobsObj is not List<Job> jobs)
        {
            throw new InvalidOperationException("No jobs were queried. Make sure to call 'When I query the database for jobs' first.");
        }

        var foundJob = jobs.FirstOrDefault(j => j.Title == jobTitle);
        if (foundJob == null)
        {
            throw new InvalidOperationException($"Job with title '{jobTitle}' was not found in the queried results.");
        }
    }

    [Then(@"the queried users should contain a user with name ""(.*)""")]
    public void ThenTheQueriedUsersShouldContainAUserWithName(string userName)
    {
        if (!_testData.TryGetValue("queried_users", out var usersObj) || usersObj is not List<User> users)
        {
            throw new InvalidOperationException("No users were queried. Make sure to call 'When I query the database for users' first.");
        }

        var foundUser = users.FirstOrDefault(u => u.Name == userName);
        if (foundUser == null)
        {
            throw new InvalidOperationException($"User with name '{userName}' was not found in the queried results.");
        }
    }

    [Then(@"(\d+) job should be deleted")]
    public void ThenJobShouldBeDeleted(int expectedDeletedRows)
    {
        if (!_testData.TryGetValue("deleted_job_rows", out var deletedRowsObj) || deletedRowsObj is not int deletedRows)
        {
            throw new InvalidOperationException("No job deletion was performed. Make sure to call a delete step first.");
        }

        if (deletedRows != expectedDeletedRows)
        {
            throw new InvalidOperationException($"Expected {expectedDeletedRows} rows to be deleted, but {deletedRows} were deleted.");
        }
    }

    [AfterScenario]
    public async Task CleanupTestData()
    {
        // Only cleanup if we have a valid connection string
        if (string.IsNullOrEmpty(_fixture.ConnectionString))
            return;

        try
        {
            // Clean up any test data created during the scenario
            await using var connection = new MySqlConnection(_fixture.ConnectionString);
            await connection.OpenAsync();

            // Remove any temporary test jobs that were created (but keep the standard test data)
            await connection.ExecuteAsync(
                "DELETE FROM jobs WHERE title = 'Temporary Job'");

            // Remove any temporary test users that were created (but keep the persistent Test User)
            await connection.ExecuteAsync(
                "DELETE FROM users WHERE email LIKE '%@test.com' AND name != 'Test User'");
        }
        catch (Exception)
        {
            // Ignore cleanup errors to prevent test failures
            // This can happen if the database connection is not available
        }
    }
}
