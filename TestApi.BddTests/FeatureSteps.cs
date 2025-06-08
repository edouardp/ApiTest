using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;  // Actually using AwesomeAssertions
using Reqnroll;
using System.Text;
using System.Text.Json;
using TestSupport.HttpFile;
using TestSupport.Json;

namespace TestApi.BddTests;

[Binding]
public class AddNumbersStepDefinitions(WebApplicationFactory<SelfHosting> factory)
    : IClassFixture<WebApplicationFactory<SelfHosting>>
{
    private readonly HttpClient _client = factory.CreateClient();
    
    private HttpResponseMessage? lastResponse;
    private string? lastBody;

    Dictionary<string, JsonElement> variables = [];

    [Given("the following request")]
    public async Task Given_TheFollowingRequest(string httpRequest)
    {
        foreach (var kvp in variables)
        {
            httpRequest = httpRequest.Replace("{{"+kvp.Key+"}}", kvp.Value.ToString());
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(httpRequest));

        var parser = new HttpFileParser();

        var request = await HttpFileParser.ParseAsync(stream);

        var requestMessage = new HttpRequestMessage(request.Method, request.Url)
        {
            Content = new StringContent(request.Body, Encoding.UTF8, "application/json")
        };

        lastResponse = await _client.SendAsync(requestMessage);

        lastBody = await lastResponse.Content.ReadAsStringAsync();
    }

    [Then(@"the API returns the following response")]
    public async Task Then_TheResponseIs(string httpResponse)
    {
        foreach (var kvp in variables)
        {
            httpResponse = httpResponse.Replace("{{" + kvp.Key + "}}", kvp.Value.ToString());
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(httpResponse));
        var parser = new HttpResponseParser();

        var expectedResponse = await parser.ParseAsync(stream);

        lastResponse.Should().NotBeNull();

        lastResponse.StatusCode.Should().Be(expectedResponse.StatusCode);
        lastResponse.StatusCode.ToString().Should().Be(expectedResponse.ReasonPhrase);

        foreach (var expectedHeader in expectedResponse.Headers)
        {
            if (lastResponse.Headers.TryGetValues(expectedHeader.Name, out IEnumerable<string>? values))
            {
                var actualValues = values.ToList();

                actualValues.Capacity.Should().Be(1);

                var value = actualValues.First();
                var actualHeader = HttpHeadersParser.ParseHeader($"{expectedHeader.Name}: {value}");
                actualHeader.Should().BeEquivalentTo(expectedHeader);
            }
            else if (lastResponse.Content.Headers.TryGetValues(expectedHeader.Name, out IEnumerable<string>? contentValues))
            {
                var actualValues = contentValues.ToList();

                actualValues.Capacity.Should().Be(1);

                var value = actualValues.First();
                var actualHeader = HttpHeadersParser.ParseHeader($"{expectedHeader.Name}: {value}");
                actualHeader.Should().BeEquivalentTo(expectedHeader);
            }
            else
            {
                var message = $"The '{expectedHeader.Name}' header is missing.";
                throw new InvalidOperationException(message);
            }
        }

        var actualBody = lastBody;

        if (actualBody == null)
        {
            var message = $"The body cannot be null";
            throw new NullReferenceException(message);
        }

        var captured = actualBody.AsJsonString().Should().ContainSubset(expectedResponse.Body);

        // Update variables with any newly captured variables (overwriting old vars with new ones if duplicated)
        variables = variables
            .Concat(captured.And.ExtractedValues)
            .GroupBy(kvp => kvp.Key)
            .ToDictionary(group => group.Key, group => group.Last().Value); 
    }

    [Then(@"the variable '(.*)' is equals to '(.*)'")]
    public void ThenTheVariableIsEqualsTo(string variableName, string variableValue)
    {
        variables[variableName].ToString().Should().Be(variableValue);
    }

    [Then(@"the variable '(.*)' is of type '(.*)'")]
    public void ThenTheVariableIsOfType(string variableName, string variableType)
    {
        // Ensure the variable is of the correct type, given that variables
        // are JsonElement, we can use the following methods to check the type
        // taking special care with Booleans, as JsonElement has a True and False
        // type, not a Boolean type.
        switch (variableType)
        {
            case "String":
                variables[variableName].ValueKind.Should().Be(JsonValueKind.String);
                break;
            case "Number":
                variables[variableName].ValueKind.Should().Be(JsonValueKind.Number);
                break;
            case "Boolean":
                variables[variableName].ValueKind.Should()
                    .BeOneOf(JsonValueKind.True, JsonValueKind.False);
                break;
            case "Object":
                variables[variableName].ValueKind.Should().Be(JsonValueKind.Object);
                break;
            case "Array":
                variables[variableName].ValueKind.Should().Be(JsonValueKind.Array);
                break;
            case "Null":
                variables[variableName].ValueKind.Should().Be(JsonValueKind.Null);
                break;
            case "Date":
                variables[variableName].ValueKind.Should().Be(JsonValueKind.String);
                DateTime.TryParse(variables[variableName].GetString(), out _).Should().BeTrue();
                break;

            default:
                throw new InvalidOperationException($"Unknown variable type '{variableType}'");
        }

    }

    // Ensure the variable matches a regular expression
    [Then(@"the variable '(.*)' matches '(.*)'")]
    public void ThenTheVariableMatches(string variableName, string regex)
    {
        variables[variableName].ToString().Should().MatchRegex(regex);
    }

    // Test the headers of the last response, using a table to define
    // the header name, their values, and whether the header is optional
    // Example:
    // Then the response headers are
    // | Header       | Value            | Optional |
    // | Content-Type | application/json | no       |
    // | X-Request-Id | 1234             | yes      |
    [Then(@"the response headers are")]
    public void ThenTheResponseHeadersAre(Table table)
    {
        if (lastResponse == null)
        {
            var message = $"The last response was null (has a request been made first?)";
            throw new NullReferenceException(message);
        }

        foreach (var row in table.Rows)
        {
            var headerName = row["Header"];
            var headerValue = row["Value"];
            var optional = row["Optional"];

            if (optional == "yes")
            {
                if (lastResponse.Headers.TryGetValues(headerName, out IEnumerable<string>? values))
                {
                    var actualValues = values.ToList();

                    actualValues.Capacity.Should().Be(1);

                    var value = actualValues.First();
                    var actualHeader = HttpHeadersParser.ParseHeader($"{headerName}: {value}");
                    actualHeader.Value.Should().Be(headerValue);
                }
            }
            else
            {
                var missingMessage = $"The '{headerName}' header is missing.";
                lastResponse.Headers.TryGetValues(headerName, out _).Should().BeTrue(missingMessage);
            }
        }
    }
}
