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
        string? currentHeaderLine = null;
        
        while ((line = await reader.ReadLineAsync()) != null)
        {
            // Check if this is an empty line (end of headers)
            if (string.IsNullOrWhiteSpace(line))
            {
                // Process any pending header before breaking
                if (!string.IsNullOrWhiteSpace(currentHeaderLine))
                {
                    var parsedHeader = HttpHeadersParser.ParseHeader(currentHeaderLine);
                    headers.Add(parsedHeader);
                    currentHeaderLine = null; // Clear to avoid double processing
                }
                break;
            }
            
            // Check if this is a continuation line (starts with whitespace)
            if (line.StartsWith(' ') || line.StartsWith('\t'))
            {
                // This is a multiline header continuation
                if (currentHeaderLine != null)
                {
                    // Append to the current header, replacing the line break with a space
                    currentHeaderLine += " " + line.Trim();
                }
                else
                {
                    // Invalid format: continuation line without a header
                    throw new InvalidDataException("Invalid HTTP header format: continuation line without header.");
                }
            }
            else
            {
                // This is a new header line
                // First, process any pending header
                if (!string.IsNullOrWhiteSpace(currentHeaderLine))
                {
                    var parsedHeader = HttpHeadersParser.ParseHeader(currentHeaderLine);
                    headers.Add(parsedHeader);
                }
                
                // Start a new header
                currentHeaderLine = line;
            }
        }

        // Process any remaining header after the loop ends (when stream ends without empty line)
        if (!string.IsNullOrWhiteSpace(currentHeaderLine))
        {
            var parsedHeader = HttpHeadersParser.ParseHeader(currentHeaderLine);
            headers.Add(parsedHeader);
        }

        // Step 4: Read the body (if any)
        // Read the remaining content in the stream as the body of the HTTP request
        string body = await reader.ReadToEndAsync();

        // Return the parsed HTTP request as a record
        return new ParsedHttpRequest(method, url, headers, body);
    }
}
