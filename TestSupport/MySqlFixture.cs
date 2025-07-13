using System.Net.NetworkInformation;
using Testcontainers.MySql;
using Xunit;

namespace TestSupport;

/// <summary>
/// Collection fixture for MySQL database setup.
/// Manages MySQL container or local instance lifecycle and provides connection string to tests.
/// </summary>
public class MySqlFixture : IAsyncLifetime
{
    static MySqlFixture()
    {
        // Disable ResourceReaper which can cause issues with Podman
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
    }
    
    private readonly MySqlContainer? mySqlContainer;

    /// <summary>
    /// Gets the connection string for the MySQL database (container or local).
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets whether the tests are using a local MySQL instance.
    /// </summary>
    public bool UseLocalMySql { get; }

    public MySqlFixture()
    {
        UseLocalMySql = IsPortInUse(3306);
        
        if (!UseLocalMySql)
        {
            mySqlContainer = new MySqlBuilder()
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .WithBindMount(GetInitSqlPath(), "/docker-entrypoint-initdb.d/init.sql")
                .WithAutoRemove(true)
                .Build();
        }
    }

    public async Task InitializeAsync()
    {
        if (UseLocalMySql)
        {
            // Assume user has followed instructions to run MySQL in Docker with init.sql
            // Connect directly to the "testdb" database
            ConnectionString = "Server=localhost;Port=3306;Database=testdb;Uid=root;Pwd=;";
        }
        else
        {
            await mySqlContainer!.StartAsync();
            ConnectionString = mySqlContainer.GetConnectionString();
        }
        
        // Both TestContainers and local Docker MySQL will use the init.sql file automatically
        // No manual database initialization needed
    }

    public async Task DisposeAsync()
    {
        if (mySqlContainer != null)
        {
            await mySqlContainer.DisposeAsync();
        }
    }

    private static bool IsPortInUse(int port)
    {
        try
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
            
            return tcpConnInfoArray.Any(endpoint => endpoint.Port == port);
        }
        catch
        {
            return false;
        }
    }

    private static string GetInitSqlPath()
    {
        // Look for init.sql in multiple possible locations
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "init.sql"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "init.sql"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "init.sql"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "TestSupport", "init.sql")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        throw new FileNotFoundException("Could not find init.sql file. Please ensure it exists in the project directory.");
    }
}
