namespace TestSupport.HttpFile;

/// <summary>
/// Represents a parsed HTTP request, including the HTTP method, URL, headers, and body.
/// </summary>
public record ParsedHttpRequest(HttpMethod Method, string Url, List<ParsedHeader> Headers, string Body);

/// <summary>
/// Provides functionality to parse an HTTP request from a stream.
/// This parser extracts the method, URL, headers, and body from a formatted HTTP request.
/// 
/// Usage:
/// <code>
/// using var stream = File.OpenRead("request.http");
/// var parser = new HttpFileParser();
/// var parsedRequest = await parser.ParseAsync(stream);
/// </code>
/// </summary>
public class HttpFileParser
{
    /// <summary>
    /// Asynchronously parses an HTTP request from a given stream.
    /// </summary>
    /// <param name="httpStream">The input stream containing the HTTP request.</param>
    /// <returns>A <see cref="ParsedHttpRequest"/> object containing the parsed request data.</returns>
    /// <exception cref="InvalidDataException">Thrown when the request format is invalid.</exception>
    public static async Task<ParsedHttpRequest> ParseAsync(Stream httpStream)
    {
        // Ensure the stream is not null
        if (httpStream == null)
        {
            throw new ArgumentNullException(nameof(httpStream), "HTTP stream cannot be null.");
        }

        // Using a StreamReader to read the HTTP request from the provided stream
        using var reader = new StreamReader(httpStream);

        // Step 1: Parse the request line (method, URL, and version)
        // The first line of the HTTP request typically contains the method, URL, and version
        string? requestLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            throw new InvalidDataException("Invalid HTTP file format: missing request line.");
        }

        // Split the request line into parts (method, URL, and version)
        var requestLineParts = requestLine.Split(' ', 3);
        if (requestLineParts.Length <= 2)
        {
            throw new InvalidDataException("Invalid HTTP request line format.");
        }

        // Step 2: Convert the method string to an HttpMethod object
        // Convert the HTTP method (GET, POST, etc.) to an HttpMethod object
        string methodString = requestLineParts[0];
        HttpMethod method = new HttpMethod(methodString.ToUpperInvariant());

        // Extract the URL from the request line
        string url = requestLineParts[1]; // The URL/path

        // Step 3: Parse headers
        // Initialize an empty list to store parsed headers
        var headers = new List<ParsedHeader>();
        string? line;
        while (!string.IsNullOrWhiteSpace(line = await reader.ReadLineAsync()))
        {
            // Parse each header using the HttpHeadersParser (assumed to be implemented elsewhere)
            var parsedHeader = HttpHeadersParser.ParseHeader(line);
            headers.Add(parsedHeader);
        }

        // Step 4: Read the body (if any)
        // Read the remaining content in the stream as the body of the HTTP request
        string body = await reader.ReadToEndAsync();

        // Return the parsed HTTP request as a record
        return new ParsedHttpRequest(method, url, headers, body);
    }
}
