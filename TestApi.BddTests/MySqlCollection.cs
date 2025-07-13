using TestSupport;

namespace TestApi.BddTests;

/// <summary>
/// Collection definition for MySQL tests in the BDD Tests project.
/// This must be in the same assembly as the tests that use it.
/// </summary>
[CollectionDefinition("MySqlCollection")]
public class MySqlCollection : ICollectionFixture<MySqlFixture>;
