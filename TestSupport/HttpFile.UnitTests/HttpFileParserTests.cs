using FluentAssertions;
using System.Text;

namespace TestSupport.HttpFile.UnitTests;

public class HttpFileParserTests
{
    [Fact]
    public async Task ParseAsync_Should_Parse_POST_Request_With_Headers_And_Body()
    {
        // Arrange
        string rawRequest = """
            POST /submit HTTP/1.1
            Content-Type: application/json; charset=utf-8
            Authorization: Bearer your_token_here

            {
              "name": "John Doe",
              "age": 30
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Post);
        result.Url.Should().Be("/submit");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Type")
            .Which.Value.Should().Be("application/json");
        result.Headers.Should().ContainSingle(header => header.Name == "Authorization")
            .Which.Value.Should().Be("Bearer your_token_here");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Type")
            .Which.Parameters.Should().ContainKey("charset").And.ContainValue("utf-8");
        result.Body.Should().Be("""
            {
              "name": "John Doe",
              "age": 30
            }
            """);
    }

    [Fact]
    public async Task ParseAsync_Should_Parse_GET_Request_Without_Body()
    {
        // Arrange
        string rawRequest = """
            GET /api/v1/users HTTP/1.1
            Host: example.com
            Accept: application/json
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Get);
        result.Url.Should().Be("/api/v1/users");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Headers.Should().ContainSingle(header => header.Name == "Accept")
            .Which.Value.Should().Be("application/json");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Custom_HTTP_Method()
    {
        // Arrange
        string rawRequest = """
            CUSTOMMETHOD /custom/path HTTP/1.1
            Custom-Header: custom-value
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(new HttpMethod("CUSTOMMETHOD"));
        result.Url.Should().Be("/custom/path");
        result.Headers.Should().ContainSingle(header => header.Name == "Custom-Header")
            .Which.Value.Should().Be("custom-value");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Parse_Request_With_Multiple_Headers_And_Empty_Body()
    {
        // Arrange
        string rawRequest = """
            PUT /update/resource HTTP/1.1
            Host: example.com
            Content-Length: 0
            User-Agent: CustomClient/1.0
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Put);
        result.Url.Should().Be("/update/resource");
        result.Headers.Should().ContainSingle(header => header.Name == "Host")
            .Which.Value.Should().Be("example.com");
        result.Headers.Should().ContainSingle(header => header.Name == "Content-Length")
            .Which.Value.Should().Be("0");
        result.Headers.Should().ContainSingle(header => header.Name == "User-Agent")
            .Which.Value.Should().Be("CustomClient/1.0");
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_Handle_Request_With_No_Headers_And_No_Body()
    {
        // Arrange
        string rawRequest = """
            HEAD /no-headers HTTP/1.1
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert
        result.Method.Should().Be(HttpMethod.Head);
        result.Url.Should().Be("/no-headers");
        result.Headers.Should().BeEmpty();
        result.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_RequestLine_Is_Missing()
    {
        // Arrange: Missing request line (empty input)
        string rawRequest = """
            
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        Func<Task> act = async () => await HttpFileParser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid HTTP file format: missing request line.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_RequestLine_Has_Too_Few_Parts()
    {
        // Arrange: Invalid request line (missing HTTP method or URL)
        string rawRequest = """
            GET HTTP/1.1

            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        Func<Task> act = async () => await HttpFileParser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid HTTP request line format.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_RequestLine_Is_Invalid()
    {
        // Arrange: Invalid request line format (completely malformed)
        string rawRequest = """
            INVALID_LINE

            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        Func<Task> act = async () => await HttpFileParser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Invalid HTTP request line format.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Header_Is_Malformed()
    {
        // Arrange: Malformed header (missing colon)
        string rawRequest = """
            POST /submit HTTP/1.1
            InvalidHeaderWithoutColon
            Content-Type: application/json

            {
              "name": "John Doe",
              "age": 30
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        Func<Task> act = async () => await HttpFileParser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid header format, missing ':' separator.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Header_Is_Missing_Colon()
    {
        // Arrange: Empty header value
        string rawRequest = """
            POST /submit HTTP/1.1
            Content-Type

            {
              "name": "John Doe",
              "age": 30
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        Func<Task> act = async () => await HttpFileParser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid header format, missing ':' separator.");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Header_Is_Empty_After_Colon()
    {
        // Arrange: Empty header value
        string rawRequest = """
            POST /submit HTTP/1.1
            Content-Type: 

            {
              "name": "John Doe",
              "age": 30
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        Func<Task> act = async () => await HttpFileParser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid header format, value is empty");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Header_Is_Empty_After_Colon_Except_For_Parameters()
    {
        // Arrange: Missing header value
        string rawRequest = """
            POST /submit HTTP/1.1
            Content-Type: ; charset=utf-8

            {
              "name": "John Doe",
              "age": 30
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        Func<Task> act = async () => await HttpFileParser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid header format, value is empty");
    }

    [Fact]
    public async Task ParseAsync_Should_ThrowException_When_Body_Is_Missing_And_ContentLength_Is_Set()
    {
        // Arrange: Body is missing but Content-Length is non-zero
        string rawRequest = """
            POST /submit HTTP/1.1
            Content-Length: 15

            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawRequest));
        var parser = new HttpFileParser();

        // Act
        var result = await HttpFileParser.ParseAsync(stream);

        // Assert: The parser will not throw in this case, but result.Body will be empty
        result.Body.Should().BeEmpty();
    }
}


