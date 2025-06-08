using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace TestApi.UnitTests;

// Web application factory to create the test server
public class TestServerFactory : WebApplicationFactory<SelfHosting>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseTestServer();
    }
}

// xUnit test class using IClassFixture to share test server between tests
public class HelloWorldTests(TestServerFactory factory) : IClassFixture<TestServerFactory>
{
    private readonly TestServerFactory factory = factory;

    [Fact]
    public async Task AddNumbers_ReturnsCorrectSum()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/math/add?a=5&b=7.5");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<double>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);

        Assert.Equal(12.5, result);
    }
}

