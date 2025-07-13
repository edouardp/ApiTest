using Reqnroll;
using TestSupport;

namespace TestApi.BddTests;

/// <summary>
/// Hooks for setting up dependency injection and test infrastructure for Reqnroll scenarios.
/// </summary>
[Binding]
public class Hooks
{
    private static MySqlFixture? _mySqlFixture;

    /// <summary>
    /// Sets up the test infrastructure before all tests run.
    /// This includes initializing the MySQL fixture.
    /// </summary>
    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _mySqlFixture = new MySqlFixture();
        await _mySqlFixture.InitializeAsync();
    }

    /// <summary>
    /// Cleans up the test infrastructure after all tests complete.
    /// </summary>
    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (_mySqlFixture != null)
        {
            await _mySqlFixture.DisposeAsync();
        }
    }

    /// <summary>
    /// Gets the shared MySqlFixture instance for use in step definitions.
    /// </summary>
    public static MySqlFixture GetMySqlFixture()
    {
        if (_mySqlFixture == null)
        {
            throw new InvalidOperationException("MySqlFixture not initialized. BeforeTestRun should have been called.");
        }
        return _mySqlFixture;
    }
}
